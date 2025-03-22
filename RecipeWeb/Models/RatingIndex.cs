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
            var ratings = await _context.Ratings
                .Where(r => r.RecipeId == recipeId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var groupedRatings = ratings.GroupBy(r => r.RatingScore)
                .ToDictionary(g => g.Key ?? 0, g => g.Count());

            var averageRating = ratings.Any() ? ratings.Average(r => r.RatingScore ?? 0) : 0;

            var viewModel = new RatingViewModel
            {
                Ratings = ratings,
                AverageRating = averageRating,
                RatingGroups = groupedRatings
            };

            return View(viewModel);
        }
    }

    public class RatingViewModel
    {
        public List<Rating> Ratings { get; set; }
        public double AverageRating { get; set; }
        public Dictionary<int, int> RatingGroups { get; set; }
    }
}
