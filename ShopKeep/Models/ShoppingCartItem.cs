using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopKeep.Models
{
    public class ShoppingCartItem
    {
        [Key]
        public int Id { get; set; }

        // Cantitatea dorită
        [Required(ErrorMessage = "Cantitatea este obligatorie")]
        [Range(1, int.MaxValue, ErrorMessage = "Cantitatea trebuie să fie cel puțin 1")]
        [Display(Name = "Cantitate")]
        public int Quantity { get; set; }

        // Data adăugării în coș
        [Required]
        [Display(Name = "Adăugat la")]
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Utilizatorul căruia îi aparține coșul
        [Required(ErrorMessage = "Utilizatorul este obligatoriu")]
        [Display(Name = "Utilizator")]
        public required string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Produsul din coș
        [Required(ErrorMessage = "Produsul este obligatoriu")]
        [Display(Name = "Produs")]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Proprietate calculată pentru subtotal
        [NotMapped]
        public decimal Subtotal => Product != null ? Product.Price * Quantity : 0;
    }
}
