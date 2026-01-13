using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShopKeep.Models;
using System.Security.Claims;

namespace ShopKeep.Controllers
{
    [Authorize]
    public class ShoppingCartController(AppDbContext context) : Controller
    {
        private readonly AppDbContext db = context;

        // Vezi coșul tău
        public ActionResult Index()
        {
            try
            {
                if (TempData.ContainsKey("message"))
                {
                    ViewBag.message = TempData["message"]?.ToString();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var savedAddress = db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Address)
                    .FirstOrDefault();
                ViewBag.SavedAddress = savedAddress;
                
                var cartItems = db.ShoppingCartItems
                    .Include(c => c.Product)
                        .ThenInclude(p => p.Category)
                    .Where(c => c.UserId == userId)
                    .ToList();

                ViewBag.CartItems = cartItems;
                ViewBag.TotalAmount = cartItems.Sum(c => c.Subtotal);
                
                return View();
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la încărcarea coșului";
                return RedirectToAction("Index", "Home");
            }
        }

        // Adaugă produs în coș
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Add(int productId, int quantity = 1)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated != true)
                {
                    var returnUrl = Url.Action("Show", "Product", new { id = productId });
                    return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (quantity <= 0)
                {
                    TempData["message"] = "Cantitatea trebuie să fie cel puțin 1";
                    return RedirectToAction("Show", "Product", new { id = productId });
                }
                
                // Verifică dacă produsul există
                var product = db.Products.Find(productId);
                if (product == null)
                {
                    TempData["message"] = "Produsul nu a fost găsit";
                    return RedirectToAction("Index", "Product");
                }

                // Verifică stocul
                if (product.Stock < quantity)
                {
                    TempData["message"] = "Stoc insuficient";
                    return RedirectToAction("Show", "Product", new { id = productId });
                }

                // Verifică dacă produsul e deja în coș
                var existingItem = db.ShoppingCartItems
                    .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

                if (existingItem != null)
                {
                    var newQuantity = existingItem.Quantity + quantity;
                    if (product.Stock < newQuantity)
                    {
                        TempData["message"] = "Stoc insuficient";
                        return RedirectToAction("Show", "Product", new { id = productId });
                    }

                    existingItem.Quantity = newQuantity;
                }
                else
                {
                    var cartItem = new ShoppingCartItem
                    {
                        UserId = userId!,
                        ProductId = productId,
                        Quantity = quantity
                    };
                    db.ShoppingCartItems.Add(cartItem);
                }

                db.SaveChanges();
                TempData["message"] = "Produs adăugat în coș";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la adăugarea produsului în coș";
                return RedirectToAction("Show", "Product", new { id = productId });
            }
        }

        // Actualizează cantitatea
        [HttpPost]
        public ActionResult UpdateQuantity(int id, int quantity)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cartItem = db.ShoppingCartItems
                    .Include(c => c.Product)
                    .FirstOrDefault(c => c.Id == id && c.UserId == userId);

                if (cartItem == null)
                {
                    return NotFound();
                }

                if (quantity <= 0)
                {
                    return RedirectToAction("Remove", new { id = id });
                }

                if (cartItem.Product == null)
                {
                    return NotFound();
                }

                if (cartItem.Product.Stock < quantity)
                {
                    TempData["message"] = "Stoc insuficient";
                    return RedirectToAction("Index");
                }

                cartItem.Quantity = quantity;
                db.SaveChanges();
                
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la actualizarea cantității";
                return RedirectToAction("Index");
            }
        }

        // Șterge produs din coș
        [HttpPost]
        public ActionResult Remove(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cartItem = db.ShoppingCartItems
                    .FirstOrDefault(c => c.Id == id && c.UserId == userId);

                if (cartItem == null)
                {
                    return NotFound();
                }

                db.ShoppingCartItems.Remove(cartItem);
                db.SaveChanges();
                TempData["message"] = "Produs eliminat din coș";
                
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la eliminarea produsului";
                return RedirectToAction("Index");
            }
        }

        // Golește coșul
        [HttpPost]
        public ActionResult Clear()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cartItems = db.ShoppingCartItems.Where(c => c.UserId == userId);
                
                db.ShoppingCartItems.RemoveRange(cartItems);
                db.SaveChanges();
                TempData["message"] = "Coșul a fost golit";
                
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la golirea coșului";
                return RedirectToAction("Index");
            }
        }

        // Finalizează comanda
        [HttpPost]
        public ActionResult Checkout(bool useSavedAddress, string? deliveryAddress)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                string? finalAddress;
                if (useSavedAddress)
                {
                    finalAddress = db.Users
                        .Where(u => u.Id == userId)
                        .Select(u => u.Address)
                        .FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(finalAddress))
                    {
                        TempData["message"] = "Nu ai o adresă salvată în cont. Te rog completează o adresă de livrare.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    finalAddress = deliveryAddress;
                }

                if (string.IsNullOrWhiteSpace(finalAddress))
                {
                    TempData["message"] = "Adresa de livrare este obligatorie";
                    return RedirectToAction("Index");
                }
                
                var cartItems = db.ShoppingCartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToList();

                if (!cartItems.Any())
                {
                    TempData["message"] = "Coșul este gol";
                    return RedirectToAction("Index");
                }

                // Verifică stocul pentru toate produsele
                foreach (var item in cartItems)
                {
                    if (item.Product == null)
                    {
                        TempData["message"] = "A apărut o problemă cu un produs din coș";
                        return RedirectToAction("Index");
                    }

                    if (item.Product.Stock < item.Quantity)
                    {
                        TempData["message"] = $"Stoc insuficient pentru {item.Product.Title}";
                        return RedirectToAction("Index");
                    }
                }

                // Creează comanda
                var order = new Order
                {
                    UserId = userId!,
                    OrderDate = DateTime.Now,
                    TotalAmount = cartItems.Sum(c => c.Subtotal),
                    Status = "Plasată",
                    DeliveryAddress = finalAddress.Trim()
                };

                db.Orders.Add(order);
                db.SaveChanges();

                // Adaugă produsele în OrderItems și actualizează stocul
                foreach (var item in cartItems)
                {
                    var orderItem = new OrderItems
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product!.Price,
                        ProductTitle = item.Product.Title,
                        ProductImageUrl = item.Product.ImageUrl
                    };
                    db.OrderItems.Add(orderItem);

                    // Scade stocul
                    item.Product.Stock -= item.Quantity;
                }

                // Golește coșul
                db.ShoppingCartItems.RemoveRange(cartItems);
                db.SaveChanges();

                TempData["message"] = "Comanda a fost plasată cu succes!";
                return RedirectToAction("Show", "Order", new { id = order.Id });
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la plasarea comenzii. Te rog încearcă din nou.";
                return RedirectToAction("Index");
            }
        }
    }
}
