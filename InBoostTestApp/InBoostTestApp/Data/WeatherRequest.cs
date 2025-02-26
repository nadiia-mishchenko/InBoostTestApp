namespace InBoostTestApp.Data
{
    public class WeatherRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public DateTime RequestDate { get; set; }
    }
}
