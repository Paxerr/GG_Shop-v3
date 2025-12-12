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

            // ngay sau khi lấy uid và cart
            // Nếu có promo trong session, validate lại: nếu hết hạn -> xóa session
            if (Session["Promo_Id"] != null)
            {
                int promoId;
                if (int.TryParse(Session["Promo_Id"].ToString(), out promoId))
                {
                    var sessionPromo = db.promotions.Find(promoId);
                    if (sessionPromo == null)
                    {
                        Session.Remove("Promo_Id"); Session.Remove("Promo_Code"); Session.Remove("Promo_Discount");
                    }
                    else
                    {
                        // nếu promo quá hạn thì mark và xóa session
                        if (!IsPromoValid(sessionPromo))
                        {
                            MarkPromoAsExpiredIfNeeded(sessionPromo);
                            Session.Remove("Promo_Id"); Session.Remove("Promo_Code"); Session.Remove("Promo_Discount");
                        }
                    }
                }
                else
                {
                    Session.Remove("Promo_Id"); Session.Remove("Promo_Code"); Session.Remove("Promo_Discount");
                }
            }

            var cart = GetUserCart(uid.Value);

            if (cart == null || !cart.Cart_Items.Any())
                return Json(new { success = true, items = new object[0], total = 0 }, JsonRequestBehavior.AllowGet);

            var items = cart.Cart_Items.Select(ci =>
            {
                var mainImg = ci.Product_Sku?.Product?.Product_Images?.FirstOrDefault(i => i.Is_Main)?.Image_Url
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
                        Name = ci.Product_Sku.Product.Title,
                        Price = ci.Product_Sku.Price,
                        Color = ci.Product_Sku.Color,
                        Size = ci.Product_Sku.Size,
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
            if (item == null)
                return Json(new { success = false, msg = "Không tìm thấy sản phẩm" });

            if (qty <= 0) qty = 1;

            // Lấy SKU tương ứng
            var sku = db.product_skus.Find(item.Sku_Id);
            if (sku == null)
                return Json(new { success = false, msg = "Sản phẩm không tồn tại." });

            // KIỂM TRA TỒN KHO
            if (qty > sku.Quantity)
                return Json(new { success = false, msg = "Số lượng vượt quá tồn kho." });

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

    // APPLY PROMO
    [HttpPost]
    public JsonResult ApplyPromotion(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { success = false, msg = "Vui lòng nhập mã." });

        // tìm promo 
        var promo = db.promotions.FirstOrDefault(p => p.Promo_Code.ToUpper() == code.ToUpper());
        if (promo == null)
            return Json(new { success = false, msg = "Mã giảm giá không hợp lệ." });

        // nếu promo đã quá hạn -> đánh dấu inactive và trả lỗi
        MarkPromoAsExpiredIfNeeded(promo);

        // load lại mã trong th thay đổi
        var status = (promo.Status ?? "").Trim().ToLower();
        if (!(status == "hoạt động"))
            return Json(new { success = false, msg = "Mã giảm giá hiện không hoạt động." });


        var now = DateTime.Now;
        if (promo.Start_Date > now)
            return Json(new { success = false, msg = "Mã giảm giá chưa bắt đầu." });
        if (promo.End_Date < now)
        {
            // double-check 
            promo.Status = "Không hoạt động";
            db.Entry(promo).State = EntityState.Modified;
            db.SaveChanges();
            return Json(new { success = false, msg = "Mã giảm giá đã hết hạn." });
        }

        var uid = CurrentUserId;
        if (uid == null)
            return Json(new { success = false, msg = "Bạn chưa đăng nhập." });

        bool usedBefore = db.orders.Any(o =>
            o.User_Id == uid.Value &&
            o.Promo_Id == promo.Id &&
            o.Status != "Đã hủy"
        );

        if (usedBefore)
        {
            return Json(new { success = false, msg = "Bạn đã sử dụng mã này rồi." });
        }

        var cart = GetUserCart(uid.Value);
        if (cart == null || !cart.Cart_Items.Any())
            return Json(new { success = false, msg = "Giỏ hàng trống." });

        decimal subtotal = cart.Cart_Items.Sum(c => c.Quantity * c.Product_Sku.Price);

        // kiểm tra min order value nếu có
        if (promo.Min_Order_Value.HasValue && subtotal < promo.Min_Order_Value.Value)
        {
            return Json(new { success = false, msg = $"Mã này yêu cầu đơn hàng tối thiểu {promo.Min_Order_Value.Value:N0}₫." });
        }

        decimal discount = 0;
        var type = (promo.Type ?? "").Trim().ToLower();

        if (type.Contains("%") || type.Contains("giảm theo %") || type.Contains("phần trăm"))
        {
            discount = subtotal * (promo.Discount_Percentage ?? 0) / 100m;
        }
        else if (type.Contains("tiền") || type.Contains("giảm theo tiền") || type.Contains("amount"))
        {
            discount = promo.Discount_Amount ?? 0m;
        }

        // LƯU VÀO SESSION: id, code, discount
        Session["Promo_Id"] = promo.Id;
        Session["Promo_Code"] = promo.Promo_Code;
        Session["Promo_Discount"] = discount;

        return Json(new
        {
            success = true,
            discount = discount,
            code = promo.Promo_Code
        });
    }

    // Kiểm tra promo còn hiệu lực 
    private bool IsPromoValid(Promotion promo)
    {
        if (promo == null) return false;

        var status = (promo.Status ?? "").Trim().ToLower();

        // Accept both Vietnamese and English
        if (!(status == "hoạt động"))
            return false;

        var now = DateTime.Now;
        if (promo.Start_Date > now) return false;
        if (promo.End_Date < now) return false;

        return true;
    }

    // Nếu promo đã quá hạn, cập nhật trạng thái trong DB 
    private void MarkPromoAsExpiredIfNeeded(Promotion promo)
    {
        if (promo == null) return;
        var now = DateTime.Now;
        if (promo.End_Date < now && !promo.Status.Equals("ngừng hoạt động", StringComparison.OrdinalIgnoreCase))
        {
            promo.Status = "Ngừng hoạt động";
            db.Entry(promo).State = EntityState.Modified;
            db.SaveChanges();
        }
    }

    // PLACE ORDER 
    [HttpPost]
    public JsonResult PlaceOrder()
    {
        var uid = CurrentUserId;
        if (uid == null)
            return Json(new { success = false, msg = "Bạn chưa đăng nhập." });

        var cart = GetUserCart(uid.Value);
        if (cart == null || !cart.Cart_Items.Any())
            return Json(new { success = false, msg = "Giỏ hàng trống." });

        return Json(new { success = true, redirectUrl = Url.Action("Index", "U_Orders") });
    }

    [HttpPost]
    public JsonResult AddToCart(int skuId, int qty = 1)
    {
        try
        {
            var uid = CurrentUserId;
            if (uid == null)
                return Json(new { success = false, msg = "Bạn chưa đăng nhập." });
            // Nếu người dùng KHÔNG nhập mã ở giỏ hàng → xóa session mã giảm giá
            if (Request["promo"] == null && (Session["Promo_Code"] != null))
            {
                Session.Remove("Promo_Id");
                Session.Remove("Promo_Code");
                Session.Remove("Promo_Discount");
            }

            if (qty <= 0) qty = 1;

            // kiểm tra sku
            var sku = db.product_skus.Find(skuId);
            if (sku == null)
                return Json(new { success = false, msg = "Sản phẩm không tồn tại." });

            // lấy hoặc tạo cart của user
            var cart = db.carts.FirstOrDefault(c => c.User_Id == uid.Value);
            if (cart == null)
            {
                cart = new Cart
                {
                    User_Id = uid.Value,
                    Created_At = DateTime.Now
                };
                db.carts.Add(cart);
                db.SaveChanges(); // cần để có cart.Id
            }

            // kiểm tra tồn kho 
            var existing = db.cart_items.FirstOrDefault(ci => ci.Cart_Id == cart.Id && ci.Sku_Id == skuId);
            int newQty = qty;
            if (existing != null)
                newQty = existing.Quantity + qty;

            if (sku.Quantity < newQty)
            {
                return Json(new { success = false, msg = "Số lượng trong kho không đủ." });
            }

            if (existing != null)
            {
                existing.Quantity = newQty;
                db.Entry(existing).State = EntityState.Modified;
            }
            else
            {
                var item = new Cart_Item
                {
                    Cart_Id = cart.Id,
                    Sku_Id = skuId,
                    Quantity = qty
                };
                db.cart_items.Add(item);
            }

            db.SaveChanges();

            return Json(new { success = true, msg = "Đã thêm vào giỏ hàng." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, msg = ex.Message });
        }
    }

}