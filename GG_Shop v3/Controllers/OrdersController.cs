using GG_Shop_v3.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace GG_Shop_v3.Controllers
{
    public class OrdersController : Controller
    {
        private DataContext db = new DataContext();

        // GET: Orders
        public ActionResult Index()
        {
            var orders = db.orders.Include(o => o.Promotion).Include(o => o.User);
            int totalSales = db.orders.Count();

            ViewBag.TotalSales = totalSales;

            decimal DoanhThu = db.orders.Sum(o => o.Total_Amount);
            ViewBag.DoanhThu = DoanhThu;

            int TongSanPham = db.order_items.Sum(o => o.Quantity);
            ViewBag.TongSanPham = TongSanPham;
            return View(orders.ToList());
        }
       
        [HttpGet]
        public JsonResult GetOrders()
        {
            var orders = db.orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .ToList()   // <-- CHUYỂN SANG LIST TRƯỚC
                .Select(o => new
                {
                    o.Id,
                    User = o.User.Username,
                    Total_Amount = o.Total_Amount,
                    Promo = o.Promotion != null ? o.Promotion.Promo_Code : "—",
                    o.Status,
                    o.Shipping_Address,
                    Created = o.Created_At.ToString("dd/MM/yyyy")  // format ở đây KHÔNG lỗi!
                })
                .ToList();

            return Json(orders, JsonRequestBehavior.AllowGet);
        }



        // GET: Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = db.orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.Order_Items.Select(i => i.Product_Sku.Product))
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return HttpNotFound();

            

            return View(order);
        }


        // ✅ GET: Orders/Create

        public JsonResult GetStatusList()
        {
            var statuses = new[] { "Đang xử lí", "Đã giao", "Hoàn thành", "Đã trả", "Đã hủy" };
            return Json(statuses, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAllSkus()
        {
            var skus = db.product_skus.Include(s => s.Product)
                       .Select(s => new {
                           s.Id,
                           ProductTitle = s.Product.Title,
                           s.Color,
                           s.Size,
                           s.Price
                       }).ToList();
            return Json(skus, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Create()
        {
            

            Session["TempOrderItems"] = new List<Order_Item>();
            return View(new Order());
        }

        // ✅ POST: Orders/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddTempItem(int SkuId, int Quantity)
        {
            var tempItems = Session["TempOrderItems"] as List<Order_Item> ?? new List<Order_Item>();
            var sku = db.product_skus.Include(s => s.Product).FirstOrDefault(s => s.Id == SkuId);
            if (sku == null)
                return Json(new { success = false, message = "SKU không hợp lệ" });

            var existing = tempItems.FirstOrDefault(i => i.Sku_Id == sku.Id);
            if (existing != null)
                existing.Quantity += Quantity;
            else
                tempItems.Add(new Order_Item
                {
                    Sku_Id = sku.Id,
                    Product_Sku = sku,
                    Quantity = Quantity,
                    Price = sku.Price
                });

            Session["TempOrderItems"] = tempItems;

            // Trả về danh sách hiện tại để render bảng
            var result = tempItems.Select(i => new
            {
                i.Sku_Id,
                ProductTitle = i.Product_Sku.Product.Title,
                i.Product_Sku.Color,
                i.Product_Sku.Size,
                i.Quantity,
                i.Price,
                Total = i.Quantity * i.Price
            }).ToList();

            return Json(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateOrder(int User_Id, string Shipping_Address, string Status, int? Promo_Id)
        {
            try
            {
                var tempItems = Session["TempOrderItems"] as List<Order_Item>;
                if (tempItems == null || !tempItems.Any())
                    return Json(new { success = false, message = "Chưa có sản phẩm nào trong đơn." });

                var order = new Order
                {
                    User_Id = User_Id,
                    Shipping_Address = Shipping_Address,
                    Status = Status,
                    Promo_Id = Promo_Id,
                    Created_At = DateTime.Now,
                    Total_Amount = tempItems.Sum(i => i.Price * i.Quantity)
                };

                // Áp dụng khuyến mãi
                if (Promo_Id.HasValue)
                {
                    var promo = db.promotions.Find(Promo_Id.Value);
                    if (promo != null)
                    {
                        bool validDate = order.Created_At >= promo.Start_Date && order.Created_At <= promo.End_Date;
                        bool validMin = promo.Min_Order_Value == null || order.Total_Amount >= promo.Min_Order_Value;
                        bool validStatus = promo.Status != null && promo.Status.ToLower() == "active";

                        if (validDate && validMin && validStatus)
                        {
                            decimal discount = promo.Discount_Percentage.HasValue ?
                                order.Total_Amount * (promo.Discount_Percentage.Value / 100) :
                                promo.Discount_Amount ?? 0;

                            if (discount > order.Total_Amount)
                                discount = order.Total_Amount;

                            order.Total_Amount -= discount;

                            promo.Uses_Count += 1;
                            db.Entry(promo).State = System.Data.Entity.EntityState.Modified;
                        }
                        else
                        {
                            order.Promo_Id = null;
                        }
                    }
                }

                db.orders.Add(order);
                db.SaveChanges();

                foreach (var item in tempItems)
                {
                    item.Order_Id = order.Id;
                    item.Product_Sku = null;
                    db.order_items.Add(item);
                }

                db.SaveChanges();
                Session["TempOrderItems"] = null;

                return Json(new { success = true });
            }
            catch (DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => e.ErrorMessage);

                return Json(new { success = false, message = string.Join(" | ", errors) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }





        // GET: Orders/Edit/5


        [HttpPost]

        public JsonResult GetOrderDetails(int id)
        {
            var order = db.orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.Order_Items.Select(i => i.Product_Sku.Product))
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return Json(null);

            // Tính số tiền giảm
            decimal discount = 0;
            if (order.Promo_Id.HasValue && order.Promotion != null)
            {
                var promo = order.Promotion;
                var total = order.Order_Items.Sum(i => i.Price * i.Quantity);
                bool validDate = order.Created_At >= promo.Start_Date && order.Created_At <= promo.End_Date;
                bool validMinValue = promo.Min_Order_Value == null || order.Total_Amount >= promo.Min_Order_Value;
                bool validStatus = promo.Status != null && promo.Status.ToLower() == "active";

                if (validDate && validMinValue && validStatus)
                {
                    if (promo.Discount_Percentage.HasValue)
                        discount = total * (promo.Discount_Percentage.Value / 100);
                    else if (promo.Discount_Amount.HasValue)
                        discount = promo.Discount_Amount.Value;
                }
            }

            return Json(new
            {
                order.Id,
                User_Id = order.User_Id,
                UserName = order.User.Username,
                order.Status,
                order.Total_Amount,
                order.Shipping_Address,
                Created_At = order.Created_At.ToString("yyyy-MM-dd"),// cho input type=date
                Promo_Id = order.Promo_Id,
                PromoCode = order.Promotion != null ? order.Promotion.Promo_Code : null,
                Discount = discount,
                Items = order.Order_Items.Select(i => new
                {
                    Product = i.Product_Sku.Product.Title,
                    i.Product_Sku.Color,
                    i.Product_Sku.Size,
                    i.Quantity,
                    i.Price,
                    Total = i.Price * i.Quantity
                })
            });

        }

        [HttpGet]
        public JsonResult GetAllPromotions()
        {
            var promos = db.promotions
                .Select(p => new { p.Id, p.Promo_Code })
                .ToList();

            return Json(promos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllUsers()
        {
            var users = db.users
                .Where(u => u.Status == "Hoạt Động")
                .Select(u => new { u.Id, u.Username })
                .ToList();

            return Json(users, JsonRequestBehavior.AllowGet);
        }


        //        [HttpPost]
        //        public JsonResult UpdateOrder(int Id,int User_Id,string Shipping_Address,string Status,int? Promo_Id,DateTime Created_At
        //)
        //        {
        //            try
        //            {
        //                var existingOrder = db.orders
        //                    .Include(o => o.Order_Items)
        //                    .FirstOrDefault(o => o.Id == Id);

        //                if (existingOrder == null)
        //                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

        //                // Cập nhật thông tin cơ bản
        //                existingOrder.User_Id = User_Id;
        //                existingOrder.Shipping_Address = Shipping_Address;
        //                existingOrder.Status = Status;
        //                existingOrder.Promo_Id = Promo_Id;
        //                existingOrder.Created_At = Created_At;

        //                // Tính tổng tiền từ Order_Items
        //                decimal total = existingOrder.Order_Items.Sum(i => i.Price * i.Quantity);

        //                // Áp dụng promotion nếu có
        //                if (Promo_Id.HasValue)
        //                {
        //                    var promo = db.promotions.Find(Promo_Id.Value);
        //                    if (promo != null)
        //                    {
        //                        // So sánh ngày chỉ tính Date, bỏ giờ
        //                        var orderDate = Created_At.Date;
        //                        bool validDate = orderDate >= promo.Start_Date.Date && orderDate <= promo.End_Date.Date;
        //                        bool validMin = promo.Min_Order_Value == null || total >= promo.Min_Order_Value;
        //                        bool validStatus = promo.Status != null && promo.Status.Trim().ToLower() == "active";

        //                        if (validDate && validMin && validStatus)
        //                        {
        //                            decimal discount = promo.Discount_Percentage.HasValue
        //                                ? total * promo.Discount_Percentage.Value / 100m
        //                                : promo.Discount_Amount ?? 0;

        //                            if (discount > total)
        //                                discount = total;

        //                            total -= discount;

        //                            promo.Uses_Count += 1;
        //                            db.Entry(promo).State = System.Data.Entity.EntityState.Modified;
        //                        }
        //                        else
        //                        {
        //                            // Nếu promo không hợp lệ thì bỏ
        //                            existingOrder.Promo_Id = null;
        //                        }
        //                    }
        //                }

        //                existingOrder.Total_Amount = total;
        //                db.SaveChanges();

        //                return Json(new { success = true, message = "Cập nhật thành công" });
        //            }
        //            catch (Exception ex)
        //            {
        //                return Json(new { success = false, message = ex.Message });
        //            }
        //        }




        //        public ActionResult Edit(int? id)
        //        {
        //            if (id == null)
        //                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

        //            Order order = db.orders.Find(id);
        //            if (order == null)
        //                return HttpNotFound();



        //            return View(order);
        //        }




        // GET: Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = db.orders
                          .Include(o => o.Order_Items.Select(oi => oi.Product_Sku.Product))
                          .Include(o => o.Promotion)
                          .Include(o => o.User)
                          .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return HttpNotFound();

            return View();
        }

        // POST: Orders/DeleteOrder
        [HttpPost]
        public JsonResult DeleteOrder(int id)
        {
            try
            {
                var order = db.orders.FirstOrDefault(o => o.Id == id);

                if (order == null) return Json("Không tìm thấy đơn hàng");

                if (order.Order_Items.Any())
                    db.order_items.RemoveRange(order.Order_Items);

                db.orders.Remove(order);
                db.SaveChanges();

                return Json("Xóa thành công");
            }
            catch (System.Exception ex)
            {
                return Json("Lỗi hệ thống: " + ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
