using Newtonsoft.Json;
using System.Collections.Generic;

namespace GeekApp.Shared.ApiModels
{
    public class TmdbCast
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("character")]
        public string Character { get; set; }

        [JsonProperty("profile_path")]
        public string ProfilePath { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }
    }

    public class TmdbCrew
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("job")]
        public string Job { get; set; }

        [JsonProperty("department")]
        public string Department { get; set; }
    }

    public class TmdbCredits
    {
        [JsonProperty("cast")]
        public List<TmdbCast> Cast { get; set; }

        [JsonProperty("crew")]
        public List<TmdbCrew> Crew { get; set; }
    }

    public class TmdbGenre
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class TmdbContentDetails
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("poster_path")]
        public string PosterPath { get; set; }

        [JsonProperty("backdrop_path")]
        public string BackdropPath { get; set; }

        [JsonProperty("genres")]
        public List<TmdbGenre> Genres { get; set; }

        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }

        [JsonProperty("first_air_date")]
        public string FirstAirDate { get; set; }

        [JsonProperty("runtime")]
        public int Runtime { get; set; }

        [JsonProperty("seasons")]
        public object Seasons { get; set; }

        [JsonProperty("vote_average")]
        public double VoteAverage { get; set; }
    }

    public class TmdbResult
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("poster_path")]
        public string PosterPath { get; set; }

        [JsonProperty("backdrop_path")]
        public string BackdropPath { get; set; }

        [JsonProperty("genre_ids")]
        public List<int> GenreIds { get; set; }

        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }

        [JsonProperty("first_air_date")]
        public string FirstAirDate { get; set; }

        [JsonProperty("vote_average")]
        public double VoteAverage { get; set; }
    }

    public class TmdbResponse
    {
        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("results")]
        public List<TmdbResult> Results { get; set; }

        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }

        [JsonProperty("total_results")]
        public int TotalResults { get; set; }
    }

    public class TmdbRoot
    {
        [JsonProperty("details")]
        public TmdbContentDetails Details { get; set; }

        [JsonProperty("credits")]
        public TmdbCredits Credits { get; set; }

        [JsonProperty("similar")]
        public TmdbResponse Similar { get; set; }
    }
}