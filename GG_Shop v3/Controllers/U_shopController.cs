using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GG_Shop_v3.Models;

namespace GG_Shop_v3.Controllers
{
    public class U_shopController : Controller

    {
        private DataContext db = new DataContext();

        // GET: UShop
        public ActionResult Index()
        {
            return View();
        }

        // Lấy danh mục
        public JsonResult GetCategories()
        {
            var categories = db.categories
                               .Select(c => new {
                                   c.Id,
                                   c.Name
                               }).ToList();
            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        // Lấy sản phẩm

        public JsonResult GetProducts(int? categoryId, int? minPrice, int? maxPrice, string size, string keyword)
        {
            var query = db.products
                .Include(p => p.Category)
                .Include(p => p.Product_Sku)
                .Include(p => p.Product_Images)
                .Where(p => p.Status == "Đang bán");

            // FILTER CATEGORY
            if (categoryId != null)
            {
                query = query.Where(p => p.Category_Id == categoryId);
            }

            // FILTER PRICE RANGE
            if (minPrice != null && maxPrice != null)
            {
                query = query.Where(p => p.Product_Sku.Any(s =>
                    s.Price >= minPrice && s.Price <= maxPrice
                ));
            }

            // FILTER SIZE
            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(p => p.Product_Sku.Any(s => s.Size == size));
            }

            // 🔍 FILTER SEARCH KEYWORD
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.Title.Contains(keyword));
            }

            var products = query.ToList()
                .Select(p => new
                {
                    p.Id,
                    p.Title,

                    Price = p.Product_Sku.FirstOrDefault() != null
                        ? p.Product_Sku.FirstOrDefault().Price
                        : 0,

                    ImageUrl = p.Product_Images.FirstOrDefault(img => img.Is_Main) != null
                        ? Url.Content(p.Product_Images.FirstOrDefault(img => img.Is_Main).Image_Url)
                        : "/images/default.png"
                })
                .ToList();

            return Json(products, JsonRequestBehavior.AllowGet);
        }





    }

}

