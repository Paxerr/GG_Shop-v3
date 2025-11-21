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
    public class DashboardController : Controller
    {
        private DataContext db = new DataContext();
        // GET: Dashboard
        public ActionResult Index()
        {
            int totalSales = db.orders.Count();

            ViewBag.TotalSales = totalSales;

            decimal DoanhThu = db.orders.Sum(o => o.Total_Amount);
            ViewBag.DoanhThu = DoanhThu;

            int TongSanPham = db.order_items.Count();
            ViewBag.TongSanPham = TongSanPham;



            return View();
        }

    }
}