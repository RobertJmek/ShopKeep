using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ShopKeep.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Nume complet")]
        public required string FullName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data nașterii")]
        public DateTime DateOfBirth { get; set; }

        [StringLength(300)]
        [Display(Name = "Adresă")]
        public string? Address { get; set; }

        [Phone]
        [Display(Name = "Telefon alternativ")]
        public string? AlternatePhoneNumber { get; set; }

        public virtual ICollection<Product>? ProposedProducts { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }
        public virtual ICollection<ShoppingCartItem>? ShoppingCartItems { get; set; }
        public virtual ICollection<WishlistItem>? WishlistItems { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }
    }
}