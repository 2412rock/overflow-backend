using System.Collections.Concurrent;

namespace OverflowBackend.Services
{
    public static class VerificationCodeCollection
    {
        public static ConcurrentDictionary<string, string> Values = new ConcurrentDictionary<string, string>();
    }
}
