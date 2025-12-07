using GG_Shop_v3.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace GG_Shop_v3.Controllers
{
    public class PaymentController : Controller
    {
        private DataContext db = new DataContext();

        // GET: Payment
        public ActionResult Index()
        {
            return View();
        }

        // ===========================
        //  XỬ LÝ THANH TOÁN / ĐẶT HÀNG
        // ===========================
        [HttpPost]
        public JsonResult CreateOrder(int userId, string shippingAddress, string promoCode)
        {
            try
            {
                // 1. Lấy cart theo user
                var cart = db.carts
                    .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku))
                    .FirstOrDefault(c => c.User_Id == userId);

                if (cart == null || cart.Cart_Items.Count == 0)
                {
                    return Json(new { status = false, message = "Giỏ hàng đang trống!" });
                }

                // 2. Tính tổng tiền
                decimal totalAmount = cart.Cart_Items.Sum(item =>
                    item.Quantity * item.Product_Sku.Price
                );

                // 3. Áp dụng mã giảm giá (nếu có)
                Promotion promo = null;
                if (!string.IsNullOrEmpty(promoCode))
                {
                    promo = db.promotions.FirstOrDefault(p =>
                        p.Promo_Code == promoCode &&
                        p.Status == "Active" &&
                        DateTime.Now >= p.Start_Date &&
                        DateTime.Now <= p.End_Date
                    );

                    if (promo != null)
                    {
                        if (promo.Type == "Giảm theo %")
                        {
                            totalAmount -= (totalAmount * (promo.Discount_Percentage ?? 0) / 100);
                        }
                        else if (promo.Type == "Giảm theo tiền")
                        {
                            totalAmount -= (promo.Discount_Amount ?? 0);
                        }

                        if (totalAmount < 0) totalAmount = 0;
                    }
                }

                // 4. Tạo Order
                var order = new Order()
                {
                    User_Id = userId,
                    Total_Amount = totalAmount,
                    Status = "Pending",
                    Shipping_Address = shippingAddress,
                    Promo_Id = promo?.Id,
                    Created_At = DateTime.Now
                };

                db.orders.Add(order);
                db.SaveChanges();

                // 5. Tạo Order_Items
                foreach (var item in cart.Cart_Items)
                {
                    var orderItem = new Order_Item()
                    {
                        Order_Id = order.Id,
                        Sku_Id = item.Product_Sku_Id,
                        Quantity = item.Quantity,
                        Price = item.Product_Sku.Price
                    };

                    db.order_items.Add(orderItem);

                    // 6. Trừ tồn kho
                    item.Product_Sku.Quantity -= item.Quantity;
                    db.Entry(item.Product_Sku).State = EntityState.Modified;
                }

                // 7. Xóa cart sau khi tạo order
                db.cart_items.RemoveRange(cart.Cart_Items);

                db.SaveChanges();

                return Json(new
                {
                    status = true,
                    message = "Đặt hàng thành công!",
                    orderId = order.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Lỗi thanh toán!", error = ex.Message });
            }
        }
    }
}
