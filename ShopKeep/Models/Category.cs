using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ShopKeep.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class Category
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Numele categoriei este obligatoriu")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Numele trebuie să aibă între 2 și 50 de caractere.")]
        public required string Name { get; set; }

        public virtual ICollection<Product>? Products { get; set; }
    }
}