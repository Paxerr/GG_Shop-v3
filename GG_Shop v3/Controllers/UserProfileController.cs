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
        public ActionResult Index()
        {
            // KIỂM TRA ĐĂNG NHẬP TRƯỚC
            if (!IsUserLoggedIn())
            {
                // Lưu URL hiện tại để quay lại sau login
                string returnUrl = Request.Url?.PathAndQuery;
                return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
            }

            int userId = GetCurrentUserId();
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
                if (!IsUserLoggedIn())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng đăng nhập",
                        redirect = Url.Action("Login", "Account",
                                   new { returnUrl = "/UserProfile/Index" })
                    }, JsonRequestBehavior.AllowGet);
                }

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
                if (!IsUserLoggedIn())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                    });
                }

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

                // CẬP NHẬT SESSION nếu có
                if (Session["User"] != null)
                {
                    var sessionUser = Session["User"] as User;
                    if (sessionUser != null)
                    {
                        sessionUser.Full_Name = fullName;
                        sessionUser.Email = email;
                        sessionUser.Phone_Number = phone;
                        Session["User"] = sessionUser;
                    }
                }

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
                if (!IsUserLoggedIn())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn"
                    });
                }

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

                // Kiểm tra độ mạnh mật khẩu
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
            Session.Remove("User_Id");
            Session.Remove("User_Role");
            Session.Remove("User");
            Session.Clear();
            Session.Abandon();

            // Xóa session cookie
            if (Response.Cookies["ASP.NET_SessionId"] != null)
            {
                Response.Cookies["ASP.NET_SessionId"].Value = string.Empty;
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddMonths(-20);
            }

            return RedirectToAction("Login", "Account");
        }

        // ==================== HÀM HỖ TRỢ ====================
        private int GetCurrentUserId()
        {
            // SỬA: Kiểm tra cả "User_Id" và "UserId" để tương thích
            if (Session["User_Id"] != null)
            {
                try
                {
                    int userId = Convert.ToInt32(Session["User_Id"]);
                    return userId;
                }
                catch
                {
                    return 0;
                }
            }
            else if (Session["UserId"] != null) // Dự phòng
            {
                try
                {
                    int userId = Convert.ToInt32(Session["UserId"]);
                    return userId;
                }
                catch
                {
                    return 0;
                }
            }

            return 0;
        }

        [NonAction]
        public bool IsUserLoggedIn()
        {
            return GetCurrentUserId() > 0;
        }
    }
}