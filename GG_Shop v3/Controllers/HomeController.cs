using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GG_Shop_v3.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
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
        public ActionResult index_admin()
        {
            ViewBag.Message = "admin";

            return View();
        }
        public ActionResult listCustomer()
        {
            ViewBag.Message = "listCustomer";

            return View();
        }
        public ActionResult addCustomer()
        {
            ViewBag.Message = "addCustomer";

            return View();
        }
        public ActionResult listUsers()
        {
            ViewBag.Message = "listUsers";

            return View();
        }
        public ActionResult addUsers()
        {
            ViewBag.Message = "addUsers";

            return View();
        }
        public ActionResult AddProduct()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult ListProduct()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult ListSale()
        {
            ViewBag.Message = "ListSale";

            return View();
        }
        public ActionResult AddSale()
        {
            ViewBag.Message = "AddSale";

            return View();
        }
    }
}