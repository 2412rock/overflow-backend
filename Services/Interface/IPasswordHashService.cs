namespace OverflowBackend.Services.Interface
{
    public interface IPasswordHashService
    {
        public string HashPassword(string password);

        public bool VerifyPassword(string password, string hashedPassword);
    }
}
