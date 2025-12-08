using GG_Shop_v3.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

public class CartController : Controller
{
    private DataContext db = new DataContext();

    // VIEW CART
    public ActionResult Index()
    {

        if (Session["User_Id"] == null)
        {
            Session["ReturnUrl"] = Url.Action("Index", "Cart");
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    // Lấy user id từ Session
    private int? CurrentUserId
    {
        get
        {
            if (Session["User_Id"] == null) return null;
            int uid;
            return int.TryParse(Session["User_Id"].ToString(), out uid) ? (int?)uid : null;
        }
    }

    // Lấy giỏ hàng 
    private Cart GetUserCart(int userId)
    {
        return db.carts
            .Include(c => c.Cart_Items)
            .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku))
            .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku.Product))
            .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku.Product.Product_Images))
            .FirstOrDefault(c => c.User_Id == userId);
    }

    // GET CART JSON
    public JsonResult GetCart()
    {
        try
        {
            var uid = CurrentUserId;
            if (uid == null)
                return Json(new { success = false, msg = "NOT_LOGIN", items = new object[0], total = 0 }, JsonRequestBehavior.AllowGet);

            var cart = GetUserCart(uid.Value);

            if (cart == null || !cart.Cart_Items.Any())
                return Json(new { success = true, items = new object[0], total = 0 }, JsonRequestBehavior.AllowGet);

            var items = cart.Cart_Items.Select(ci =>
            {
                var mainImg = ci.Product_Sku?.Product?.Product_Images?.FirstOrDefault(i => i.Is_Main == true)?.Image_Url
                              ?? ci.Product_Sku?.Product?.Product_Images?.FirstOrDefault()?.Image_Url;

                string imageUrl = Url.Content("~/uploads/products/no-image.png");

                if (!string.IsNullOrEmpty(mainImg))
                {
                    mainImg = mainImg.Trim();

                    if (mainImg.StartsWith("http") || mainImg.StartsWith("/"))
                        imageUrl = mainImg;
                    else
                        imageUrl = Url.Content("~/uploads/products/" + mainImg);
                }

                return new
                {
                    Id = ci.Id,
                    Quantity = ci.Quantity,
                    Product = new
                    {
                        Name = ci.Product_Sku?.Product?.Title ?? "",
                        Price = ci.Product_Sku?.Price ?? 0,
                        Color = ci.Product_Sku?.Color,
                        Size = ci.Product_Sku?.Size,
                        Image = imageUrl
                    }
                };
            }).ToList();

            var total = items.Sum(x => x.Product.Price * x.Quantity);

            return Json(new { success = true, items = items, total = total }, JsonRequestBehavior.AllowGet);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
        }
    }

    // UPDATE QTY
    [HttpPost]
    public JsonResult UpdateQuantity(int id, int qty)
    {
        try
        {
            var item = db.cart_items.Find(id);
            if (item == null) return Json(new { success = false, msg = "Không tìm thấy sản phẩm" });

            if (qty <= 0) qty = 1;
            item.Quantity = qty;
            db.SaveChanges();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, msg = ex.Message });
        }
    }

    // DELETE ITEM
    [HttpPost]
    public JsonResult DeleteItem(int id)
    {
        try
        {
            var item = db.cart_items.Find(id);
            if (item == null) return Json(new { success = false, msg = "Không tìm thấy sản phẩm" });

            db.cart_items.Remove(item);
            db.SaveChanges();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, msg = ex.Message });
        }
    }

    // APPLY PROMO (giảm ở giỏ – không lưu vào order)
    [HttpPost]
    public JsonResult ApplyPromotion(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { success = false, msg = "Vui lòng nhập mã." });

        var promo = db.promotions.FirstOrDefault(p => p.Promo_Code.ToUpper() == code.ToUpper());
        if (promo == null)
            return Json(new { success = false, msg = "Mã giảm giá không hợp lệ." });

        var uid = CurrentUserId;
        if (uid == null) return Json(new { success = false, msg = "Bạn chưa đăng nhập." });

        var cart = GetUserCart(uid.Value);
        if (cart == null || !cart.Cart_Items.Any())
            return Json(new { success = false, msg = "Giỏ hàng trống." });

        decimal subtotal = cart.Cart_Items.Sum(c => c.Quantity * c.Product_Sku.Price);
        decimal discount = 0;

        string type = promo.Type?.ToUpper() ?? "";

        if (type.Contains("PERCENT"))
            discount = subtotal * (promo.Discount_Percentage ?? 0) / 100m;
        else if (type.Contains("AMOUNT"))
            discount = promo.Discount_Amount ?? 0;

        return Json(new { success = true, discount = discount, code = promo.Promo_Code });
    }

    // PLACE ORDER 
    [HttpPost]
    public JsonResult PlaceOrder(string shippingAddress, string promoCode)
    {
        try
        {
            var uid = CurrentUserId;
            if (uid == null) return Json(new { success = false, msg = "Bạn chưa đăng nhập." });

            var cart = GetUserCart(uid.Value);
            if (cart == null || !cart.Cart_Items.Any())
                return Json(new { success = false, msg = "Giỏ hàng trống." });

            // Tổng tiền (KHÔNG ÁP PROMO)
            decimal total = cart.Cart_Items.Sum(i => i.Product_Sku.Price * i.Quantity);

            // Tạo đơn
            var order = new Order
            {
                User_Id = uid.Value,
                Shipping_Address = shippingAddress ?? "",
                Total_Amount = total,
                Created_At = DateTime.Now,
                Status = "Đang xử lý"
            };

            db.orders.Add(order);
            db.SaveChanges();

            // ORDER ITEMS + trừ tồn kho
            foreach (var item in cart.Cart_Items.ToList())
            {
                var sku = db.product_skus.FirstOrDefault(s => s.Id == item.Sku_Id);

                db.order_items.Add(new Order_Item
                {
                    Order_Id = order.Id,
                    Sku_Id = item.Sku_Id,
                    Quantity = item.Quantity,
                    Price = sku?.Price ?? 0
                });

                if (sku != null)
                    sku.Quantity = Math.Max(0, sku.Quantity - item.Quantity);
            }

            // Clear cart
            db.cart_items.RemoveRange(cart.Cart_Items);
            db.SaveChanges();

            // CHUYỂN TỚI TRANG ĐẶT HÀNG 
            string redirectUrl = Url.Action("Index", "U_Orders");
            if (string.IsNullOrWhiteSpace(redirectUrl))
                redirectUrl = "/U_Orders/Index";

            return Json(new { success = true, redirectUrl = redirectUrl });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, msg = ex.Message });
        }
    }
}
