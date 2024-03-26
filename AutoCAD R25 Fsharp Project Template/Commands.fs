module $safeprojectname$.Commands

open Autodesk.AutoCAD.ApplicationServices
open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.EditorInput
open Autodesk.AutoCAD.Geometry
open Autodesk.AutoCAD.Runtime

type AcCoreApp = Autodesk.AutoCAD.ApplicationServices.Core.Application
type AcExn = Autodesk.AutoCAD.Runtime.Exception

open Helpers

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
            return (ppr.Value, pir.Value * 4 + 1, pdr.Value)
        }

    match inputs with
    | None -> ()
    | Some (pt, n, w) ->
        use tr = Active.db.TransactionManager.StartTransaction()
        let ratio = sqrt 1.25 - 0.5
        let bulge = - tan(System.Math.PI / 8.)
        let pline = new Polyline()

        (Point2d(pt.X, pt.Y), (ratio, ratio), w)
        |> Seq.unfold (fun (p, (a, b), d) -> Some(p, (p + Vector2d(d * a, d * b), (b, -a), ratio * d)))
        |> Seq.take n
        |> Seq.iteri (fun i p -> pline.AddVertexAt(i, p, bulge, 0., 0.))

        pline.TransformBy(Active.ed.CurrentUserCoordinateSystem)

        Active.db.CurrentSpaceId
        |> getObject<BlockTableRecord> OpenMode.ForWrite
        |> addEntity pline
        |> ignore

        tr.Commit()
