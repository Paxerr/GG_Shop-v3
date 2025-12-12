using GG_Shop_v3.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace GG_Shop_v3.Controllers
{
    public class AccountController : Controller
    {
        private readonly DataContext db = new DataContext();

        // GET: Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Trả về lỗi validation
                return Json(new
                {
                    success = false,
                    errors = ModelState.Where(x => x.Value.Errors.Any())
                        .ToDictionary(
                            kv => kv.Key,
                            kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        )
                });
            }

            // Check username hoặc email:
            var user = await db.users
                 .FirstOrDefaultAsync(x => x.Email == model.UserInput || x.Username == model.UserInput);

            // Nếu không tồn tại hoặc pwd sai -> trả lỗi 
            if (user == null || user.Password != model.Password)
            {
                return Json(new
                {
                    success = false,
                    errors = new { _global = new[] { "Email hoặc mật khẩu không đúng" } }
                });
            }

            // Kiểm tra trạng thái tài khoản
            // Nếu user.Status không phải "Hoạt động" thì chặn login
            string status = (user.Status ?? "").Trim();
            if (!string.Equals(status, "Hoạt động", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    errors = new { _global = new[] { "Tài khoản của bạn đã bị chặn/khóa. Vui lòng liên hệ quản trị." } }
                });
            }

            // LƯU SESSION CHỈ KHI TẤT CẢ OK
            Session["User"] = user;
            Session["User_Id"] = user.Id;
            Session["User_Role"] = user.Role;

            // Nếu có ReturnUrl lưu trong session (ví dụ từ Cart), trả về url đó
            string returnUrl = Session["ReturnUrl"] != null ? Session["ReturnUrl"].ToString() : Url.Action("Index", "Home");
            Session.Remove("ReturnUrl");

            return Json(new
            {
                success = true,
                redirectUrl = returnUrl
            });
        }


        public ActionResult Logout()
        {
            Session.Remove("User_Id");
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }


        // GET: Forgot Password
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Forgot Password (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập email hợp lệ."
                });
            }

            var user = db.users.FirstOrDefault(x => x.Email == model.Email);

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Email không tồn tại trong hệ thống"
                });
            }

            string code = Guid.NewGuid().ToString();
            Session["ResetCode"] = code;
            Session["ResetEmail"] = model.Email;

            return Json(new
            {
                success = true,
                message = "Mã xác nhận đã được gửi đến email.",
                redirectUrl = Url.Action("ResetPassword")
            });
        }

        // GET: Reset Password
        public ActionResult ResetPassword()
        {
            if (Session["ResetEmail"] == null)
                return RedirectToAction("ForgotPassword");

            return View();
        }

        // POST: Reset Password (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.Where(x => x.Value.Errors.Any())
                        .ToDictionary(
                            kv => kv.Key,
                            kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        )
                });
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return Json(new
                {
                    success = false,
                    errors = new { _global = new[] { "Mật khẩu xác nhận không khớp" } }
                });
            }

            var user = db.users.FirstOrDefault(x => x.Email == model.Email);
            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    errors = new { _global = new[] { "Không tìm thấy người dùng" } }
                });
            }

            user.Password = model.NewPassword;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Đặt lại mật khẩu thành công!",
                redirectUrl = Url.Action("Login")
            });
        }

        public ActionResult Create()
        {
            return View("Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(CreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
            }

            // kiểm tra trùng email
            if (db.users.Any(x => x.Email == model.Email))
                return Json(new { success = false, message = "Email đã được sử dụng" });

            // kiểm tra trùng username
            if (db.users.Any(x => x.Username == model.Username))
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });

            // kiểm tra trung sdt
            if (db.users.Any(x => x.Phone_Number == model.PhoneNumber))
                return Json(new { success = false, message = "Số điện thoại đã tồn tại" });

            try
            {
                User u = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password,
                    Full_Name = model.FullName,
                    Phone_Number = model.PhoneNumber,
                    Country = model.Country,
                    Orders = 0,
                    Rank = "Bronze",
                    Total_Spent = 0,
                    Role = "User",
                    Status = "Hoạt động"
                };

                db.users.Add(u);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Tạo tài khoản thành công!",
                    redirectUrl = Url.Action("Login", "Account")
                });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi hệ thống: Không thể tạo tài khoản" });
            }
        }

        [HttpPost]
        public JsonResult CheckEmail(string email)
        {
            bool exists = db.users.Any(x => x.Email == email);
            return Json(new { exists });
        }

        [HttpPost]
        public JsonResult CheckUsername(string username)
        {
            bool exists = db.users.Any(x => x.Username == username);
            return Json(new { exists });
        }

        [HttpPost]
        public JsonResult CheckPhone(string phone)
        {
            bool exists = db.users.Any(x => x.Phone_Number == phone);
            return Json(new { exists });
        }


    }
}
