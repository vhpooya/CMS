using System.Security.Cryptography;
using System.Text;

namespace RemoteDesktopApp.Helper
{
    public static class AppHelpers
    {
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // تبدیل رمز عبور به بایت‌ها
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                // محاسبه hash با استفاده از SHA-256
                byte[] hashedBytes = sha256.ComputeHash(passwordBytes);

                // تبدیل hash به رشته هگزادسیمال
                string hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

                return hashedPassword;
            }

        }
    }
}
