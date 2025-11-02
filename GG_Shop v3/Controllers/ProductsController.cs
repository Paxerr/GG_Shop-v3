using GG_Shop_v3.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace GG_Shop_v3.Controllers
{
    public class ProductsController : Controller
    {
        private DataContext db = new DataContext();

        // GET: Products
        public ActionResult Index()
        {
            var products = db.products
                .Include(p => p.Category)
                .Include(p => p.Product_Images)
                .Include(p => p.Product_Skus)
                .ToList();

            return View(products);
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Eager-load Category, Product_Images and Product_Skus
            var product = db.products
                            .Include(p => p.Category)
                            .Include(p => p.Product_Images)
                            .Include(p => p.Product_Skus)
                            .SingleOrDefault(p => p.Id == id.Value);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }


        // GET: Products/Create
        public ActionResult Create()
        {
            ViewBag.Category_Id = new SelectList(db.categories, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
    [Bind(Include = "Id,Title,Category_Id,Description,Status")] Product product,
    List<Product_Sku> skus,
    IEnumerable<HttpPostedFileBase> images,
    decimal? BasePrice,
    int? BaseQuantity)
        {
            if (ModelState.IsValid)
            {
                using (var tran = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1) Save product
                        db.products.Add(product);
                        db.SaveChanges();

                        // 2) Save skus (if any)
                        if (skus != null)
                        {
                            foreach (var s in skus)
                            {
                                // skip empty rows
                                var isEmpty = string.IsNullOrWhiteSpace(s.Sku)
                                              && string.IsNullOrWhiteSpace(s.Color)
                                              && string.IsNullOrWhiteSpace(s.Size)
                                              && (s.Quantity == 0)
                                              && (s.Price == 0m);
                                if (isEmpty) continue;

                                s.Product_Id = product.Id;
                                // if Price/Quantity empty but you supplied base, use them
                                if ((s.Price == 0m || s.Price == null) && BasePrice.HasValue) s.Price = BasePrice.Value;
                                if ((s.Quantity == 0) && BaseQuantity.HasValue) s.Quantity = BaseQuantity.Value;

                                db.product_skus.Add(s);
                            }
                        }

                        // 3) Save images
                        if (images != null)
                        {
                            var uploadFolder = Server.MapPath("~/uploads/products");
                            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                            foreach (var file in images)
                            {
                                if (file == null || file.ContentLength <= 0) continue;
                                var ext = Path.GetExtension(file.FileName);
                                var unique = Guid.NewGuid().ToString("N") + ext;
                                var path = Path.Combine(uploadFolder, unique);
                                file.SaveAs(path);

                                db.product_images.Add(new Product_Image
                                {
                                    Product_Id = product.Id,
                                    Image_Url = "/uploads/products/" + unique,
                                    Is_Main = false
                                });
                            }
                        }

                        db.SaveChanges();
                        tran.Commit();
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        ModelState.AddModelError("", "Error saving product: " + ex.Message);
                    }
                }
            }

            ViewBag.Category_Id = new SelectList(db.categories, "Id", "Name", product.Category_Id);
            return View(product);
        }




        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.products
                .Include(p => p.Product_Images)
                .Include(p => p.Product_Skus)
                .SingleOrDefault(p => p.Id == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.Category_Id = new SelectList(db.categories, "Id", "Name", product.Category_Id);
            return View(product);
        }

        // POST: Products/Edit/5
        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product, List<Product_Sku> skus, int? mainImageId, int[] deleteImages, IEnumerable<HttpPostedFileBase> newImages)
        {
            if (product == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // tìm bản ghi hiện có từ DB (kèm các relation cần thiết khi hiển thị lại)
            var existingProduct = db.products
                                    .Include(p => p.Product_Images)
                                    .Include(p => p.Product_Skus)
                                    .SingleOrDefault(p => p.Id == product.Id);

            if (existingProduct == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                using (var tran = db.Database.BeginTransaction())
                {
                    try
                    {
                        // ----- 1) XÓA ẢNH (nếu có)
                        var deletedSet = new HashSet<int>((deleteImages ?? new int[0]));
                        if (deletedSet.Count > 0)
                        {
                            foreach (var imgId in deletedSet)
                            {
                                var pi = db.product_images.Find(imgId);
                                if (pi != null)
                                {
                                    // nếu ảnh đang là main, tạm bỏ flag (server sẽ chọn lại sau)
                                    if (pi.Is_Main) pi.Is_Main = false;

                                    // xóa file vật lý (try/catch)
                                    try
                                    {
                                        var physical = Server.MapPath(pi.Image_Url);
                                        if (System.IO.File.Exists(physical))
                                        {
                                            System.IO.File.Delete(physical);
                                        }
                                    }
                                    catch
                                    {
                                        // ignore hoặc log
                                    }

                                    db.product_images.Remove(pi);
                                }
                            }
                            db.SaveChanges();
                        }

                        // ----- 2) LƯU ẢNH MỚI (nếu có)
                        if (newImages != null)
                        {
                            var uploadFolder = Server.MapPath("~/uploads/products");
                            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                            foreach (var file in newImages)
                            {
                                if (file == null || file.ContentLength <= 0) continue;
                                var ext = Path.GetExtension(file.FileName);
                                var unique = Guid.NewGuid().ToString("N") + ext;
                                var path = Path.Combine(uploadFolder, unique);
                                file.SaveAs(path);

                                var pimg = new Product_Image
                                {
                                    Product_Id = existingProduct.Id,
                                    Image_Url = "/uploads/products/" + unique,
                                    Is_Main = false
                                };
                                db.product_images.Add(pimg);
                            }

                            db.SaveChanges(); // cần để ảnh mới tồn tại trong DB nếu cần set main sau
                        }

                        // ----- 3) XỬ LÝ MAIN IMAGE
                        // Nếu user chọn mainImageId nhưng đó là ảnh đã bị xóa, ta bỏ qua
                        if (mainImageId.HasValue && deletedSet.Contains(mainImageId.Value))
                        {
                            mainImageId = null;
                        }

                        // đặt lại Is_Main cho tất cả ảnh của product
                        var imgsAll = db.product_images.Where(x => x.Product_Id == existingProduct.Id).ToList();
                        if (mainImageId.HasValue)
                        {
                            foreach (var it in imgsAll)
                            {
                                it.Is_Main = (it.Id == mainImageId.Value);
                                db.Entry(it).State = EntityState.Modified;
                            }
                        }
                        else
                        {
                            // nếu không chỉ định main, đảm bảo có 1 ảnh làm main nếu còn ảnh
                            if (imgsAll.Any())
                            {
                                // nếu không có ảnh nào là main, set ảnh đầu làm main
                                if (!imgsAll.Any(x => x.Is_Main))
                                {
                                    imgsAll.First().Is_Main = true;
                                    db.Entry(imgsAll.First()).State = EntityState.Modified;
                                }
                            }
                        }
                        db.SaveChanges();

                        // ----- 4) XỬ LÝ SKUs
                        if (skus != null)
                        {
                            foreach (var s in skus)
                            {
                                if (s.Id > 0)
                                {
                                    // update existing SKU: tìm từ DB và cập nhật trường rõ ràng
                                    var existSku = db.product_skus.Find(s.Id);
                                    if (existSku != null)
                                    {
                                        existSku.Sku = s.Sku;
                                        existSku.Color = s.Color;
                                        existSku.Size = s.Size;
                                        existSku.Quantity = s.Quantity;
                                        existSku.Price = s.Price;
                                        db.Entry(existSku).State = EntityState.Modified;
                                    }
                                }
                                else
                                {
                                    // thêm SKU mới nếu có Sku hoặc color/size
                                    if (!string.IsNullOrWhiteSpace(s.Sku) || !string.IsNullOrWhiteSpace(s.Color) || !string.IsNullOrWhiteSpace(s.Size))
                                    {
                                        s.Product_Id = existingProduct.Id;
                                        db.product_skus.Add(s);
                                    }
                                }
                            }
                            db.SaveChanges();
                        }

                        // ----- 5) CẬP NHẬT TRƯỜNG PRODUCT
                        existingProduct.Title = product.Title;
                        existingProduct.Category_Id = product.Category_Id;
                        existingProduct.Description = product.Description;
                        existingProduct.Status = product.Status;
                        db.Entry(existingProduct).State = EntityState.Modified;

                        db.SaveChanges();
                        tran.Commit();

                        return RedirectToAction("Details", new { id = existingProduct.Id });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        ModelState.AddModelError("", "Có lỗi khi lưu: " + ex.Message);
                    }
                }
            }

            // nếu lỗi: nạp lại dữ liệu để view dùng
            ViewBag.Category_Id = new SelectList(db.categories, "Id", "Name", product.Category_Id);
            // reload images & skus để view hiển thị
            existingProduct = db.products
                                .Include(p => p.Product_Images)
                                .Include(p => p.Product_Skus)
                                .SingleOrDefault(p => p.Id == product.Id);
            return View(existingProduct);
        }




        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = db.products
                .Include(p => p.Product_Skus)
                .Include(p => p.Product_Images)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return HttpNotFound();

            // Xóa các ảnh vật lý và bản ghi Product_Images
            if (product.Product_Images != null)
            {
                foreach (var img in product.Product_Images.ToList())
                {
                    // xóa file vật lý nếu tồn tại
                    try
                    {
                        var path = Server.MapPath(img.Image_Url);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }
                    catch { /* ignore */ }

                    db.product_images.Remove(img);
                }
            }

            // Xóa các SKU liên quan
            if (product.Product_Skus != null)
            {
                foreach (var sku in product.Product_Skus.ToList())
                {
                    db.product_skus.Remove(sku);
                }
            }

            // Cuối cùng xóa product
            db.products.Remove(product);
            db.SaveChanges();

            return RedirectToAction("Index");
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
