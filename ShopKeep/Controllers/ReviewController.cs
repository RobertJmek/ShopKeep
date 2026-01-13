using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShopKeep.Models;
using System.Security.Claims;

namespace ShopKeep.Controllers
{
    [Authorize]
    public class ReviewController(AppDbContext context) : Controller
    {
        private readonly AppDbContext db = context;

        // Adaugă review pentru un produs
        [HttpPost]
        public ActionResult Add(int productId, int? rating, string? text)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Verifică dacă produsul există
            var product = db.Products.Find(productId);
            if (product == null)
            {
                TempData["message"] = "Produsul nu a fost găsit";
                return RedirectToAction("Index", "Product");
            }

            // Nu permitem review-uri pentru produse nepublice
            if (product.Status != (int)ProductStatus.Approved)
            {
                TempData["message"] = "Nu poți adăuga review la un produs care nu este public";
                return RedirectToAction("Show", "Product", new { id = productId });
            }

            // Verifică dacă userul a mai lăsat review
            var existingReview = db.Reviews
                .FirstOrDefault(r => r.UserId == userId && r.ProductId == productId);

            if (existingReview != null)
            {
                TempData["message"] = "Ai lăsat deja un review pentru acest produs";
                return RedirectToAction("Show", "Product", new { id = productId });
            }

            var review = new Review
            {
                UserId = userId!,
                ProductId = productId,
                Rating = rating,
                Text = text,
                CreatedAt = DateTime.Now
            };

            db.Reviews.Add(review);
            db.SaveChanges();
            TempData["message"] = "Review-ul a fost adăugat cu succes";
            
            return RedirectToAction("Show", "Product", new { id = productId });
        }

        // Editează review-ul tău
        [HttpPost]
        public ActionResult Edit(int id, int? rating, string? text)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = db.Reviews.FirstOrDefault(r => r.Id == id && r.UserId == userId);

            if (review == null)
            {
                return NotFound();
            }

            var product = db.Products.Find(review.ProductId);
            if (product == null)
            {
                TempData["message"] = "Produsul nu a fost găsit";
                return RedirectToAction("Index", "Product");
            }

            if (product.Status != (int)ProductStatus.Approved)
            {
                TempData["message"] = "Nu poți modifica review-ul pentru un produs care nu este public";
                return RedirectToAction("Show", "Product", new { id = review.ProductId });
            }

            review.Rating = rating;
            review.Text = text;
            review.UpdatedAt = DateTime.Now;
            
            db.SaveChanges();
            TempData["message"] = "Review-ul a fost actualizat";
            
            return RedirectToAction("Show", "Product", new { id = review.ProductId });
        }

        // Șterge review-ul tău (sau Admin poate șterge orice review)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = db.Reviews.Find(id);

            if (review == null)
            {
                return NotFound();
            }

            // Verifică permisiuni - userul poate șterge doar review-ul său, sau Admin poate șterge orice
            if (review.UserId != userId && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți permisiunea să ștergeți acest review";
                return RedirectToAction("Show", "Product", new { id = review.ProductId });
            }

            var productId = review.ProductId;
            db.Reviews.Remove(review);
            db.SaveChanges();
            TempData["message"] = "Review-ul a fost șters";
            
            return RedirectToAction("Show", "Product", new { id = productId });
        }
    }
}
