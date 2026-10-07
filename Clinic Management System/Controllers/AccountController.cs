//using Microsoft.AspNetCore.Authorization;
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
            var user = new ApplicationUser { UserName = vm.UserName, Email = vm.Email };
            var result = await _userManager.CreateAsync(user, vm.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(vm);
        }

        [HttpGet] public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user != null)
            {
                var r = await _signInManager.PasswordSignInAsync(user, vm.Password, vm.RememberMe, false);
                if (r.Succeeded) return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Invalid email or password");
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }

}

