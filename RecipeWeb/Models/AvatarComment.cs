using Microsoft.AspNetCore.Mvc;
using RecipeWeb.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeWeb.Models
{
    public class AvatarComment : ViewComponent
    {
        private readonly RecipeDbContext _context;

        public AvatarComment(RecipeDbContext context)
        {   
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int userId)
        {
            // Tìm user theo userId được truyền từ View
            var user = await _context.Users.FindAsync(userId);

            // Nếu user không có hoặc avatar rỗng, dùng avatar mặc định
            return View(user ?? new User { Avatar = "/images/default-avatar.jpg" });
        }
    }
}
