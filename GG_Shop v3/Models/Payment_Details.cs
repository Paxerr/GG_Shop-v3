using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("payment_details")]
    public class Payment_Detail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int Order_Id { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(30)]
        public string Payment_Method { get; set; }

        [MaxLength(30)]
        public string Payment_Status { get; set; }

        public DateTime Created_At { get; set; }

        public virtual Order Order { get; set; }
    }
}