using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RecipeWeb.Controllers
{
    public class FavouritesController : Controller
    {
        private readonly RecipeDbContext _context;

        public FavouritesController(RecipeDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavourite(int recipeId)
        {
            var userId = HttpContext.Session.GetInt32("AccountId");

            if (userId == null)
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var favourite = await _context.Favourites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.RecipeId == recipeId);

            if (favourite == null)
            {
                _context.Favourites.Add(new Favourite
                {
                    UserId = userId.Value,
                    RecipeId = recipeId
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, isFavourite = true });
            }
            else
            {
                _context.Favourites.Remove(favourite);
                await _context.SaveChangesAsync();

                return Json(new { success = true, isFavourite = false });
            }
        }

        public async Task<IActionResult> MyFavourites()
        {
            var userId = HttpContext.Session.GetInt32("AccountId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var favouriteRecipes = await _context.Favourites
                .Where(f => f.UserId == userId)
                .Include(f => f.Recipe)
                .ThenInclude(r => r.Category)  
                .ToListAsync();

            return View(favouriteRecipes);
        }
        [HttpPost]
        public async Task<IActionResult> RemoveFromFavourites(int recipeId)
        {
            var userId = HttpContext.Session.GetInt32("AccountId");

            if (userId == null)
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var favourite = await _context.Favourites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.RecipeId == recipeId);

            if (favourite != null)
            {
                _context.Favourites.Remove(favourite);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Recipe not found in favourites" });
        }

        public async Task<IActionResult> TopFavouriteRecipes()
        {
            var topRecipes = await _context.Favourites
                .Include(f => f.Recipe)
                    .ThenInclude(r => r.Category)
                .GroupBy(f => f.Recipe)
                .Select(g => new
                {
                    Recipe = g.Key,
                    FavouriteCount = g.Count()  
                })
                .OrderByDescending(g => g.FavouriteCount)
                .Take(10)  
                .ToListAsync();

            var viewModel = topRecipes
                .Select((r, index) => new TopRecipeViewModel
                {
                    Rank = index + 1, 
                    Recipe = r.Recipe,
                    FavouriteCount = r.FavouriteCount
                })
                .ToList();

            return View(viewModel);
        }
    }
}
