using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopKeep.Models
{
    // Index pentru a preveni duplicatele (același user + același produs)
    [Index(nameof(UserId), nameof(ProductId), IsUnique = true)]
    public class WishlistItem
    {
        [Key]
        public int Id { get; set; }

        // Data adăugării în wishlist
        [Required]
        [Display(Name = "Adăugat la")]
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Utilizatorul căruia îi aparține wishlist-ul
        [Required(ErrorMessage = "Utilizatorul este obligatoriu")]
        [Display(Name = "Utilizator")]
        public required string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Produsul din wishlist
        [Required(ErrorMessage = "Produsul este obligatoriu")]
        [Display(Name = "Produs")]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
