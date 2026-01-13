using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShopKeep.Models;

namespace ShopKeep.Controllers;

public class HomeController(ILogger<HomeController> logger, AppDbContext context) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;
    private readonly AppDbContext db = context;

    public IActionResult Index()
    {
        // Get statistics for dashboard
        ViewBag.TotalCategories = db.Categories.Count();
        ViewBag.TotalProducts = db.Products.Count(p => p.Status == (int)ProductStatus.Approved);
        ViewBag.TotalOrders = db.Orders.Count();
        ViewBag.TotalUsers = db.Users.Count();
        
        // Get recent categories
        var recentCategories = db.Categories
            .OrderByDescending(c => c.Id)
            .Take(4)
            .ToList();
        ViewBag.RecentCategories = recentCategories;
        
        // Get featured products
        var featuredProducts = db.Products
            .Where(p => p.Status == (int)ProductStatus.Approved)
            .OrderByDescending(p => p.Id)
            .Take(6)
            .ToList();
        ViewBag.FeaturedProducts = featuredProducts;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
