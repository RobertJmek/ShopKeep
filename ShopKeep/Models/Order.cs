using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ShopKeep.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Utilizatorul este obligatoriu")]
        [Display(Name = "Utilizator")]
        public required string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Data comenzii este obligatorie")]
        [Display(Name = "Data comenzii")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Valoarea totală este obligatorie")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valoare totală")]
        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Plasată";

        [StringLength(500)]
        [Display(Name = "Adresă de livrare")]
        public string? DeliveryAddress { get; set; }

        public virtual ICollection<OrderItems>? OrderItems { get; set; }
    }
}