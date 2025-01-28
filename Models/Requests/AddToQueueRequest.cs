namespace OverflowBackend.Models.Requests
{
    public class AddToQueueRequest
    {
        public bool? Prematch { get; set; }
        public string? WithUsername { get; set; }
        public bool? IsBot { get; set; }
    }
}
