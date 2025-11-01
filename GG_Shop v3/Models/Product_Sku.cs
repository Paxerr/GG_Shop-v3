using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("product_skus")]
    public class Product_Sku
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int Product_Id { get; set; }

        [Required, MaxLength(50)]
        public string Sku { get; set; }

        [MaxLength(30)]
        public string Color { get; set; }

        [MaxLength(10)]
        public string Size { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public virtual Product Product { get; set; }
        public virtual ICollection<Order_Item> Order_Items { get; set; }
        public virtual ICollection<Cart_Item> Cart_Items { get; set; }
    }

}