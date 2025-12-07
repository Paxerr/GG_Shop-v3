using GG_Shop_v3.Models;
using System.Linq;
using System.Web.Mvc;

public class CartController : Controller
{
    private DataContext db = new DataContext();

    private Cart GetUserCart()
    {
        if (Session["UserId"] == null) return null;

        int userId = (int)Session["UserId"];

        return db.carts
            .Include("Cart_Items.Product_Sku")
            .Include("Cart_Items.Product_Sku.Product")
            .Include("Cart_Items.Product_Sku.Product.Product_Images")
            .FirstOrDefault(x => x.User_Id == userId);
    }

    public ActionResult Index()
    {
        return View();
    }

    public JsonResult GetCart()
    {
        var cart = GetUserCart();
        if (cart == null)
        {
            return Json(new { success = false, items = new object[0], total = 0 },
                        JsonRequestBehavior.AllowGet);
        }

        var items = cart.Cart_Items.Select(i => new
        {
            Id = i.Id,

            Product = new
            {
                Name = i.Product_Sku.Product.Title,
                Price = i.Product_Sku.Price,
                Color = i.Product_Sku.Color,
                Size = i.Product_Sku.Size,
                Image = i.Product_Sku.Product.Product_Images
                        .FirstOrDefault(img => img.Is_Main == true)?.Image_Url
            },

            Quantity = i.Quantity
        }).ToList();

        var total = items.Sum(x => x.Product.Price * x.Quantity);

        return Json(new { success = true, items = items, total = total },
                    JsonRequestBehavior.AllowGet);
    }

    [HttpPost]
    public JsonResult UpdateQuantity(int id, int qty)
    {
        var item = db.cart_items.Find(id);

        if (item == null)
            return Json("Sản phẩm không tồn tại");

        item.Quantity = qty;
        db.SaveChanges();

        return Json("OK");
    }

    [HttpPost]
    public JsonResult DeleteItem(int id)
    {
        var item = db.cart_items.Find(id);
        if (item == null) return Json("Không tìm thấy");

        db.cart_items.Remove(item);
        db.SaveChanges();

        return Json("Đã xóa");
    }
}
