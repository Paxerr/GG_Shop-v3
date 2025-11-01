using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("order_items")]
    public class Order_Item
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int Order_Id { get; set; }

        [ForeignKey("Product_Sku")]
        public int Sku_Id { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public virtual Order Order { get; set; }
        public virtual Product_Sku Product_Sku { get; set; }
    }
}