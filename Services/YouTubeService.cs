using Newtonsoft.Json.Linq;
using Watchly.Web.Services;

namespace Watchly.Web.Services
{
    public class YouTubeService : IYouTubeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YouTubeService> _logger;
        private readonly string _apiKey;
        private const string BaseUrl = "https://www.googleapis.com/youtube/v3";

        public YouTubeService(HttpClient httpClient, ILogger<YouTubeService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["YouTube:ApiKey"] ?? "";
        }

        public async Task<string?> SearchTrailerAsync(string movieTitle)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("YouTube API key is not configured");
                    return null;
                }

                var query = $"{movieTitle} trailer official";
                var url = $"{BaseUrl}/search?part=snippet&q={Uri.EscapeDataString(query)}&type=video&videoEmbeddable=true&key={_apiKey}&maxResults=3";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("YouTube API search failed: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var items = json["items"]?.ToList() ?? new List<JToken>();
                return items.Select(i => i["id"]?["videoId"]?.Value<string>())
                    .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching YouTube trailer");
                return null;
            }
        }

        public string GetEmbedUrl(string videoId)
        {
            return $"https://www.youtube.com/embed/{videoId}";
        }

        public string GetThumbnailUrl(string videoId)
        {
            return $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg";
        }
    }
}