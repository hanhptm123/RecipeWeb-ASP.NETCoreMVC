using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;

namespace RecipeWeb.Models
{
    public class RatingAverage : ViewComponent
    {
        private readonly RecipeDbContext _context;

        public RatingAverage(RecipeDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(int recipeId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.RecipeId == recipeId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var groupedRatings = ratings.GroupBy(r => r.RatingScore)
                .ToDictionary(g => g.Key ?? 0, g => g.Count());

            var averageRating = ratings.Any() ? ratings.Average(r => r.RatingScore ?? 0) : 0;
            int totalRatings = ratings.Count; // Đếm số lượng đánh giá

            var viewModel = new RatingViewModel
            {
                Ratings = ratings,
                AverageRating = averageRating,
                RatingGroups = groupedRatings,
                TotalRatings = totalRatings // Gán số lượng đánh giá
            };

            return View(viewModel);
        }
    }
}
