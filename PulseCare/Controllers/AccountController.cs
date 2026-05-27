using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PulseCare.Data;
using System.Linq;

namespace PulseCare.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.FullName);

                if (user.Role == "Doctor")
                {
                    return RedirectToAction("DoctorDashboard", "Home");
                }
                // 🔑 Route safely onto AdminController namespace
                else if (user.Role == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else
                {
                    return RedirectToAction("PatientDashboard", "Home");
                }
            }

            ViewBag.Error = "Invalid email or password";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}