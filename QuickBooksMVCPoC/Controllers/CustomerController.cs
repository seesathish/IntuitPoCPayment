using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.Security;
using QuickBooksMVCPoC.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace QuickBooksMVCPoC.Controllers
{
    public class CustomerController : Controller
    {
        // Step 3.1: Retrieve Customer Data from QuickBooks
        public async Task<ActionResult> Index()
        {
            if (Session["AccessToken"] == null || Session["RefreshToken"] == null || Session["RealmId"] == null)
            {
                return RedirectToAction("Authorize", "OAuth");
            }

            string accessToken = Session["AccessToken"] as string;
            string refreshToken = Session["RefreshToken"] as string;
            string realmId = Session["RealmId"] as string;

            try
            {
                // Step 1: Refresh the token if needed
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

                // Step 2: Create the ServiceContext with the updated access token
                var oauthValidator = new OAuth2RequestValidator(tokenResponse.AccessToken);
                var serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
                var dataService = new DataService(serviceContext);

                // Step 3: Retrieve customer data
                List<Customer> customers = dataService.FindAll(new Customer(), 1, 100).ToList();

                return View(customers);
            }
            catch (Exception ex)
            {
                return Content("An error occurred while fetching customers: " + ex.Message);
            }
        }
    }
}