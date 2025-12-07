using GG_Shop_v3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using System.Data.Entity;

namespace GG_Shop_v3.C_Controllers
{
    public class HomeController : Controller
    {
        private DataContext db = new DataContext();
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetHomeProducts()
        {
            try
            {
                // 1. Truy vấn các sản phẩm Đang hoạt động (Status = "Đang bán")
                //    Kèm theo Product_Images (để lấy ảnh chính) và Product_Sku (để lấy giá)
                var productsQuery = db.products
                    .Where(p => p.Status == "Đang bán") // Giả định Status = "Active" là trạng thái hoạt động
                    .Include(p => p.Product_Images)
                    .Include(p => p.Product_Sku)
                    .OrderByDescending(p => p.Id) // Sắp xếp theo ID mới nhất (hoặc theo logic riêng)
                    .Take(8); // Giới hạn số lượng sản phẩm hiển thị trên trang chủ

                var products = await productsQuery.ToListAsync();

                // 2. Chuyển đổi dữ liệu sang DTO (Data Transfer Object) cho JSON
                var productList = products.Select(p =>
                {
                    // Lấy ảnh chính (Is_Main = true) hoặc ảnh đầu tiên nếu không có ảnh chính
                    var mainImage = p.Product_Images.FirstOrDefault(img => img.Is_Main)
                                    ?? p.Product_Images.FirstOrDefault();

                    string imageUrl = mainImage?.Image_Url ?? "/Content/img/no-image.jpg"; // URL ảnh

                    // Lấy giá thấp nhất từ tất cả SKU (SKU có Price khác 0)
                    // Lưu ý: Đây là Price ban đầu. Logic khuyến mãi (PriceSale) cần được xử lý riêng nếu có.
                    var lowestPriceSku = p.Product_Sku
                        .Where(sku => sku.Price > 0)
                        .OrderBy(sku => sku.Price)
                        .FirstOrDefault();

                    // Lấy giá bán (thấp nhất)
                    decimal price = lowestPriceSku?.Price ?? 0;

                    // --- Logic Bổ sung (Bạn cần tự định nghĩa các trường này trong Model nếu có) ---
                    // Giả định các trường sau được tính toán hoặc lấy từ nơi khác:
                    decimal priceSale = price * 0.9m; // Ví dụ: Giảm 10%
                    int rating = 4; // Ví dụ: Rating cứng
                    bool isSale = priceSale < price;
                    // Lấy ngày tạo (CreatedAt) từ database
// GIẢ SỬ: Tên trường trong DB là "CreatedAt" và kiểu của nó là DateTime.
DateTime createdAt = (DateTime)db.Entry(p).Property("CreatedAt").CurrentValue; 

// Phép trừ (DateTime - DateTime) sẽ trả về kiểu TimeSpan
TimeSpan productAge = DateTime.Now - createdAt;

// So sánh tuổi sản phẩm với 30 ngày
bool isNew = productAge < TimeSpan.FromDays(30);
                    // --------------------------------------------------------------------------

                    return new
                    {
                        Id = p.Id,
                        Name = p.Title,
                        Price = price,
                        PriceSale = priceSale, // Giá sau khuyến mãi (cần tùy chỉnh logic này)
                        ImageUrl = imageUrl,
                        Rating = rating,
                        IsNew = isNew, // Logic tự định nghĩa
                        IsSale = isSale, // Logic tự định nghĩa
                        CategorySlug = p.Category?.Name.Replace(" ", "-").ToLower() // Tạo slug đơn giản cho filter
                    };
                }).ToList();

                // 3. Trả về JSON
                return Json(productList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi hệ thống
                Response.StatusCode = 500;
                return Json(new { error = "Lỗi hệ thống khi tải sản phẩm: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}