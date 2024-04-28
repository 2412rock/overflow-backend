namespace OverflowBackend.Models.Requests
{
    public class SignUpRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
    }
}
