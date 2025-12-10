using GG_Shop_v3.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace GG_Shop_v3.Controllers
{
    public class ProductsAjaxController : Controller
    {
        DataContext db = new DataContext();

        // Load trang chi tiết sản phẩm
        public ActionResult Index(int id)
        {
            ViewBag.ProductId = id;
            return View();
        }

        // API lấy thông tin sản phẩm
        public JsonResult GetProduct(int id)
        {
            var product = db.products
                .Include(p => p.Product_Images)
                .Include(p => p.Product_Sku)
                .Include(p => p.Category)
                .FirstOrDefault(x => x.Id == id);

            if (product == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            var result = new
            {
                product.Id,
                product.Title,
                product.Description,

                Images = product.Product_Images.Select(i => new {
                    i.Image_Url,
                    i.Is_Main
                }),

                Skus = product.Product_Sku.Select(s => new {
                    s.Id,
                    s.Size,
                    s.Color,
                    s.Price,
                    s.Quantity
                }),

                Category = product.Category.Name
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // API lấy sản phẩm cùng loại
        public JsonResult GetRelated(int id)
        {
            var product = db.products.Find(id);
            if (product == null)
                return Json(new { }, JsonRequestBehavior.AllowGet);

            var related = db.products
                .Include(p => p.Product_Images)
               
                .Where(x => x.Category_Id == product.Category_Id && x.Id != id)
                .Take(4)
                .ToList()
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                
                    Image = x.Product_Images.FirstOrDefault(i => i.Is_Main).Image_Url,
                    
                });

            return Json(related, JsonRequestBehavior.AllowGet);
        }
    }
}
