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
        public string PromoCode { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public decimal? DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal? MinOrderValue { get; set; }

        public int UsesCount { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}