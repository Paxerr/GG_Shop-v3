using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GG_Shop_v3.Models;

namespace GG_Shop_v3.Controllers
{
    public class CategoriesController : Controller
    {
        private DataContext db = new DataContext();

        // GET: Categories (chỉ trả View, data sẽ load bằng Ajax)
        public ActionResult Index()
        {
            return View();
        }

        // GET: Lấy danh sách danh mục (hỗ trợ tìm kiếm realtime)
        [HttpGet]
        public JsonResult GetCategories(string search = "")
        {
            var categories = db.categories.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                categories = categories.Where(c => c.Name.ToLower().StartsWith(search.ToLower()));
            }
            var list = categories.OrderBy(c => c.Name)
                                 .Select(c => new
                                 {
                                     c.Id,
                                     c.Name,
                                     c.Description
                                 })
                                 .ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        // GET: Chi tiết 1 danh mục (dùng cho modal edit/delete)
        [HttpGet]
        public JsonResult GetCategoryDetails(int? id)
        {
            if (id == null)
                return Json(new { success = false, message = "ID không hợp lệ" }, JsonRequestBehavior.AllowGet);

            var category = db.categories.Find(id);
            if (category == null)
                return Json(new { success = false, message = "Không tìm thấy danh mục" }, JsonRequestBehavior.AllowGet);

            var result = new
            {
                category.Id,
                category.Name,
                category.Description
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // GET: Categories/Create (trả View form)
        public ActionResult Create()
        {
            return View();
        }

        // POST: Tạo mới danh mục (Ajax) 
        [HttpPost]

        public JsonResult CreateCategory(Category category)
        {
            if (category == null)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ (null)" });

            // Trim dữ liệu
            category.Name = (category.Name ?? "").Trim();
            category.Description = (category.Description ?? "").Trim();

            // Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
            }

            // Kiểm tra tên trùng
            bool exists = db.categories.Any(c => c.Name.ToLower() == category.Name.ToLower());
            if (exists)
                return Json(new { success = false, message = "Tên danh mục đã tồn tại." });

            try
            {
                db.categories.Add(category);
                db.SaveChanges();
                return Json(new { success = true, message = "Tạo danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lưu: " + ex.Message });
            }
        }

        // GET: Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Category category = db.categories.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // POST: Cập nhật danh mục (Ajax) 
        [HttpPost]
        public JsonResult UpdateCategory(Category category)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            var existing = db.categories.Find(category.Id);
            if (existing == null)
                return Json(new { success = false, message = "Không tìm thấy danh mục" });

            // Kiểm tra trùng tên (trừ chính nó)
            bool exists = db.categories.Any(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);
            if (exists)
                return Json(new { success = false, message = "Tên danh mục đã tồn tại." });

            existing.Name = category.Name;
            existing.Description = category.Description;
            db.Entry(existing).State = EntityState.Modified;
            db.SaveChanges();

            return Json(new { success = true, message = "Cập nhật thành công!" });
        }

        // GET: Categories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var category = db.categories.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // POST: Xóa danh mục (Ajax) 
        [HttpPost]
        public JsonResult DeleteCategory(int id)
        {
            try
            {
                var category = db.categories.Find(id);
                if (category == null)
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });

                db.categories.Remove(category);
                db.SaveChanges();
                return Json(new { success = true, message = "Xóa thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}