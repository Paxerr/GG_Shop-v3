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

        public JsonResult getPromotionsList()
        {
            var listPromotions = db.promotions.ToList();
            return Json(listPromotions, JsonRequestBehavior.AllowGet);
        }


        public ActionResult Create()
        {
            return View();
        }

        public ActionResult Edit()
        {
            return View();
        }

        public ActionResult Details()
        {
            return View();
        }


        public String delePromotions()
        {
            string rs = "";
            string Id_str = Request["Id"];
            int Id;
            int.TryParse(Id_str, out Id);
            try
            {
                Promotion promo = db.promotions.Find(Id);
                User user = db.users.Find(Id);
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