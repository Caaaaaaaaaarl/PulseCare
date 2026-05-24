using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace PulseCare.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Auto-redirect to the correct page if they are already logged in
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Doctor") return RedirectToAction("DoctorDashboard");
            if (role == "Patient") return RedirectToAction("PatientDashboard");

            return RedirectToAction("Login", "Account");
        }

        public IActionResult PatientDashboard()
        {
            // Block access if not a Patient
            if (HttpContext.Session.GetString("UserRole") != "Patient") return RedirectToAction("Login", "Account");

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        public IActionResult DoctorDashboard()
        {
            // Block access if not a Doctor
            if (HttpContext.Session.GetString("UserRole") != "Doctor") return RedirectToAction("Login", "Account");

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }
    }
}