using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShopKeep.Models;
using System.Security.Claims;

namespace ShopKeep.Controllers
{
    [Authorize]
    public class WishlistController(AppDbContext context) : Controller
    {
        private readonly AppDbContext db = context;

        // Vezi lista de dorințe
        public ActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"]?.ToString();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var wishlistItems = db.WishlistItems
                .Include(w => w.Product)
                    .ThenInclude(p => p.Category)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToList();

            ViewBag.WishlistItems = wishlistItems;
            
            return View();
        }

        // Adaugă produs la favorite
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Add(int productId)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Show", "Product", new { id = productId });
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Verifică dacă produsul există
            var product = db.Products.Find(productId);
            if (product == null)
            {
                TempData["message"] = "Produsul nu a fost găsit";
                return RedirectToAction("Index", "Product");
            }

            // Verifică dacă produsul e deja la favorite
            var existingItem = db.WishlistItems
                .FirstOrDefault(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
            {
                TempData["message"] = "Produsul este deja la favorite";
                return RedirectToAction("Index");
            }

            var wishlistItem = new WishlistItem
            {
                UserId = userId!,
                ProductId = productId,
                AddedAt = DateTime.Now
            };

            db.WishlistItems.Add(wishlistItem);
            db.SaveChanges();
            TempData["message"] = "Produs adăugat la favorite";
            
            return RedirectToAction("Index");
        }

        // Șterge produs din favorite
        [HttpPost]
        public ActionResult Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishlistItem = db.WishlistItems
                .FirstOrDefault(w => w.Id == id && w.UserId == userId);

            if (wishlistItem == null)
            {
                return NotFound();
            }

            db.WishlistItems.Remove(wishlistItem);
            db.SaveChanges();
            TempData["message"] = "Produs eliminat din favorite";
            
            return RedirectToAction("Index");
        }

        // Mută toate favoritele în coș
        [HttpPost]
        public ActionResult MoveAllToCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var wishlistItems = db.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .ToList();

            if (!wishlistItems.Any())
            {
                TempData["message"] = "Lista de favorite este goală";
                return RedirectToAction("Index");
            }

            int addedCount = 0;
            foreach (var item in wishlistItems)
            {
                // Verifică stocul
                if (item.Product.Stock > 0)
                {
                    // Verifică dacă e deja în coș
                    var existingCartItem = db.ShoppingCartItems
                        .FirstOrDefault(c => c.UserId == userId && c.ProductId == item.ProductId);

                    if (existingCartItem != null)
                    {
                        existingCartItem.Quantity += 1;
                    }
                    else
                    {
                        var cartItem = new ShoppingCartItem
                        {
                            UserId = userId!,
                            ProductId = item.ProductId,
                            Quantity = 1
                        };
                        db.ShoppingCartItems.Add(cartItem);
                    }
                    addedCount++;
                }
            }

            // Șterge toate din favorite
            db.WishlistItems.RemoveRange(wishlistItems);
            db.SaveChanges();

            TempData["message"] = $"{addedCount} produse au fost adăugate în coș";
            return RedirectToAction("Index", "ShoppingCart");
        }

        // Golește lista de favorite
        [HttpPost]
        public ActionResult Clear()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishlistItems = db.WishlistItems.Where(w => w.UserId == userId);
            
            db.WishlistItems.RemoveRange(wishlistItems);
            db.SaveChanges();
            TempData["message"] = "Lista de favorite a fost golită";
            
            return RedirectToAction("Index");
        }
    }
}
