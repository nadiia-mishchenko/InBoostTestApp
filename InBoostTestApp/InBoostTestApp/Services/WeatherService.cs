namespace InBoostTestApp.Services
{
    /// <summary>
    /// Weather interface
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Receives current weather
        /// </summary>
        /// <param name="city">City name</param>
        /// <returns>Current weather data</returns>
        Task<string> GetWeather(string city);
    }

    /// <summary>
    /// Weather implementation
    /// </summary>
    public class WeatherService : IWeatherService
    {
        const string APP_ID = "your_id";

        private readonly ILogger<WeatherService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="httpClientFactory">Http client factory</param>
        public WeatherService(ILogger<WeatherService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Receives current weather
        /// </summary>
        /// <param name="city">City name</param>
        /// <returns>Current weather data</returns>
        public async Task<string> GetWeather(string city)
        {
            var request = $"https://api.openweathermap.org/data/2.5/weather?q={city}&APPID={APP_ID}";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(request));
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                try
                {
                    return await httpResponseMessage.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }

            return "cannot get weather";
        }
    }
}
