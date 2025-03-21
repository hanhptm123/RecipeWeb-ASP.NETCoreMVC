using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeWeb.Controllers
{
    public class RatingsController : Controller
    {
        private readonly RecipeDbContext _context;

        public RatingsController(RecipeDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRating(Rating rating)
        {
            if (rating.RatingScore < 1 || rating.RatingScore > 5)
            {
                ModelState.AddModelError("RatingScore", "Rating must be between 1 and 5.");
                return RedirectToAction("Details", "Recipes", new { id = rating.RecipeId });
            }

            var userId = HttpContext.Session.GetInt32("AccountId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Users");
            }

            rating.UserId = userId.Value;
            rating.CreatedAt = DateTime.Now;

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == rating.RecipeId);

            if (existingRating != null)
            {
                existingRating.RatingScore = rating.RatingScore;
                existingRating.Commnent = rating.Commnent;
            }
            else
            {
                _context.Ratings.Add(rating);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Recipes", new { id = rating.RecipeId });
        }
    }
}
