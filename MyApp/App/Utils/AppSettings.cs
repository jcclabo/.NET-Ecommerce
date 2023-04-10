using Microsoft.Data.SqlClient;

namespace MyApp.App.Utils
{
    public static class AppSettings
    {
        public static string GetString(string key)
        {
            var configuation = GetConfiguration();
            return configuation.GetSection("Data").GetSection(key).Value;
        }

        private static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            return builder.Build();
        }
    }
}
