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
open System.Collections.Generic

/// Represents one piece of DNA for assembly, capturing its origins and relevant details
type DNASliceJson =
   {id: string; 
    extId: string; 
    dna: string;  
    sourceChr: string; 
    sourceFr: string; 
    sourceTo: string; 
    sourceFwd: bool;
    destFr: string; 
    destTo: string;
    destFwd: bool; 
    amplified: bool; 
    sliceName: string;
    sliceType: string;
    breed: string;
    description: string ; 
}

type AssemblyOutJson = 
    { id: string;
    name: string;
    dnaSlices: DNASliceJson list;
}

///  Emit JSON format
///  tag: string  prefix for files  assemblies : List of AssemblyOut
let dumpJsonAssemblies (prefix:string) (assemblies : AssemblyOut list) =
    (*let debugFileName = ["/tmp/"; "debug.json"] |> String.concat ""
    use outF = new StreamWriter(debugFileName)
    let s:string = Newtonsoft.Json.JsonConvert.SerializeObject(assemblies,Formatting.Indented);
    outF.WriteLine(s) *)

    use outF = new StreamWriter(prefix)
    let assemblyHash =
        assemblies 
            |> List.map(fun a ->
            { id = a.id.Value.ToString(); name = a.name.ToString();
            dnaSlices = (a.dnaParts |> List.map(fun d ->
            { id = (match d.id with | None -> "0" | Some v -> d.id.Value.ToString());
            extId = "";
            dna = String.Join("", d.dna);
            sourceChr = d.sourceChr;
            sourceFr = d.sourceFr.ToString();
            sourceTo = d.sourceTo.ToString();
            sourceFwd = d.sourceFwd;
            destFr = d.destFr.ToString();
            destTo = d.destTo.ToString();
            destFwd = d.destFwd;
            amplified = d.amplified;
            sliceName = d.sliceName.ToString();
            sliceType = d.sliceType.ToString();
            breed = d.breed.ToString();
            description = d.description.ToString()}));
            })
    
    let newstring:string = Newtonsoft.Json.JsonConvert.SerializeObject(assemblyHash, Formatting.Indented)
    outF.WriteLine(newstring)

