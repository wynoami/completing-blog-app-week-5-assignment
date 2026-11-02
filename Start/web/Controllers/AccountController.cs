using AtlasBlog.Models.Views;
using Microsoft.AspNetCore.Mvc;

namespace AtlasBlog.Controllers
{
    public class AccountController : Controller
    {

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // TODO: Validate the model state
            // TODO: Attempt to sign in the user with the provided email and password
            // TODO: If login is successful, redirect to the Home page
            // TODO: If login fails, add an error message to ModelState and return the view with the model

            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // TODO: Validate the model state
            // TODO: Create a new user using the provided email and password
            // TODO: Add Claims to the user and a default blog if an author
            // TODO: If registration is successful, sign in the user and redirect to the Home page
            // TODO: If registration fails, add validation errors to ModelState and return the view with the model

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // TODO: Sign out the user
            // TODO: Redirect to the Home page after logout


            return RedirectToAction("Index", "Home");
        }
    }
}
