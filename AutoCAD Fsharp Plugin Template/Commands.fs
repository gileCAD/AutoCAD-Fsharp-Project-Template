module $safeprojectname$.Commands

open Autodesk.AutoCAD.ApplicationServices
open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.EditorInput
open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.Runtime

type AcCoreApp = Autodesk.AutoCAD.ApplicationServices.Core.Application
type AcExn = Autodesk.AutoCAD.Runtime.Exception

// =================================== HELPERS ===================================== //

// Easy access to the active Document, Database and Editor
module Active =
    let doc = AcCoreApp.DocumentManager.MdiActiveDocument
    let db = doc.Database
    let ed = doc.Editor

// Computation expression to wrap user prompts into a workflow
type PromptBuilder() =
    member _.Bind(v: #PromptResult, f) = if v.Status = PromptStatus.OK then f v else None
    member _.Return(v) = Some v
    member _.Zero() = None

let prompt = PromptBuilder()

// Editor extension methods
type Editor with
    member ed.GetPoint(pt, msg) =
        ed.GetPoint(new PromptPointOptions(msg, UseBasePoint = true, BasePoint = pt))

    member ed.GetDistance(pt, msg) =
        ed.GetDistance(new PromptDistanceOptions(msg, UseBasePoint = true, BasePoint = pt))

// Helper functions for writing common tasks in a functional style
let getTopTransaction (db: Database) =
    match db.TransactionManager.TopTransaction with
    | null -> raise (AcExn(ErrorStatus.NoActiveTransactions))
    | t -> t

let getObject<'T when 'T :> DBObject> mode (id: ObjectId) =
    let tr = getTopTransaction id.Database
    tr.GetObject(id, mode) :?> 'T

let getModelSpace mode db =
    SymbolUtilityServices.GetBlockModelSpaceId(db)
    |> getObject<BlockTableRecord> mode

let getObjects<'T when 'T :> DBObject> mode (ids: System.Collections.IEnumerable) =
    let rx = RXObject.GetClass typeof<'T>

    ids
    |> Seq.cast<ObjectId>
    |> Seq.choose
        (fun id ->
            if id.ObjectClass.IsDerivedFrom rx then
                Some(getObject<'T> mode id)
            else
                None)

let upgradeOpen<'T when 'T :> DBObject> (objs: seq<'T>) =
    seq {
        for o in objs do
            if o.IsWriteEnabled then
                yield o
            else
                yield getObject<'T> OpenMode.ForWrite o.ObjectId
    }

let disposeAll (objs: seq<#DBObject>) =
    let mutable (last: Exception) = null

    objs
    |> Seq.iter
        (fun o ->
            if o <> null then
                try
                    o.Dispose()
                with
                | :? Exception as ex -> last <- if last = null then ex else last)

    if last <> null then raise last

let addEntity (ent: #Entity) (btr: BlockTableRecord) =
    let tr = getTopTransaction btr.Database
    let id = btr.AppendEntity(ent)
    tr.AddNewlyCreatedDBObject(ent, true)
    id

// ===================================== MAIN ====================================== //

// Custom command example
[<CommandMethod("GOLDENSPIRAL")>]
let drawGoldenSpiral () =
    let opts = PromptIntegerOptions("\nNumber of loops: ")
    opts.AllowNone <- true
    opts.LowerLimit <- 1
    opts.UpperLimit <- 16
    opts.DefaultValue <- 4
    opts.UseDefaultValue <- true

    let inputs =
        prompt {
            let! pir = Active.ed.GetInteger(opts)
            let! ppr = Active.ed.GetPoint("\nStart point: ")
            let! pdr = Active.ed.GetDistance(ppr.Value, "\nWidth: ")
            return (ppr.Value, pir.Value * 4, pdr.Value)
        }

    match inputs with
    | None -> ()
    | Some (p, n, w) ->
        use tr = Active.db.TransactionManager.StartTransaction()
        let ratio = sqrt 1.25 - 0.5
        let bulge = - tan(System.Math.PI / 8.)
        let pline = new Polyline()

        let rec loop i p d =
            if i <= n then
                pline.AddVertexAt(i, p, bulge, 0., 0.)

                match i % 4 with
                | 1 -> loop (i + 1) (p + Vector2d(d, -d)) (d * ratio)
                | 2 -> loop (i + 1) (p + Vector2d(-d, -d)) (d * ratio)
                | 3 -> loop (i + 1) (p + Vector2d(-d, d)) (d * ratio)
                | _ -> loop (i + 1) (p + Vector2d(d, d)) (d * ratio)

        loop 0 (Point2d(p.X, p.Y)) (w * ratio)
        pline.TransformBy(Active.ed.CurrentUserCoordinateSystem)

        Active.db.CurrentSpaceId
        |> getObject<BlockTableRecord> OpenMode.ForWrite
        |> addEntity pline
        |> ignore

        tr.Commit()