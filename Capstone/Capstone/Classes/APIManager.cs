using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Identity.Client;
using Capstone.TokenStorage;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Data.OleDb;
using Capstone.Models;
using System.Globalization;
using System.IO;
using Capstone.Classes;
using Capstone.Controllers;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Capstone.Classes
{
    public class APIManager
    {
        public async Task<string> GetAccessToken(HttpContextBase httpContextBase)
        {
            string token = null;

            // Load the app config from web.config
            string appId = ConfigurationManager.AppSettings["ida:AppId"];
            string appPassword = ConfigurationManager.AppSettings["ida:AppPassword"];
            string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
            string[] scopes = ConfigurationManager.AppSettings["ida:AppScopes"]
                .Replace(' ', ',').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Get the current user's ID
            string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Get the user's token cache
                SessionTokenCache tokenCache = new SessionTokenCache(userId, httpContextBase);

                ConfidentialClientApplication cca = new ConfidentialClientApplication(
                    appId, redirectUri, new ClientCredential(appPassword), tokenCache.GetMsalCacheInstance(), null);

                // Call AcquireTokenSilentAsync, which will return the cached
                // access token if it has not expired. If it has expired, it will
                // handle using the refresh token to get a new one.
                AuthenticationResult result = await cca.AcquireTokenSilentAsync(scopes, cca.Users.FirstOrDefault());
                token = result.AccessToken;
            }
            return token;
        }
        public async Task<string> UploadSheet(HttpContextBase httpContextBase, string path, string saveName, bool examSheet = false)
        {
            // This whole process is returning a strange exception that doesn't seem to actually be doing anything.
            // File is uploaded properly without corruption and application continues normally.
            // Exception thrown: 'System.InvalidOperationException' in mscorlib.dll

            string token = await GetAccessToken(httpContextBase);
            if (string.IsNullOrEmpty(token))
                return null;

            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);

                        return Task.FromResult(0);
                    }));
            
            byte[] data = System.IO.File.ReadAllBytes(path);
            // Writeable stream from byte array for drive upload
            Stream stream = new MemoryStream(data);

            string dir = "/PROGRAM SCHEDULES/";
            if (examSheet)
                dir = "/EXAM SCHEDULES/";

            Microsoft.Graph.DriveItem file = client.Me.Drive.Root.ItemWithPath(dir + saveName).Content.Request().PutAsync<DriveItem>(stream).Result;
            return file.WebUrl;
        }

        public async Task<IDriveItemChildrenCollectionPage> GetDriveItems(HttpContextBase httpContextBase)
        {
            // This whole process is returning a strange exception that doesn't seem to actually be doing anything.
            // File is uploaded properly without corruption and application continues normally.
            // Exception thrown: 'System.InvalidOperationException' in mscorlib.dll

            string token = await GetAccessToken(httpContextBase);
            if (string.IsNullOrEmpty(token))
                return null;

            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);

                        return Task.FromResult(0);
                    }));

            //Get all items in drive
            IDriveItemChildrenCollectionPage items = await client.Me.Drive.Root.ItemWithPath("/PROGRAM SCHEDULES/").Children.Request().GetAsync();
            return items;
        }
        
        public async Task<bool> DownloadSheet(HttpContextBase httpContextBase, string driveItemID, string path)
        {
            string token = await GetAccessToken(httpContextBase);
            if (string.IsNullOrEmpty(token))
                return false;

            GraphServiceClient client = new GraphServiceClient(
             new DelegateAuthenticationProvider(
             (requestMessage) =>
             {
                 requestMessage.Headers.Authorization =
                     new AuthenticationHeaderValue("Bearer", token);

                 return Task.FromResult(0);
             }));

            Stream stream = await client.Me.Drive.Items[driveItemID].Content.Request().GetAsync();

            FileStream fs = System.IO.File.Create(path, (int)stream.Length);
            byte[] bytesInStream = new byte[stream.Length];
            stream.Read(bytesInStream, 0, bytesInStream.Length);
            fs.Write(bytesInStream, 0, bytesInStream.Length);
            fs.Close();
            fs.Dispose();
            stream.Close();
            stream.Dispose();
            return true;
        }
    }
}