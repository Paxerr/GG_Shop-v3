using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("cart_items")]
    public class Cart_Item
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Cart")]
        public int Cart_Id { get; set; }

        [ForeignKey("Product_Sku")]
        public int Sku_Id { get; set; }

        public int Quantity { get; set; }

        public virtual Cart Cart { get; set; }
        public virtual Product_Sku Product_Sku { get; set; }
    }
}