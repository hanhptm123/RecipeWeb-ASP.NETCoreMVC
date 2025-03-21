using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeWeb.Models
{
    public class RatingIndex : ViewComponent
    {
        private readonly RecipeDbContext _context;

        public RatingIndex(RecipeDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int recipeId)
        {
            var userId = HttpContext.Session.GetInt32("AccountId");

            var ratings = await _context.Ratings
                .Where(r => r.RecipeId == recipeId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(); // LUÔN đảm bảo đây là danh sách!

            if (userId.HasValue)
            {
                var userRating = ratings.FirstOrDefault(r => r.UserId == userId);
                if (userRating != null)
                {
                    ratings.Remove(userRating);
                    ratings.Insert(0, userRating);
                }
            }

            return View(ratings); // Phải là danh sách, không được là một đối tượng đơn lẻ
        }

    }
}
