using GG_Shop_v3.Models;
using System;
using System.Linq;
using System.Web.Mvc;

public class CartController : Controller
{
    private DataContext db = new DataContext();

    // GET: /Cart/Index
    public ActionResult Index()
    {
        return View();
    }

    // Lấy giỏ hàng 
    private Cart GetUserCart()
    {
        if (Session["UserId"] == null) return null;
        int userId = (int)Session["UserId"];

        var cart = db.carts
            .Include("Cart_Items.Product_Sku.Product.Product_Images")
            .FirstOrDefault(c => c.User_Id == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                User_Id = userId,
                Created_At = DateTime.Now
            };
            db.carts.Add(cart);
            db.SaveChanges();
        }

        return cart;
    }

    // lấy dữ liệu giỏ hàng
    public JsonResult GetCart()
    {
        var cart = GetUserCart();
        if (cart == null)
            return Json(new { success = true, items = new object[0], total = 0 }, JsonRequestBehavior.AllowGet);

        var items = cart.Cart_Items.Select(i => new
        {
            Id = i.Id,
            Name = i.Product_Sku.Product.Title,
            Price = i.Product_Sku.Price,
            Quantity = i.Quantity,
            Color = i.Product_Sku.Color,
            Size = i.Product_Sku.Size,
            Image = i.Product_Sku.Product.Product_Images.FirstOrDefault(img => img.Is_Main).Image_Url
        }).ToList();

        var total = items.Sum(x => x.Price * x.Quantity);

        return Json(new { success = true, items = items, total = total }, JsonRequestBehavior.AllowGet);
    }

    // cập nhật số lượng
    [HttpPost]
    public JsonResult UpdateQty(int id, int qty)
    {
        var item = db.cart_items.Find(id);
        if (item != null)
        {
            item.Quantity = qty;
            db.SaveChanges();
        }
        return Json(new { success = true });
    }

    // xoá
    [HttpPost]
    public JsonResult DeleteItem(int id)
    {
        var item = db.cart_items.Find(id);
        if (item != null)
        {
            db.cart_items.Remove(item);
            db.SaveChanges();
        }
        return Json(new { success = true });
    }

    [HttpPost]
    public JsonResult ApplyPromotion(string code)
    {
        var promo = db.promotions
                      .FirstOrDefault(x => x.Promo_Code == code
                                       && x.Start_Date <= DateTime.Now
                                       && x.End_Date >= DateTime.Now
                                       && x.Status == "active");

        if (promo == null)
        {
            return Json(new { success = false, message = "Mã giảm giá không hợp lệ!" });
        }

        // Lấy giỏ hàng
        var userId = (int)Session["User_Id"];
        var cart = db.carts.FirstOrDefault(c => c.User_Id == userId);

        if (cart == null)
            return Json(new { success = false, message = "Giỏ hàng trống!" });

        var total = cart.Cart_Items.Sum(i => i.Quantity * i.Product_Sku.Price);

        decimal discount = 0;

        if (promo.Type == "percent")
            discount = (decimal)(total * promo.Discount_Percentage / 100);

        if (promo.Type == "amount")
            discount = promo.Discount_Amount ?? 0;

        return Json(new { success = true, discount = discount });
    }
}
