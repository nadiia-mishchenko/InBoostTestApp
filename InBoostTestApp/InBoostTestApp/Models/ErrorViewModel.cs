namespace InBoostTestApp.Models
{
    /// <summary>
    /// Error view model
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Request id
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Show request id
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
