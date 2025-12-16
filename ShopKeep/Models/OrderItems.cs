using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ShopKeep.Models
{
    public class OrderItems
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Comanda e obligatorie")]
        public int OrderId { get; set; }
        [Required(ErrorMessage = "Produsul este obligatoriu")]
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Cantitatea e obligatorie")]
        public int Quantity {get; set; } 
        public virtual Order Order { get; set;}
        public virtual Product Product { get; set; }
    }
}