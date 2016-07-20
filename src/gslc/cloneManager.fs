﻿module cloneManager
open System.IO
open commonTypes
open parseTypes
open Amyris.utils
open Amyris.biolib
open shared
open constants

/// Clone manager formatted output
let dumpCM (outDir:string) (tag:string) (assemblies: AssemblyOut list) (primers : DivergedPrimerPair list list option) =
    
    let primers' = match primers with
                        | None -> List.init assemblies.Length (fun _ -> None)
                        | Some(p) -> p |> List.map (Some)
    let joint = List.zip assemblies primers'
    for a,primers in joint do
        let path = sprintf "%s.%d.cx5" tag (match a.id with None -> failwith "ERROR: unassigned assembly id" | Some(i) -> i )
                        |> opj outDir
        printf "Writing CM5 output to dir=%s tag=%s path=%s\n" outDir tag (path.Replace(@"\", "/"))
        use outF = new StreamWriter(path)
        let w (s:string) = outF.Write(s)
        
        let molName = sprintf "%s_%d" tag a.id.Value
        let molDescription = sprintf "GSL assembly %s_%d" tag a.id.Value
        let molSize = a.dnaParts |> List.map (fun p -> p.dna.Length) |> Seq.sum
        
        let primersInner = match primers with
                            | None -> a.dnaParts |> List.map (fun _ -> None)
                            | Some(p) -> p |> List.map (Some)

        let totSequence = a.dnaParts |> List.map (fun p -> p.dna)
                             |> Array.concat |> arr2seq

        let deGT(x:string) = x.Replace(">","&gt;").Replace("<" , "&lt;")

        sprintf @"<?xml version=""1.0""?>
<!--SciEd XML 1 - DO NOT EDIT THIS FILE-->

<MOLECULE name=""%s"">
<FLAGS>1</FLAGS>
<SIZEBP>%d</SIZEBP>
<DESCRIPTION>%s </DESCRIPTION>" molName molSize molDescription|> w
        for p,dpp in List.zip a.dnaParts primersInner do
            let partName = if p.sliceName <> "" then p.sliceName elif p.description <> "" then p.description else (sprintf "part_%s" (ambId p.id))
            let partType = match p.sliceType with
                            | LINKER -> "Label"
                            | INLINEST -> "Label"
                            | _ -> "Gene"

            let fr = (zero2One p.destFr)
            let t = (zero2One p.destTo)
            sprintf @"<FEATURE name=""%s"">
                    <TYPE>%s</TYPE>
                    <STARTBP>%A</STARTBP>
                    <ENDBP>%A</ENDBP>
                    <COMPLEMENT>%s</COMPLEMENT>
                    </FEATURE>" (deGT partName) partType (if p.destFwd then fr else t) (if p.destFwd then t else fr)  (if p.destFwd then "0" else "1") |> w
            w "\n"
            match dpp with
                | None -> ()
                | Some(GAP) -> () // Nothing to do here
                | Some(DPP(dp)) ->
                    // Diverged primer pair off this part, need to show them


                    if dp.rev.Primer.Length <> 0 then
                        // Simple example, tail is part of the linker region.
                        // It might not span the entire region
                        // .......[XXXXXXXXXXXXXXXXXXX].........
                        //  <bbbbbbtttttttttttttttt????  (body and tail of primer)  
                        //
                        // Complex example, a sandwich region sss might be part of the
                        // tail - need to find the longest prefix of the tail that is the
                        // sequence leading up to left end of the linker region
                        //..........[XXXXXXXXXXXXXXXXXXX].........
                        //<bbbbbbsssstttttttttttttttt????  (body and tail of primer)   
                        let rec longestPrefix (n:int) =
                            if p.destFr + ((n-1)*1<ZeroOffset>) >= totSequence.Length*1<ZeroOffset> then longestPrefix (n-1) 
                            else if dp.rev.Primer.[..n-1] |> revComp = totSequence.Substring(p.destFr/1<ZeroOffset>,n).ToCharArray() then n*1<ZeroOffset>
                                else longestPrefix (n-1)
                        let prefix = longestPrefix (dp.rev.Primer.Length)
                        sprintf @"
                                <PRIMER name=""PR"">
       
                                <VERSION>1</VERSION>
                                <SEQHOMOL>%s</SEQHOMOL>
                                <BINDSITE>%A,1,0,%d,0,%d</BINDSITE>
                                <DESCRIPTION>PR</DESCRIPTION></PRIMER>
                                " 
                                    (arr2seq dp.rev.Primer) 
                                    // .......[XXXXXXXXXXXXXXXXXXX].........
                                    //  <bbbbbbtttttttttttttttttttt  (body and tail of primer)
                                    (zero2One (p.destFr+prefix-1<ZeroOffset>  (* (dp.rev.tail.Length-1) * 1<ZeroOffset>  *)  )) // from
                                    dp.rev.Primer.Length //length
                                    dp.rev.Primer.Length
                                     |> w
                                     
                        w "\n"                  
                    if dp.fwd.Primer.Length <> 0 then
                        // simple example
                        //
                        // ............[XXXXXXXXXXX]...........
                        //              ttttttttttttbbbbbbbbbbb>> (tail/body)
                        //
                        // complex example  (see above)
                        // ............[XXXXXXXXXXX]...........
                        //               ?ttttttttttsssbbbbbbbbbbb>> (tail/sandwich/body)
                        let rec longestPrefixFwd (n:int) =
                            if p.destTo - ((n-1)*1<ZeroOffset>)< 0<ZeroOffset> then longestPrefixFwd (n-1) 
                            else if dp.fwd.Primer.[..n-1] = totSequence.Substring((p.destTo/1<ZeroOffset>)-n+1,n).ToCharArray() then n*1<ZeroOffset>
                                else longestPrefixFwd (n-1)

                        // TODOTODO - doesn't handle case of prefixFwd = 0 correctly
                        let prefixFwd = longestPrefixFwd (dp.fwd.Primer.Length)
                        //printf "CM primer fwd prefixFwd=%A prefix = %s\n" prefixFwd (dp.fwd.Primer.[..(prefixFwd/1<ZeroOffset>)-1] |> arr2seq)

                        sprintf @"
                                <PRIMER name=""PF"">
                                <VERSION>0</VERSION>
                                <SEQHOMOL>%s</SEQHOMOL>
                                <BINDSITE>%A,0,0,%d,0,%d</BINDSITE>
                                <DESCRIPTION>PF</DESCRIPTION></PRIMER>
                                " 
                                    (arr2seq dp.fwd.Primer) 
                                    //
                                    // ............[XXXXXXXXXXX]...........
                                    //              ttttttttttttbbbbbbbbbbb>> (tail/body)

                                    ((p.destTo-prefixFwd+1<ZeroOffset> (* (dp.fwd.tail.Length-1)*1<ZeroOffset> *) ) |> zero2One)
                                    dp.fwd.Primer.Length
                                    dp.fwd.Primer.Length
                                     |> w
       
        sprintf @"
            <SEQUENCE>%s</SEQUENCE>
            </MOLECULE>" totSequence |> w     
 