using System;
using System.Collections.Generic;

namespace RecipeWeb.Data;

public partial class Recipe
{
    public int RecipeId { get; set; }

    public string? RecipeName { get; set; }

    public string? Description { get; set; }

    public string? Instructions { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsApproved { get; set; }

    public int? CategoryId { get; set; }

    public int? UserId { get; set; }

    public int? OriginId { get; set; }

    public int? CookTime { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<DetailRecipeIngredient> DetailRecipeIngredients { get; set; } = new List<DetailRecipeIngredient>();

    public virtual ICollection<Favourite> Favourites { get; set; } = new List<Favourite>();

    public virtual Origin? Origin { get; set; }

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual User? User { get; set; }
}
