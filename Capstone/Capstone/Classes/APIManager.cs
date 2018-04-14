﻿using System;
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
        public async Task Upload(HttpContextBase httpContextBase, string path, string saveName)
        {
            string token = await GetAccessToken(httpContextBase);
            if (string.IsNullOrEmpty(token))
                return;
            
            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);

                        return Task.FromResult(0);
                    }));

            //convert file into byte array
            byte[] data = System.IO.File.ReadAllBytes(path);
            //convert byte array into writable stream
            Stream stream = new MemoryStream(data);

            //Get all items in drive
            var driveItem = await client.Me.Drive.Root.Children.Request().GetAsync();

            // for updating existing file, specify file you want to update using Items[itemid]
            //await client.Me.Drive.Items["55BBAC51A4E4017D!104"].Content.Request().PutAsync<DriveItem>(stream);

            // For uploading new file, specify file name is necessary with ItemWithPath(filename)
            await client.Me.Drive.Root.ItemWithPath(saveName).Content.Request().PutAsync<DriveItem>(stream);
        }


        // call this to download the file
        public async Task Download(HttpContextBase httpContextBase)
        {
            string token = await GetAccessToken(httpContextBase);
            if (string.IsNullOrEmpty(token))
                return;

            GraphServiceClient client = new GraphServiceClient(
             new DelegateAuthenticationProvider(
             (requestMessage) =>
             {
                 requestMessage.Headers.Authorization =
                     new AuthenticationHeaderValue("Bearer", token);

                 return Task.FromResult(0);
             }));
            var DriveItem = await client.Me.Drive.Root.Children.Request().GetAsync();
            //Get specific item based on itemID using items[itemid], use .content to get stream (byte data)
            Stream stream = await client.Me.Drive.Items["55BBAC51A4E4017D!104"].Content.Request().GetAsync();
            //path where you want to download
            string path = @"C:/Users/b_paquette/Desktop/test.xlsx";
            if (!System.IO.File.Exists(path))
            {
                // create filestream for writing data
                FileStream fs = System.IO.File.Create(path, (int)stream.Length);
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                fs.Write(bytesInStream, 0, bytesInStream.Length);
                fs.Close();
                fs.Dispose();
                stream.Close();
                stream.Dispose();
            }
            else
            {
                System.IO.File.Delete(path);
                FileStream fs = System.IO.File.Create(path, (int)stream.Length);
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                fs.Write(bytesInStream, 0, bytesInStream.Length);
                fs.Close();
                fs.Dispose();
                stream.Close();
                stream.Dispose();
            }
        }
    }
}