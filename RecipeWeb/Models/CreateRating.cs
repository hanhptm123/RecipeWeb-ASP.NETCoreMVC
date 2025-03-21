using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeWeb.Models
{
    public class CreateRating : ViewComponent
    {
        private readonly RecipeDbContext _context;

        public CreateRating(RecipeDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int recipeId)
        {
            var userId = HttpContext.Session.GetInt32("AccountId");
            if (userId == null)
            {
                return View("NotLoggedIn");
            }

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId);

            if (existingRating != null)
            {
                return Content(string.Empty); // Không hiển thị gì nếu đã đánh giá
            }

            var model = new Rating
            {
                RecipeId = recipeId,
                UserId = userId.Value,
                CreatedAt = DateTime.Now,
                RatingScore = 1
            };

            return View(model);
        }

    }
}
