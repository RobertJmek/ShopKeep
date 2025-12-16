using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ShopKeep.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Numele produsului este obligatoriu")]
        [StringLength(200, ErrorMessage = "Numele este prea lung. Maxim 200 de caractere.")]
        public string Name { get; set; }
        public string? Desciption { get; set; }
        [Required(ErrorMessage = "Produsul trebuie sa aiba un pret")]
        public decimal Price {get; set; }
        public string? ImageUrl { get; set; }
        [Required(ErrorMessage = "Categoria este obligatorie")]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }        
    }
}