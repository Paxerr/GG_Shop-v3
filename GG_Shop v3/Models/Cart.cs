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
        public int User_Id { get; set; }

        public DateTime Created_At { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<Cart_Item> Cart_Items { get; set; }
    }
}