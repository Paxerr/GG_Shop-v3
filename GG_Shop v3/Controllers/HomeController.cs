using GG_Shop_v3.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.Ajax.Utilities;

namespace GG_Shop_v3.Controllers
{
    public class HomeController : Controller
    {
        private DataContext db = new DataContext();

        public ActionResult Index()
        {
            return View();
        }

        // 1. LẤY DANH MỤC (Chỉ lấy Id và Name)
        [HttpGet]
        public JsonResult GetCategoriesWithProducts()
        {
            try
            {
                var categories = db.categories
                    .Where(c => c.Products.Any()) //CHỈ LẤY CATEGORY CÓ SẢN PHẨM
                    .Select(c => new
                    {
                        c.Id,
                        c.Name
                    })
                    .ToList();

                return Json(categories, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        // 2. LẤY TẤT CẢ SẢN PHẨM (Cho trang chủ)
        [HttpGet]
        public JsonResult GetHomeProducts()
        {
            try
            {
                var products = db.products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.Id)
                    .Take(12) 
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Title,
                        Price = p.Product_Sku.OrderBy(s => s.Price).Select(s => s.Price).FirstOrDefault(),
                        MainImg = p.Product_Images.Where(i => i.Is_Main == true).Select(i => i.Image_Url).FirstOrDefault() ?? "/Content/img/no-image.jpg"
                    })
                    .ToList();

                return Json(products, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 3. LỌC SẢN PHẨM THEO DANH MỤC
        [HttpGet]
        public JsonResult GetProductsByCategory(int categoryId)
        {
            try
            {
                var products = db.products
                .Where(p => p.Category.Id == categoryId)
                .OrderByDescending(p => p.Id)
                .Take(5) // lấy 5 sản phẩm
                .Select(p => new {
                    Id = p.Id,
                    Name = p.Title,
                    Price = p.Product_Sku.OrderBy(s => s.Price).Select(s => s.Price).FirstOrDefault(),
                    MainImg = p.Product_Images.Where(i => i.Is_Main).Select(i => i.Image_Url).FirstOrDefault()
                })
                .ToList();


                return Json(products, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpGet]
        public JsonResult GetCartItem()
        {
            try
            {
                if (Session == null || Session["User_Id"] == null)
                {
                    return Json(new { total = 0 }, JsonRequestBehavior.AllowGet);
                }

                int userId;
                if (!int.TryParse(Session["User_Id"].ToString(), out userId))
                {
                    return Json(new { total = 0 }, JsonRequestBehavior.AllowGet);
                }
                var cart = db.carts.FirstOrDefault(c => c.User_Id == userId);
                if (cart == null)
                {
                    return Json(new { total = 0 }, JsonRequestBehavior.AllowGet);
                }
                int total = db.cart_items
                    .Where(ci => ci.Cart_Id == cart.Id)
                    .Select(ci => (int?)ci.Quantity)
                    .DefaultIfEmpty(0)
                    .Sum() ?? 0;

                return Json(new { total = total }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { total = 0, error = "Server error" }, JsonRequestBehavior.AllowGet);
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}