using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GG_Shop_v3.Models;

namespace GG_Shop_v3.Controllers
{
    public class U_OrdersController : Controller
    {
        private DataContext db = new DataContext();

        public U_OrdersController()
        {
            // Tắt proxy + lazy loading để trả JSON an toàn
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;
        }

        // GET: U_Orders
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult LoadData()
        {
            try
            {
                
                if (Session["User_Id"] == null)
                {
                    return Json(new { error = true, message = "Chưa login", user = new { Full_Name = "", Email = "", Phone_Number = "" }, cartItems = new List<object>() }, JsonRequestBehavior.AllowGet);
                }
                int userId = (int)Session["User_Id"];
                // Lấy user, kiểm tra null
                var user = db.users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                {
                    var emptyUser = new
                    {
                        Full_Name = "",
                        Email = "",
                        Phone_Number = "",
                        
                    };

                    return Json(new { user = emptyUser, cartItems = new List<object>() }, JsonRequestBehavior.AllowGet);
                }

                // Lấy cart và items
                var cart = db.carts
                    .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku.Product))
                    .FirstOrDefault(c => c.User_Id == userId);

                var cartItemsList = new List<object>();
                if (cart != null && cart.Cart_Items != null && cart.Cart_Items.Any())
                {
                    cartItemsList = cart.Cart_Items
                        .Select(i => new
                        {
                            i.Id,
                            Title = i.Product_Sku?.Product?.Title ?? "",
                            Color = i.Product_Sku?.Color ?? "",
                            Size = i.Product_Sku?.Size ?? "",
                            Price = i.Product_Sku?.Price ?? 0m,
                            i.Quantity
                        })
                        .ToList<object>();
                }

                var result = new
                {
                    user = new
                    {
                        user.Full_Name,
                        user.Email,
                        user.Phone_Number,
                        
                    },
                    cartItems = cartItemsList
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { error = true, message = msg }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult PlaceOrder( string shipping_address)
        {
            try
            {
                
                if (Session["User_Id"] == null)
                {
                    return Json(new { error = true, message = "Chưa login", user = new { Full_Name = "", Email = "", Phone_Number = "" }, cartItems = new List<object>() }, JsonRequestBehavior.AllowGet);
                }
                int userId = (int)Session["User_Id"];
                var cart = db.carts
                    .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku))
                    .FirstOrDefault(c => c.User_Id == userId);

                if (cart == null || cart.Cart_Items == null || !cart.Cart_Items.Any())
                    return Json(new { success = false, message = "Giỏ hàng trống" });

                decimal total = cart.Cart_Items.Sum(x => (x.Product_Sku?.Price ?? 0m) * x.Quantity);

                var order = new Order
                {
                    User_Id = userId,
                    Shipping_Address = shipping_address ?? "",
                    Total_Amount = total,
                    Created_At = DateTime.Now,
                    Status = "Đang xử lý"
                };

                db.orders.Add(order);
                db.SaveChanges();

                foreach (var item in cart.Cart_Items.ToList())
                {
                    var sku = db.product_skus.FirstOrDefault(s => s.Id == item.Sku_Id);
                    decimal price = sku?.Price ?? 0m;

                    db.order_items.Add(new Order_Item
                    {
                        Order_Id = order.Id,
                        Sku_Id = item.Sku_Id,
                        Quantity = item.Quantity,
                        Price = price
                    });

                    if (sku != null)
                    {
                        sku.Quantity = Math.Max(0, sku.Quantity - item.Quantity);
                    }
                }

                db.cart_items.RemoveRange(cart.Cart_Items);
                db.SaveChanges();

                return Json(new { success = true, message = "Đặt hàng thành công" });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = msg });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
