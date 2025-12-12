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

        // Online payment view (full AJAX)
        public ActionResult Online(int orderId)
        {
            var order = db.orders.Find(orderId);
            if (order == null)
                return RedirectToAction("Index", "U_Orders");

            ViewBag.OrderId = orderId;
            ViewBag.TotalAmount = order.Total_Amount;

            // ⭐ QR demo (client sẽ dùng JS tạo QR code từ URL này)
            ViewBag.QRContent = $"https://bank.fakeapi.com/pay?amount={order.Total_Amount}&orderId={orderId}";

            return View();
        }

        // POST: Confirm payment (AJAX)
        [HttpPost]
        public JsonResult ConfirmPayment(int orderId)
        {
            if (orderId <= 0)
                return Json(new { success = false, msg = "orderId không hợp lệ" });

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = db.orders
                        .Include(o => o.Order_Items.Select(oi => oi.Product_Sku))
                        .FirstOrDefault(o => o.Id == orderId);

                    if (order == null)
                        return Json(new { success = false, msg = "Không tìm thấy đơn hàng" });

                    if (!string.Equals(order.Status, "Chờ thanh toán", StringComparison.OrdinalIgnoreCase))
                        return Json(new { success = false, msg = "Đơn hàng đã xử lý hoặc không ở trạng thái chờ thanh toán" });

                    // Cập nhật trạng thái
                    order.Status = "Đã thanh toán";

                    // Trừ kho
                    if (order.Order_Items != null)
                    {
                        foreach (var it in order.Order_Items)
                        {
                            if (it?.Product_Sku != null)
                                it.Product_Sku.Quantity = Math.Max(0, it.Product_Sku.Quantity - it.Quantity);
                        }
                    }

                    // Clear cart
                    var cart = db.carts.Include(c => c.Cart_Items).FirstOrDefault(c => c.User_Id == order.User_Id);
                    if (cart != null && cart.Cart_Items != null && cart.Cart_Items.Any())
                        db.cart_items.RemoveRange(cart.Cart_Items);

                    // Lưu payment detail
                    db.payment_details.Add(new Payment_Detail
                    {
                        Order_Id = order.Id,
                        Amount = order.Total_Amount,
                        Payment_Method = "QR",
                        Payment_Status = "Success",
                        Created_At = DateTime.Now
                    });

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        msg = "Thanh toán thành công!",
                        redirectUrl = Url.Action("Index", "U_Shop") // redirect thẳng về sản phẩm
                    });
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    return Json(new { success = false, msg });
                }
            }
        }

        // API AJAX check thanh toán
        [HttpPost]
        public JsonResult CheckPayment(int orderId, string token)
        {
            if (Session["QRToken_" + orderId]?.ToString() != token)
                return Json(new { success = false, msg = "Token không hợp lệ" });

            var order = db.orders.Include(o => o.Order_Items.Select(oi => oi.Product_Sku))
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
                return Json(new { success = false, msg = "Không tìm thấy đơn hàng" });

            if (order.Status != "Chờ thanh toán")
                return Json(new { success = false, msg = "Đơn hàng đã xử lý hoặc không ở trạng thái chờ thanh toán" });

            // Cập nhật trạng thái
            order.Status = "Đã thanh toán";

            // Trừ kho
            if (order.Order_Items != null)
            {
                foreach (var it in order.Order_Items)
                {
                    if (it?.Product_Sku != null)
                        it.Product_Sku.Quantity = Math.Max(0, it.Product_Sku.Quantity - it.Quantity);
                }
            }

            // Clear cart
            var cart = db.carts.Include(c => c.Cart_Items).FirstOrDefault(c => c.User_Id == order.User_Id);
            if (cart?.Cart_Items != null && cart.Cart_Items.Any())
                db.cart_items.RemoveRange(cart.Cart_Items);

            // Lưu PaymentDetail
            db.payment_details.Add(new Payment_Detail
            {
                Order_Id = order.Id,
                Amount = order.Total_Amount,
                Payment_Method = "QR",
                Payment_Status = "Success",
                Created_At = DateTime.Now
            });

            db.SaveChanges();

            return Json(new { success = true, msg = "Thanh toán thành công!" });
        }

        private int? GetUserIdFromSession()
        {
            if (Session["User_Id"] != null && int.TryParse(Session["User_Id"].ToString(), out int v))
                return v;
            if (Session["UserId"] != null && int.TryParse(Session["UserId"].ToString(), out int v2))
                return v2;
            if (Session["User"] is User u)
                return u.Id;

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
