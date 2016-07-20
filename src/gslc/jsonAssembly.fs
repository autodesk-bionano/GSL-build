module jsonAssembly

open System.IO
open System.Text
open System
open commonTypes
open constants
open Amyris.Bio.utils
open parseTypes
open shared
open Newtonsoft.Json

/// Emit JSON format
///  tag: string  prefix for files  assemblies : List of AssemblyOut
let dumpJsonAssemblies (prefix:string) (assemblies : AssemblyOut list) =
    use outF = new StreamWriter(prefix)
    let s:string = assemblies |> Newtonsoft.Json.JsonConvert.SerializeObject
    outF.WriteLine(s)