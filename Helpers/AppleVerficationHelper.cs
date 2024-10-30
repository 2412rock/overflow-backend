using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace OverflowBackend.Helpers
{
    public static class AppleVerficationHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static List<SecurityKey> _applePublicKeys;

        public static async Task<List<SecurityKey>> GetApplePublicKeysAsync()
        {
            // Check if the public keys are already cached
            if (_applePublicKeys != null)
                return _applePublicKeys;

            try
            {
                // Fetch the Apple public keys
                var response = await _httpClient.GetStringAsync("https://appleid.apple.com/auth/keys");
                var jsonDoc = JsonDocument.Parse(response);

                // Ensure the keys property exists and is an array
                if (jsonDoc.RootElement.TryGetProperty("keys", out JsonElement keys) && keys.ValueKind == JsonValueKind.Array)
                {
                    var publicKeys = new List<SecurityKey>();

                    foreach (var key in keys.EnumerateArray())
                    {
                        // Extract modulus and exponent
                        if (key.TryGetProperty("n", out JsonElement modulusElement) &&
                            key.TryGetProperty("e", out JsonElement exponentElement))
                        {
                            // Convert modulus and exponent from Base64 URL to bytes
                            var modulus = Base64UrlDecode(modulusElement.GetString());
                            var exponent = Base64UrlDecode(exponentElement.GetString());

                            // Create RSA parameters
                            var rsaParameters = new RSAParameters { Modulus = modulus, Exponent = exponent };

                            // Generate and add the RSA security key
                            var rsa = RSA.Create();
                            rsa.ImportParameters(rsaParameters);
                            publicKeys.Add(new RsaSecurityKey(rsa));
                        }
                        else
                        {
                            Console.WriteLine("Modulus or exponent not found in key.");
                        }
                    }

                    _applePublicKeys = publicKeys;
                }
                else
                {
                    throw new Exception("No keys found or keys are not in an array format.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching Apple public keys: " + ex.Message);
                throw;
            }

            return _applePublicKeys;
        }

        // Helper method to decode Base64 URL encoded strings
        public static byte[] Base64UrlDecode(string base64Url)
        {
            string padded = base64Url.Length % 4 == 0 ? base64Url :
                base64Url + new string('=', 4 - base64Url.Length % 4);
            string base64 = padded.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64);
        }

        public static async Task<bool> ValidateAppleIdToken(string idToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Fetch Apple's public keys
            var applePublicKeys = await GetApplePublicKeysAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudience = "com.overflow.ios", // Replace with your actual Service ID
                ValidateLifetime = true,
                IssuerSigningKeys = applePublicKeys
            };

            SecurityToken validatedToken;

            try
            {
                var principal = tokenHandler.ValidateToken(idToken, validationParameters, out validatedToken);

                // Token is valid, you can use the claims
                var claims = ((JwtSecurityToken)validatedToken).Claims;

                // Example: Extract user information
                var userId = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                // Additional claim processing as needed
                return true;
            }
            catch (SecurityTokenException ex)
            {
                // Handle token validation failure
                return false;
            }
        }
    }
}
