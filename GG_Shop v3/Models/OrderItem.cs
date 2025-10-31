using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("order_items")]
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; } // Khóa ngoại

        [ForeignKey("Sku")]
        public int SkuId { get; set; } // Khóa ngoại

        public int Quantity { get; set; }

        [Required, Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        // Navigation Properties
        public virtual Order Order { get; set; }
        public virtual ProductSku Sku { get; set; }
    }
}