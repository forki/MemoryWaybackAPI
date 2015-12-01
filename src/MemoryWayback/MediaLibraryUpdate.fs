module MemoryWayback.MediaLibraryUpdate

open System
open System.IO
open MemoryWayback.Persistence
open MemoryWayback.DbTypes
open ExtCore.Control
open ExifLib

module Internal =
   // SIMPLE
  let itemUpdate (newValue:medias) existingItems (per:IPersistence) =
    match existingItems with
      | [] ->
        newValue.Id <- -1 // get a new id
        per.Insert(newValue)
      | itm :: _ ->
        newValue.Id <- itm.Id
        per.Update(newValue)

  let matchExisting (newMedia:medias) (per:IPersistence) =
    per.Select(<@ fun (media:medias) ->
      media.Url = newMedia.Url
    @>)

  let photoExtensions = [".jpg";".jpeg";".png";".bmp"]

  let (|Photo|Video|) (ext:string) =
    if (List.contains ext photoExtensions) then
      Photo
    else
      Video

  let getTaken (file:FileInfo) fileType =
    use reader = new ExifReader(file.FullName)
    let taken = DateTime.UtcNow
    ignore <| reader.GetTagValue(ExifTags.DateTimeDigitized, ref taken)
    taken

  let createNewMedia takenF time (rootDir:string) (file:FileInfo) =
    let filetype =
      match file.Extension with
      | Photo -> MediaType.Photo
      | Video -> MediaType.Video
    let taken = takenF file filetype
    {
      Id = -1
      Url = file.FullName.Substring(rootDir.Length)
      Taken = taken
      Added = time
      Type = filetype
    }

  let rec removeAll (recs:medias list) (p:IPersistence) =
    match recs with
    | recd :: tail ->
      let (_,p2) = p.Delete(recd)
      removeAll tail p2
    | [] -> p

  let getOldMedias (p:IPersistence) (t:DateTime) : (medias list * IPersistence) =
    p.Select(<@ fun (media:medias) ->
      media.Added < t
    @>)

  let dirFinder (name:string) : (FileInfo list) =
    (new DirectoryInfo(name)).GetFiles()
    |> Array.toList

   // COMPOSITE
  let removeOld getOldF (time:DateTime) (p:IPersistence) =
    let old, newP = getOldF p time
    removeAll old newP

  let fileUpdate makeNewF matchF updateF takenF time rootDir file (p:'p) : 'p =
    let newMedia = makeNewF takenF time rootDir file
    let fn = state {
      let! items = matchF newMedia
      let! dbRec = updateF newMedia items
      return dbRec
    }
    State.execute fn p

  let updateMedia dirFinderF fileHandlerF removeOldF dir per =
    let time = DateTime.UtcNow
    let files = dirFinderF dir
    files
    |> List.fold (fun p file ->
      fileHandlerF time dir file p
    ) per
    |> removeOldF time

  // PARTIALS
  let removeOldC = removeOld getOldMedias
  let fileUpdateC = fileUpdate createNewMedia matchExisting itemUpdate getTaken

let updateMedia : string -> IPersistence -> IPersistence =
  Internal.updateMedia Internal.dirFinder Internal.fileUpdateC Internal.removeOldC
