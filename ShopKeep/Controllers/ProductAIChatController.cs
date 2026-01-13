using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopKeep.Models;
using System.Text.RegularExpressions;

namespace ShopKeep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductAIChatController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductAIChatController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskQuestion([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new { error = "Întrebarea nu poate fi goală." });
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId);

            if (product == null)
            {
                return NotFound(new { error = "Produsul nu a fost găsit." });
            }

            // Normalizează întrebarea pentru comparație
            var normalizedQuestion = NormalizeText(request.Question);

            // Caută în FAQ-uri existente
            var existingFaq = await _context.ProductFAQs
                .Where(f => f.ProductId == request.ProductId)
                .ToListAsync();

            // Verifică dacă există un FAQ similar
            foreach (var faq in existingFaq)
            {
                var normalizedFaqQuestion = NormalizeText(faq.Question);
                var similarity = CalculateSimilarity(normalizedQuestion, normalizedFaqQuestion);
                
                if (similarity > 0.7) // 70% similaritate
                {
                    // Incrementează contorul
                    faq.AskCount++;
                    faq.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        answer = faq.Answer,
                        source = "faq",
                        confidence = "high"
                    });
                }
            }

            // Dacă nu există în FAQ, generează răspuns din descriere
            var answer = GenerateAnswerFromDescription(product, request.Question);

            // Salvează întrebarea dacă e frecventă (pragul poate fi ajustat)
            await SaveQuestionIfFrequent(request.ProductId, request.Question, answer);

            return Ok(new
            {
                answer = answer,
                source = answer.Contains("Nu am suficiente informații") ? "default" : "description",
                confidence = answer.Contains("Nu am suficiente informații") ? "low" : "medium"
            });
        }

        private string GenerateAnswerFromDescription(Product product, string question)
        {
            var normalizedQuestion = NormalizeText(question);
            var description = product.Description ?? "";
            var normalizedDescription = NormalizeText(description);

            // Cuvinte cheie pentru tipuri de întrebări
            var priceKeywords = new[] { "pret", "costa", "cost", "bani", "lei", "valoare" };
            var sizeKeywords = new[] { "marime", "dimensiuni", "cat de mare", "spatiu" };
            var colorKeywords = new[] { "culoare", "culori", "ce culoare" };
            var materialKeywords = new[] { "material", "fabric", "tesatura" };
            var warrantyKeywords = new[] { "garantie", "garantat" };
            var deliveryKeywords = new[] { "livrare", "livrat", "transport", "cand ajunge" };
            var stockKeywords = new[] { "stoc", "disponibil", "stock" };

            // Întrebări despre preț
            if (priceKeywords.Any(k => normalizedQuestion.Contains(k)))
            {
                return $"Prețul produsului este {product.Price:F2} lei.";
            }

            // Întrebări despre stoc
            if (stockKeywords.Any(k => normalizedQuestion.Contains(k)))
            {
                return product.Stock > 0 
                    ? $"Da, produsul este în stoc. Avem {product.Stock} bucăți disponibile."
                    : "Din păcate, produsul nu este momentan în stoc.";
            }

            // Caută în descriere răspunsuri relevante
            var questionWords = normalizedQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3) // Doar cuvinte semnificative
                .ToList();

            var descriptionSentences = description.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var relevantSentences = new List<(string sentence, int matches)>();

            foreach (var sentence in descriptionSentences)
            {
                var normalizedSentence = NormalizeText(sentence);
                var matchCount = questionWords.Count(word => normalizedSentence.Contains(word));
                
                if (matchCount > 0)
                {
                    relevantSentences.Add((sentence.Trim(), matchCount));
                }
            }

            // Returnează cele mai relevante propoziții
            if (relevantSentences.Any())
            {
                var bestSentences = relevantSentences
                    .OrderByDescending(s => s.matches)
                    .Take(2)
                    .Select(s => s.sentence);

                return string.Join(" ", bestSentences);
            }

            // Răspuns default dacă nu găsește informații
            return "Nu am suficiente informații în descrierea produsului pentru a răspunde la această întrebare. " +
                   "Vă rugăm să contactați vânzătorul pentru mai multe detalii.";
        }

        private async Task SaveQuestionIfFrequent(int productId, string question, string answer)
        {
            // Verifică dacă întrebarea e similară cu una existentă
            var normalizedQuestion = NormalizeText(question);
            
            var existingFaq = await _context.ProductFAQs
                .Where(f => f.ProductId == productId)
                .ToListAsync();

            foreach (var faq in existingFaq)
            {
                var normalizedFaqQuestion = NormalizeText(faq.Question);
                var similarity = CalculateSimilarity(normalizedQuestion, normalizedFaqQuestion);
                
                if (similarity > 0.7)
                {
                    return; // Deja există în FAQ
                }
            }

            // Creează un FAQ temporar - va fi promovat la FAQ permanent după mai multe întrebări
            var newFaq = new ProductFAQ
            {
                ProductId = productId,
                Question = question,
                Answer = answer,
                AskCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProductFAQs.Add(newFaq);
            await _context.SaveChangesAsync();
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            // Elimină diacritice și convertește la lowercase
            text = text.ToLowerInvariant();
            text = text.Replace("ă", "a").Replace("â", "a").Replace("î", "i")
                       .Replace("ș", "s").Replace("ț", "t");
            
            // Elimină punctuație și caractere speciale
            text = Regex.Replace(text, @"[^\w\s]", "");
            
            // Elimină spații multiple
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            var words1 = text1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var words2 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            if (words1.Count == 0 || words2.Count == 0)
                return 0;

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return (double)intersection / union; // Jaccard similarity
        }
    }

    public class ChatRequest
    {
        public int ProductId { get; set; }
        public string Question { get; set; } = "";
    }
}
