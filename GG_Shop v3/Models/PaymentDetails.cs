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
        public int OrderId { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(30)]
        public string PaymentMethod { get; set; }

        [MaxLength(30)]
        public string PaymentStatus { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual Order Order { get; set; }
    }
}