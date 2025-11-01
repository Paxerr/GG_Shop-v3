using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [ForeignKey("Category")]
        public int Category_Id { get; set; }

        public string Description { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<Product_Sku> Product_Skus { get; set; }
        public virtual ICollection<Product_Image> Product_Images { get; set; }
    }
}

