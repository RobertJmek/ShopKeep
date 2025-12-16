using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopKeep.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        // Rating opțional (1-5)
        [Range(1, 5, ErrorMessage = "Rating-ul trebuie să fie între 1 și 5")]
        [Display(Name = "Rating")]
        public int? Rating { get; set; }

        // Text opțional
        [StringLength(1000, ErrorMessage = "Review-ul este prea lung. Maxim 1000 de caractere.")]
        [Display(Name = "Comentariu")]
        public string? Text { get; set; }

        // Data creării
        [Required]
        [Display(Name = "Data")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Ultima modificare
        [Display(Name = "Ultima modificare")]
        public DateTime? UpdatedAt { get; set; }

        // Utilizatorul care a creat review-ul
        [Required(ErrorMessage = "Utilizatorul este obligatoriu")]
        [Display(Name = "Utilizator")]
        public required string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Produsul pentru care este review-ul
        [Required(ErrorMessage = "Produsul este obligatoriu")]
        [Display(Name = "Produs")]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
