using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeWeb.Data;

public partial class Recipe
{
    public int RecipeId { get; set; }

    public string? RecipeName { get; set; }

    public string? Description { get; set; }

    public string? Instructions { get; set; }

    public string? ImageUrl { get; set; }
    [NotMapped]
    public IFormFile? ImageFile { get; set; }

    public DateTime? CreatedAt { get; set; }
    public string? RejectReason { get; set; }


    public bool? IsApproved { get; set; }

    public int? CategoryId { get; set; }

    public int? UserId { get; set; }

    public int? OriginId { get; set; }

    public int? CookTime { get; set; }

    public int? Countview { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<DetailRecipeIngredient> DetailRecipeIngredients { get; set; } = new List<DetailRecipeIngredient>();

    public virtual ICollection<Favourite> Favourites { get; set; } = new List<Favourite>();

    public virtual Origin? Origin { get; set; }

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual User? User { get; set; }
}