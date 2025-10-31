using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("product_skus")]
    public class ProductSku
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; } // Khóa ngoại

        [Required, MaxLength(50)]
        public string Sku { get; set; }

        [MaxLength(30)]
        public string Color { get; set; }

        [MaxLength(10)]
        public string Size { get; set; }

        public int Quantity { get; set; }

        [Required, Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        // Navigation Properties
        public virtual Product Product { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
    }
}