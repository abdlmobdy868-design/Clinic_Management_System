
using ClinicManagementSystem.Models;
using ClinicManagementSystem.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
//using ClinicManagementSystem.ViewModel.ClinicManagementSystem.ViewModels;

namespace ClinicManagementSystem.Controllers
{
    public class AccountController : Controller
    {
  
////////////////////////////////

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> u, SignInManager<ApplicationUser> s)
        { _userManager = u; _signInManager = s; }

        [HttpGet] public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
          
            if (!ModelState.IsValid) return View(vm);
            var user = new ApplicationUser
            {
                UserName = vm.UserName,
                FullName = vm.FullName,
                Email = vm.Email,
                Address = vm.Address,
                UserType = vm.UserType
            };
            var result = await _userManager.CreateAsync(user, vm.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(vm);
        }

        [HttpGet] public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
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
                {
                    ModelState.AddModelError("", "to many attemps please try again later");
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError("", "please confirme your email");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid user name or password");
                }
                return View(loginVM);

            }

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }

}

