using GG_Shop_v3.Models;
using System;
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
            // Kiểm tra đăng nhập
            if (!IsUserLoggedIn())
            {
                string returnUrl = Request.Url?.PathAndQuery;
                return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
            }

            return View();
        }

        // API: Lấy danh sách đơn hàng của user
        [HttpGet]
        public JsonResult GetUserOrders(int page = 1, int pageSize = 10)
        {
            try
            {
                int userId = GetCurrentUserId();

                if (userId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng đăng nhập",
                        redirect = Url.Action("Login", "Account", new { returnUrl = "/OrdersHistory/Index" })
                    }, JsonRequestBehavior.AllowGet);
                }

                // Lấy tổng số đơn hàng
                int totalCount = db.orders.Count(o => o.User_Id == userId);
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Lấy danh sách đơn hàng theo trang với thông tin cần thiết
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
                        PromoCode = o.Promotion.Promo_Code,
                        Date = o.Created_At,
                        // Đếm số lượng sản phẩm trong đơn hàng
                        ItemCount = o.Order_Items.Count
                    })
                    .ToList()
                    .Select(o => new
                    {
                        o.Id,
                        o.Total_Amount,
                        o.Status,
                        Shipping_Address = o.Shipping_Address ?? "Chưa có địa chỉ",
                        o.PromoCode,
                        Date = o.Date.ToString("dd/MM/yyyy HH:mm"),
                        o.ItemCount
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
                int userId = GetCurrentUserId();

                if (userId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng đăng nhập",
                        redirect = Url.Action("Login", "Account", new { returnUrl = "/OrdersHistory/Index" })
                    }, JsonRequestBehavior.AllowGet);
                }

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
                        PromoCode = o.Promotion.Promo_Code,
                        Discount = o.Promotion.Discount_Percentage ?? 0,
                        Date = o.Created_At,
                        PaymentMethod = o.Payment_Details.FirstOrDefault().Payment_Method,
                        PaymentStatus = o.Payment_Details.FirstOrDefault().Payment_Status
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

                // Lấy thông tin user
                var userInfo = db.users
                    .Where(u => u.Id == userId)
                    .Select(u => new
                    {
                        u.Phone_Number,
                        u.Full_Name
                    })
                    .FirstOrDefault();

                // Lấy danh sách sản phẩm trong đơn hàng
                var orderItems = db.order_items
                    .Include(oi => oi.Product_Sku.Product)
                    .Include(oi => oi.Product_Sku.Product.Product_Images)
                    .Where(oi => oi.Order_Id == orderId)
                    .Select(oi => new
                    {
                        oi.Id,
                        ProductId = oi.Product_Sku.Product_Id,
                        ProductTitle = oi.Product_Sku.Product.Title,
                        ProductSku = oi.Product_Sku.Sku,
                        Color = oi.Product_Sku.Color,
                        Size = oi.Product_Sku.Size,
                        oi.Quantity,
                        oi.Price,
                        TotalPrice = oi.Quantity * oi.Price,
                        ImageUrl = oi.Product_Sku.Product.Product_Images
                            .Where(pi => pi.Is_Main)
                            .Select(pi => pi.Image_Url)
                            .FirstOrDefault()
                    })
                    .ToList()
                    .Select(oi => new
                    {
                        oi.Id,
                        oi.ProductId,
                        oi.ProductTitle,
                        oi.ProductSku,
                        oi.Color,
                        oi.Size,
                        oi.Quantity,
                        oi.Price,
                        oi.TotalPrice,
                        ImageUrl = !string.IsNullOrEmpty(oi.ImageUrl)
                            ? oi.ImageUrl
                            : "/Content/images/default-product.jpg"
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    order = new
                    {
                        order.Id,
                        order.Total_Amount,
                        order.Status,
                        Shipping_Address = order.Shipping_Address ?? "Chưa có địa chỉ",
                        order.PromoCode,
                        order.Discount,
                        Date = order.Date.ToString("dd/MM/yyyy HH:mm"),
                        PaymentMethod = order.PaymentMethod ?? "Chưa xác định",
                        PaymentStatus = order.PaymentStatus ?? "Chưa xác định",
                        PhoneNumber = userInfo != null ? userInfo.Phone_Number : "",
                        FullName = userInfo != null ? userInfo.Full_Name : ""
                    },
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
                int userId = GetCurrentUserId();

                if (userId <= 0)
                {
                    return Json(new { success = false, count = 0, message = "Chưa đăng nhập" },
                               JsonRequestBehavior.AllowGet);
                }

                // Kiểm tra order có thuộc user không
                var orderExists = db.orders.Any(o => o.Id == orderId && o.User_Id == userId);
                if (!orderExists)
                {
                    return Json(new { success = false, count = 0, message = "Đơn hàng không tồn tại" },
                               JsonRequestBehavior.AllowGet);
                }

                int count = db.order_items.Count(oi => oi.Order_Id == orderId);
                return Json(new { success = true, count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, count = 0, message = ex.Message },
                           JsonRequestBehavior.AllowGet);
            }
        }

        // ==================== HÀM HỖ TRỢ ====================
        private int GetCurrentUserId()
        {
            // Kiểm tra session theo AccountController
            if (System.Web.HttpContext.Current != null &&
                System.Web.HttpContext.Current.Session != null)
            {
                // AccountController sử dụng "User_Id"
                if (Session["User_Id"] != null)
                {
                    try
                    {
                        int userId = Convert.ToInt32(Session["User_Id"]);
                        return userId;
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }

            return 0;
        }

        private bool IsUserLoggedIn()
        {
            return GetCurrentUserId() > 0;
        }

        // Thêm API để debug session
        [HttpGet]
        public JsonResult GetSessionInfo()
        {
            return Json(new
            {
                SessionUserId = Session["User_Id"],
                SessionUser = Session["User"],
                SessionKeys = Session.Keys.Cast<string>().ToList(),
                IsLoggedIn = IsUserLoggedIn(),
                CurrentUserId = GetCurrentUserId()
            }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult TestOrders()
        {
            var orders = db.orders.ToList();
            return Json(orders, JsonRequestBehavior.AllowGet);
        }
    }
}