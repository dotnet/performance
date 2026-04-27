#load "Result.fs"
#load "Option.fs"
#load "ResultCE.fs"
#load "Validation.fs"
#load "ValidationOp.fs"

open System
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Validation
open FsToolkit.ErrorHandling.CE.Result

type Tweet =
    private
    | Tweet of string
    member this.Value =
        let (Tweet tweet) = this
        tweet

    static member TryCreate(tweet: string) =
        match tweet with
        | x when String.IsNullOrEmpty x -> Error "Tweet shouldn't be empty"
        | x when x.Length > 280 -> Error "Tweet shouldn't contain more than 280 characters"
        | x -> Ok(Tweet x)


type Latitude =
    private
    | Latitude of double
    member this.Value =
        let (Latitude lat) = this
        lat

    static member TryCreate(lat: double) =
        if lat > -180. && lat < 180. then
            Ok(Latitude lat)
        else
            sprintf "%A is a invalid latitude value" lat
            |> Error

type Longitude =
    private
    | Longitude of double
    member this.Value =
        let (Longitude lng) = this
        lng

    static member TryCreate(lng: double) =
        if lng > -90. && lng < 90. then
            Ok(Longitude lng)
        else
            sprintf "%A is a invalid longitude value" lng
            |> Error

type Location = {
    Latitude: Latitude
    Longitude: Longitude
}

let location lat lng = { Latitude = lat; Longitude = lng }

type CreatePostRequest = {
    Tweet: Tweet
    Location: Location option
}

let createPostRequest location tweet = { Tweet = tweet; Location = location }


type LocationDto = { Latitude: double; Longitude: double }

type CreatePostRequestDto = {
    Tweet: string
    Location: LocationDto option
}

let validateLocation (dto: LocationDto) =
    location
    <!^> Result.tryCreate "latitude" dto.Latitude
    <*^> Result.tryCreate "longitude" dto.Longitude

let validateCreatePostRequest (dto: CreatePostRequestDto) =
    createPostRequest
    <!> Option.traverseResult validateLocation dto.Location
    <*^> Result.tryCreate "tweet" dto.Tweet
