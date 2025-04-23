using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace RecipeWeb.Controllers
{
    public class UsersController : Controller
    {
        private readonly RecipeDbContext _context;

        public UsersController(RecipeDbContext context)
        {
            _context = context;
        }

        // Feature: Login
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var account = _context.Users.Where(t => t.Email == email && t.Password == password).FirstOrDefault<User>();

            if (account == null)
            {
                TempData["ErrorMessage"] = "Invalid username or password.";
                return RedirectToAction("Login");
            }
            // Kiểm tra nếu tài khoản bị cấm
            if (account.IsBanned == true)
            {
                TempData["BannedMessage"] = "Your account has been banned!";
                TempData["BanReason"] = account.BanReason;
                return RedirectToAction("Login");
            }

            TempData["SuccessMessage"] = "Login successful! ";

            HttpContext.Session.SetInt32("AccountId", account.UserId);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, account.Email),
        new Claim(ClaimTypes.Role, account.Role),
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Chuyển hướng đến Home và giữ lại thông báo
            return RedirectToAction("Index", "Home");
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string userName, string email, string password)
        {
            // Kiểm tra độ mạnh của mật khẩu
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$");
            if (!passwordRegex.IsMatch(password))
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.";
                return RedirectToAction("Register");
            }

            // Kiểm tra xem email đã tồn tại chưa
            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["ErrorMessage"] = "Email is already registered.";
                return RedirectToAction("Register");
            }

            // Tạo tài khoản mới
            var newUser = new User
            {
                UserName = userName,
                Email = email,
                Password = password,
                Role = "User",   // Mặc định là User
                IsBanned = false, // Mặc định không bị cấm
                Avatar = "/Images/default-avatar.jpg"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful! Please log in.";

            return RedirectToAction("Login");
        }
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32("AccountId");

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        public IActionResult EditProfile()
        {
            int? userId = HttpContext.Session.GetInt32("AccountId");

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(User updatedUser, IFormFile? avatarFile)
        {
            int? userId = HttpContext.Session.GetInt32("AccountId");

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.UserName = updatedUser.UserName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Address = updatedUser.Address;
            user.Gender = updatedUser.Gender;

            // Xử lý cập nhật avatar nếu có tải lên file mới
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(avatarFile.FileName);
                var filePath = Path.Combine("wwwroot/uploads", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                user.Avatar = "/uploads/" + fileName;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["EditSuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        [Authorize(Roles = "Admin")]
        // Feature: User Management
        [HttpGet]
        public IActionResult UserManagement(string? status)
        {
            IQueryable<User> users = _context.Users.Where(u => u.Role != "Admin"); // Loại bỏ Admin

            switch (status)
            {
                case "blocked":
                    users = users.Where(u => u.IsBanned == true);
                    break;
                default:
                    users = users.Where(u => u.IsBanned == false);
                    break;
            }

            ViewBag.CurrentStatus = status;
            return View(users.ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult BanUser(int id, bool isBanned, string? banReason, string? customReason)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsBanned = isBanned;

                // Save reason only when banning
                if (isBanned)
                {
                    user.BanReason = banReason == "Other" ? customReason : banReason;
                }
                else
                {
                    user.BanReason = null; // Clear reason when unbanning
                }

                _context.SaveChanges();
            }
            return RedirectToAction("UserManagement", new { status = isBanned ? "blocked" : "active" });
        }


    }
}
