using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("payment_details")]
    public class PaymentDetail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; } // Khóa ngoại

        [Required, Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        [MaxLength(30)]
        public string PaymentMethod { get; set; }

        [MaxLength(30)]
        public string PaymentStatus { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Property
        public virtual Order Order { get; set; }
    }
}