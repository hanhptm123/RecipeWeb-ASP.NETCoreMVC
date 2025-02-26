using System;
using System.Collections.Generic;

namespace RecipeWeb.Data;

public partial class Category
{
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
