using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using ClinicManagementSystem.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        // Roles a visitor may pick on the public Register form.
        // Admin / SuperAdmin are created only from the Admin area or the seeder.
        private static readonly string[] SelfRegisterRoles = { CD.DOCTOR_ROLE, CD.PATIENT_ROLE };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> u, SignInManager<ApplicationUser> s, ApplicationDbContext context)
        { _userManager = u; _signInManager = s; _context = context; }

        [HttpGet]
        public IActionResult Register() => View(new RegisterVM());

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // UserType comes from the browser, so never trust it
            if (!SelfRegisterRoles.Contains(vm.UserType ?? string.Empty))
            {
                ModelState.AddModelError(nameof(vm.UserType), "Invalid user type");
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.UserName,
                FullName = vm.FullName ?? string.Empty,
                Email = vm.Email,
                Address = vm.Address,
                UserType = vm.UserType,
                // No email-confirmation flow yet, and RequireConfirmedAccount is on:
                // without this the user could register but never log in again.
                // Remove this line once confirmation emails work.
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, vm.UserType!);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user); // don't leave a user without a role
                foreach (var e in roleResult.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // The old code built a Patient object but never saved it. Save it.
            if (vm.UserType == CD.PATIENT_ROLE)
            {
                _context.Patients.Add(new Patient
                {
                    ApplicationUserId = user.Id,
                    Name = user.FullName,
                    Address = user.Address,
                    Phone = string.Empty,
                    Age = 0,
                    Gender = Gender.male
                });
                await _context.SaveChangesAsync();
            }
            // A Doctor row is NOT created here: the Admin adds doctors (with department),
            // otherwise anyone could list themselves as a doctor on the public page.

            await _signInManager.SignInAsync(user, false);
            return await RedirectByRole(user);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(loginVM);

            var user = await _userManager.FindByEmailAsync(loginVM.UserNameOrEmail) ??
                await _userManager.FindByNameAsync(loginVM.UserNameOrEmail);

            if (user is null)
            {
                ModelState.AddModelError("", "Invalid user name or password");
                return View(loginVM);
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    ModelState.AddModelError("", "Too many attempts, please try again later");
                else if (result.IsNotAllowed)
                    ModelState.AddModelError("", "Please confirm your email first");
                else
                    ModelState.AddModelError("", "Invalid user name or password");
                return View(loginVM);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return await RedirectByRole(user);
        }

        // Identity sends users here when they open a page their role can't access
        [HttpGet]
        public async Task<IActionResult> AccessDenied()
        {
            var user = await _userManager.GetUserAsync(User);
            return user is null ? RedirectToAction(nameof(Login)) : await RedirectByRole(user);
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // Admin / SuperAdmin -> Admin dashboard, everyone else -> Customer area
        private async Task<IActionResult> RedirectByRole(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, CD.SUPER_ADMIN_ROLE) ||
                await _userManager.IsInRoleAsync(user, CD.ADMIN_ROLE))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
    }
}
