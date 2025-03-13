using DevilStudio.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Web;

namespace DevilStudio
{
    public class OAuthService
    {
        private readonly string _googleClientId;
        private readonly string _googleClientSecret;
        private readonly string _githubClientId;
        private readonly string _githubClientSecret;
        private readonly string _facebookAppId;
        private readonly string _facebookAppSecret;
        private readonly string _instagramClientId;
        private readonly string _instagramClientSecret;
        private readonly string _youtubeClientId;
        private readonly string _youtubeClientSecret;
        private readonly string _linkedInClientId;
        private readonly string _linkedInClientSecret;

        private readonly string _redirectUri = "http://localhost:5000/callback";
        //private readonly string testRedirectUri = "https://www.apisecuniversity.com/facebook/callback";

        public OAuthService()
        {
            _googleClientId = OAuthConfig.GetClientId("Google");
            _googleClientSecret = OAuthConfig.GetClientSecret("Google");
            _githubClientId = OAuthConfig.GetClientId("GitHub");
            _githubClientSecret = OAuthConfig.GetClientSecret("GitHub");
            _facebookAppId = OAuthConfig.GetAppId("Facebook");
            _facebookAppSecret = OAuthConfig.GetAppSecret("Facebook");
            _instagramClientId = OAuthConfig.GetAppId("Instagram");
            _instagramClientSecret = OAuthConfig.GetAppSecret("Instagram");
            _youtubeClientId = OAuthConfig.GetClientId("YouTube");
            _youtubeClientSecret = OAuthConfig.GetClientSecret("YouTube");
            _linkedInClientId = OAuthConfig.GetClientId("LinkedIn");
            _linkedInClientSecret = OAuthConfig.GetClientSecret("LinkedIn");
        }

        public string GetOAuthUrl(string provider)
        {
            switch (provider)
            {
                case "Google":
                    return $"https://accounts.google.com/o/oauth2/auth?client_id={_googleClientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope=openid%20email%20profile";
                case "GitHub":
                    return $"https://github.com/login/oauth/authorize?client_id={_githubClientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&scope=user";
                case "Facebook":
                    return $"https://www.facebook.com/v12.0/dialog/oauth?client_id={_facebookAppId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope=public_profile";
                case "Instagram":
                    return $"https://api.instagram.com/oauth/authorize?client_id={_instagramClientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope=user_profile,user_media";
                case "YouTube":
                    return $"https://accounts.google.com/o/oauth2/auth?client_id={_youtubeClientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope=openid%20email%20profile";
                case "LinkedIn":
                    return $"https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={_linkedInClientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&scope=openid%20profile%20email";
                default:
                    throw new ArgumentException("Unsupported provider", nameof(provider));
            }
        }

        public async Task<TokenResponse> GetAccessTokenAsync(string provider, string code)
        {
            string tokenUrl;
            string clientId;
            string clientSecret;
            string grantType = "authorization_code";

            switch (provider)
            {
                case "Google":
                case "YouTube":
                    tokenUrl = "https://oauth2.googleapis.com/token";
                    clientId = provider == "Google" ? _googleClientId : _youtubeClientId;
                    clientSecret = provider == "Google" ? _googleClientSecret : _youtubeClientSecret;
                    break;
                case "GitHub":
                    tokenUrl = "https://github.com/login/oauth/access_token";
                    clientId = _githubClientId;
                    clientSecret = _githubClientSecret;
                    break;
                case "Facebook":
                    tokenUrl = "https://graph.facebook.com/v12.0/oauth/access_token";
                    clientId = _facebookAppId;
                    clientSecret = _facebookAppSecret;
                    break;
                case "Instagram":
                    tokenUrl = "https://api.instagram.com/oauth/access_token";
                    clientId = _instagramClientId;
                    clientSecret = _instagramClientSecret;
                    break;
                case "LinkedIn":
                    tokenUrl = "https://www.linkedin.com/oauth/v2/accessToken";
                    clientId = _linkedInClientId;
                    clientSecret = _linkedInClientSecret;
                    break;
                default:
                    throw new ArgumentException("Unsupported provider", nameof(provider));
            }

            using (HttpClient client = new HttpClient())
            {
                var requestData = new Dictionary<string, string>
                {
                    { "grant_type", grantType },
                    { "code", code },
                    { "redirect_uri", _redirectUri }, // ✅ Must match LinkedIn App Settings
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                };

                var content = new FormUrlEncodedContent(requestData);

                // ✅ LinkedIn requires `application/x-www-form-urlencoded`
                if (provider == "LinkedIn")
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                }

                HttpResponseMessage response = await client.PostAsync(tokenUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"LinkedIn Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        JObject json = JObject.Parse(responseContent);
                        string? accessToken = json["access_token"]?.ToString();
                        string? refreshToken = json["refresh_token"]?.ToString();
                        string? idToken = json["id_token"]?.ToString();
                        string? email = null;

                        // 🔹 LinkedIn Only: Extract Email from id_token
                        if (provider == "LinkedIn" && !string.IsNullOrEmpty(idToken))
                        {
                            email = ExtractEmailFromIdToken(idToken);
                            Debug.WriteLine($"Extracted Email from LinkedIn id_token: {email}");
                        }

                        return new TokenResponse
                        {
                            AccessToken = accessToken,
                            RefreshToken = refreshToken,
                            IdToken = idToken,
                            Email = email
                        };
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.WriteLine($"JSON Parsing Error: {ex.Message}");
                        throw new Exception("Failed to parse the token response.");
                    }
                }
                else
                {
                    throw new Exception($"Error response from {provider}: {responseContent}");
                }
            }
        }

        public string ExtractEmailFromIdToken(string idToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(idToken);

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            return emailClaim ?? "Email not found";
        }

        public async Task<string?> RefreshAccessTokenAsync(string refreshToken, string provider)
        {
            string tokenUrl = "https://oauth2.googleapis.com/token";
            string clientId = provider == "Google" ? _googleClientId : _youtubeClientId;
            string clientSecret = provider == "Google" ? _googleClientSecret : _youtubeClientSecret;

            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("grant_type", "refresh_token")
        });

                HttpResponseMessage response = await client.PostAsync(tokenUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JObject json = JObject.Parse(responseContent);
                    return json["access_token"]?.ToString(); // Return new access token
                }
                else
                {
                    throw new Exception($"Error refreshing token: {responseContent}");
                }
            }
        }

        public async Task<JObject> GetUserInfoAsync(string provider, string accessToken)
        {
            string userInfoEndpoint;

            switch (provider)
            {
                case "Google":
                    userInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
                    break;
                case "GitHub":
                    userInfoEndpoint = "https://api.github.com/user";
                    break;
                case "Facebook":
                    userInfoEndpoint = "https://graph.facebook.com/me?fields=id,name,email";
                    break;
                case "Instagram":
                    userInfoEndpoint = "https://graph.instagram.com/me?fields=id,username,account_type";
                    break;
                case "YouTube":
                    userInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
                    break;
                case "LinkedIn":
                    userInfoEndpoint = "https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements*(handle~:emailAddress))";
                    break;
                default:
                    throw new ArgumentException("Unsupported provider", nameof(provider));
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                client.DefaultRequestHeaders.Add("User-Agent", "YourAppName");

                HttpResponseMessage response = await client.GetAsync(userInfoEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error fetching data from {provider}: {response.StatusCode} - {errorResponse}");
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                return JObject.Parse(responseContent);
            }
        }

        public async Task<List<ConnectedAccount>> GetConnectedAccountsAsync(string email)
        {
            using (var context = new MyDbContext())
            {
                var connectedAccounts = await context.ConnectedAccounts
                                                     .Include(ca => ca.UserDetail)
                                                     .Where(ca => ca.UserDetail.Email == email)
                                                     .ToListAsync();
                return connectedAccounts;
            }
        }

        public class TokenResponse
        {
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            public string? IdToken { get; set; }
            public string? Email { get; set; }
        }

    }
}