using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DevilStudio
{
    public class OAuthConfig
    {
        public static readonly JObject Config;

        static OAuthConfig()
        {
            string fileName = "oauthconfig.json";
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while(directory != null && !File.Exists(Path.Combine(directory.FullName, fileName))) {
                directory = directory.Parent;
                if(directory != null)
                {
                    Debug.WriteLine(directory.FullName);
                }
            }

            string oAuthConfigFilePath = Path.Combine(directory.FullName, fileName);
            if (!File.Exists(oAuthConfigFilePath))
            {
                throw new FileNotFoundException($"Could not find file '{oAuthConfigFilePath}'");
            }
            string json = File.ReadAllText(oAuthConfigFilePath);
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
    