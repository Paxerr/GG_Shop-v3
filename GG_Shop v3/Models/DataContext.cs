using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace GG_Shop_v3.Models
{
    public partial class DataContext : DbContext
    {
        public DataContext()
            : base("name=DataContext")
        {
        }

        public virtual DbSet<Cart> carts { get; set; }
        public virtual DbSet<Category> categories { get; set; }
        public virtual DbSet<OrderItem> order_items { get; set; }
        public virtual DbSet<Order> orders { get; set; }
        public virtual DbSet<PaymentDetail> payment_details { get; set; }
        public virtual DbSet<ProductImage> product_images { get; set; }
        public virtual DbSet<ProductSku> product_skus { get; set; }
        public virtual DbSet<Product> products { get; set; }
        public virtual DbSet<Promotion> promotions { get; set; }
        public virtual DbSet<User> users { get; set; }

        
    }
}
