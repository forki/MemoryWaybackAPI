module MemoryWayback.DbTypes

open System
open ServiceStack.OrmLite
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations
open MemoryWayback.Persistence

type MediaType =
  | Photo = 0
  | Video = 1

[<CLIMutableAttribute>]
type medias = {
  Id : int
  MediaDirectoryId : int
  Url : string
  Taken : DateTime
  Added : DateTime
  Type : MediaType
}

[<CLIMutableAttribute>]
type media_directories = {
  Id : int
  Path : string
  Mount : string
}
