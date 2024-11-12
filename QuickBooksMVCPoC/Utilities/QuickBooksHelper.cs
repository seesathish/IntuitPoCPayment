using Intuit.Ipp.Core;
using Intuit.Ipp.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace QuickBooksMVCPoC.Utilities
{
    public static class QuickBooksHelper
    {
        public static ServiceContext CreateServiceContext(string accessToken, string realmId)
        {
            var oauthValidator = new OAuth2RequestValidator(accessToken);
            var serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
            serviceContext.IppConfiguration.BaseUrl.Qbo = ConfigurationManager.AppSettings["Environment"] == "sandbox"
                ? "https://sandbox-quickbooks.api.intuit.com/"
                : "https://quickbooks.api.intuit.com/";
            serviceContext.IppConfiguration.MinorVersion.Qbo = "65";
            return serviceContext;
        }
    }
}