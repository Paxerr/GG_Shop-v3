using GG_Shop_v3.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace GG_Shop_v3.Controllers
{
    public class UserProfileController : Controller
    {
        private DataContext db = new DataContext();

        // GET: /UserProfile/Index - Trang chính
        // UserProfileController.cs
        public ActionResult Index(int? id)
        {
            int userId;

            // Nếu có id trong URL thì dùng id đó, nếu không thì dùng user đang đăng nhập
            if (id.HasValue)
            {
                userId = id.Value;
            }
            else
            {
                // Lấy từ session (hoặc fake cho testing)
                userId = GetCurrentUserId();
            }

            var user = db.users.Find(userId);
            if (user == null)
            {
                return HttpNotFound("Không tìm thấy người dùng");
            }

            return View(user);
        }

        // API: Lấy thông tin user
        [HttpGet]
        public JsonResult GetUserInfo()
        {
            try
            {
                int userId = GetCurrentUserId();
                var user = db.users
                    .Where(u => u.Id == userId)
                    .Select(u => new
                    {
                        u.Id,
                        u.Full_Name,
                        u.Email,
                        u.Phone_Number,
                        u.Username
                    })
                    .FirstOrDefault();

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy thông tin người dùng"
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    user
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // API: Cập nhật thông tin cá nhân
        [HttpPost]
        public JsonResult UpdateProfile(string fullName, string phone, string email)
        {
            try
            {
                int userId = GetCurrentUserId();
                var user = db.users.Find(userId);

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy người dùng"
                    });
                }

                // Kiểm tra email đã tồn tại chưa (trừ chính user này)
                if (db.users.Any(u => u.Email == email && u.Id != userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Email đã được sử dụng bởi tài khoản khác"
                    });
                }

                // Cập nhật thông tin
                user.Full_Name = fullName;
                user.Phone_Number = phone;
                user.Email = email;

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thông tin thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

        // API: Đổi mật khẩu
        [HttpPost]
        public JsonResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                int userId = GetCurrentUserId();
                var user = db.users.Find(userId);

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy người dùng"
                    });
                }

                // Kiểm tra mật khẩu hiện tại
                if (user.Password != currentPassword)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mật khẩu hiện tại không đúng"
                    });
                }

                // Kiểm tra mật khẩu mới và xác nhận
                if (newPassword != confirmPassword)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mật khẩu mới không khớp"
                    });
                }

                // Kiểm tra độ mạnh mật khẩu (tuỳ chọn)
                if (newPassword.Length < 6)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mật khẩu phải có ít nhất 6 ký tự"
                    });
                }

                // Cập nhật mật khẩu
                user.Password = newPassword;
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Đổi mật khẩu thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

        // Đăng xuất
        public ActionResult Logout()
        {
            // Xoá session
            Session.Clear();

            // Nếu dùng Forms Authentication
            // FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        // ==================== HÀM HỖ TRỢ ====================
        private int GetCurrentUserId()
        {
            // Fake user ID cho testing - thay bằng Session thực tế
            if (Session["UserId"] != null)
                return Convert.ToInt32(Session["UserId"]);

            return 1; // User ID mặc định cho testing
        }
    }
}