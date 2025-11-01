using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GG_Shop_v3.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required, MaxLength(255)]
        public string Password { get; set; }

        [MaxLength(100)]
        public string Full_Name { get; set; }

        [MaxLength(20)]
        public string Phone_Number { get; set; }

        [MaxLength(100)]
        public string Country { get; set; }

        public int Orders { get; set; }

        [MaxLength(100)]
        public string Rank { get; set; }

        public double Total_Spent { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; }

        public virtual ICollection<Order> OrdersList { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
    }
}