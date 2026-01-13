using ShopKeep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ShopKeep.Controllers
{
    public class ProductController(AppDbContext context, IWebHostEnvironment env) : Controller
    {
        private readonly AppDbContext db = context;
        private readonly IWebHostEnvironment _env = env;

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private bool IsAdmin => User?.IsInRole("Admin") == true;
        private bool IsEditor => User?.IsInRole("Editor") == true;

        private bool CanSeeProduct(Product product)
        {
            if (product.Status == (int)ProductStatus.Approved)
            {
                return true;
            }

            if (IsAdmin)
            {
                return true;
            }

            // Editors can view non-approved products as well (for proposal tracking/moderation visibility).
            if (IsEditor)
            {
                return true;
            }

            return false;
        }

        private bool CanEditProduct(Product product)
        {
            if (IsAdmin)
            {
                return true;
            }

            if (!IsEditor)
            {
                return false;
            }

            // Editor can only manage their own products (regardless of approval status)
            if (product.ProposedByUserId == null || product.ProposedByUserId != CurrentUserId)
            {
                return false;
            }

            return true;
        }

        public ActionResult Index(int? categoryId, string? search, string? sortBy)
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }

            ViewBag.AllCategories = db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToList();

            IQueryable<Product> productsQuery = db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Reviews);

            if (IsAdmin)
            {
                // admin sees all
            }
            else if (IsEditor)
            {
                // editors can see all products (approved + pending/rejected)
            }
            else
            {
                // customers/guests only see approved products
                productsQuery = productsQuery.Where(p => p.Status == (int)ProductStatus.Approved);
            }

            // Search by product name (partial match)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                productsQuery = productsQuery.Where(p => p.Title.ToLower().Contains(searchTerm));
                ViewBag.SearchTerm = search;
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);

                var selectedCategory = db.Categories.FirstOrDefault(c => c.Id == categoryId.Value);
                ViewBag.SelectedCategoryId = categoryId.Value;
                ViewBag.SelectedCategoryName = selectedCategory?.Name;
            }
            else
            {
                ViewBag.SelectedCategoryId = null;
                ViewBag.SelectedCategoryName = null;
            }

            // Convert to list before sorting by calculated properties
            var products = productsQuery.ToList();

            // Sort products
            ViewBag.SortBy = sortBy;
            products = sortBy switch
            {
                "price_asc" => products.OrderBy(p => p.Price).ToList(),
                "price_desc" => products.OrderByDescending(p => p.Price).ToList(),
                "rating_asc" => products.OrderBy(p => p.AverageRating).ToList(),
                "rating_desc" => products.OrderByDescending(p => p.AverageRating).ToList(),
                _ => products.OrderByDescending(p => p.Id).ToList()
            };
            
            ViewBag.Products = products;
            return View();
        }

        public ActionResult Show(int id)
        {
            Product? product = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProposedBy)
                .Include(p => p.Reviews!)
                    .ThenInclude(r => r.User)
                .FirstOrDefault(p => p.Id == id);
            
            if (product == null)
            {
                return NotFound();
            }

            if (!CanSeeProduct(product))
            {
                return NotFound();
            }
            return View(product);
        }

        [Authorize(Roles = "Admin,Editor")]
        public ActionResult New()
        {
            ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name");
            return View(new ProductFormVM());
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public async Task<ActionResult> New(ProductFormVM form)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name");
                    return View(form);
                }

                if (form.ImageFile == null)
                {
                    ModelState.AddModelError("ImageFile", "Imaginea este obligatorie");
                    ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name");
                    return View(form);
                }

                var imageUrl = await SaveNormalizedProductImageAsync(form.ImageFile);
                if (imageUrl == null)
                {
                    ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name");
                    return View(form);
                }

                var product = new Product
                {
                    Title = form.Title.Trim(),
                    Description = form.Description.Trim(),
                    ImageUrl = imageUrl,
                    Price = form.Price,
                    Stock = form.Stock,
                    CategoryId = form.CategoryId
                };

                // Prevent overposting of moderation fields
                if (IsEditor)
                {
                    var userId = CurrentUserId;
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return Forbid();
                    }
                    product.Status = (int)ProductStatus.Pending;
                    product.ProposedByUserId = userId;
                    product.AdminFeedback = null;
                }
                else
                {
                    product.Status = (int)ProductStatus.Approved;
                    product.ProposedByUserId = null;
                    product.AdminFeedback = null;
                }

                db.Products.Add(product);
                db.SaveChanges();

                TempData["message"] = IsEditor
                    ? "Produsul a fost trimis spre aprobare" 
                    : "Produsul a fost adăugat";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name");
                return View(form);
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

            if (!CanEditProduct(product))
            {
                return Forbid();
            }
            ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
            ViewBag.CategoryName = db.Categories.FirstOrDefault(c => c.Id == product.CategoryId)?.Name;

            var vm = new ProductFormVM
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryId = product.CategoryId,
                ExistingImageUrl = product.ImageUrl
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public async Task<ActionResult> Edit(int id, ProductFormVM form)
        {
            Product? product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            if (!CanEditProduct(product))
            {
                return Forbid();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
                    ViewBag.CategoryName = db.Categories.FirstOrDefault(c => c.Id == form.CategoryId)?.Name;
                    form.ExistingImageUrl = product.ImageUrl;
                    return View(form);
                }

                product.Title = form.Title.Trim();
                product.Description = form.Description.Trim();
                product.Price = form.Price;
                product.Stock = form.Stock;
                product.CategoryId = form.CategoryId;

                if (form.ImageFile != null)
                {
                    var oldUrl = product.ImageUrl;
                    var newUrl = await SaveNormalizedProductImageAsync(form.ImageFile);
                    if (newUrl == null)
                    {
                        ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
                        ViewBag.CategoryName = db.Categories.FirstOrDefault(c => c.Id == form.CategoryId)?.Name;
                        form.ExistingImageUrl = product.ImageUrl;
                        return View(form);
                    }

                    product.ImageUrl = newUrl;
                    TryDeleteLocalProductImage(oldUrl);
                }

                // If an editor updates their proposal, it should go back to Pending review
                if (IsEditor)
                {
                    product.Status = (int)ProductStatus.Pending;
                    product.AdminFeedback = null;
                }
                
                db.SaveChanges();

                TempData["message"] = IsEditor
                    ? "Propunerea a fost actualizată și retrimisă spre aprobare!"
                    : "Produsul a fost modificat!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ViewBag.Categories = new SelectList(db.Categories.OrderBy(c => c.Name), "Id", "Name", product.CategoryId);
                ViewBag.CategoryName = db.Categories.FirstOrDefault(c => c.Id == form.CategoryId)?.Name;
                form.ExistingImageUrl = product.ImageUrl;
                return View(form);
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

            if (!CanEditProduct(product))
            {
                return Forbid();
            }

            TryDeleteLocalProductImage(product.ImageUrl);
            db.Products.Remove(product);
            db.SaveChanges();
            TempData["message"] = "Produsul a fost șters";
            return RedirectToAction("Index");
        }

        private async Task<string?> SaveNormalizedProductImageAsync(IFormFile file)
        {
            const long maxBytes = 2 * 1024 * 1024; // 2MB
            const int targetSize = 800;
            const int minSize = 300;

            if (file.Length <= 0)
            {
                ModelState.AddModelError("ImageFile", "Fișierul încărcat este gol");
                return null;
            }

            if (file.Length > maxBytes)
            {
                ModelState.AddModelError("ImageFile", "Imaginea este prea mare. Maxim 2MB");
                return null;
            }

            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType))
            {
                ModelState.AddModelError("ImageFile", "Tip de fișier invalid. Acceptăm doar JPG/JPEG și PNG");
                return null;
            }

            try
            {
                await using var stream = file.OpenReadStream();
                using var image = await Image.LoadAsync(stream);

                if (image.Width < minSize || image.Height < minSize)
                {
                    ModelState.AddModelError("ImageFile", $"Imaginea este prea mică. Minim {minSize}x{minSize} px");
                    return null;
                }

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(targetSize, targetSize),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center
                }));

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid():N}.jpg";
                var fullPath = Path.Combine(uploadsDir, fileName);

                await image.SaveAsJpegAsync(fullPath, new JpegEncoder { Quality = 85 });

                return $"/uploads/products/{fileName}";
            }
            catch
            {
                ModelState.AddModelError("ImageFile", "Nu am putut procesa imaginea. Încearcă un JPG/PNG valid.");
                return null;
            }
        }

        private void TryDeleteLocalProductImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            if (!imageUrl.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relative);
            try
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
                // ignore
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Pending()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }

            var pending = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProposedBy)
                .Where(p => p.Status == (int)ProductStatus.Pending)
                .OrderByDescending(p => p.Id)
                .ToList();

            ViewBag.Products = pending;
            return View();
        }

        [Authorize(Roles = "Editor")]
        public ActionResult MyProposals()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }

            var userId = CurrentUserId;
            if (userId == null)
            {
                return Forbid();
            }

            var mine = db.Products
                .Include(p => p.Category)
                .Where(p => p.ProposedByUserId == userId)
                .OrderByDescending(p => p.Id)
                .ToList();

            ViewBag.Products = mine;
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Approve(int id)
        {
            var product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Status = (int)ProductStatus.Approved;
            product.AdminFeedback = null;
            db.SaveChanges();

            TempData["message"] = "Produsul a fost aprobat și publicat";
            return RedirectToAction("Pending");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Reject(int id, string? adminFeedback)
        {
            var product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Status = (int)ProductStatus.Rejected;
            product.AdminFeedback = string.IsNullOrWhiteSpace(adminFeedback) ? "Necesită îmbunătățiri." : adminFeedback.Trim();
            db.SaveChanges();

            TempData["message"] = "Produsul a fost respins";
            return RedirectToAction("Pending");
        }
    }
}