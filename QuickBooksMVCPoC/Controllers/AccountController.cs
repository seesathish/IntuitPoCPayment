using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.Security;
using Newtonsoft.Json.Linq;
using QuickBooksMVCPoC.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Xml.Linq;

namespace QuickBooksMVCPoC.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public async Task<ActionResult> Index()
        {
            if (Session["AccessToken"] == null || Session["RefreshToken"] == null || Session["RealmId"] == null)
            {
                return RedirectToAction("Authorize", "OAuth");
            }

            string accessToken = Session["AccessToken"] as string;
            string refreshToken = Session["RefreshToken"] as string;
            string realmId = Session["RealmId"] as string;

            // Check if token is expired
            if (DateTime.UtcNow > (DateTime)Session["TokenExpiryTime"])
            {
                try
                {
                    OAuth2Client oauthClient = new OAuth2Client(
                        ConfigurationManager.AppSettings["ClientId"],
                        ConfigurationManager.AppSettings["ClientSecret"],
                        ConfigurationManager.AppSettings["RedirectUri"],
                        ConfigurationManager.AppSettings["Environment"]
                    );

                    var tokenResponse = await oauthClient.RefreshTokenAsync(refreshToken);

                    // Update the session with new tokens
                    Session["AccessToken"] = tokenResponse.AccessToken;
                    Session["RefreshToken"] = tokenResponse.RefreshToken;
                    Session["TokenExpiryTime"] = DateTime.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiresIn);
                }
                catch (Exception ex)
                {
                    return Content("Error refreshing token: " + ex.Message);
                }
            }

            try
            {
                var serviceContext = QuickBooksHelper.CreateServiceContext(accessToken, realmId);
                var dataService = new DataService(serviceContext);

                // Step 3: Retrieve account data
                List<Account> accounts = dataService.FindAll(new Account(), 1, 100).ToList();

                return View("AccountList", accounts);
            }
            catch (Exception ex)
            {
                return Content("An error occurred while fetching accounts: " + ex.Message);
            }
        }

        public async Task<ActionResult> CompanyDetails()
        {
            if (Session["AccessToken"] == null || Session["RealmId"] == null)
            {
                return RedirectToAction("Authorize", "OAuth");
            }

            string accessToken = Session["AccessToken"] as string;
            string realmId = Session["RealmId"] as string;
            string refreshToken = Session["RefreshToken"] as string;

            try
            {
                var serviceContext = QuickBooksHelper.CreateServiceContext(accessToken, realmId);
                var dataService = new DataService(serviceContext);

                // Step 4: Retrieve company information
                CompanyInfo companyInfo = dataService.FindAll(new CompanyInfo()).FirstOrDefault();
                //new Payment


                JObject cardChargeResponse = await paymentsApiCall(accessToken, refreshToken, realmId);

                StringBuilder sb = new StringBuilder();
                foreach (var obj in cardChargeResponse)
                {
                    sb.Append(obj.Key);
                    sb.Append(": ");
                    sb.Append(obj.Value);
                    sb.Append(": ");
                }

                ViewBag.PaymentDetails = sb.ToString();

                return View("CompanyDetails", companyInfo);
            }
            catch (Exception ex)
            {
                return Content("An error occurred while fetching company details: " + ex.Message);
            }
        }


        public async Task<JObject> paymentsApiCall(string access_token, string refresh_token, string realmId)
        {
            try
            {
                if (realmId != "")
                {
                    //output("Making Payments API call.");

                    // Get card token
                    string cardToken = getCardToken();

                    // Charge card using card token
                    JObject cardChargeResponse = executePaymentsCharge(cardToken, realmId,
                       access_token, refresh_token);
                    return cardChargeResponse;
                    //output("Payments call successful.");
                    //lblPaymentsCall.Visible = true;
                    //lblPaymentsCall.Text = "Payments Call successful";

                }
            }
            catch (Exception ex)
            {
                //if (ex.Message == "UnAuthorized-401")
                //{
                //    //output("Invalid/Expired Access Token.");

                //    // If 401 token expiry, then perform token refresh
                //    //await performRefreshToken(refresh_token);
                //    //if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("accessToken"))
                //    //   && (dictionary.ContainsKey("realmId")))
                //    //{
                //    //    await paymentsApiCall(dictionary["accessToken"], dictionary["refreshToken"],
                //    //       dictionary["realmId"]);
                //    //}
                //}
                //else
                //{
                //    //output(ex.Message);
                //}
            }

            return null;
        }


        /// <summary>
        /// Get card token
        /// </summary>
        /// <returns>string</returns>

        public string getCardToken()
        {
            string cardToken = "";
            JObject jsonDecodedResponse;
            string cardTokenJson = "";
            // Payments sandbox URL used here. Change to Prod URL (https://api.intuit.com/)
            // if prod keys are used in the config.
            string paymentsBaseUrl = "https://sandbox.api.intuit.com/";
            string cardTokenEndpoint = "quickbooks/v4/payments/tokens";
            string uri = paymentsBaseUrl + cardTokenEndpoint;

            // Build the request
            string cardTokenRequestBody = "{\"card\":{\"expYear\":\"2025\",\"expMonth\":\"02\",\"address\":{\"region\":\"CA\",\"postalCode\":\"94086\",\"streetAddress\":\"1130 Kifer Rd\",\"country\":\"US\",\"city\":\"Sunnyvale\"},\"name\":\"emulate=0\",\"cvc\":\"123\",\"number\":\"4111111111111112\"}}";

            // Send the request (Token API call does not require Authorization header.
            // Other Payments API calls do)
            HttpWebRequest cardTokenRequest = (HttpWebRequest)WebRequest.Create(uri);
            cardTokenRequest.Method = "POST";
            cardTokenRequest.ContentType = "application/json";
            cardTokenRequest.Headers.Add("Request-Id", Guid.NewGuid().ToString());//assign guid

            byte[] _byteVersion = Encoding.ASCII.GetBytes(cardTokenRequestBody);
            cardTokenRequest.ContentLength = _byteVersion.Length;
            Stream stream = cardTokenRequest.GetRequestStream();
            stream.Write(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            // Get the response
            HttpWebResponse cardTokenResponse = (HttpWebResponse)cardTokenRequest.GetResponse();
            using (Stream data = cardTokenResponse.GetResponseStream())
            {
                // Return XML response
                cardTokenJson = new StreamReader(data).ReadToEnd();
                jsonDecodedResponse = JObject.Parse(cardTokenJson);
                if (!string.IsNullOrEmpty(jsonDecodedResponse.TryGetString("value")))
                {
                    cardToken = jsonDecodedResponse["value"].ToString();
                }
            }
            return cardToken;
        }

        /// <summary>
        /// Execute Charge on the card
        /// </summary>
        /// <param name="cardToken"></param>
        /// <param name="realmId"></param>
        /// <param name="access_token"></param>
        /// <param name="refresh_token"></param>
        /// <returns>JObject</returns>

        public JObject executePaymentsCharge(string cardToken, string realmId, string access_token, string refresh_token)
        {
            string cardChargeJson = "";
            JObject jsonDecodedResponse;
            // Payments sandbox URL used here. Change to Prod URL (https://api.intuit.com/)
            // if prod keys are used in the config.
            string paymentsBaseUrl = "https://sandbox.api.intuit.com";
            string cardChargeEndpoint = "/quickbooks/v4/payments/charges";
            string uri = paymentsBaseUrl + cardChargeEndpoint;

            // Build the request
            //string cardChargeRequestBody = "{\"amount\": \"11.55\",\"token\":\"" + cardToken + "\", \"currency\": \"USD\"}";

            string cardChargeRequestBody = "{\r\n  \"currency\": \"USD\", \r\n  \"amount\": \"10.55\", \r\n  \"context\": {\r\n    \"mobile\": \"false\", \r\n    \"isEcommerce\": \"true\"\r\n  }, \r\n  \"card\": {\r\n    \"name\": \"emulate=0\", \r\n    \"number\": \"4111111111111111\", \r\n    \"expMonth\": \"02\", \r\n    \"address\": {\r\n      \"postalCode\": \"94086\", \r\n      \"country\": \"US\", \r\n      \"region\": \"CA\", \r\n      \"streetAddress\": \"1130 Kifer Rd\", \r\n      \"city\": \"Sunnyvale\"\r\n    }, \r\n    \"expYear\": \"2025\", \r\n    \"cvc\": \"123\"\r\n  }\r\n}";



            // Send the request
            HttpWebRequest cardChargeRequest = (HttpWebRequest)WebRequest.Create(uri);
            cardChargeRequest.Method = "POST";
            cardChargeRequest.Headers.Add(string.Format("Authorization: Bearer {0}", access_token));
            cardChargeRequest.ContentType = "application/json";
            cardChargeRequest.Accept = "application/json";
            // Assign unique guid everytime
            cardChargeRequest.Headers.Add("Request-Id", Guid.NewGuid().ToString());

            byte[] _byteVersion = Encoding.ASCII.GetBytes(cardChargeRequestBody);
            cardChargeRequest.ContentLength = _byteVersion.Length;
            Stream stream = cardChargeRequest.GetRequestStream();
            stream.Write(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            // Get the response
            HttpWebResponse cardChargeResponse = (HttpWebResponse)cardChargeRequest.GetResponse();
            using (Stream data = cardChargeResponse.GetResponseStream())
            {
                // Return the XML response
                cardChargeJson = new StreamReader(data).ReadToEnd();
                jsonDecodedResponse = JObject.Parse(cardChargeJson);
            }
            return jsonDecodedResponse;
        }

    }
}