using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Identity.Client;

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
                TempData["ErrorMessage"] = "Your account has been banned!";
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
                IsBanned = false // Mặc định không bị cấm
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful! Please log in.";

            return RedirectToAction("Login");
        }

    }
}
