using Microsoft.AspNetCore.Identity;
using MyApp.App.Utils;
using System.Globalization;

namespace MyApp.App.Biz
{
    public static class Authenticator
    {
        private static bool VerifyHash(Customer customer, string hashedPassword, string password) {
            PasswordHasher<Customer> passwordHasher = new PasswordHasher<Customer>();
            PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(customer, hashedPassword, password);
            if (result == 0)
                return false;
            else
                return true;
        }

        public static bool AdminAuth(string email, string password) {
            email = email.ToLower(new CultureInfo("en-US", false));
            App.Biz.Customer customer = new();
            string adminEmail = AppSettings.GetString("AdminEmail");
            bool correctEmail = VerifyHash(customer, adminEmail, email);
            if (correctEmail) {
                string hashedPassword = AppSettings.GetString("AdminPassword");
                return VerifyHash(customer, hashedPassword, password);
            }
            return false;
        }

        public static bool CustomerAuth(string email, string password) {
            email = email.ToLower(new CultureInfo("en-US", false));
            App.Biz.Customer customer = new();
            customer = customer.GetByEmail(email);
            if (customer.Error == "")
                return VerifyHash(customer, customer.HashedPswd, password);
            else
                return false;
        }
    }
}