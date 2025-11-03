module $safeprojectname$.Helpers

open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.EditorInput
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
let getObject<'T when 'T :> DBObject> (tr: Transaction) mode (id: ObjectId) =
    tr.GetObject(id, mode) :?> 'T

let getModelSpace tr mode db =
    SymbolUtilityServices.GetBlockModelSpaceId(db)
    |> getObject<BlockTableRecord> tr mode

let getObjects<'T when 'T :> DBObject> tr mode (ids: System.Collections.IEnumerable) =
    let rx = RXObject.GetClass typeof<'T>

    ids
    |> Seq.cast<ObjectId>
    |> Seq.choose
        (fun id ->
            if id.ObjectClass.IsDerivedFrom rx then
                Some(getObject<'T> tr mode id)
            else
                None)

let upgradeOpen<'T when 'T :> DBObject> tr (objs: seq<'T>) =
    seq {
        for o in objs do
            if o.IsWriteEnabled then
                yield o
            else
                yield getObject<'T> tr OpenMode.ForWrite o.ObjectId
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

let addEntity (tr: Transaction) (ent: #Entity) (btr: BlockTableRecord) =
    let id = btr.AppendEntity(ent)
    tr.AddNewlyCreatedDBObject(ent, true)
    id

