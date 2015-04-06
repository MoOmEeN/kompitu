
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Security.Authentication.Web;
using Windows.Storage.Streams;

namespace Kompitu
{
    class Authenticator
    {
        private const string clientId = "CLIENT_ID";
        private const string clientSecret = "CLIENT_SECRET";
        private const string redirectUrl = "urn:ietf:wg:oauth:2.0:oob";
        private const string scope = "https://www.googleapis.com/auth/tasks";
        private const string tokenUri = "https://accounts.google.com/o/oauth2/token";

        private const string refreshTokenValueKey = "21afa3123";

        private HttpClient httpClient;

        private string accessToken;
        private string refreshToken;

        public async Task RefreshTokens()
        {
            await TryRestoreRefreshToken();
            if (refreshToken == null)
            {
                await AuthenticateUser();
            }
            else
            {
                await RefreshAccessToken();
            }
                
            Debug.WriteLine("r: " + refreshToken);
            Debug.WriteLine("a: " + accessToken);
        }

        public async Task<string> GetValidAccessToken()
        {
            await RefreshTokens();
            return accessToken;
        }

        private async Task AuthenticateUser()
        {
            // request for code
            string starteUriString = "https://accounts.google.com/o/oauth2/auth?client_id=" + Uri.EscapeDataString(clientId) + 
                "&redirect_uri=" + Uri.EscapeDataString(redirectUrl) + 
                "&scope=" + Uri.EscapeDataString(scope) +
                "&response_type=code&access_type=offline";
            string endUriString = "https://accounts.google.com/o/oauth2/approval?";

            WebAuthenticationResult WebAuthResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.UseTitle, 
                new Uri(starteUriString), new Uri(endUriString));

            if (WebAuthResult.ResponseStatus == WebAuthenticationStatus.Success)
            {
                string authorizationCode = WebAuthResult.ResponseData.Split('=')[1];

                //request for tokens
                string postContent = "code=" + authorizationCode + 
                    "&client_id=" + clientId + 
                    "&client_secret=" + clientSecret + 
                    "&redirect_uri=" + redirectUrl + 
                    "&grant_type=authorization_code";

                HttpResponseMessage response = await GetHttpClient().PostAsync(new Uri(tokenUri), 
                    new StringContent(postContent, Encoding.UTF8, "application/x-www-form-urlencoded"));

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    JsonObject jsonContent = JsonObject.Parse(content);
                    string accessTokenTmp = jsonContent.GetNamedString("access_token");
                    string refreshTokenTmp = jsonContent.GetNamedString("refresh_token");
                    if (accessTokenTmp == null || refreshTokenTmp == null)
                    {
                        throw new Exception("Cannot find expected token: " + accessTokenTmp == null ? "access_token" : "refresh_token" + " in response");
                    }
                    accessToken = accessTokenTmp;
                    refreshToken = refreshTokenTmp;
                    await StoreRefreshToken();
                }
                else
                {
                    throw new Exception("Unspecified exception during requesting tokens");
                }
            }
            else if (WebAuthResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
            {
                throw new Exception("HTTP error during requesting for authorization code");
            }
            else if (WebAuthResult.ResponseStatus == WebAuthenticationStatus.UserCancel)
            {
                throw new Exception("Login unsuccessful during requesting for authorization code");
            }
            else
            {
                throw new Exception("Unspecified exception during requesting for authorization code");
            }
        }

        private async Task RefreshAccessToken()
        {
            string postContent = "client_id=" + clientId +
                "&client_secret=" + clientSecret +
                "&refresh_token=" + refreshToken +
                "&grant_type=refresh_token";

            HttpResponseMessage response = await GetHttpClient().PostAsync(new Uri(tokenUri),
                    new StringContent(postContent, Encoding.UTF8, "application/x-www-form-urlencoded"));

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                JsonObject jsonContent = JsonObject.Parse(content);
                string accessTokenTmp = jsonContent.GetNamedString("access_token");
                if (accessTokenTmp != null)
                {
                    accessToken = accessTokenTmp;
                }
                else
                {
                    throw new Exception("Cannot find access_token in response");
                }
            }
            else
            {
                 throw new Exception("Unspecified exception during requesting for authorization code");
            }
        }

        private async Task StoreRefreshToken()
        {
            String protectedString = await DataProtector.ProtectAsync(refreshToken);
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[refreshTokenValueKey] = protectedString;
        }

        private async Task TryRestoreRefreshToken()
        {
            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(refreshTokenValueKey))
            {
                string protectedString = (string) Windows.Storage.ApplicationData.Current.LocalSettings.Values[refreshTokenValueKey];
                refreshToken = await DataProtector.UnprotectAsync(protectedString);
            }
        }

        private HttpClient GetHttpClient()
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }
            return httpClient;
        }

    }
}
