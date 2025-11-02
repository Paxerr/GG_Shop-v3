using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("promotions")]
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Promo_Code { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public decimal? Discount_Percentage { get; set; }

        public decimal? Discount_Amount { get; set; }

        public DateTime Start_Date { get; set; }

        public DateTime End_Date { get; set; }

        public decimal? Min_Order_Value { get; set; }

        public int Uses_Count { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}