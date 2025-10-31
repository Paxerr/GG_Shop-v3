using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("carts")]
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; } // Khóa ngoại

        [ForeignKey("Sku")]
        public int SkuId { get; set; } // Khóa ngoại

        public int Quantity { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual ProductSku Sku { get; set; }
    }
}