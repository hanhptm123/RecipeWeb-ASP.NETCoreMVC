using System;
using System.Collections.Generic;

namespace RecipeWeb.Data;

public partial class Origin
{
    public int OriginId { get; set; }

    public string OriginName { get; set; } = null!;

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
