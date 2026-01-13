using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopKeep.Models;

namespace ShopKeep.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : Controller
{
    private const string SuperAdminEmail = "admin@shopkeep.com";

    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    public async Task<IActionResult> Index()
    {
        if (TempData.ContainsKey("message"))
        {
            ViewBag.message = TempData["message"]?.ToString();
        }

        var users = _userManager.Users.OrderBy(u => u.Email).ToList();
        var list = new List<AdminUserListItemVM>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            list.Add(new AdminUserListItemVM
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Roles = roles,
                LockoutEnd = user.LockoutEnd
            });
        }

        return View(list);
    }

    public async Task<IActionResult> EditRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var allRoles = _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToList();
        var selectedRoles = (await _userManager.GetRolesAsync(user)).ToList();

        var vm = new AdminEditUserRolesVM
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            AllRoles = allRoles,
            SelectedRoles = selectedRoles
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> EditRoles(AdminEditUserRolesVM vm)
    {
        var user = await _userManager.FindByIdAsync(vm.Id);
        if (user == null)
        {
            return NotFound();
        }

        var allRoles = _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToList();
        vm.AllRoles = allRoles;

        // Normalize null
        vm.SelectedRoles ??= new List<string>();

        var currentUserId = _userManager.GetUserId(User);
        var isEditingSelf = currentUserId == user.Id;

        // Everyone must be in the "User" role
        if (!await _roleManager.RoleExistsAsync("User"))
        {
            ModelState.AddModelError("SelectedRoles", "Rolul obligatoriu 'User' nu există în sistem");
            return View(vm);
        }

        var attemptedToRemoveUserRole = (await _userManager.IsInRoleAsync(user, "User")) && !vm.SelectedRoles.Contains("User");
        if (!vm.SelectedRoles.Contains("User"))
        {
            vm.SelectedRoles.Add("User");
        }

        // Validate roles
        foreach (var role in vm.SelectedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                ModelState.AddModelError("SelectedRoles", $"Rol invalid: {role}");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Super admin protection: nobody can remove Admin from admin@shopkeep.com
        if (!string.IsNullOrWhiteSpace(user.Email)
            && string.Equals(user.Email, SuperAdminEmail, StringComparison.OrdinalIgnoreCase)
            && currentRoles.Contains("Admin")
            && !vm.SelectedRoles.Contains("Admin"))
        {
            ModelState.AddModelError("SelectedRoles", $"Contul {SuperAdminEmail} este Admin suprem și nu poate pierde rolul de Admin");
            return View(vm);
        }

        // Security rules:
        // 1) An admin cannot remove their own Admin role
        if (isEditingSelf && currentRoles.Contains("Admin") && !vm.SelectedRoles.Contains("Admin"))
        {
            ModelState.AddModelError("SelectedRoles", "Nu îți poți scoate propriul rol de Admin");
            return View(vm);
        }

        var toRemove = currentRoles.Except(vm.SelectedRoles).ToList();
        var toAdd = vm.SelectedRoles.Except(currentRoles).ToList();

        if (toRemove.Any())
        {
            var result = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!result.Succeeded)
            {
                TempData["message"] = "Nu am putut actualiza rolurile (remove).";
                return RedirectToAction("Index");
            }
        }

        if (toAdd.Any())
        {
            var result = await _userManager.AddToRolesAsync(user, toAdd);
            if (!result.Succeeded)
            {
                TempData["message"] = "Nu am putut actualiza rolurile (add).";
                return RedirectToAction("Index");
            }
        }

        TempData["message"] = attemptedToRemoveUserRole
            ? "Rolurile au fost actualizate. Rolul 'User' este obligatoriu și nu poate fi eliminat."
            : "Rolurile au fost actualizate";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Don't lock yourself out
        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == user.Id)
        {
            TempData["message"] = "Nu îți poți bloca propriul cont";
            return RedirectToAction("Index");
        }

        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = isLocked ? null : DateTimeOffset.UtcNow.AddYears(100);

        var result = await _userManager.UpdateAsync(user);
        TempData["message"] = result.Succeeded
            ? (isLocked ? "Cont deblocat" : "Cont blocat")
            : "Nu am putut actualiza starea contului";

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == user.Id)
        {
            TempData["message"] = "Nu îți poți șterge propriul cont";
            return RedirectToAction("Index");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var isLastAdmin = admins.Count == 1 && admins[0].Id == user.Id;
            if (isLastAdmin)
            {
                TempData["message"] = "Nu poți șterge ultimul Admin din sistem";
                return RedirectToAction("Index");
            }
        }

        var result = await _userManager.DeleteAsync(user);
        TempData["message"] = result.Succeeded ? "Utilizator șters" : "Nu am putut șterge utilizatorul";
        return RedirectToAction("Index");
    }
}
