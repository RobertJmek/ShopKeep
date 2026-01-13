using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopKeep.Models
{
    public class ProductFAQ
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(500)]
        public required string Question { get; set; }

        [Required]
        [StringLength(2000)]
        public required string Answer { get; set; }

        // Numărul de ori când această întrebare a fost pusă
        public int AskCount { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
