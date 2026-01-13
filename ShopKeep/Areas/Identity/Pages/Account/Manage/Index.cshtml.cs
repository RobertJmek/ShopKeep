using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopKeep.Models;

namespace ShopKeep.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;

    public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
    }

    public string Username { get; private set; } = "";

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Nume complet")]
        public string FullName { get; set; } = "";

        [Phone]
        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [StringLength(300)]
        [Display(Name = "Adresă")]
        public string? Address { get; set; }

        [Phone]
        [Display(Name = "Telefon alternativ")]
        public string? AlternatePhoneNumber { get; set; }
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        Username = await userManager.GetUserNameAsync(user) ?? "";

        Input = new InputModel
        {
            FullName = user.FullName,
            PhoneNumber = await userManager.GetPhoneNumberAsync(user),
            Address = user.Address,
            AlternatePhoneNumber = user.AlternatePhoneNumber
        };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Nu s-a putut încărca utilizatorul curent.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Nu s-a putut încărca utilizatorul curent.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var newFullName = (Input.FullName ?? "").Trim();
        var newAddress = string.IsNullOrWhiteSpace(Input.Address) ? null : Input.Address.Trim();
        var newAlternatePhone = string.IsNullOrWhiteSpace(Input.AlternatePhoneNumber) ? null : Input.AlternatePhoneNumber.Trim();

        var hasUserUpdates = false;

        if (!string.Equals(user.FullName, newFullName, StringComparison.Ordinal))
        {
            user.FullName = newFullName;
            hasUserUpdates = true;
        }

        if (!string.Equals(user.Address, newAddress, StringComparison.Ordinal))
        {
            user.Address = newAddress;
            hasUserUpdates = true;
        }

        if (!string.Equals(user.AlternatePhoneNumber, newAlternatePhone, StringComparison.Ordinal))
        {
            user.AlternatePhoneNumber = newAlternatePhone;
            hasUserUpdates = true;
        }

        if (hasUserUpdates)
        {
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "Eroare: nu s-a putut actualiza profilul.";
                return RedirectToPage();
            }
        }

        var phoneNumber = await userManager.GetPhoneNumberAsync(user);
        if (!string.Equals(phoneNumber, Input.PhoneNumber, StringComparison.Ordinal))
        {
            var setPhoneResult = await userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            if (!setPhoneResult.Succeeded)
            {
                StatusMessage = "Eroare: nu s-a putut actualiza numărul de telefon.";
                return RedirectToPage();
            }
        }

        await signInManager.RefreshSignInAsync(user);
        StatusMessage = "Profilul a fost actualizat.";
        return RedirectToPage();
    }
}
