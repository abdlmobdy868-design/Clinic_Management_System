using ClinicManagementSystem.Models;
using ClinicManagementSystem.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index() => View(_userManager.Users.ToList());

        // Create User
        public IActionResult Create() => View(new RegisterVM());

        [HttpPost]
        public async Task<IActionResult> Create(RegisterVM vm, string RoleName)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = vm.UserName, Email = vm.Email, FullName = vm.FullName, Address = vm.Address, UserType = RoleName };
                var result = await _userManager.CreateAsync(user, vm.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(RoleName))
                        await _userManager.AddToRoleAsync(user, RoleName);
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }
            return View(vm);
        }

        // Edit User
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(ApplicationUser user)
        {
            var userFromDb = await _userManager.FindByIdAsync(user.Id);
            userFromDb.FullName = user.FullName;
            userFromDb.Email = user.Email;
            userFromDb.UserName = user.UserName;
            userFromDb.Address = user.Address;
            await _userManager.UpdateAsync(userFromDb);
            return RedirectToAction(nameof(Index));
        }

        // Roles Management
        public async Task<IActionResult> ManageRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            ViewBag.UserId = id;
            ViewBag.UserName = user.UserName;
            ViewBag.Roles = _roleManager.Roles.ToList();
            var userRoles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRoles = userRoles;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ManageRoles(string userId, List<string> selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var oldRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRolesAsync(user, selectedRoles);
            return RedirectToAction(nameof(Index));
        }

        // Lock / Unlock
        public async Task<IActionResult> LockUnlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
                user.LockoutEnd = DateTime.Now;
            else
                user.LockoutEnd = DateTime.Now.AddYears(100);
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }
}