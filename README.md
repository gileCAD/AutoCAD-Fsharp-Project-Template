# AutoCAD-Fsharp-Project-Template
### A F# Visual Studio Project Template for an AutoCAD Plugin.
This template allows to start a F# project for an AutoCAD plugin in Visual Studio . It is designed to automatically start the specified AutoCAD version and load the assemby when starting the debugging.

For AutoCAD 2016 and later versions it is imperative that the LEGACYCODESEARCH system variable is set to 1 to allow automatic loading of the assembly. 

The command.fs file contains an example of command and helpers to write some common AutoCAD tasks in a more declarative functional style.

Example using pipeline style to erase lines shorter than a supplied value
```F#
let eraseShortLines minLength =
    use tr = Active.db.TransactionManager.StartTransaction()

    Active.db
    |> getModelSpace OpenMode.ForRead
    |> getObjects<Line> OpenMode.ForRead
    |> Seq.filter (fun l -> l.Length < minLength)
    |> upgradeOpen
    |> Seq.iter (fun l -> l.Erase())

    tr.Commit()
```

Example using a prompts workflow to get the center and radius of a circle
```F#
[<CommandMethod("DrawCircle")>]
let drawCircle () =
    let inputs =
        prompt {
            let! ppr = Active.ed.GetPoint("\nCenter: ")
            let! pdr = Active.ed.GetDistance(ppr.Value, "\nRadius: ")
            return (ppr.Value, pdr.Value)
        }

    match inputs with
    | None -> ()
    | Some (center, radius) ->
        use tr = Active.db.TransactionManager.StartTransaction()

        Active.db.CurrentSpaceId
        |> getObject<BlockTableRecord> OpenMode.ForWrite
        |> addEntity (new Circle(center, Vector3d.ZAxis, radius))
        |> ignore

        tr.Commit()
```

### Editing the template files
In order for the template to work, the paths to the acad.exe file and to the autoCAD libraries must match those on the local computer.

#### Properties\launchSettings.json
The path to the acad.exe file of the AutoCAD version to be launched at debugging startup must be consistent with that of the local computer.
```	json
{
  "profiles": {
    "$safeprojectname$": {
      "commandName": "Executable",
      "executablePath": "C:\\Program Files\\Autodesk\\AutoCAD 2022\\acad.exe",
      "commandLineArgs": "/nologo /b \"start.scr\""
    }
  }
}
```

#### AcadPlugin.fsproj
The MSBuild project file (.fsproj) is an xml file that describe and control the process of generation of the applications.

The paths to the AutoCAD libraries referenced by the project must be consistent with those of the local computer.
```xml
    <!-- Change the paths to the targeted AutoCAD libraries -->
    <Reference Include="AcCoreMgd">
      <HintPath>C:\ObjectARX 2017\inc\AcCoreMgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="AcDbMgd">
      <HintPath>C:\ObjectARX 2017\inc\AcDbMgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="AcMgd">
      <HintPath>C:\ObjectARX 2017\inc\AcMgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
```
It is preferable that the required version of .NET Framework is the one installed by the targeted AutoCAD version (see [this page](https://help.autodesk.com/view/OARX/2022/ENU/?guid=GUID-450FD531-B6F6-4BAE-9A8C-8230AAC48CB4)).
```xml
    <!-- Change the targeted .NET Framework version -->
    <TargetFramework>net48</TargetFramework> 
```
#### MyTemplate.vstemplate
This file describes the template.

Name and Desription of the template.
```xml
    <!-- Change the name and description as desired -->
    <Name>Fsharp Plugin for AutoCAD</Name>
    <Description>Fsharp Project for AutoCAD Plugin</Description>
```
Default name of the assembly.
```xml
    <!-- Change the default name as desired -->
    <DefaultName>FsharpPluginForAutoCAD</DefaultName>
```
### Installation of the template
The 'AutoCAD Plugin Template' folder (possibly zipped) have to be pasted in the 'Visual Studio 20XX\Templates\ProjectTemplates' directory.
