using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using GG_Shop_v3.Models;

namespace GG_Shop_v3.Controllers
{
    public class OrdersController : Controller
    {
        private DataContext db = new DataContext();

        // GET: Orders
        public ActionResult Index()
        {
            var orders = db.orders.Include(o => o.Promotion).Include(o => o.User);
            return View(orders.ToList());
        }

        // GET: Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = db.orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.Order_Items.Select(i => i.Product_Sku.Product))
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // ✅ GET: Orders/Create
        public ActionResult Create()
        {
            ViewBag.User_Id = new SelectList(db.users, "Id", "Username");
            ViewBag.Promo_Id = new SelectList(db.promotions, "Id", "Promo_Code");
            ViewBag.SkuList = new SelectList(db.product_skus.Include(p => p.Product), "Id", "Sku");

            // Danh sách trạng thái đơn hàng
            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "Pending", Text = "Pending" },
                new { Value = "Shipped", Text = "Shipped" },
                new { Value = "Completed", Text = "Completed" },
                new { Value = "Cancelled", Text = "Cancelled" }
            }, "Value", "Text");

            Session["TempOrderItems"] = new List<Order_Item>();
            return View(new Order());
        }

        // ✅ POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Order order, string action, int? SelectedSkuId, int? SelectedQuantity)
        {
            ViewBag.User_Id = new SelectList(db.users, "Id", "Username", order.User_Id);
            ViewBag.Promo_Id = new SelectList(db.promotions, "Id", "Promo_Code", order.Promo_Id);
            ViewBag.SkuList = new SelectList(db.product_skus.Include(p => p.Product), "Id", "Sku");
            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "Pending", Text = "Pending" },
                new { Value = "Shipped", Text = "Shipped" },
                new { Value = "Completed", Text = "Completed" },
                new { Value = "Cancelled", Text = "Cancelled" }
            }, "Value", "Text", order.Status);

            var tempItems = Session["TempOrderItems"] as List<Order_Item> ?? new List<Order_Item>();

            // Khi bấm "Thêm sản phẩm"
            if (action == "AddItem" && SelectedSkuId.HasValue && SelectedQuantity.HasValue)
            {
                var sku = db.product_skus.Include(s => s.Product).FirstOrDefault(s => s.Id == SelectedSkuId.Value);
                if (sku != null)
                {
                    var existing = tempItems.FirstOrDefault(i => i.Sku_Id == sku.Id);
                    if (existing != null)
                    {
                        existing.Quantity += SelectedQuantity.Value;
                    }
                    else
                    {
                        tempItems.Add(new Order_Item
                        {
                            Product_Sku = sku,
                            Sku_Id = sku.Id,
                            Quantity = SelectedQuantity.Value,
                            Price = sku.Price
                        });
                    }
                    Session["TempOrderItems"] = tempItems;
                }

                ModelState.Clear();
                ViewBag.TempItems = tempItems;
                return View(order);
            }

            // Khi bấm "Lưu đơn hàng"
            if (action == "SaveOrder" && ModelState.IsValid)
            {
                var itemsToSave = Session["TempOrderItems"] as List<Order_Item> ?? new List<Order_Item>();

                order.Created_At = DateTime.Now;
                order.Total_Amount = itemsToSave.Sum(i => i.Price * i.Quantity);
                db.orders.Add(order);
                db.SaveChanges();

                foreach (var item in itemsToSave)
                {
                    item.Order_Id = order.Id;
                    item.Product_Sku = null;
                    db.order_items.Add(item);
                }

                db.SaveChanges();
                Session["TempOrderItems"] = null;

                return RedirectToAction("Index");
            }

            ViewBag.TempItems = tempItems;
            return View(order);
        }

        // GET: Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Order order = db.orders.Find(id);
            if (order == null)
                return HttpNotFound();

            var promos = db.promotions
                .Select(p => new { Id = (int?)p.Id, p.Promo_Code })
                .ToList();
            promos.Insert(0, new { Id = (int?)null, Promo_Code = "(Không áp dụng khuyến mãi)" });

            ViewBag.Promo_Id = new SelectList(promos, "Id", "Promo_Code", order.Promo_Id);
            ViewBag.User_Id = new SelectList(db.users, "Id", "Username", order.User_Id);

            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,User_Id,Total_Amount,Status,Shipping_Address,Promo_Id,Created_At")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            var promos = db.promotions
                .Select(p => new { Id = (int?)p.Id, p.Promo_Code })
                .ToList();
            promos.Insert(0, new { Id = (int?)null, Promo_Code = "(Không áp dụng khuyến mãi)" });

            ViewBag.Promo_Id = new SelectList(promos, "Id", "Promo_Code", order.Promo_Id);
            ViewBag.User_Id = new SelectList(db.users, "Id", "Username", order.User_Id);

            return View(order);
        }

        // GET: Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Order order = db.orders.Find(id);
            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.orders.Find(id);
            db.orders.Remove(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
