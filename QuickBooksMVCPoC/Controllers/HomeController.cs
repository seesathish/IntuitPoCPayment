using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuickBooksMVCPoC.Controllers
{
    public class HomeController : Controller
    {
        // Step 1.1: Display the Login Page
        public ActionResult Index()
        {
            return View();
        }

        // Step 1.2: Handle User Login
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                // Assuming any non-empty username/password combination is successful login
                Session["LoggedIn"] = true;
                return RedirectToAction("Authorize", "OAuth");
            }
            ViewBag.Error = "Invalid username or password.";
            return View("Index");
        }

        // Step 1.3: Handle User Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }
    }
}