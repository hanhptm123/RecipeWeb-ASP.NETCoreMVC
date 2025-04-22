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
using Microsoft.AspNetCore.Authorization;

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
        // GET: Recipes
        public async Task<IActionResult> Index(string? status)
        {
            if (string.IsNullOrEmpty(status))
            {
                status = "accepted"; // Mặc định hiển thị danh sách Accepted
            }

            int? userId = HttpContext.Session.GetInt32("AccountId");
            if (!userId.HasValue)
            {
                return View(new List<Recipe>()); // Trả về danh sách rỗng nếu chưa đăng nhập
            }

            List<int> favouriteRecipeIds = await _context.Favourites
                .Where(f => f.UserId == userId.Value)
                .Select(f => f.RecipeId)
                .ToListAsync();

            IQueryable<Recipe> recipes = _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Origin)
                .Include(r => r.User)
                .Where(r => r.UserId == userId.Value); // Chỉ lấy công thức của user hiện tại

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

            recipes = recipes.OrderByDescending(r => r.CreatedAt); // Sắp xếp theo thời gian đăng mới nhất

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
                .ThenInclude(dri => dri.Ingredient)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
                return NotFound();

            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", recipe.CategoryId);
            ViewBag.OriginId = new SelectList(_context.Origins, "OriginId", "OriginName", recipe.OriginId);
            ViewBag.Ingredients = new SelectList(_context.Ingredients, "IngredientId", "IngredientName");

            return View(recipe);
        }

        // POST: Recipes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("RecipeId,RecipeName,Description,Instructions,ImageUrl,CreatedAt,IsApproved,CategoryId,UserId,OriginId,CookTime")]
    Recipe recipe,
            IFormFile? imageFile,
            List<DetailRecipeIngredient> DetailRecipeIngredients)
        {
            if (id != recipe.RecipeId)
                return NotFound();

            // 1) BẮT BUỘC NHẬP ÍT NHẤT 1 NGUYÊN LIỆU
            if (DetailRecipeIngredients == null || !DetailRecipeIngredients.Any())
            {
                ModelState.AddModelError("", "You must enter at least one ingredient.");
            }
            else
            {
                for (int i = 0; i < DetailRecipeIngredients.Count; i++)
                {
                    var item = DetailRecipeIngredients[i];

                    // 2) BẮT BUỘC NHẬP AMOUNT
                    if (string.IsNullOrWhiteSpace(item.Amount))
                    {
                        ModelState.AddModelError($"DetailRecipeIngredients[{i}].Amount", "You must enter the amount.");
                    }

                    // 3) Phải có ingredient chọn sẵn hoặc nhập tay
                    if (item.IngredientId == 0 && string.IsNullOrWhiteSpace(item.IngredientName))
                    {
                        ModelState.AddModelError($"DetailRecipeIngredients[{i}].IngredientName", "You must select or enter an ingredient.");
                    }
                }

                // 4) XỬ LÝ TÊN NGUYÊN LIỆU NHẬP TAY → chuyển thành ID
                for (int i = 0; i < DetailRecipeIngredients.Count; i++)
                {
                    var item = DetailRecipeIngredients[i];
                    if (!string.IsNullOrWhiteSpace(item.IngredientName))
                    {
                        var existing = await _context.Ingredients
                            .FirstOrDefaultAsync(i => i.IngredientName.ToLower() == item.IngredientName.ToLower());

                        if (existing != null)
                        {
                            item.IngredientId = existing.IngredientId;
                        }
                        else
                        {
                            var newIngredient = new Ingredient { IngredientName = item.IngredientName.Trim() };
                            _context.Ingredients.Add(newIngredient);
                            await _context.SaveChangesAsync();
                            item.IngredientId = newIngredient.IngredientId;
                        }
                    }
                }

                // 5) TRÁNH TRÙNG LẶP NGUYÊN LIỆU (dù là chọn hay nhập tay)
                var duplicateIds = DetailRecipeIngredients
                    .GroupBy(x => x.IngredientId)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.Select((item, idx) => new { item, idx }))
                    .ToList();

                foreach (var dup in duplicateIds)
                {
                    ModelState.AddModelError($"DetailRecipeIngredients[{dup.idx}].IngredientId", "Duplicate ingredient.");
                }
            }

            if (ModelState.IsValid)
            {
                var existingRecipe = await _context.Recipes
                    .Include(r => r.DetailRecipeIngredients)
                    .FirstOrDefaultAsync(r => r.RecipeId == id);

                if (existingRecipe == null)
                    return NotFound();

                // Cập nhật thông tin chung
                existingRecipe.RecipeName = recipe.RecipeName;
                existingRecipe.Description = recipe.Description;
                existingRecipe.Instructions = recipe.Instructions;
                existingRecipe.CategoryId = recipe.CategoryId;
                existingRecipe.OriginId = recipe.OriginId;
                existingRecipe.CookTime = recipe.CookTime;
                existingRecipe.IsApproved = recipe.IsApproved;
                existingRecipe.UpdateAt = DateTime.Now;

                // Xử lý ảnh
                if (imageFile != null)
                {
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
                    string uniqueFileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using var fs = new FileStream(filePath, FileMode.Create);
                    await imageFile.CopyToAsync(fs);
                    existingRecipe.ImageUrl = "/images/" + uniqueFileName;
                }

                // XÓA những nguyên liệu không còn
                var toRemove = existingRecipe.DetailRecipeIngredients
                    .Where(old => !DetailRecipeIngredients.Any(n => n.IngredientId == old.IngredientId))
                    .ToList();

                foreach (var old in toRemove)
                    existingRecipe.DetailRecipeIngredients.Remove(old);

                // THÊM mới hoặc cập nhật
                foreach (var newItem in DetailRecipeIngredients)
                {
                    var exist = existingRecipe.DetailRecipeIngredients
                        .FirstOrDefault(d => d.IngredientId == newItem.IngredientId);

                    if (exist != null)
                    {
                        exist.Amount = newItem.Amount;
                    }
                    else
                    {
                        existingRecipe.DetailRecipeIngredients.Add(new DetailRecipeIngredient
                        {
                            RecipeId = id,
                            IngredientId = newItem.IngredientId,
                            Amount = newItem.Amount
                        });
                    }
                }

                _context.Update(existingRecipe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Trả lại view khi lỗi
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", recipe.CategoryId);
            ViewBag.OriginId = new SelectList(_context.Origins, "OriginId", "OriginName", recipe.OriginId);
            ViewBag.Ingredients = new SelectList(_context.Ingredients, "IngredientId", "IngredientName");
            recipe.DetailRecipeIngredients = DetailRecipeIngredients;
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
                .Include(r => r.Favourites) // ⚠️ Nạp thêm danh sách yêu thích liên quan
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
            {
                return NotFound();
            }

            _context.DetailRecipeIngredients.RemoveRange(recipe.DetailRecipeIngredients);

            _context.Favourites.RemoveRange(recipe.Favourites);

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

            // Lọc theo trạng thái nếu có
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

            ViewBag.CurrentStatus = status;
            return View(recipes.ToList());
        }

        [HttpPost]
        public IActionResult Approve(int id, bool isApproved, string? rejectReason)
        {
            var recipe = _context.Recipes.FirstOrDefault(r => r.RecipeId == id);
            if (recipe == null)
            {
                return NotFound();
            }

            recipe.IsApproved = isApproved;

            // Nếu bị từ chối, lưu lý do từ chối
            if (!isApproved)
            {
                recipe.RejectReason = rejectReason;
            }
            else
            {
                recipe.RejectReason = null; // reset lại nếu từng bị từ chối
            }

            _context.SaveChanges();

            return RedirectToAction("ApproveList");
        }
        [HttpGet]
        public async Task<IActionResult> SearchByCategory(int? categoryId)
        {
            var recipes = await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Origin)
                .Include(r => r.User)
                .Where(r => r.IsApproved == true && (!categoryId.HasValue || r.CategoryId == categoryId))
                .ToListAsync();

            var rankedRecipes = recipes
                .Select(recipe => new
                {
                    Recipe = recipe,
                    AvgRating = _context.Ratings
                        .Where(r => r.RecipeId == recipe.RecipeId)
                        .Average(r => (double?)r.RatingScore) ?? 0
                })
                .OrderByDescending(r => r.AvgRating)
                .Select(r => r.Recipe)
                .ToList();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(rankedRecipes);
        }


        [HttpGet]
        public async Task<IActionResult> SearchByIngredients(string ingredients)
        {
            if (string.IsNullOrEmpty(ingredients))
            {
                return View(new List<Recipe>()); // Trả về danh sách rỗng nếu không nhập nguyên liệu
            }

            var ingredientList = ingredients.Split(',')
                .Select(i => i.Trim().ToLower())
                .ToList();

            var recipes = await _context.Recipes
                .Include(r => r.DetailRecipeIngredients)
                .ThenInclude(dri => dri.Ingredient)
                .Include(r => r.Category)
                .Include(r => r.Origin)
                .Include(r => r.User)
                .Where(r => r.IsApproved == true) // Chỉ lấy công thức đã được duyệt
                .ToListAsync();

            var rankedRecipes = recipes
                 .Select(recipe => new
                 {
                     Recipe = recipe,
                     MatchCount = recipe.DetailRecipeIngredients
                         .Count(dri => ingredientList.Contains(dri.Ingredient.IngredientName.ToLower())),
                     AvgRating = _context.Ratings
                         .Where(r => r.RecipeId == recipe.RecipeId)
                         .Average(r => (double?)r.RatingScore) ?? 0
                 })
                 .Where(r => r.MatchCount > 0)
                 .OrderByDescending(r => r.MatchCount)
                 .ThenByDescending(r => r.AvgRating)
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
             .Include(r => r.Origin)
            .Include(r => r.Category)
            .Include(r => r.User)
            .Where(r => r.RecipeName.Contains(searchTerm) && r.IsApproved == true)
            .ToListAsync();
            var rankedRecipes = recipes
                .Select(recipe => new
                {
                    Recipe = recipe,
                    AvgRating = _context.Ratings
                        .Where(r => r.RecipeId == recipe.RecipeId)
                        .Average(r => (double?)r.RatingScore) ?? 0
                })
                .OrderByDescending(r => r.AvgRating)
                .Select(r => r.Recipe)
                .ToList();

            return View(rankedRecipes);

        }
    }
}
