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
        public ActionResult Index()
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

            ViewBag.Orders = orders.ToList();
            return View();
        }

        // Detalii comandă - Admin vede orice, userii doar ale lor
        public ActionResult Show(int id)
        {
            Order? order = db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Product)
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

        // Doar Admin poate schimba statusul comenzii
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult UpdateStatus(int id, string status)
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

        // Doar Admin poate șterge comenzi
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
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
    }
}