using flight.Models;
using flight.Services;
using flight.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace flight.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ReCaptchaService reCaptchaService;
        private readonly LoginAttemptTracker loginAttemptTracker;
        private readonly ILogger<AccountController> logger;

        public AccountController(
            SignInManager<Users> signInManager,
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager,
            ReCaptchaService reCaptchaService,
            LoginAttemptTracker loginAttemptTracker,
            ILogger<AccountController> logger)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.reCaptchaService = reCaptchaService;
            this.loginAttemptTracker = loginAttemptTracker;
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get the client's IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Check if the IP is blocked
            if (loginAttemptTracker.IsIPBlocked(ipAddress))
            {
                ModelState.AddModelError("", "Too many failed attempts. Please try again later.");
                return View(model);
            }

            // Verify reCAPTCHA
            var isReCaptchaValid = await reCaptchaService.VerifyReCaptcha(model.RecaptchaResponse);
            if (!isReCaptchaValid)
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            // Check if user exists before attempting to sign in
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null && await userManager.IsLockedOutAsync(user))
            {
                // Calculate remaining lockout time
                var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
                var remainingTime = lockoutEnd.HasValue ?
                    lockoutEnd.Value.Subtract(DateTimeOffset.UtcNow) :
                    TimeSpan.Zero;

                if (remainingTime > TimeSpan.Zero)
                {
                    ModelState.AddModelError("", $"Your account is locked. Please try again in {(int)remainingTime.TotalMinutes} minutes.");
                    return View(model);
                }
            }

            // Introduce a delay for repeated failed attempts
            var delaySeconds = loginAttemptTracker.GetDelaySeconds(ipAddress);
            if (delaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }

            // Changed from false to true to enable lockout
            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, true);

            if (result.Succeeded)
            {
                // Reset lockout count on successful login
                if (user != null)
                {
                    await userManager.ResetAccessFailedCountAsync(user);
                    loginAttemptTracker.ResetAttempts(ipAddress); // Reset IP attempt tracker

                    if (await userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Dashboard", "Home");
                    }
                    else if (await userManager.IsInRoleAsync(user, "User"))
                    {
                        return RedirectToAction("User", "Home");
                    }
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked due to too many failed attempts. Please try again later.");
            }
            else
            {
                // Record failed attempt
                loginAttemptTracker.RecordAttempt(ipAddress);
                ModelState.AddModelError("", "Invalid Login Attempt");
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify reCAPTCHA
            var isReCaptchaValid = await reCaptchaService.VerifyReCaptcha(model.RecaptchaResponse);
            if (!isReCaptchaValid)
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
            };
            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var roleExists = await roleManager.RoleExistsAsync("User");
                if (!roleExists)
                {
                    var role = new IdentityRole("User");
                    await roleManager.CreateAsync(role);
                }
                await userManager.AddToRoleAsync(user, "User");

                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Login", "Account");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify reCAPTCHA
            var isReCaptchaValid = await reCaptchaService.VerifyReCaptcha(model.RecaptchaResponse);
            if (!isReCaptchaValid)
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }
            else
            {
                return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
            }
        }

        [HttpGet]
        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(model);
            }

            // Verify reCAPTCHA
            var isReCaptchaValid = await reCaptchaService.VerifyReCaptcha(model.RecaptchaResponse);
            if (!isReCaptchaValid)
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }

            var result = await userManager.RemovePasswordAsync(user);
            if (result.Succeeded)
            {
                result = await userManager.AddPasswordAsync(user, model.NewPassword);
                return RedirectToAction("Login", "Account");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Landingpage", "Home");
        }
    }
}