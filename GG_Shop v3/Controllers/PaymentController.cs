using GG_Shop_v3.Models;
using System.Linq;
using System.Web.Mvc;

public class PaymentController : Controller
{
    private DataContext db = new DataContext();

    // --- LOAD VIEW PAYMENT ---
    public ActionResult Index()
    {
        return View();
    }

    // --- LẤY GIỎ HÀNG NGƯỜI DÙNG ---
    private Cart GetUserCart()
    {
        if (Session["UserId"] == null) return null;

        int userId = (int)Session["UserId"];

        return db.carts
            .Include("Cart_Items.Product_Sku")
            .Include("Cart_Items.Product_Sku.Product")
            .Include("Cart_Items.Product_Sku.Product.Product_Images")
            .FirstOrDefault(c => c.User_Id == userId);
    }

    // --- LOAD CART CHO PAYMENT ---
    public JsonResult LoadPaymentCart()
    {
        var cart = GetUserCart();
        if (cart == null)
        {
            return Json(new
            {
                success = false,
                items = new object[0],
                total = 0
            }, JsonRequestBehavior.AllowGet);
        }

        var items = cart.Cart_Items.Select(i => new
        {
            Name = i.Product_Sku.Product.Title,
            Price = i.Product_Sku.Price,
            Quantity = i.Quantity
        }).ToList();

        var total = items.Sum(x => x.Price * x.Quantity);

        return Json(new { success = true, items = items, total = total },
            JsonRequestBehavior.AllowGet);
    }

    // --- ÁP MÃ GIẢM GIÁ ---
    [HttpPost]
    public JsonResult ApplyPromotion(string code)
    {
        var promo = db.promotions.FirstOrDefault(p => p.Promo_Code == code);

        if (promo == null)
            return Json(new { success = false, msg = "Mã không hợp lệ!" });

        var cart = GetUserCart();
        var subtotal = cart.Cart_Items.Sum(i => i.Quantity * i.Product_Sku.Price);

        decimal discount = 0;
        string text = "";

        if (promo.Type == "PERCENT")
        {
            discount = subtotal * (promo.Discount_Percentage ?? 0) / 100;
            text = "-" + promo.Discount_Percentage + "%";
        }
        else if (promo.Type == "AMOUNT")
        {
            discount = promo.Discount_Amount ?? 0;
            text = "-" + discount.ToString("N0") + "₫";
        }
        else if (promo.Type == "FREESHIP")
        {
            text = "Miễn phí vận chuyển";
        }

        return Json(new
        {
            success = true,
            discount = discount,
            text = text
        });
    }
}
