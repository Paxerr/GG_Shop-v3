using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("product_images")]
    public class Product_Image
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int Product_Id { get; set; }

        [Required, MaxLength(255)]
        public string Image_Url { get; set; }

        public bool Is_Main { get; set; }

        public virtual Product Product { get; set; }
    }
}