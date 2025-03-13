using System.IO;
using Newtonsoft.Json.Linq;

namespace DevilStudio
{
    public class OAuthConfig
    {
        public static readonly JObject Config;

        static OAuthConfig()
        {
            string configFilePath = Path.Combine(AppContext.BaseDirectory, "oauthconfig.json");
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"Could not find file '{configFilePath}'");
            }
            string json = File.ReadAllText(configFilePath);
            Config = JObject.Parse(json);
        }

        public static string GetClientId(string provider)
        {
            return Config[provider]["ClientId"]?.ToString();
        }

        public static string GetClientSecret(string provider)
        {
            return Config[provider]["ClientSecret"]?.ToString();
        }

        public static string GetAppId(string provider)
        {
            return Config[provider]["AppId"]?.ToString();
        }

        public static string GetAppSecret(string provider)
        {
            return Config[provider]["AppSecret"]?.ToString();
        }
    }


}
    