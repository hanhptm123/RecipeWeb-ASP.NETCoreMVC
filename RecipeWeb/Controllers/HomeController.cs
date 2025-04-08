using System.Diagnostics;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;
using RecipeWeb.Models;

namespace RecipeWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly RecipeDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public HomeController(RecipeDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }
        public async Task<IActionResult> Index(string? status)
        {
            int? userId = HttpContext.Session.GetInt32("AccountId");
            List<int> favouriteRecipeIds = new List<int>();

            if (userId.HasValue)
            {
                favouriteRecipeIds = await _context.Favourites
                    .Where(f => f.UserId == userId.Value)
                    .Select(f => f.RecipeId)
                    .ToListAsync();
            }

            IQueryable<Recipe> recipes = _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Origin)
                .Include(r => r.User);

            ViewBag.CurrentStatus = status;
            ViewBag.FavouriteRecipeIds = favouriteRecipeIds;

            return View(await recipes.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavourite(int recipeId)
        {
            int? userId = HttpContext.Session.GetInt32("AccountId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "B?n c?n ??ng nh?p ?? yêu thích công th?c!" });
            }

            var favourite = await _context.Favourites
                .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.RecipeId == recipeId);

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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Origin)
                .Include(r => r.User)
                .Include(r => r.DetailRecipeIngredients)
                    .ThenInclude(dri => dri.Ingredient)
                .FirstOrDefaultAsync(m => m.RecipeId == id);

            if (recipe == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(recipe.Instructions))
            {
                recipe.Instructions = Markdown.ToHtml(recipe.Instructions);
            }

            var userId = HttpContext.Session.GetInt32("AccountId");

            if (userId != null)
            {
                bool isFavourite = await _context.Favourites
                    .AnyAsync(f => f.UserId == userId && f.RecipeId == id);

                ViewBag.IsFavourite = isFavourite;
            }
            else
            {
                ViewBag.IsFavourite = false;
            }

            try
            {
                // N?u CreateAt ch?a có giá tr?, ??t m?c ??nh là th?i ?i?m hi?n t?i
                if (recipe.CreatedAt == null)
                {
                    recipe.CreatedAt = DateTime.UtcNow;
                }

                recipe.UpdateAt = DateTime.UtcNow;
                recipe.Countview = (recipe.Countview ?? 0) + 1;

                _context.Entry(recipe).Property(x => x.Countview).IsModified = true;
                _context.Entry(recipe).Property(x => x.UpdateAt).IsModified = true;
                _context.Entry(recipe).Property(x => x.CreatedAt).IsModified = false; // ??m b?o không b? thay ??i

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Countview & timestamps: {ex.Message}");
            }

            return View(recipe);
        }


        public IActionResult AdminLayout()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
