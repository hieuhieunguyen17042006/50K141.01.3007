using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace THomeApp.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            return "PBKDF2$" + new PasswordHasher<string>().HashPassword("T-HOME", password);
        }

        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (storedHash.StartsWith("PBKDF2$"))
            {
                try { return new PasswordHasher<string>().VerifyHashedPassword("T-HOME", storedHash[7..], inputPassword) != PasswordVerificationResult.Failed; }
                catch (FormatException) { return false; }
            }
            // Keep existing demo accounts usable, then upgrade on successful login.
            var legacy = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(inputPassword + "_THOME_SALT_2026")));
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(legacy), Encoding.UTF8.GetBytes(storedHash));
        }

        public static (bool IsValid, string Message) ValidatePasswordComplexity(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length <= 8)
            {
                return (false, "Mật khẩu phải trên 8 kí tự!");
            }
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            if (!hasUpper || !hasLower || !hasSpecial)
            {
                return (false, "Mật khẩu phải có cả chữ hoa, chữ thường và kí tự đặc biệt!");
            }

            return (true, string.Empty);
        }
    }
}
