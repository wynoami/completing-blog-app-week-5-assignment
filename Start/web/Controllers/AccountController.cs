using AtlasBlog.Data;
using AtlasBlog.Models.EF;
using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AtlasBlog.Controllers
{
    /// <summary>
    /// Handles user accounts: registering, logging in, logging out,
    /// and the page shown when a user lacks permission.
    /// </summary>
    public class AccountController : Controller
    {
        // The only account types allowed on the Register form.
        // Anything else (e.g., a tampered form submission) is rejected.
        private static readonly string[] AllowedAccountTypes = { "User", "Author", "Moderator", "Admin" };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _cache = cache;
        }

        /// <summary>Shows the login form.</summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Checks the email and password. On success, sends the user back to the
        /// page they were trying to reach (if it's on this site) or to Home.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // lockoutOnFailure: true locks the account after repeated wrong passwords
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Only follow returnUrl if it points to this site (prevents open redirects)
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "This account is locked after too many failed attempts. Please try again in a few minutes.");
                return View(model);
            }

            // Same message whether the email or the password was wrong,
            // so attackers can't find out which emails have accounts.
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        /// <summary>Shows the registration form with "User" pre-selected.</summary>
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        /// <summary>
        /// Creates a new account, adds the claim for the chosen account type,
        /// creates a starter blog for authors, then signs the user in.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!AllowedAccountTypes.Contains(model.AccountType))
            {
                ModelState.AddModelError(string.Empty, "Please choose a valid account type.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // All-or-nothing: if anything below fails, none of it is saved
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                // e.g., "Passwords must have at least one uppercase ('A'-'Z')."
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            switch (model.AccountType)
            {
                case "Author":
                    await _userManager.AddClaimAsync(user, new Claim("author", "true"));
                    _context.Blogs.Add(new Blog
                    {
                        Title = $"{model.Email}'s Blog",
                        Description = "Welcome to my new blog!",
                        CreatedAt = DateTime.UtcNow,
                        LastModifiedAt = DateTime.UtcNow,
                        AuthorId = user.Id
                    });
                    await _context.SaveChangesAsync();
                    break;

                case "Moderator":
                    await _userManager.AddClaimAsync(user, new Claim("moderator", "true"));
                    break;

                case "Admin":
                    await _userManager.AddClaimAsync(user, new Claim("admin", "true"));
                    break;

                // "User" is a regular reader and gets no extra claim
            }

            await transaction.CommitAsync();

            if (model.AccountType == "Author")
            {
                // A new blog exists, so the cached home page list is out of date
                _cache.Remove(CacheKeys.BlogList);
            }
            
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["Message"] = "Welcome! Your account has been created.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>Signs the user out and returns to Home.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Message"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Shown when a logged-in user tries to open a page their account
        /// can't access (e.g., a reader opening /Post/Create).
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}