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
        public string PromoCode { get; set; } // UNIQUE

        [MaxLength(255)]
        public string Description { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? DiscountAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? MinOrderValue { get; set; }

        public int UsesCount { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }

        // Navigation Property
        public virtual ICollection<Order> Orders { get; set; }
    }
}