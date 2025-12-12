using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using GG_Shop_v3.Models;


namespace GG_Shop_v3.Controllers
{
    public class PromotionsController : Controller
    {
        private DataContext db = new DataContext();
        // GET: Promotions
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult ActivePromo(int id)
        {
            var promo = db.promotions.FirstOrDefault(x => x.Id == id);
                promo.Status = "Hoạt động";
                db.SaveChanges();
            return Json("Đã cập nhật trạng thái");
        }

        [HttpPost]
        public JsonResult AutoDisable(int id)
        {
            var promo = db.promotions.FirstOrDefault(x => x.Id == id);

            if (promo == null)
                return Json("Không tìm thấy mã");

            if (promo.Status != "Ngừng hoạt động")
            {
                promo.Status = "Ngừng hoạt động";
                db.SaveChanges();
            }

            return Json("Đã cập nhật trạng thái");
        }


        public JsonResult getPromotionsList()
        {
            var list = db.promotions
                .AsEnumerable()   // ⬅ CHUYỂN sang xử lý bằng C#, không còn SQL
                .Select(p => new
                {
                    p.Id,
                    p.Promo_Code,
                    p.Description,
                    p.Type,
                    p.Discount_Percentage,
                    p.Discount_Amount,
                    Start_Date = p.Start_Date.ToString("yyyy-MM-dd"),
                    End_Date = p.End_Date.ToString("yyyy-MM-dd"),
                    p.Min_Order_Value,
                    p.Uses_Count,
                    p.Status
                })
                .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }




        public ActionResult Create()
        {
            return View();
        }
        public string Insert()
        {
            string rs = "";

            try
            {
                string promo_code = Request["Promo_Code"];
                string description = Request["Description"];
                string type = Request["Type"];

                string discount_percentage_str = Request["Discount_Percentage"];
                string discount_amount_str = Request["Discount_Amount"];
                string status_str = Request["Status"];
                string start_date_str = Request["Start_Date"];
                string end_date_str = Request["End_Date"];
                string min_order_value_str = Request["Min_Order_Value"];


                //// Mặc định
                int uses_count = 0;
                //string status = "Active";

                // Convert
                decimal? discount_percentage = null;
                decimal? discount_amount = null;
                decimal? min_order_value = null;

                DateTime start_date;
                DateTime end_date;
                    

                DateTime.TryParse(start_date_str, out start_date);
                DateTime.TryParse(end_date_str, out end_date);

                if(type == "Giảm theo %")
                {
                    discount_amount = 0;
                    discount_percentage = decimal.Parse(discount_percentage_str);
                }
                else
                {
                    discount_amount = decimal.Parse(discount_amount_str);
                    discount_percentage = 0;
                }
                min_order_value = decimal.Parse(min_order_value_str);

                // Check mã giảm giá trùng
                if (db.promotions.Any(a => a.Promo_Code == promo_code))
                {
                    return "Mã giảm giá đã tồn tại!";
                }

                // Tạo object Promotion
                Promotion p = new Promotion()
                {
                    Promo_Code = promo_code,
                    Description = description,
                    Type = type,
                    Discount_Percentage = discount_percentage,
                    Discount_Amount = discount_amount,
                    Start_Date = start_date,
                    End_Date = end_date,
                    Min_Order_Value = min_order_value,
                    Uses_Count = uses_count,
                    Status = status_str
                };

                db.promotions.Add(p);
                db.SaveChanges();

                rs = "Thêm chương trình giảm giá thành công!";
            }
            catch (Exception ex)
            {
                rs = "Thêm chương trình giảm giá thất bại!";
            }

            return rs;
        }

        [HttpPost]
        public JsonResult AutoDisablePromo(string code)
        {
            if (string.IsNullOrEmpty(code))
                return Json("Không có mã", JsonRequestBehavior.AllowGet);

            var promo = db.promotions.FirstOrDefault(x => x.Promo_Code == code);

            if (promo == null)
                return Json("Không tìm thấy mã", JsonRequestBehavior.AllowGet);

            promo.Status = "Ngừng hoạt động";
            db.SaveChanges();

            return Json("Đã cập nhật trạng thái");
        }


        public ActionResult Edit()
        {
            return View();
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Promotion promotion = db.promotions.Find(id);
            if (promotion == null)
            {
                return HttpNotFound();
            }
            return View(promotion);
        }

        [HttpGet]
        public JsonResult GetPromotions(int? id)
        {
            var Promo = db.promotions.Find(id);

            return Json(Promo, JsonRequestBehavior.AllowGet);
        }




        public String delePromotions()
        {
            string rs = "";
            string Id_str = Request["id"];
            int Id;
            int.TryParse(Id_str, out Id);
            try
            {
                Promotion promo = db.promotions.Find(Id);
                promo.Status = "Ngừng hoạt động";
                db.Entry(promo).State = EntityState.Modified;
                db.SaveChanges();
                rs = "Mã giảm giá đã dừng hoạt động";
            }
            catch (Exception ex)
            {
                rs = "Dừng hoạt động mã giảm giá thất bại";
            }
            return rs;
        }

    }
}