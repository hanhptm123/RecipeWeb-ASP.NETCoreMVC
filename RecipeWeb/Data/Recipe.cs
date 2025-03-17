using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeWeb.Data;

public partial class Recipe
{
    public int? Countview { get; set; }
    public int RecipeId { get; set; }

    [Required(ErrorMessage = "Recipe name is required.")]
    public string? RecipeName { get; set; }

    [Required(ErrorMessage = "Description name is required.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Instructions name is required.")]
    public string? Instructions { get; set; }

    public string? ImageUrl { get; set; }

    [NotMapped]
    public IFormFile? ImageFile { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsApproved { get; set; }

    public int? CategoryId { get; set; }

    public int? UserId { get; set; }

    public int? OriginId { get; set; }


    [Range(1, int.MaxValue, ErrorMessage = "Cook time must be greater than 0.")]
    public int? CookTime { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<DetailRecipeIngredient> DetailRecipeIngredients { get; set; } = new List<DetailRecipeIngredient>();

    public virtual ICollection<Favourite> Favourites { get; set; } = new List<Favourite>();

    public virtual Origin? Origin { get; set; }

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual User? User { get; set; }
}
