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
            try
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
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la încărcarea favoritelor";
                return RedirectToAction("Index", "Home");
            }
        }

        // Toggle produs la favorite (adaugă sau șterge)
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Add(int productId, string? returnUrl)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated != true)
                {
                    var loginReturnUrl = Url.Action("Show", "Product", new { id = productId });
                    return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl = loginReturnUrl });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Verifică dacă produsul există
                var product = db.Products.Find(productId);
                if (product == null)
                {
                    return RedirectToAction("Index", "Product");
                }

                // Verifică dacă produsul e deja la favorite
                var existingItem = db.WishlistItems
                    .FirstOrDefault(w => w.UserId == userId && w.ProductId == productId);

                bool isAdded;
                if (existingItem == null)
                {
                    // Adaugă la favorite
                    var wishlistItem = new WishlistItem
                    {
                        UserId = userId!,
                        ProductId = productId,
                        AddedAt = DateTime.Now
                    };

                    db.WishlistItems.Add(wishlistItem);
                    isAdded = true;
                }
                else
                {
                    // Șterge din favorite
                    db.WishlistItems.Remove(existingItem);
                    isAdded = false;
                }
                
                db.SaveChanges();
                
                // Pentru AJAX requests, returnează JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, isAdded = isAdded });
                }
                
                // Redirecționează înapoi la pagina de unde a venit
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                // Fallback: încearcă Referer header
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                {
                    if (refererUri.Host == Request.Host.Host)
                    {
                        return Redirect(referer);
                    }
                }
                
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // Pentru AJAX requests
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, error = "A apărut o eroare" });
                }
                
                TempData["message"] = "A apărut o eroare la gestionarea favoritelor";
                return RedirectToAction("Show", "Product", new { id = productId });
            }
        }

        // Șterge produs din favorite
        [HttpPost]
        public ActionResult Remove(int id)
        {
            try
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
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la eliminarea produsului";
                return RedirectToAction("Index");
            }
        }

        // Mută toate favoritele în coș
        [HttpPost]
        public ActionResult MoveAllToCart()
        {
            try
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
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la mutarea produselor în coș";
                return RedirectToAction("Index");
            }
        }

        // Golește lista de favorite
        [HttpPost]
        public ActionResult Clear()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var wishlistItems = db.WishlistItems.Where(w => w.UserId == userId);
                
                db.WishlistItems.RemoveRange(wishlistItems);
                db.SaveChanges();
                TempData["message"] = "Lista de favorite a fost golită";
                
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la golirea favoritelor";
                return RedirectToAction("Index");
            }
        }
    }
}
