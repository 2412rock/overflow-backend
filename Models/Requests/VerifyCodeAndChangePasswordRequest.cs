namespace OverflowBackend.Models.Requests
{
    public class VerifyCodeAndChangePasswordRequest
    {
        public string Username { get; set; }
        public string VerificationCode { get; set; }
        public string NewPassword {get; set;}
    }
}
