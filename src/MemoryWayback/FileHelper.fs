module MemoryWayback.FileHelper

open System.IO
open System
open ExifLib
open ExtCore.Collections
open ExtCore.Control

type MediaDirectory = MediaDirectory of string

let photoExtensions = [".jpg";".jpeg"] //;".png";".bmp"]

let (|Photo|Video|) (ext:string) =
  if (List.contains ext photoExtensions) then
    Photo
  else
    Video

type FileHelper = {
  urlBuilder : MediaDirectory -> FileInfo -> string
  takenTime : FileInfo -> DateTime option
  fileFinder : MediaDirectory -> FileInfo list
}

module Internal =
  let urlBuilder (MediaDirectory rootDir) (file:FileInfo) =
    file.FullName.Substring(rootDir.Length)
  let takenTime (file:FileInfo) : DateTime option =
    let exifRead (file:FileInfo) =
      try
        use reader = new ExifReader(file.FullName)
        let taken = DateTime.UtcNow
        if (reader.GetTagValue(ExifTags.DateTimeDigitized, ref taken)) then
          Some taken
        else
          None
      with e ->
        None

    let fileStampRead (file:FileInfo) =
      if (file.CreationTime < file.LastWriteTime) then
        Some file.CreationTime
      else
        Some file.LastWriteTime

    let mpgRead (_:FileInfo) = None

    let finders = [ exifRead ; mpgRead ; fileStampRead ]
    List.fold (fun s fn -> Option.tryFillWith (fun _ -> fn file) s) None finders

  let rec fileFinder (MediaDirectory name) =
    let di = new DirectoryInfo(name)
    let lst = di.GetFiles() |> Array.toList
    let photos = lst |> List.filter (fun f -> List.contains f.Extension photoExtensions)
    let subDirPhotos = 
      di.GetDirectories()
      |> Array.toList
      |> List.collect (fileFinder << (fun d ->  MediaDirectory d.FullName))
    List.append photos subDirPhotos

let realFileHelper = {
  urlBuilder = Internal.urlBuilder
  takenTime = Internal.takenTime
  fileFinder = Internal.fileFinder
}
