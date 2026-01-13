using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShopKeep.Models;
using System.Security.Claims;

namespace ShopKeep.Controllers
{
    [Authorize]
    public class OrderController(AppDbContext context) : Controller
    {
        private readonly AppDbContext db = context;

        // Lista comenzilor - Admin vede toate, userii doar ale lor
        public ActionResult Index(string? search)
        {
            try
            {
                if (TempData.ContainsKey("message"))
                {
                    ViewBag.message = TempData["message"].ToString();
                }

                IQueryable<Order> orders;

                if (User.IsInRole("Admin"))
                {
                    // Admin vede toate comenzile
                    orders = db.Orders
                        .Include(o => o.User)
                        .Include(o => o.OrderItems)
                        .OrderByDescending(o => o.OrderDate);
                }
                else
                {
                    // Userii văd doar comenzile lor
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    orders = db.Orders
                        .Include(o => o.OrderItems)
                        .Where(o => o.UserId == userId)
                        .OrderByDescending(o => o.OrderDate);
                }

                // Filtrare după search (doar pentru Admin)
                if (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    orders = orders.Where(o => 
                        o.Id.ToString().Contains(search) ||
                        (o.User != null && (
                            (o.User.Email != null && o.User.Email.ToLower().Contains(searchLower)) ||
                            (o.User.UserName != null && o.User.UserName.ToLower().Contains(searchLower)) ||
                            (o.User.FullName != null && o.User.FullName.ToLower().Contains(searchLower))
                        ))
                    );
                    ViewBag.Search = search;
                }

                ViewBag.Orders = orders.ToList();
                return View();
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la încărcarea comenzilor";
                return RedirectToAction("Index", "Home");
            }
        }

        // Detalii comandă - Admin vede orice, userii doar ale lor
        public ActionResult Show(int id)
        {
            try
            {
                Order? order = db.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product!)
                        .ThenInclude(p => p.Category)
                    .FirstOrDefault(o => o.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                // Verifică dacă userul are dreptul să vadă comanda
                if (!User.IsInRole("Admin"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (order.UserId != userId)
                    {
                        TempData["message"] = "Nu aveți permisiunea să vedeți această comandă";
                        return RedirectToAction("Index");
                    }
                }

                return View(order);
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la încărcarea detaliilor comenzii";
                return RedirectToAction("Index");
            }
        }

        // Doar Admin poate schimba statusul comenzii
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult UpdateStatus(int id, string status)
        {
            try
            {
                Order? order = db.Orders.Find(id);
                if (order == null)
                {
                    return NotFound();
                }

                order.Status = status;
                db.SaveChanges();
                TempData["message"] = "Statusul comenzii a fost actualizat";
                return RedirectToAction("Show", new { id = id });
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la actualizarea statusului";
                return RedirectToAction("Show", new { id = id });
            }
        }

        // Doar Admin poate șterge comenzi
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                Order? order = db.Orders.Find(id);
                if (order == null)
                {
                    return NotFound();
                }

                db.Orders.Remove(order);
                db.SaveChanges();
                TempData["message"] = "Comanda a fost ștearsă";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la ștergerea comenzii";
                return RedirectToAction("Index");
            }
        }

        // Userul poate anula doar comanda lui, cât timp e "Plasată"
        [HttpPost]
        public ActionResult Cancel(int id)
        {
            try
            {
                var order = db.Orders
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefault(o => o.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                if (!User.IsInRole("Admin"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (order.UserId != userId)
                    {
                        return Forbid();
                    }
                }

                if (order.Status != "Plasată")
                {
                    TempData["message"] = "Comanda nu mai poate fi anulată";
                    return RedirectToAction("Show", new { id });
                }

                // Restore stock
                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.Stock += item.Quantity;
                        }
                    }
                }

                order.Status = "Anulată";
                db.SaveChanges();

                TempData["message"] = "Comanda a fost anulată";
                return RedirectToAction("Show", new { id });
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la anularea comenzii";
                return RedirectToAction("Show", new { id });
            }
        }
    }
}