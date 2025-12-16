using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShopKeep.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = " Userul este obligatoriu")]
        public int UserId { get; set; }
        [Required(ErrorMessage = "Data comenzii este obligatorie")]
        public DateTime OrderDate { get; set; }
        [Required(ErrorMessage = "Valoarea comenzii este obligatorie ")]
        public decimal TotalAmount { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<OrderItems> OrderItems { get; set; }

    }
}