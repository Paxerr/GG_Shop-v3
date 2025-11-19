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

        [Required, MaxLength(100)]
        public string Full_Name { get; set; }

        [Required, MaxLength(20)]
        public string Phone_Number { get; set; }

        [Required, MaxLength(100)]
        public string Country { get; set; }

        public int Orders { get; set; }

        [Required, MaxLength(100)]
        public string Rank { get; set; }

        public double Total_Spent { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; }

        public virtual ICollection<Order> OrdersList { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }

        public User()
        {
            // Khởi tạo các Collection
            OrdersList = new HashSet<Order>();
            Carts = new HashSet<Cart>();
        }

        // Constructor để khởi tạo User
        public User(string username, string email, string password, string full_name, string phone_number, string country, int orders, string rank, double total_spent, string role)
        {
            this.Username = username;
            this.Email = email;
            this.Password = password;
            this.Full_Name = full_name;
            this.Phone_Number = phone_number;
            this.Country = country;
            this.Orders = orders;
            this.Rank = rank;
            this.Total_Spent = total_spent;
            this.Role = role;

            // Khởi tạo các Collection
            OrdersList = new HashSet<Order>();
            Carts = new HashSet<Cart>();
        }
    }
}