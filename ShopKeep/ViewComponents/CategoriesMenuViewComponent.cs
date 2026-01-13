using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopKeep.Models;

namespace ShopKeep.ViewComponents;

public record CategoriesMenuItemVM(int Id, string Name, bool IsSelected);

public class CategoriesMenuViewComponent(AppDbContext db) : ViewComponent
{
    private readonly AppDbContext _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        int? selectedCategoryId = null;
        if (HttpContext?.Request?.Query.TryGetValue("categoryId", out var raw) == true
            && int.TryParse(raw.ToString(), out var parsed))
        {
            selectedCategoryId = parsed;
        }

        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoriesMenuItemVM(
                c.Id,
                c.Name,
                selectedCategoryId.HasValue && c.Id == selectedCategoryId.Value))
            .ToListAsync();

        return View(categories);
    }
}
