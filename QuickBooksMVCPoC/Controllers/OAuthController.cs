using Intuit.Ipp.OAuth2PlatformClient;
using QuickBooksMVCPoC.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace QuickBooksMVCPoC.Controllers
{
    public class OAuthController : Controller
    {
        private readonly OAuth2Client _oauthClient;
        private readonly QuickBooksSettings _settings;
        private readonly string _stateToken = Guid.NewGuid().ToString();
        public OAuthController()
        {
            _settings = new QuickBooksSettings
            {
                ClientId = ConfigurationManager.AppSettings["ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["ClientSecret"],
                RedirectUri = ConfigurationManager.AppSettings["RedirectUri"],
                Environment = ConfigurationManager.AppSettings["Environment"]
            };

            if (string.IsNullOrEmpty(_settings.RedirectUri))
            {
                throw new ArgumentNullException("RedirectUri cannot be null or empty. Please ensure it is correctly configured in Web.config.");
            }

            _oauthClient = new OAuth2Client(
                _settings.ClientId,
                _settings.ClientSecret,
                _settings.RedirectUri,
                _settings.Environment
          
            );
            _oauthClient.EnableAdvancedLoggerInfoMode = true;
            // _oauthClient.EnableAdvancedLoggerInfoMode = false;
            // _oauthClient.EnableSerilogRequestResponseLoggingForConsole = false;
            // _oauthClient.EnableSerilogRequestResponseLoggingForFile = false;
            // _oauthClient.EnableSerilogRequestResponseLoggingForDebug = false;
            // _oauthClient.EnableSerilogRequestResponseLoggingForTrace = false;

        }

        // Step 4.1: Initiate Authorization
        public ActionResult Authorize()
        {
            if (Session["LoggedIn"] == null || !(bool)Session["LoggedIn"])
            {
                return RedirectToAction("Index", "Home");
            }

            var scopes = new List<OidcScopes> { OidcScopes.Payment, OidcScopes.Accounting }; // Updated scopes to include Payment
            string authorizeUrl = _oauthClient.GetAuthorizationURL(scopes, _stateToken);
            Session["StateToken"] = _stateToken;
            return Redirect(authorizeUrl);
        }

        public async Task<ActionResult> Callback(string code, string realmId, string state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(realmId) || string.IsNullOrEmpty(state))
            {
                return Content("Invalid callback parameters. Make sure code, realmId, and state are provided.");
            }

            // Validate state to protect against CSRF attacks
            if (Session["StateToken"] == null || state != Session["StateToken"].ToString())
            {
                return Content("Invalid state token. Potential CSRF attack detected.");
            }

            try
            {
                // Exchange authorization code for access and refresh tokens
                var tokenResponse = await _oauthClient.GetBearerTokenAsync(code);
                Session["AccessToken"] = tokenResponse.AccessToken;
                Session["RefreshToken"] = tokenResponse.RefreshToken;
                Session["RealmId"] = realmId;  // Store the Realm ID for subsequent API requests
                Session["TokenExpiryTime"] = DateTime.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiresIn); // Store token expiry time

                return RedirectToAction("Connected");
            }
            catch (Exception ex)
            {
                return Content("An error occurred during the callback: " + ex.Message);
            }
        }

        public ActionResult Connected()
        {
            ViewBag.Message = "Successfully connected to QuickBooks.";
            return View();
        }
    }
}