using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int User_Id { get; set; }

        public decimal Total_Amount { get; set; }

        [MaxLength(30)]
        public string Status { get; set; }

        [MaxLength(255)]
        public string Shipping_Address { get; set; }

        [ForeignKey("Promotion")]
        public int? Promo_Id { get; set; }

        public DateTime Create_At { get; set; }

        public virtual User User { get; set; }
        public virtual Promotion Promotion { get; set; }

        public virtual ICollection<Order_Item> Order_Items { get; set; }
        public virtual ICollection<Payment_Detail> Payment_Details { get; set; }
    }
}