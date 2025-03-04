using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;

namespace RecipeWeb.Controllers
{
    public class RecipesController : Controller
    {
        private readonly RecipeDbContext _context;

        public RecipesController(RecipeDbContext context)
        {
            _context = context;
        }

        // GET: Recipes
        public async Task<IActionResult> Index()
        {
            var recipeDbContext = _context.Recipes.Include(r => r.Category).Include(r => r.Origin).Include(r => r.User);
            return View(await recipeDbContext.ToListAsync());
        }

        // GET: Recipes/Details/5
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
                .FirstOrDefaultAsync(m => m.RecipeId == id);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        // GET: Recipes/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId");
            ViewData["OriginId"] = new SelectList(_context.Origins, "OriginId", "OriginId");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Recipes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RecipeId,RecipeName,Description,Instructions,ImageUrl,CreatedAt,IsApproved,CategoryId,UserId,OriginId,CookTime")] Recipe recipe)
        {
            if (ModelState.IsValid)
            {
                _context.Add(recipe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", recipe.CategoryId);
            ViewData["OriginId"] = new SelectList(_context.Origins, "OriginId", "OriginId", recipe.OriginId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", recipe.UserId);
            return View(recipe);
        }

        // GET: Recipes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", recipe.CategoryId);
            ViewData["OriginId"] = new SelectList(_context.Origins, "OriginId", "OriginId", recipe.OriginId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", recipe.UserId);
            return View(recipe);
        }

        // POST: Recipes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RecipeId,RecipeName,Description,Instructions,ImageUrl,CreatedAt,IsApproved,CategoryId,UserId,OriginId,CookTime")] Recipe recipe)
        {
            if (id != recipe.RecipeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recipe);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipeExists(recipe.RecipeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", recipe.CategoryId);
            ViewData["OriginId"] = new SelectList(_context.Origins, "OriginId", "OriginId", recipe.OriginId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", recipe.UserId);
            return View(recipe);
        }

        // GET: Recipes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Origin)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.RecipeId == id);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        // POST: Recipes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecipeExists(int id)
        {
            return _context.Recipes.Any(e => e.RecipeId == id);
        }
        [HttpGet]
        public IActionResult ApproveList(string? status)
        {
            IQueryable<Recipe> recipes = _context.Recipes;

            switch (status)
            {
                case "accepted":
                    recipes = recipes.Where(r => r.IsApproved == true);
                    break;
                case "cancelled":
                    recipes = recipes.Where(r => r.IsApproved == false);
                    break;
                default: // Mặc định là danh sách chờ duyệt
                    recipes = recipes.Where(r => r.IsApproved == null);
                    break;
            }

            ViewBag.CurrentStatus = status;
            return View(recipes.ToList());
        }



        // Xử lý duyệt hoặc từ chối công thức
        [HttpPost]
        public IActionResult Approve(int id, bool isApproved)
        {
            var recipe = _context.Recipes.Find(id);
            if (recipe != null)
            {
                recipe.IsApproved = isApproved; // Gán trực tiếp true/false
                _context.SaveChanges();
            }
            return RedirectToAction("ApproveList");
        }

    }
}
