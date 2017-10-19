module BuildHelpers

open Fake
open Fake.ReleaseNotesHelper
open System.IO
open System.Linq

let Exec command args =
    let result = Shell.Exec(command, args)
    if result <> 0 then failwithf "%s exited with error %d" command result

type ReleaseInfo =
    {
        Version : string
        Notes : string
    }

let getReleaseInfo file =
    let release =
        ReadFile file
        |> ReleaseNotesHelper.parseReleaseNotes

    {
        Version = release.NugetVersion
        Notes = release.Notes |> List.fold (fun acc next -> acc + "* " + next + "\n") ""
    }