using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("product_images")]
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; } // Khóa ngoại

        [Required, MaxLength(255)]
        public string ImageUrl { get; set; }

        public bool IsMain { get; set; } // BIT maps to bool

        // Navigation Property
        public virtual Product Product { get; set; }
    }
}