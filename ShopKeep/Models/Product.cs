using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopKeep.Models
{

    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul produsului este obligatoriu")]
        [StringLength(200, ErrorMessage = "Titlul este prea lung. Maxim 200 de caractere.")]
        [Display(Name = "Titlu")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [StringLength(2000, ErrorMessage = "Descrierea este prea lungă. Maxim 2000 de caractere.")]
        [Display(Name = "Descriere")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Imaginea este obligatorie")]
        [Display(Name = "URL Imagine")]
        public required string ImageUrl { get; set; }

        [Required(ErrorMessage = "Prețul este obligatoriu")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie mai mare decât 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Preț")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stocul este obligatoriu")]
        [Range(0, int.MaxValue, ErrorMessage = "Stocul trebuie să fie mai mare sau egal cu 0")]
        [Display(Name = "Stoc")]
        public int Stock { get; set; }

        // Rating calculat automat din media review-urilor
        [Range(0, 5)]
        [Display(Name = "Rating mediu")]
        public double AverageRating { get; set; } = 0;


        // Feedback de la administrator
        [StringLength(500)]
        [Display(Name = "Feedback Administrator")]
        public string? AdminFeedback { get; set; }

        // ID-ul colaboratorului care a propus produsul
        [Display(Name = "Propus de")]
        public string? ProposedByUserId { get; set; }
        
        [ForeignKey("ProposedByUserId")]
        public virtual ApplicationUser? ProposedBy { get; set; }

        // Categoria produsului
        [Required(ErrorMessage = "Categoria este obligatorie")]
        [Display(Name = "Categorie")]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // Relații
        public virtual ICollection<Review>? Reviews { get; set; }
        public virtual ICollection<ShoppingCartItem>? ShoppingCartItems { get; set; }
        public virtual ICollection<WishlistItem>? WishlistItems { get; set; }
        public virtual ICollection<OrderItems>? OrderItems { get; set; }

        // Proprietăți calculate
        [NotMapped]
        public int ReviewCount => Reviews?.Count ?? 0;

        [NotMapped]
        public bool IsInStock => Stock > 0;
    }
}