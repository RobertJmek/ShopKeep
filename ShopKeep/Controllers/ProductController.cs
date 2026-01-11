using ShopKeep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ShopKeep.Controllers
{
    public class ProductController(AppDbContext context) : Controller
    {
        private readonly AppDbContext db = context;

        public ActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }

            var products = db.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .ToList();
            
            ViewBag.Products = products;
            return View();
        }

        public ActionResult Show(int id)
        {
            Product? product = db.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews!)
                    .ThenInclude(r => r.User)
                .FirstOrDefault(p => p.Id == id);
            
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [Authorize(Roles = "Admin,Editor")]
        public ActionResult New()
        {
            ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name");
            return View();
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public ActionResult New(Product product)
        {
            try
            {
                db.Products.Add(product);
                db.SaveChanges();
                TempData["message"] = "Produsul a fost adăugat";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name");
                return View(product);
            }
        }

        [Authorize(Roles = "Admin,Editor")]
        public ActionResult Edit(int id)
        {
            Product? product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public ActionResult Edit(int id, Product requestProduct)
        {
            Product? product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                product.Title = requestProduct.Title;
                product.Description = requestProduct.Description;
                product.ImageUrl = requestProduct.ImageUrl;
                product.Price = requestProduct.Price;
                product.Stock = requestProduct.Stock;
                product.CategoryId = requestProduct.CategoryId;
                
                db.SaveChanges();
                TempData["message"] = "Produsul a fost modificat!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
                return View(requestProduct);
            }
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Product? product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            db.Products.Remove(product);
            db.SaveChanges();
            TempData["message"] = "Produsul a fost șters";
            return RedirectToAction("Index");
        }
    }
}