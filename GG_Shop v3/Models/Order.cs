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
        public int UserId { get; set; }

        public decimal TotalAmount { get; set; }

        [MaxLength(30)]
        public string Status { get; set; }

        [MaxLength(255)]
        public string ShippingAddress { get; set; }

        [ForeignKey("Promotion")]
        public int? PromoId { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual User User { get; set; }
        public virtual Promotion Promotion { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<PaymentDetail> PaymentDetails { get; set; }
    }
}