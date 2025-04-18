﻿using Microsoft.AspNetCore.Mvc;
using RecipeWeb.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeWeb.Models
{
    public class Avatar : ViewComponent
    {
        private readonly RecipeDbContext _context;

        public Avatar(RecipeDbContext context)
        {   
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy UserId từ Session trong HttpContext
            int? userId = HttpContext.Session.GetInt32("AccountId");
            // Nếu chưa đăng nhập, hiển thị avatar mặc định
            if (userId == null)
            {
                return View(new User { Avatar = "/images/default-avatar.jpg" });
            }

            // Tìm user trong database
            var user = await _context.Users.FindAsync(userId);

            // Nếu user không tồn tại, dùng avatar mặc định
            return View(user ?? new User { Avatar = "/images/default-avatar.jpg" });
        }
    }
}
