using Newtonsoft.Json.Linq;
using Watchly.Web.Services;

namespace Watchly.Web.Services
{
    public class TmdbService : ITmdbService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TmdbService> _logger;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.themoviedb.org/3";

        public TmdbService(HttpClient httpClient, ILogger<TmdbService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Tmdb:ApiKey"] ?? "";
        }

        public async Task<IEnumerable<TmdbMovieDto>> SearchMoviesAsync(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("TMDB API key is not configured");
                    return new List<TmdbMovieDto>();
                }

                var url = $"{BaseUrl}/search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&language=ru";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("TMDB API search failed: {StatusCode}", response.StatusCode);
                    return new List<TmdbMovieDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var results = json["results"]?.ToList() ?? new List<JToken>();

                return results.Select(r => new TmdbMovieDto
                {
                    Id = r["id"]?.Value<int>() ?? 0,
                    Title = r["title"]?.Value<string>() ?? "Unknown",
                    PosterPath = r["poster_path"]?.Value<string>(),
                    VoteAverage = r["vote_average"]?.Value<decimal>() ?? 0,
                    ReleaseYear = r["release_date"]?.Value<string>()?.Split('-').FirstOrDefault() is string year && int.TryParse(year, out var y) ? y : null,
                    Overview = r["overview"]?.Value<string>()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching TMDB movies");
                return new List<TmdbMovieDto>();
            }
        }

        public async Task<TmdbMovieDetailDto?> GetMovieDetailAsync(int tmdbId)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return null;

                var url = $"{BaseUrl}/movie/{tmdbId}?api_key={_apiKey}&language=ru&append_to_response=credits";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                return new TmdbMovieDetailDto
                {
                    Id = json["id"]?.Value<int>() ?? 0,
                    Title = json["title"]?.Value<string>() ?? "Unknown",
                    PosterPath = json["poster_path"]?.Value<string>(),
                    VoteAverage = json["vote_average"]?.Value<decimal>() ?? 0,
                    ReleaseYear = json["release_date"]?.Value<string>()?.Split('-').FirstOrDefault() is string year && int.TryParse(year, out var y) ? y : null,
                    Overview = json["overview"]?.Value<string>(),
                    Runtime = json["runtime"]?.Value<int>(),
                    Country = json["production_countries"]?.FirstOrDefault()?["name"]?.Value<string>(),
                    Director = json["credits"]?["crew"]?
                        .FirstOrDefault(c => string.Equals(c["job"]?.Value<string>(), "Director", StringComparison.OrdinalIgnoreCase))?["name"]?.Value<string>(),
                    Genres = json["genres"]?.Select(g => g["name"]?.Value<string>() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TMDB movie detail");
                return null;
            }
        }

        public async Task<IEnumerable<TmdbMovieDto>> GetPopularMoviesAsync(int page = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return new List<TmdbMovieDto>();

                var safePage = Math.Clamp(page, 1, 500);
                var url = $"{BaseUrl}/movie/popular?api_key={_apiKey}&language=ru&page={safePage}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<TmdbMovieDto>();

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var results = json["results"]?.ToList() ?? new List<JToken>();

                return results.Select(r => new TmdbMovieDto
                {
                    Id = r["id"]?.Value<int>() ?? 0,
                    Title = r["title"]?.Value<string>() ?? "Unknown",
                    PosterPath = r["poster_path"]?.Value<string>(),
                    VoteAverage = r["vote_average"]?.Value<decimal>() ?? 0,
                    ReleaseYear = r["release_date"]?.Value<string>()?.Split('-').FirstOrDefault() is string year && int.TryParse(year, out var y) ? y : null,
                    Overview = r["overview"]?.Value<string>()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular movies");
                return new List<TmdbMovieDto>();
            }
        }
        public async Task<string?> GetTrailerVideoIdAsync(int tmdbId)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return null;

                var url = $"{BaseUrl}/movie/{tmdbId}/videos?api_key={_apiKey}&language=en-US";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var results = json["results"]?.ToList() ?? new List<JToken>();

                var trailer = results.FirstOrDefault(v =>
                    string.Equals(v["site"]?.Value<string>(), "YouTube", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(v["type"]?.Value<string>(), "Trailer", StringComparison.OrdinalIgnoreCase) &&
                    (v["official"]?.Value<bool>() ?? false));

                trailer ??= results.FirstOrDefault(v =>
                    string.Equals(v["site"]?.Value<string>(), "YouTube", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(v["type"]?.Value<string>(), "Trailer", StringComparison.OrdinalIgnoreCase));

                return trailer?["key"]?.Value<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TMDB trailer videos");
                return null;
            }
        }

    }
}
