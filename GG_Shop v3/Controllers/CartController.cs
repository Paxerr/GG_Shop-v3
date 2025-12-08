using GG_Shop_v3.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

public class CartController : Controller
{
    private DataContext db = new DataContext();

    // VIEW INDEX - giữ session test ở đây chỉ khi bạn muốn test nhanh.
    public ActionResult Index()
    {
        //Session["UserId"] = 6; //DÙNG ĐỂ TEST

        // Nếu chưa login, redirect sang login, ( NẾU TEST THÌ COMMENT CÁI NÀY LẠI)
        if (Session["UserId"] == null)
        {
             Session["ReturnUrl"] = Url.Action("Index", "Cart");
             return RedirectToAction("Login", "Account");
        }

        return View();
    }

    private int? CurrentUserId
    {
        get
        {
            if (Session["UserId"] == null) return null;
            int tmp;
            return int.TryParse(Session["UserId"].ToString(), out tmp) ? (int?)tmp : null;
        }
    }

    // HELP: lấy giỏ hàng của user theo Session + Include đầy đủ
    private Cart GetUserCart(int userId)
    {
        // load đầy đủ liên quan, tránh lazy-loading null
        return db.carts
            .Include(c => c.Cart_Items)
            .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku))
            .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku.Product))
            .Include(c => c.Cart_Items.Select(ci => ci.Product_Sku.Product.Product_Images))
            .FirstOrDefault(c => c.User_Id == userId);
    }

    // JSON API
    public JsonResult GetCart()
    {
        try
        {
            var uid = CurrentUserId;
            if (uid == null)
            {
                // Trả về rõ ràng cho client biết chưa login
                return Json(new { success = false, msg = "NOT_LOGIN", items = new object[0], total = 0 }, JsonRequestBehavior.AllowGet);
            }

            var cart = GetUserCart(uid.Value);

            if (cart == null || cart.Cart_Items == null || !cart.Cart_Items.Any())
            {
                return Json(new { success = true, items = new object[0], total = 0 }, JsonRequestBehavior.AllowGet);
            }

            var items = cart.Cart_Items.Select(ci =>
            {
                // Lấy ảnh chính hoặc ảnh đầu tiên
                var mainImg = ci.Product_Sku?.Product?.Product_Images?.FirstOrDefault(i => i.Is_Main == true)?.Image_Url
                              ?? ci.Product_Sku?.Product?.Product_Images?.FirstOrDefault()?.Image_Url;

                // Normalize image URL -> trả về site-root-relative hoặc absolute
                string imageUrl = Url.Content("~/uploads/products/no-image.png"); // default fallback (đảm bảo file exist)
                if (!string.IsNullOrEmpty(mainImg))
                {
                    mainImg = mainImg.Trim();
                    // nếu bắt đầu bằng http/https hoặc / -> dùng nguyên
                    if (mainImg.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || mainImg.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        || mainImg.StartsWith("/"))
                    {
                        imageUrl = mainImg;
                    }
                    else
                    {
                        imageUrl = Url.Content("~/uploads/products/") + mainImg;
                    }
                }

                return new
                {
                    Id = ci.Id,
                    Quantity = ci.Quantity,
                    Product = new
                    {
                        Name = ci.Product_Sku?.Product?.Title ?? "",
                        Price = ci.Product_Sku != null ? ci.Product_Sku.Price : 0,
                        Color = ci.Product_Sku?.Color,
                        Size = ci.Product_Sku?.Size,
                        Image = imageUrl
                    }
                };
            }).ToList();

            var total = items.Sum(x => (decimal)x.Product.Price * x.Quantity);

            return Json(new { success = true, items = items, total = total }, JsonRequestBehavior.AllowGet);
        }
        catch (Exception ex)
        {
            // Trả lỗi có ích cho debug (cẩn thận khi deploy production)
            return Json(new { success = false, msg = ex.Message, items = new object[0], total = 0 }, JsonRequestBehavior.AllowGet);
        }
    }

    // Update số lượng
    [HttpPost]
    public JsonResult UpdateQuantity(int id, int qty)
    {
        try
        {
            var item = db.cart_items.Find(id);
            if (item == null) return Json(new { success = false, msg = "Sản phẩm không tồn tại" });

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

    // Xóa item
    [HttpPost]
    public JsonResult DeleteItem(int id)
    {
        try
        {
            var item = db.cart_items.Find(id);
            if (item == null) return Json(new { success = false, msg = "Không tìm thấy" });

            db.cart_items.Remove(item);
            db.SaveChanges();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, msg = ex.Message });
        }
    }

    // Áp mã giảm giá (trả discount và text, code)
    [HttpPost]
    public JsonResult ApplyPromotion(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { success = false, msg = "Vui lòng nhập mã." });

        var promo = db.promotions.FirstOrDefault(p =>
            (p.Promo_Code != null && p.Promo_Code.ToUpper() == code.ToUpper())
        );

        if (promo == null) return Json(new { success = false, msg = "Mã không hợp lệ hoặc đã hết hạn." });

        var uid = CurrentUserId;
        if (uid == null) return Json(new { success = false, msg = "Bạn chưa đăng nhập." });

        var cart = GetUserCart(uid.Value);
        if (cart == null || cart.Cart_Items == null || !cart.Cart_Items.Any())
            return Json(new { success = false, msg = "Giỏ hàng trống." });

        decimal subtotal = cart.Cart_Items.Sum(i => i.Quantity * i.Product_Sku.Price);
        decimal discount = 0;
        string text = "";
        string type = (promo.Type ?? "").ToString().ToUpper();

        if (type.Contains("PERCENT"))
        {
            var pct = (promo.Discount_Percentage ?? 0);
            discount = subtotal * pct / 100m;
            text = "-" + pct.ToString("0.#") + "%";
        }
        else if (type.Contains("AMOUNT"))
        {
            discount = (promo.Discount_Amount ?? 0);
            text = "-" + discount.ToString("N0") + "₫";
        }
        else if (type.Contains("FREESHIP"))
        {
            text = "Miễn phí vận chuyển";
        }

        return Json(new { success = true, discount = discount, text = text, code = (promo.Promo_Code) });
    }

    // PlaceOrder: tạo order, trừ tồn kho, xóa cart_items, trả redirectUrl
    [HttpPost]
    public JsonResult PlaceOrder(string shippingAddress, string promoCode)
    {
        try
        {
            var uid = CurrentUserId;
            if (uid == null) return Json(new { success = false, msg = "Bạn chưa đăng nhập." });

            var cart = GetUserCart(uid.Value);
            if (cart == null || !cart.Cart_Items.Any()) return Json(new { success = false, msg = "Giỏ hàng trống." });

            decimal subtotal = cart.Cart_Items.Sum(i => i.Quantity * i.Product_Sku.Price);
            decimal discount = 0;
            Promotion promo = null;

            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                promo = db.promotions.FirstOrDefault(p =>
                    (p.Promo_Code != null && p.Promo_Code.ToUpper() == promoCode.ToUpper())
                );
                if (promo != null)
                {
                    string type = (promo.Type ?? "").ToString().ToUpper();
                    if (type.Contains("PERCENT"))
                    {
                        var pct = (promo.Discount_Percentage ?? 0);
                        discount = subtotal * pct / 100m;
                    }
                    else if (type.Contains("AMOUNT"))
                    {
                        discount = (promo.Discount_Amount ?? 0);
                    }
                }
            }

            decimal totalAmount = Math.Max(subtotal - discount, 0);

            var order = new Order
            {
                User_Id = uid.Value,
                Total_Amount = totalAmount,
                Status = "pending",
                Shipping_Address = shippingAddress,
                Promo_Id = promo?.Id ?? (int?)null,
                Created_At = DateTime.Now
            };
            db.orders.Add(order);
            db.SaveChanges();

            foreach (var item in cart.Cart_Items.ToList())
            {
                if (item.Product_Sku.Quantity < item.Quantity)
                    return Json(new { success = false, msg = "Sản phẩm " + item.Product_Sku.Product.Title + " không đủ số lượng." });

                var orderItem = new Order_Item
                {
                    Order_Id = order.Id,
                    Sku_Id = item.Sku_Id,
                    Quantity = item.Quantity,
                    Price = item.Product_Sku.Price
                };
                db.order_items.Add(orderItem);

                item.Product_Sku.Quantity -= item.Quantity;
                db.Entry(item.Product_Sku).State = EntityState.Modified;
            }

            db.cart_items.RemoveRange(cart.Cart_Items);
            db.SaveChanges();

            string redirectUrl = Url.Action("Detail", "Order", new { id = order.Id });
            if (string.IsNullOrWhiteSpace(redirectUrl)) redirectUrl = "/Order/Detail/" + order.Id;

            return Json(new { success = true, redirectUrl = redirectUrl });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, msg = "Lỗi khi đặt hàng: " + ex.Message });
        }
    }
}
