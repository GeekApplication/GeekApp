namespace GeekApp.Server.Services
{
    public interface IGenreService
    {
        string GetGenreName(int genreId, string mediaType);
    }

    public class GenreService : IGenreService
    {
        private readonly Dictionary<int, string> _movieGenres = new()
        {
            [28] = "Action",
            [12] = "Adventure",
            [16] = "Animation",
            [35] = "Comedy",
            [80] = "Crime",
            [18] = "Drama",
            [10751] = "Family",
            [14] = "Fantasy",
            [36] = "History",
            [27] = "Horror",
            [10402] = "Music",
            [9648] = "Mystery",
            [10749] = "Romance",
            [878] = "Sci-Fi",
            [10770] = "TV Movie",
            [53] = "Thriller",
            [10752] = "War",
            [37] = "Western"
        };

        private readonly Dictionary<int, string> _tvGenres = new()
        {
            [10759] = "Action & Adventure",
            [16] = "Animation",
            [35] = "Comedy",
            [80] = "Crime",
            [99] = "Documentary",
            [18] = "Drama",
            [10751] = "Family",
            [10762] = "Kids",
            [9648] = "Mystery",
            [10763] = "News",
            [10764] = "Reality",
            [10765] = "Sci-Fi & Fantasy",
            [10766] = "Soap",
            [10767] = "Talk",
            [10768] = "War & Politics",
            [37] = "Western"
        };

        public string GetGenreName(int genreId, string mediaType)
        {
            return mediaType == "movie"
                ? _movieGenres.GetValueOrDefault(genreId, "Genre")
                : _tvGenres.GetValueOrDefault(genreId, "Genre");
        }
    }
}
