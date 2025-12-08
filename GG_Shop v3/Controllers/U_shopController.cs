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

        public JsonResult GetProducts(int? categoryId, int? minPrice, int? maxPrice, string size)
        {
            var query = db.products
                          .Include(p => p.Category)
                          .Include(p => p.Product_Sku)
                          .Include(p => p.Product_Images)
                          .Where(p => p.Status == "Đang bán");

            // ============================
            // FILTER CATEGORY
            // ============================
            if (categoryId != null)
            {
                query = query.Where(p => p.Category_Id == categoryId);
            }

            // ============================
            // FILTER PRICE RANGE
            // ============================
            if (minPrice != null && maxPrice != null)
            {
                query = query.Where(p => p.Product_Sku.Any(s =>
                    s.Price >= minPrice && s.Price <= maxPrice
                ));
            }

            // ============================
            // FILTER SIZE
            // ============================
            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(p => p.Product_Sku.Any(s => s.Size == size));
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
                                }).ToList();

            return Json(products, JsonRequestBehavior.AllowGet);
        }



        // Lấy sản phẩm theo filter
        public JsonResult GetProducts1()
        {
            var products = db.products
                .Include(p => p.Category)
                .Include(p => p.Product_Images)
                .Include(p => p.Product_Sku)   // chính xác tên collection
                .ToList();

            var result = products.Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                Category = p.Category != null ? new { p.Category.Id, p.Category.Name } : null,

                Product_Images = (p.Product_Images ?? new List<Product_Image>())
                    .Select(i => new
                    {
                        i.Id,
                        Image_Url = string.IsNullOrEmpty(i.Image_Url) ? null : Url.Content(i.Image_Url),
                        i.Is_Main
                    }).ToList(),

                Product_Skus = (p.Product_Sku ?? new List<Product_Sku>())
                    .Select(s => new
                    {
                        s.Id,
                        s.Sku,
                        s.Color,
                        s.Size,
                        s.Price,
                        s.Quantity
                    }).ToList(),

                TotalQty = (p.Product_Sku?.Sum(s => s.Quantity)) ?? 0,
                MinPrice = (p.Product_Sku?.Any() ?? false) ? (p.Product_Sku.Min(s => s.Price)) : 0m,
                MaxPrice = (p.Product_Sku?.Any() ?? false) ? (p.Product_Sku.Max(s => s.Price)) : 0m,

                p.Status
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }

}

