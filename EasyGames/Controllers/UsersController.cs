// Controllers/UsersController.cs
//  userscontroller admin crud for accounts
//  secured with authorize roles admin so only admins can reach it
//  index shows all users
//  create lets an admin create a new user account with optional admin role
//  edit lets admins update user profile fields like email fullname dob
//  toggleadmin switches a user between admin and non admin but guards against removing the last admin
//  delete removes a user but blocks self deletion and prevents deleting the last admin
//  uses usermanager and rolemanager from aspnet core identity to enforce all rules
using System;
using System.Linq;
using System.Threading.Tasks;
using EasyGames.Models;               
using EasyGames.Models.Admin;
using EasyGames.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EasyGames.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private const string AdminRole = "Admin";
        private const string ProprietorRole = "Proprietor";

        public UsersController(UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Users
        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        public IActionResult Create() => View(new CreateUserVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            if (vm.IsAdmin)
            {
                if (!await _roleManager.RoleExistsAsync(AdminRole))
                    await _roleManager.CreateAsync(new IdentityRole(AdminRole));
                await _userManager.AddToRoleAsync(user, AdminRole);
            }

            TempData["Msg"] = "User created.";
            return RedirectToAction(nameof(Index));
        }

        //  Update the user info

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var vm = new EditUserVm
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                DateOfBirth = user.DateOfBirth
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user is null) return NotFound();

            user.Email = vm.Email;
            user.UserName = vm.Email; 
            user.FullName = vm.FullName;
            user.DateOfBirth = vm.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            TempData["Msg"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }

        //  ROLE TOGGLE FOR THE ADMIN

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(AdminRole))
                await _roleManager.CreateAsync(new IdentityRole(AdminRole));

            var isAdmin = await _userManager.IsInRoleAsync(user, AdminRole);

            if (isAdmin && await IsLastAdminAsync(user))
            {
                TempData["Msg"] = "Cannot remove Admin role from the last Admin.";
                return RedirectToAction(nameof(Index));
            }

            var result = isAdmin
                ? await _userManager.RemoveFromRoleAsync(user, AdminRole)
                : await _userManager.AddToRoleAsync(user, AdminRole);

            TempData["Msg"] = result.Succeeded ? "Role updated." :
                string.Join("; ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        //  Proprietor role is now automatically managed based on shop ownership in ShopsController

        //  DELETE

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Msg"] = "Invalid user id.";
                return RedirectToAction(nameof(Index));
            }

            if (id == currentUserId)
            {
                TempData["Msg"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            //  prevent last admin gettin' murked
            if (await _userManager.IsInRoleAsync(user, AdminRole) && await IsLastAdminAsync(user))
            {
                TempData["Msg"] = "Cannot delete the last Admin account.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            TempData["Msg"] = result.Succeeded ? "User deleted." :
                string.Join("; ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        //  check if last admin
        private async Task<bool> IsLastAdminAsync(ApplicationUser target)
        {
            var admins = await _userManager.GetUsersInRoleAsync(AdminRole);
            return admins.Count == 1 && admins[0].Id == target.Id;
        }
    }
}
