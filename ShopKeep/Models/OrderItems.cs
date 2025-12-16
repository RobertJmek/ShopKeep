using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopKeep.Models
{
    public class OrderItems
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Comanda este obligatorie")]
        [Display(Name = "Comandă")]
        public int OrderId { get; set; }
        
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required(ErrorMessage = "Produsul este obligatoriu")]
        [Display(Name = "Produs")]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required(ErrorMessage = "Cantitatea este obligatorie")]
        [Range(1, int.MaxValue, ErrorMessage = "Cantitatea trebuie să fie cel puțin 1")]
        [Display(Name = "Cantitate")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Prețul este obligatoriu")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Preț unitar")]
        public decimal UnitPrice { get; set; }

        // Proprietate calculată pentru subtotal
        [NotMapped]
        public decimal Subtotal => UnitPrice * Quantity;
    }
}