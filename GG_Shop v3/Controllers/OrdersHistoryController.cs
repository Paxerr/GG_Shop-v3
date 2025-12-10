using GG_Shop_v3.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace GG_Shop_v3.Controllers
{
    public class OrdersHistoryController : Controller
    {
        private DataContext db = new DataContext();

        // GET: /OrdersHistory/Index - Trang lịch sử đơn hàng
        public ActionResult Index()
        {

            return View();
        }

        // API: Lấy danh sách đơn hàng của user
        [HttpGet]
        public JsonResult GetUserOrders(int page = 1, int pageSize = 10)
        {
            try
            {
                int userId = 1; // Fake user ID - thay bằng Session/Claim thực tế

                // Lấy tổng số đơn hàng
                int totalCount = db.orders.Count(o => o.User_Id == userId);
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Lấy danh sách đơn hàng theo trang
                var orders = db.orders
                    .Where(o => o.User_Id == userId)
                    .OrderByDescending(o => o.Created_At)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.Id,
                        o.Total_Amount,
                        o.Status,
                        o.Shipping_Address,
                        PromoCode = o.Promo_Id != null ? o.Promotion.Promo_Code : null,
                        Date = o.Created_At.ToString("dd/MM/yyyy HH:mm"),
                        PaymentMethod = o.Payment_Details.FirstOrDefault().Payment_Method
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    orders,
                    paging = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi lấy danh sách đơn hàng: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // API: Lấy chi tiết đơn hàng
        [HttpGet]
        public JsonResult GetOrderDetails(int orderId)
        {
            try
            {
                int userId = 1; // Fake user ID

                // Lấy thông tin đơn hàng
                var order = db.orders
                    .Include(o => o.Promotion)
                    .Include(o => o.Payment_Details)
                    .Where(o => o.Id == orderId && o.User_Id == userId)
                    .Select(o => new
                    {
                        o.Id,
                        o.Total_Amount,
                        o.Status,
                        o.Shipping_Address,
                        PromoCode = o.Promo_Id != null ? o.Promotion.Promo_Code : null,
                        Discount = o.Promo_Id != null ? o.Promotion.Discount_Amount : 0,
                        Date = o.Created_At.ToString("dd/MM/yyyy HH:mm"),
                        PaymentMethod = o.Payment_Details.FirstOrDefault().Payment_Method,
                        PaymentStatus = o.Payment_Details.FirstOrDefault().Payment_Status,
                        PhoneNumber = db.users.Where(u => u.Id == o.User_Id).Select(u => u.Phone_Number).FirstOrDefault()
                    })
                    .FirstOrDefault();

                if (order == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy đơn hàng"
                    }, JsonRequestBehavior.AllowGet);
                }

                // Lấy danh sách sản phẩm trong đơn hàng
                var orderItems = db.order_items
                    .Where(oi => oi.Order_Id == orderId)
                    .Select(oi => new
                    {
                        oi.Id,
                        ProductId = oi.Product_Sku.Product_Id,
                        ProductTitle = oi.Product_Sku.Product.Title,
                        ProductSku = oi.Product_Sku.Sku,
                        Color = oi.Product_Sku.Color,
                        Size = oi.Product_Sku.Size,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        TotalPrice = oi.Quantity * oi.Price,
                        ImageUrl = oi.Product_Sku.Product.Product_Images
                            .Where(pi => pi.Is_Main)
                            .Select(pi => pi.Image_Url)
                            .FirstOrDefault()
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    order,
                    orderItems
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi lấy chi tiết đơn hàng: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // API: Lấy số lượng sản phẩm trong đơn hàng
        [HttpGet]
        public JsonResult GetOrderItemsCount(int orderId)
        {
            try
            {
                int count = db.order_items.Count(oi => oi.Order_Id == orderId);
                return Json(new { count }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}