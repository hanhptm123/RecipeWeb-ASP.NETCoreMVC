using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;
using Markdig;
using Microsoft.AspNetCore.Hosting;

namespace RecipeWeb.Controllers
{
    public class RecipesController : Controller
    {
        private readonly RecipeDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public RecipesController(RecipeDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
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
                .Include(r => r.DetailRecipeIngredients) // Load danh sách nguyên liệu
                    .ThenInclude(dri => dri.Ingredient) // Load tên nguyên liệu
                .FirstOrDefaultAsync(m => m.RecipeId == id);

            if (recipe == null)
            {
                return NotFound();
            }

            // Chuyển đổi Markdown thành HTML
            if (!string.IsNullOrEmpty(recipe.Instructions))
            {
                recipe.Instructions = Markdown.ToHtml(recipe.Instructions);
            }

            return View(recipe);
        }

        // GET: Recipes/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");

            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            ViewBag.OriginId = new SelectList(_context.Origins, "OriginId", "OriginName");
            return View();
        }

        // POST: Recipes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Recipes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [Bind("RecipeId,RecipeName,Description,Instructions,ImageFile,CreatedAt,IsApproved,CategoryId,UserId,OriginId,CookTime")] Recipe recipe,
    List<string> IngredientNames, // Danh sách tên nguyên liệu
    List<string> IngredientAmounts // Danh sách số lượng nguyên liệu
)
        {
            // Kiểm tra nếu bất kỳ trường nào bị thiếu
            if (string.IsNullOrWhiteSpace(recipe.RecipeName) ||
                string.IsNullOrWhiteSpace(recipe.Description) ||
                string.IsNullOrWhiteSpace(recipe.Instructions) ||
                recipe.CategoryId == 0 ||
                recipe.OriginId == 0 ||
                recipe.CookTime <= 0 ||
                IngredientNames == null || IngredientAmounts == null ||
                IngredientNames.Count == 0 || IngredientAmounts.Count == 0 ||
                IngredientNames.All(string.IsNullOrWhiteSpace) || IngredientAmounts.All(string.IsNullOrWhiteSpace))
            {
                ModelState.AddModelError(string.Empty, "All fields must be filled, including at least one ingredient.");
            }

            if (ModelState.IsValid)
            {
                // Xử lý ảnh nếu có tải lên
                if (recipe.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder); // Đảm bảo thư mục tồn tại

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(recipe.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await recipe.ImageFile.CopyToAsync(fileStream);
                    }

                    recipe.ImageUrl = "/uploads/" + uniqueFileName; // Lưu đường dẫn vào DB
                }

                recipe.CreatedAt = DateTime.Now;
                _context.Add(recipe);
                await _context.SaveChangesAsync();

                // Thêm nguyên liệu vào bảng DetailRecipeIngredients nếu hợp lệ
                for (int i = 0; i < IngredientNames.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(IngredientNames[i]) && !string.IsNullOrWhiteSpace(IngredientAmounts[i]))
                    {
                        var existingIngredient = _context.Ingredients.FirstOrDefault(ing => ing.IngredientName == IngredientNames[i].Trim());

                        if (existingIngredient == null)
                        {
                            existingIngredient = new Ingredient { IngredientName = IngredientNames[i].Trim() };
                            _context.Ingredients.Add(existingIngredient);
                            await _context.SaveChangesAsync();
                        }

                        var detailIngredient = new DetailRecipeIngredient
                        {
                            RecipeId = recipe.RecipeId,
                            IngredientId = existingIngredient.IngredientId,
                            Amount = IngredientAmounts[i].Trim()
                        };
                        _context.DetailRecipeIngredients.Add(detailIngredient);
                    }
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ, hiển thị lại form với thông báo lỗi
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", recipe.CategoryId);
            ViewBag.OriginId = new SelectList(_context.Origins, "OriginId", "OriginName", recipe.OriginId);
            return View(recipe);
        }


        // GET: Recipes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.DetailRecipeIngredients)
                .ThenInclude(dri => dri.Ingredient) // Load thông tin thành phần từ bảng Ingredient
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
            {
                return NotFound();
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", recipe.CategoryId);
            ViewBag.OriginId = new SelectList(_context.Origins, "OriginId", "OriginName", recipe.OriginId);
            ViewBag.Ingredients = new SelectList(_context.Ingredients, "IngredientId", "IngredientName"); // Danh sách tất cả thành phần có thể chọn

            return View(recipe);
        }


        // POST: Recipes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RecipeId,RecipeName,Description,Instructions,ImageUrl,CreatedAt,IsApproved,CategoryId,UserId,OriginId,CookTime")] Recipe recipe, IFormFile? imageFile, List<DetailRecipeIngredient> DetailRecipeIngredients)
        {
            if (id != recipe.RecipeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRecipe = await _context.Recipes
                        .Include(r => r.DetailRecipeIngredients)
                        .FirstOrDefaultAsync(r => r.RecipeId == id);

                    if (existingRecipe == null)
                    {
                        return NotFound();
                    }

                    existingRecipe.RecipeName = recipe.RecipeName;
                    existingRecipe.Description = recipe.Description;
                    existingRecipe.Instructions = recipe.Instructions;
                    existingRecipe.CategoryId = recipe.CategoryId;
                    existingRecipe.OriginId = recipe.OriginId;
                    existingRecipe.CookTime = recipe.CookTime;
                    existingRecipe.IsApproved = recipe.IsApproved;

                    if (imageFile != null)
                    {
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        existingRecipe.ImageUrl = "/images/" + uniqueFileName;
                    }

                    // 🔥 Cập nhật danh sách nguyên liệu (DetailRecipeIngredients)
                    existingRecipe.DetailRecipeIngredients.Clear();
                    foreach (var item in DetailRecipeIngredients)
                    {
                        existingRecipe.DetailRecipeIngredients.Add(new DetailRecipeIngredient
                        {
                            RecipeId = id,
                            IngredientId = item.IngredientId,
                            Amount = item.Amount
                        });
                    }

                    _context.Update(existingRecipe);
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

            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", recipe.CategoryId);
            ViewBag.OriginId = new SelectList(_context.Origins, "OriginId", "OriginName", recipe.OriginId);
            ViewBag.Ingredients = new SelectList(_context.Ingredients, "IngredientId", "IngredientName");

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.DetailRecipeIngredients)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
            {
                return NotFound();
            }

            _context.DetailRecipeIngredients.RemoveRange(recipe.DetailRecipeIngredients);
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private bool RecipeExists(int id)
        {
            return _context.Recipes.Any(e => e.RecipeId == id);
        }
    }
}
