namespace OverflowBackend.Models.Response
{
    public class Tokens
    {
        public string BearerToken {get;set;}
        public string RefreshToken { get; set; }
        public string Session { get; set; }
        public string Username { get; set; }
    }
}
