using GG_Shop_v3.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace GG_Shop_v3.Controllers // Kiểm tra lại namespace cho đúng với project của bạn
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
                    .Where(c => c.Products.Any()) // 🔥 CHỈ LẤY CATEGORY CÓ SẢN PHẨM
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
                    .OrderByDescending(p => p.Id) // Lấy sản phẩm mới nhất
                    .Take(12) // Chỉ lấy 12 sản phẩm để load nhanh
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Title, // Hoặc p.Name tùy model của bạn
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
    }
}