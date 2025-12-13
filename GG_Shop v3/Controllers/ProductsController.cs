using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GG_Shop_v3.Models;

namespace GG_Shop_v3.Controllers
{
    public class ProductsController : Controller
    {
        private DataContext db = new DataContext();

        // GET: Products
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetProducts()
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

        // GET: Products/Details
        public ActionResult Details(int id)
        {
            ViewBag.Id = id;
            return View();
        }

        [HttpGet]
        public JsonResult DetailsProduct(int id)
        {
            var product = db.products
                .Include(p => p.Category)
                .Include(p => p.Product_Images)
                .Include(p => p.Product_Sku) // sửa tên collection
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            var imageList = (product.Product_Images ?? new List<Product_Image>())
                .Select(i => new
                {
                    i.Id,
                    Url = string.IsNullOrEmpty(i.Image_Url) ? null : Url.Content(i.Image_Url),
                    i.Is_Main
                })
                .ToList();

            var skuList = (product.Product_Sku ?? new List<Product_Sku>())
                .Select(s => new
                {
                    s.Id,
                    s.Sku,
                    s.Color,
                    s.Size,
                    Price = s.Price,
                    Quantity = s.Quantity,
                    Total = s.Price * s.Quantity
                })
                .ToList();

            var mainImgUrl = imageList.FirstOrDefault(i => i.Is_Main)?.Url ?? imageList.FirstOrDefault()?.Url;

            var json = new
            {
                product.Id,
                product.Title,
                product.Description,
                CategoryName = product.Category?.Name,
                MainImageUrl = mainImgUrl,
                Images = imageList,
                Skus = skuList,
                TotalQty = skuList.Sum(s => (int?)s.Quantity) ?? 0,
                MinPrice = skuList.Any() ? skuList.Min(s => s.Price) : 0m,
                MaxPrice = skuList.Any() ? skuList.Max(s => s.Price) : 0m,
                product.Status
            };

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        // GET: Create Product (form)
        public ActionResult Create()
        {
            ViewBag.Category_Id = new SelectList(db.categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public JsonResult CreateAjax()
        {
            using (var tran = db.Database.BeginTransaction())
            {
                try
                {
                    string title = Request["Title"];
                    string description = Request["Description"];
                    int categoryId = int.Parse(Request["Category_Id"]);
                    string status = Request["Status"];

                    if (string.IsNullOrWhiteSpace(title))
                        return Json(new { success = false, message = "Tên sản phẩm không được để trống." });

                    if (db.products.Any(p => p.Title.Trim().ToLower() == title.Trim().ToLower()))
                        return Json(new { success = false, message = "Tên sản phẩm đã tồn tại." });

                    var product = new Product
                    {
                        Title = title.Trim(),
                        Description = description,
                        Status = status,
                        Category_Id = categoryId,
                    };

                    //Validate Sku
                    var skuIndexes = Request.Form.AllKeys
                        .Where(k => k.StartsWith("skus[") && k.EndsWith("].Sku"))
                        .Select(k => k.Substring(5, k.IndexOf("]") - 5))
                        .Distinct()
                        .ToList();

                    if (!skuIndexes.Any())
                        return Json(new { success = false, message = "Sản phẩm phải có ít nhất 1 SKU." });

                    foreach (var idx in skuIndexes)
                    {
                        string color = Request[$"skus[{idx}].Color"];
                        string size = Request[$"skus[{idx}].Size"];
                        string sku = Request[$"skus[{idx}].Sku"];
                        string qtyStr = Request[$"skus[{idx}].Quantity"];
                        string priceStr = Request[$"skus[{idx}].Price"];

                        if (string.IsNullOrWhiteSpace(color) || string.IsNullOrWhiteSpace(size))
                            return Json(new { success = false, message = $"SKU thứ {idx + 1} phải có màu và kích cỡ." });

                        if (!int.TryParse(qtyStr, out int qty) || qty < 1)
                            return Json(new { success = false, message = $"Phải có ít nhất 1 sản phẩm ở SKU thứ {idx + 1} ." });

                        if (!decimal.TryParse(priceStr, out decimal price) || price <= 0)
                            return Json(new { success = false, message = $"Giá của SKU thứ {idx + 1} phải > 0." });
                    }

                    db.products.Add(product);
                    db.SaveChanges();

                    // Lưu ảnh
                    var uploadFolder = Server.MapPath("~/Uploads/Products");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    var files = Request.Files;
                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        if (file == null || file.ContentLength == 0) continue;

                        var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                        var path = Path.Combine(uploadFolder, fileName);

                        file.SaveAs(path);

                        db.product_images.Add(new Product_Image
                        {
                            Product_Id = product.Id,
                            Image_Url = "/Uploads/Products/" + fileName,
                            Is_Main = i == 0
                        });
                    }

                    // Lưu SKU
                    // Lưu SKU
                    var keys = Request.Form.AllKeys
                        .Where(k => k.StartsWith("skus[") && k.EndsWith("].Sku"));

                    foreach (var key in keys)
                    {
                        string idx = key.Substring(5, key.IndexOf("]") - 5);

                        string color = Request[$"skus[{idx}].Color"];
                        string size = Request[$"skus[{idx}].Size"];

                        // ❗ LUÔN TỰ SINH SKU Ở BE – KHÔNG TIN FE
                        var baseSku = $"{GetInitials(product.Title)}-{NormalizeForSku(color)}-{NormalizeForSku(size)}"
                                        .ToUpperInvariant();

                        var finalSku = baseSku;
                        int counter = 1;

                        while (db.product_skus.Any(s => s.Sku == finalSku))
                        {
                            finalSku = $"{baseSku}-{counter}";
                            counter++;
                        }

                        db.product_skus.Add(new Product_Sku
                        {
                            Product_Id = product.Id,
                            Sku = finalSku,               // ✅ DÙNG SKU ĐÃ CHỐNG TRÙNG
                            Color = color,
                            Size = size,
                            Quantity = int.Parse(Request[$"skus[{idx}].Quantity"]),
                            Price = decimal.Parse(Request[$"skus[{idx}].Price"])
                        });
                    }


                    db.SaveChanges();
                    tran.Commit();

                    return Json(new { success = true, redirectUrl = Url.Action("Index") });
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }
        [HttpGet]
        public JsonResult CheckTitle(string title)
        {
            bool exists = db.products.Any(p => p.Title.ToLower() == title.Trim().ToLower());
            return Json(new { exists }, JsonRequestBehavior.AllowGet);
        }


        // GET: Products/Edit
        public ActionResult Edit(int id)
        {
            ViewBag.Id = id;
            return View(); 
        }

        [HttpGet]
        public JsonResult EditProduct(int id)
        {
            var product = db.products
                .Include(p => p.Category)
                .Include(p => p.Product_Images)
                .Include(p => p.Product_Sku)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            // Danh sách ảnh
            var imageList = (product.Product_Images ?? new List<Product_Image>())
                .Select(i => new
                {
                    i.Id,
                    Url = string.IsNullOrEmpty(i.Image_Url) ? null : Url.Content(i.Image_Url),
                    i.Is_Main
                }).ToList();

            // Danh sách SKU
            var skuList = (product.Product_Sku ?? new List<Product_Sku>())
                .Select(s => new
                {
                    s.Id,
                    s.Sku,
                    s.Color,
                    s.Size,
                    Price = s.Price,
                    Quantity = s.Quantity
                }).ToList();

            // Danh sách danh mục
            var categories = db.categories
                .Select(c => new { c.Id, c.Name })
                .ToList();

            // Danh sách trạng thái cố định
            var statusList = new[] {
                "Đang bán",
                "Chưa bán",
                "Đã hết"
            };

            // Ảnh đại diện
            var mainImgUrl = imageList.FirstOrDefault(i => i.Is_Main)?.Url
                             ?? imageList.FirstOrDefault()?.Url;

            // JSON trả về
            var json = new
            {
                product.Id,
                product.Title,
                product.Description,
                product.Category_Id,
                CategoryName = product.Category?.Name,
                Categories = categories,
                Status = product.Status,
                StatusList = statusList,
                MainImageUrl = mainImgUrl,
                Images = imageList,
                Skus = skuList,
                TotalQty = skuList.Sum(s => (int?)s.Quantity) ?? 0,
                MinPrice = skuList.Any() ? skuList.Min(s => s.Price) : 0m,
                MaxPrice = skuList.Any() ? skuList.Max(s => s.Price) : 0m
            };

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult EditAjax()
        {
            try
            {
                int id = int.Parse(Request["Id"]);
                string title = Request["Title"];
                string description = Request["Description"];
                int categoryId = int.Parse(Request["Category_Id"]);
                string status = Request["Status"];

                // Tìm sản phẩm cần sửa
                var product = db.products.Include(p => p.Product_Images).Include(p => p.Product_Sku).FirstOrDefault(p => p.Id == id);
                if (product == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

                // Cập nhật thông tin cơ bản
                product.Title = title;
                product.Description = description;
                product.Category_Id = categoryId;
                product.Status = status;

                // Xử lý ảnh cần xóa
                var deleteImageIds = Request.Form.GetValues("deleteImages");
                if (deleteImageIds != null)
                {
                    foreach (var imgIdStr in deleteImageIds)
                    {
                        if (int.TryParse(imgIdStr, out int imgId))
                        {
                            var img = product.Product_Images.FirstOrDefault(i => i.Id == imgId);
                            if (img != null)
                                db.product_images.Remove(img);
                        }
                    }
                }

                // Cập nhật ảnh đại diện
                string mainImageIdStr = Request["mainImageId"];
                if (int.TryParse(mainImageIdStr, out int mainImageId))
                {
                    foreach (var img in product.Product_Images)
                    {
                        img.Is_Main = (img.Id == mainImageId);
                    }
                }

                // Xử lý ảnh mới upload
                var files = Request.Files;
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string path = Path.Combine(Server.MapPath("~/Uploads/Products"), fileName);
                        file.SaveAs(path);

                        product.Product_Images.Add(new Product_Image
                        {
                            Image_Url = "/Uploads/Products/" + fileName,
                            Is_Main = false,
                            Product_Id = product.Id
                        });
                    }
                }

                // Xóa SKU cũ
                db.product_skus.RemoveRange(product.Product_Sku);

                // Thêm SKU mới
                var skuKeys = Request.Form.AllKeys.Where(k => k.StartsWith("skus[") && k.EndsWith("].Sku")).ToList();
                foreach (var key in skuKeys)
                {
                    var prefix = key.Substring(0, key.IndexOf("].Sku") + 1); 
                    string sku = Request[prefix + ".Sku"];
                    string color = Request[prefix + ".Color"];
                    string size = Request[prefix + ".Size"];
                    int quantity = int.TryParse(Request[prefix + ".Quantity"], out int q) ? q : 0;
                    decimal price = decimal.TryParse(Request[prefix + ".Price"], out decimal p) ? p : 0;

                    product.Product_Sku.Add(new Product_Sku
                    {
                        Sku = sku,
                        Color = color,
                        Size = size,
                        Quantity = quantity,
                        Price = price,
                        Product_Id = product.Id
                    });
                }

                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true, redirectUrl = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lưu: " + ex.Message });
            }
        }


        // POST: Delete Product (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteAjax(int id)
        {
            var product = db.products
                .Include(p => p.Product_Images)
                .Include(p => p.Product_Sku)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

            // Xóa ảnh vật lý
            if (product.Product_Images != null)
            {
                foreach (var img in product.Product_Images.ToList())
                {
                    try
                    {
                        var path = Server.MapPath(img.Image_Url);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }
                    catch { }
                }
            }

            db.product_images.RemoveRange(product.Product_Images ?? new List<Product_Image>());
            db.product_skus.RemoveRange(product.Product_Sku ?? new List<Product_Sku>());
            db.products.Remove(product);
            db.SaveChanges();

            return Json(new { success = true, message = "Xóa thành công" });
        }

        // GET: Trạng thái sản phẩm
        [HttpGet]
        public JsonResult GetStatusList()
        {
            var statuses = new[] { "Đang bán", "Chưa bán", "Đã hết" };
            return Json(statuses, JsonRequestBehavior.AllowGet);
        }

        // GET: Danh sách category
        [HttpGet]
        public JsonResult GetCategories()
        {
            var categories = db.categories
                .Select(c => new { c.Id, c.Name })
                .ToList();
            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        private string NormalizeForSku(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var noDiac = RemoveDiacritics(text);
            noDiac = System.Text.RegularExpressions.Regex.Replace(noDiac, @"[^\w\-]", "");
            noDiac = noDiac.Replace("_", "");
            return noDiac.ToUpperInvariant();
        }

        private string GetInitials(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";
            var parts = RemoveDiacritics(title).Trim().Split(' ');
            return string.Join("", parts.Where(x => x.Length > 0).Select(x => char.ToUpperInvariant(x[0])));
        }

    }
}
