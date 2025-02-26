namespace InBoostTestApp.Data
{
    public class User
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string Name { get; set; }
        public List<WeatherRequest> Requests { get; set; } = new List<WeatherRequest>();

        public WeatherRequest GetLastRequest()
        {
            return Requests.OrderBy(item => item.RequestDate).Last();
        }
    }
}
