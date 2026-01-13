using ShopKeep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ShopKeep.Controllers
{
    public class CategoryController(AppDbContext context) : Controller
    {
        private readonly AppDbContext db = context;

        public ActionResult Index()
        {
            try
            {
                if (TempData.ContainsKey("message"))
                {
                    ViewBag.message = TempData["message"].ToString();
                }

                var canSeeAllProducts = User?.Identity?.IsAuthenticated == true
                    && (User.IsInRole("Admin") || User.IsInRole("Editor"));

                IQueryable<Category> query = db.Categories.AsNoTracking();
                if (canSeeAllProducts)
                {
                    query = query.Include(c => c.Products);
                }
                else
                {
                    query = query.Include(c => c.Products!.Where(p => p.Status == (int)ProductStatus.Approved));
                }

                var categories = query
                    .OrderBy(c => c.Name)
                    .ToList();

                ViewBag.Categories = categories;
                return View();
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la încărcarea categoriilor";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Show(int id)
        {
            try
            {
                var canSeeAllProducts = User?.Identity?.IsAuthenticated == true
                    && (User.IsInRole("Admin") || User.IsInRole("Editor"));

                IQueryable<Category> query = db.Categories.AsNoTracking();
                if (canSeeAllProducts)
                {
                    query = query.Include(c => c.Products);
                }
                else
                {
                    query = query.Include(c => c.Products!.Where(p => p.Status == (int)ProductStatus.Approved));
                }

                Category? category = query.FirstOrDefault(c => c.Id == id);
                if (category == null)
                {
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la încărcarea categoriei";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult New()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult New(Category cat)
        {
            if (!ModelState.IsValid)
            {
                return View(cat);
            }

            try
            {
                var name = cat.Name!.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    ModelState.AddModelError("Name", "Numele categoriei este obligatoriu");
                    return View(cat);
                }

                var nameLower = name.ToLower();
                var exists = db.Categories.Any(c => c.Name.ToLower() == nameLower);
                if (exists)
                {
                    ModelState.AddModelError("Name", "Categorie deja existentă");
                    return View(cat);
                }

                cat.Name = name;

                db.Categories.Add(cat);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost adaugata";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("Name", "Categorie deja existentă");
                return View(cat);
            }
            catch (Exception)
            {
                return View(cat);
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            try
            {
                Category? category = db.Categories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .FirstOrDefault(c => c.Id == id);
                if (category == null)
                {
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la încărcarea categoriei";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Edit(int id, Category requestCategory)
        {
            Category? category = db.Categories.Find(id);
            if (category == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(requestCategory);
            }

            try
            {
                var name = requestCategory.Name!.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    ModelState.AddModelError("Name", "Numele categoriei este obligatoriu");
                    return View(requestCategory);
                }

                var nameLower = name.ToLower();
                var exists = db.Categories.Any(c => c.Id != id && c.Name.ToLower() == nameLower);
                if (exists)
                {
                    ModelState.AddModelError("Name", "Categorie deja existentă");
                    return View(requestCategory);
                }

                category.Name = name;
                db.SaveChanges();
                TempData["message"] = "Categoria a fost modificata!";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("Name", "Categorie deja existentă");
                return View(requestCategory);
            }
            catch (Exception)
            {
                return View(requestCategory);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                Category? category = db.Categories.Find(id);
                if (category == null)
                {
                    return NotFound();
                }
                db.Categories.Remove(category);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost stearsa";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["message"] = "A apărut o eroare la ștergerea categoriei";
                return RedirectToAction("Index");
            }
        }
    }
}