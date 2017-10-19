module BuildHelpers

open Fake
open Fake.Core
open Fake.Core.Globbing
open Fake.Core.Globbing.Operators
open Fake.Core.Process
open Fake.Core.String
open Fake.IO
open Fake.ReleaseNotesHelper
open System
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
        File.read file
        |> ReleaseNotesHelper.parseReleaseNotes

    {
        Version = release.NugetVersion
        Notes = release.Notes |> List.fold (fun acc next -> acc + "* " + next + "\n") ""
    }