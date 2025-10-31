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
        public int UserId { get; set; } // Khóa ngoại

        [Required, Column(TypeName = "decimal(10, 2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(30)]
        public string Status { get; set; }

        [MaxLength(255)]
        public string ShippingAddress { get; set; }

        [ForeignKey("Promotion")]
        [MaxLength(50)]
        public string PromoCode { get; set; } // Khóa ngoại (string)

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual Promotion Promotion { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<PaymentDetail> PaymentDetails { get; set; } // Quan hệ 1-N (do Order có thể có nhiều payment_details trong một số cấu hình)
    }
}