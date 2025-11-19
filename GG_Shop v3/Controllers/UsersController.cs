using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using GG_Shop_v3.Models;

namespace GG_Shop_v3.Controllers
{
    public class UsersController : Controller
    {
        private DataContext db = new DataContext();

        // GET: Users
        public ActionResult Index()
        {
            return View(db.users.ToList());
        }

        // GET: Users/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public string Insert()
        {
            //lấy về mssv.
            string rs = "";
            string username_str = Request["username"];
            string email_str = Request["email"];
            string password_str = Request["password"];
            string full_name_str = Request["full_name"];
            string phone_number_str = Request["phone_number"];
            string country_str = Request["country"];
            string orders_str = Request["orders"];
            string rank_str = Request["slc_rank"];
            string total_spent_str = Request["total_spent"];
            string role_str = Request["slc_role"];

            int orders;
            int.TryParse(orders_str, out orders);
            double total_spent;
            double.TryParse(total_spent_str, out total_spent);

            //kiểm tra xem đã tồn tại đối tượng sv có mssv này chưa
            if (db.users.Any(o => o.Username == username_str))
            {
                rs = "Tên đăng nhập đã tồn tại";
            }
            else
            {
                //chưa tồn tại teen ddawng nhap.



                //update thông tin cho đối tượng
                User user = new User(username_str, email_str, password_str, full_name_str, phone_number_str, country_str, orders, rank_str, total_spent, role_str);

                try
                {
                    db.users.Add(user);
                    db.SaveChanges();
                    rs = "Thêm người dùng thành công";
                }
                catch (Exception ex)
                {
                    rs = "Thêm người dùng thất bại";
                }
            }

            return rs;
        }




        // GET: Users/Edit/5
        public ActionResult Edit(int? id)
        {
            ViewBag.RankList = new SelectList(new List<string> { "Vip", "Thường" });
            ViewBag.RoleList = new SelectList(new List<string> { "Admin", "Khách hàng" });
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Username,Email,Password,Full_Name,Phone_Number,Country,Orders,Rank,Total_Spent,Role")] User user)
        {
            if (db.users.Where(u => u.Id != user.Id).Any(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Tên tài khoản đã tồn tại");
            }
            if (db.users.Where(u => u.Id != user.Id).Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại");
            }
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RankList = new SelectList(new List<string> { "Vip", "Thường" });
            ViewBag.RoleList = new SelectList(new List<string> { "Admin", "Khách hàng" });
            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.users.Find(id);
            db.users.Remove(user);
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
