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
using System.Net;
using System.IO;

namespace Capstone.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                string userName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Email).Value;
                if (string.IsNullOrEmpty(userId))
                {
                    // Invalid principal, sign out
                    return RedirectToAction("SignOut");
                }

                // Since we cache tokens in the session, if the server restarts
                // but the browser still has a cached cookie, we may be
                // authenticated but not have a valid token cache. Check for this
                // and force signout.
                SessionTokenCache tokenCache = new SessionTokenCache(userId, HttpContext);
                if (!tokenCache.HasData())
                {
                    // Cache is empty, sign out
                    return RedirectToAction("SignOut");
                }

                ViewBag.UserName = userName;
            }
            return View();
        }

        public async Task<ActionResult> Inbox()
        {
            if (Request.IsAuthenticated)
            {
                string token = await GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    // If there's no token in the session, redirect to Home
                    return Redirect("/");
                }

                GraphServiceClient client = new GraphServiceClient(
                    new DelegateAuthenticationProvider(
                        (requestMessage) =>
                        {
                            requestMessage.Headers.Authorization =
                                new AuthenticationHeaderValue("Bearer", token);

                            return Task.FromResult(0);
                        }));

                try
                {
                    var mailResults = await client.Me.MailFolders.Inbox.Messages.Request()
                                        .OrderBy("receivedDateTime DESC")
                                        .Select("subject,receivedDateTime,from")
                                        .Top(10)
                                        .GetAsync();

                    return View(mailResults.CurrentPage);
                }
                catch (ServiceException ex)
                {
                    return RedirectToAction("Error", "Home", new { message = "ERROR retrieving messages", debug = ex.Message });
                }
            }
            else { return RedirectToAction("SignOut", "Home", null); }
        }

        public async Task<ActionResult> OneDrive()
        {
            string token = await GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                // If there's no token in the session, redirect to Home
                return Redirect("/");
            }

            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);

                        return Task.FromResult(0);
                    }));

            try
            {
                
                var DriveItem = await client.Me.Drive.Root.Children.Request().GetAsync();
                Stream stream = await client.Me.Drive.Items["55BBAC51A4E4017D!104"].Content.Request().GetAsync();
                FileStream fs = System.IO.File.Create(@"C:/Users/b_paquette/Desktop/test.xlsx",(int)stream.Length);
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                fs.Write(bytesInStream, 0, bytesInStream.Length);

                return View(DriveItem);
            }
            catch (ServiceException ex)
            {
                return RedirectToAction("Error", "Home", new { message = "ERROR retrieving messages", debug = ex.Message });
            }
        }
        // call this to download the file
        public async Task<ActionResult> OneDriveDownload()
        {
            string token = await GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                // If there's no token in the session, redirect to Home
                return Redirect("/");
            }

            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);

                        return Task.FromResult(0);
                    }));

            try
            {

                var DriveItem = await client.Me.Drive.Root.Children.Request().GetAsync();
                Stream stream = await client.Me.Drive.Items["55BBAC51A4E4017D!104"].Content.Request().GetAsync();
                string path = @"C:/Users/b_paquette/Desktop/test.xlsx";
                if (!System.IO.File.Exists(@"C:/Users/b_paquette/Desktop/test.xlsx"))
                {
                    FileStream fs = System.IO.File.Create(@"C:/Users/b_paquette/Desktop/test.xlsx", (int)stream.Length);
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    fs.Write(bytesInStream, 0, bytesInStream.Length);
                    fs.Close();
                    fs.Dispose();
                    stream.Close();
                    stream.Dispose();
                    ViewBag.error = "SUCCESS!";
                    return View("Index");
                }
                else
                {
                    ViewBag.error = "File already Exists";
                    return View("Index");
                }
                
            }
            catch (ServiceException ex)
            {
                return RedirectToAction("Error", "Home", new { message = "ERROR retrieving messages", debug = ex.Message });
            }
        }


        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        public async Task<ActionResult> Calendar()
        {
            if (Request.IsAuthenticated)
            {
                string token = await GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    // If there's no token in the session, redirect to Home
                    return Redirect("/");
                }

                GraphServiceClient client = new GraphServiceClient(
                    new DelegateAuthenticationProvider(
                        (requestMessage) =>
                        {
                            requestMessage.Headers.Authorization =
                                new AuthenticationHeaderValue("Bearer", token);

                            return Task.FromResult(0);
                        }));

                try
                {
                    var eventResults = await client.Me.Events.Request()
                                        .OrderBy("start/dateTime DESC")
                                        .Select("subject,start,end")
                                        .Top(10)
                                        .GetAsync();

                    return View(eventResults.CurrentPage);
                }
                catch (ServiceException ex)
                {
                    return RedirectToAction("Error", "Home", new { message = "ERROR retrieving events", debug = ex.Message });
                }
            }
            else { return RedirectToAction("SignOut", "Home", null); }
        }
        
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        public void SignOut()
        {
            if (Request.IsAuthenticated)
            {
                string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Get the user's token cache and clear it
                    SessionTokenCache tokenCache = new SessionTokenCache(userId, HttpContext);
                    tokenCache.Clear();
                }
            }
            // Send an OpenID Connect sign-out request. 
            HttpContext.GetOwinContext().Authentication.SignOut(
                CookieAuthenticationDefaults.AuthenticationType);
            Response.Redirect("/");
        }
        public async Task<string> GetAccessToken()
        {
            string accessToken = null;

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
                SessionTokenCache tokenCache = new SessionTokenCache(userId, HttpContext);

                ConfidentialClientApplication cca = new ConfidentialClientApplication(
                    appId, redirectUri, new ClientCredential(appPassword), tokenCache.GetMsalCacheInstance(), null);

                // Call AcquireTokenSilentAsync, which will return the cached
                // access token if it has not expired. If it has expired, it will
                // handle using the refresh token to get a new one.
                AuthenticationResult result = await cca.AcquireTokenSilentAsync(scopes, cca.Users.FirstOrDefault());

                accessToken = result.AccessToken;
            }

            return accessToken;
        }
        public ActionResult Preferences()
        {
            if (Request.IsAuthenticated)
            {
                return View();
            }
            else { return RedirectToAction("SignOut", "Home", null); }
        }
        public ActionResult History()
        {
            if (Request.IsAuthenticated)
            {
                return View();
            }
            else { return RedirectToAction("SignOut", "Home", null); }
        }
    }
}