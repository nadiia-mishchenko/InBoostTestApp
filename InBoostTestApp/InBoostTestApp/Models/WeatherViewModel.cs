using System.ComponentModel;

namespace InBoostTestApp.Models
{
    /// <summary>
    /// Weather requests view model
    /// </summary>
    public class WeatherViewModel
    {
        /// <summary>
        /// Request id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// City name
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Request date
        /// </summary>
        [DisplayName("Request Date")]
        public DateTime RequestDate { get; set; }

        /// <summary>
        /// Request date in short date format
        /// </summary>
        public string RequestDateString { get => RequestDate.ToShortDateString(); }
    }
}
