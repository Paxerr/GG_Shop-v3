using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("cart_items")]
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Cart")]
        public int CartId { get; set; }

        [ForeignKey("ProductSku")]
        public int SkuId { get; set; }

        public int Quantity { get; set; }

        public virtual Cart Cart { get; set; }
        public virtual ProductSku ProductSku { get; set; }
    }
}