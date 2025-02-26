using System.ComponentModel;

namespace InBoostTestApp.Models
{
    /// <summary>
    /// User view model
    /// </summary>
    public class UserViewModel
    {
        /// <summary>
        /// User id
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// User name
        /// </summary>
        [DisplayName("User name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Weather requests
        /// </summary>
        public List<WeatherViewModel> WeatherRequests { get; set; } = new List<WeatherViewModel>();
    }
}
