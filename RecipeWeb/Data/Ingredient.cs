using System;
using System.Collections.Generic;

namespace RecipeWeb.Data;

public partial class Ingredient
{
    public int IngredientId { get; set; }

    public string? IngredientName { get; set; }

    public virtual ICollection<DetailRecipeIngredient> DetailRecipeIngredients { get; set; } = new List<DetailRecipeIngredient>();
}
