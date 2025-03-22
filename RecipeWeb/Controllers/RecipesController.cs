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
using System.Security.Claims;

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
        public async Task<IActionResult> Index(string? status)
        {
            if (string.IsNullOrEmpty(status))
            {
                status = "accepted"; // Mặc định hiển thị danh sách Accepted
            }

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

            switch (status)
            {
                case "accepted":
                    recipes = recipes.Where(r => r.IsApproved == true);
                    break;
                case "cancelled":
                    recipes = recipes.Where(r => r.IsApproved == false);
                    break;
                default:
                    recipes = recipes.Where(r => r.IsApproved == null);
                    break;
            }

            recipes = recipes.OrderByDescending(r => r.CreatedAt); // Sắp xếp công thức theo thời gian đăng mới nhất

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
                return Json(new { success = false, message = "Bạn cần đăng nhập để yêu thích công thức!" });
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
                .Include(r => r.DetailRecipeIngredients)
                    .ThenInclude(dri => dri.Ingredient)
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

            // Kiểm tra người dùng đã đăng nhập chưa
            var userId = HttpContext.Session.GetInt32("AccountId");

            if (userId != null)
            {
                // Kiểm tra công thức này có trong danh sách yêu thích của người dùng hay không
                bool isFavourite = await _context.Favourites
                    .AnyAsync(f => f.UserId == userId && f.RecipeId == id);

                ViewBag.IsFavourite = isFavourite; // Truyền xuống View
            }
            else
            {
                ViewBag.IsFavourite = false;
            }

            // Tăng số lượt xem
            try
            {
                recipe.Countview = (recipe.Countview ?? 0) + 1;
                _context.Entry(recipe).Property(x => x.Countview).IsModified = true;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Countview: {ex.Message}");
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
    [Bind("RecipeId,RecipeName,Description,Instructions,ImageFile,CreatedAt,IsApproved,CategoryId,OriginId,CookTime")] Recipe recipe,
    List<string> IngredientNames, // Danh sách tên nguyên liệu
    List<string> IngredientAmounts // Danh sách số lượng nguyên liệu
)
        {
            // Kiểm tra nếu người dùng đã đăng nhập
            int? userId = HttpContext.Session.GetInt32("AccountId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account"); // Chuyển hướng nếu chưa đăng nhập
            }

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
                // Thiết lập UserId từ session
                recipe.UserId = userId.Value;
                recipe.CreatedAt = DateTime.Now;

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
        [HttpGet]
        public async Task<IActionResult> TopRecipes(int? month, int? year)
        {
            // Nếu không có tháng và năm, mặc định lấy tháng và năm hiện tại
            if (!month.HasValue || !year.HasValue)
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }

            var topRecipes = await _context.Recipes
                .Where(r => r.CreatedAt.HasValue && r.CreatedAt.Value.Month == month && r.CreatedAt.Value.Year == year)
                .OrderByDescending(r => r.Countview)
                .Take(20)
                .ToListAsync();

            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;

            return View(topRecipes);
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
        [HttpGet]
        public async Task<IActionResult> SearchByCategory(int? categoryId)
        {
            // Lấy danh sách tất cả loại món ăn để hiển thị trong dropdown
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Nếu không có categoryId, hiển thị toàn bộ công thức
            var recipes = _context.Recipes.Include(r => r.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                recipes = recipes.Where(r => r.CategoryId == categoryId);
            }

            var result = await recipes.ToListAsync();
            return View(result);
        }
        [HttpGet]
        public async Task<IActionResult> SearchByIngredients(string ingredients)
        {
            if (string.IsNullOrEmpty(ingredients))
            {
                return View(new List<Recipe>()); // Trả về danh sách rỗng nếu không có nguyên liệu nhập vào
            }

            // Chuyển danh sách nguyên liệu thành mảng
            var ingredientList = ingredients.Split(',').Select(i => i.Trim().ToLower()).ToList();

            // Lấy danh sách công thức cùng với nguyên liệu của chúng
            var recipes = await _context.Recipes
                .Include(r => r.DetailRecipeIngredients)
                .ThenInclude(dri => dri.Ingredient)
                .ToListAsync();

            // Lọc công thức dựa trên số lượng nguyên liệu trùng khớp
            var rankedRecipes = recipes
                .Select(recipe => new
                {
                    Recipe = recipe,
                    MatchCount = recipe.DetailRecipeIngredients
                        .Count(dri => ingredientList.Contains(dri.Ingredient.IngredientName.ToLower()))
                })
                .Where(r => r.MatchCount > 0) // Loại bỏ công thức không có nguyên liệu trùng
                .OrderByDescending(r => r.MatchCount) // Sắp xếp theo số nguyên liệu trùng giảm dần
                .Select(r => r.Recipe)
                .ToList();

            return View(rankedRecipes);
        }
        [HttpGet]
        public async Task<IActionResult> SearchByName(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return View(new List<Recipe>());
            }

            var recipes = await _context.Recipes
                .Where(r => r.RecipeName.Contains(searchTerm))
                .ToListAsync();

            return View(recipes);
        }
    }
}
