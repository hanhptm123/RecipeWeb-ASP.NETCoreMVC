using System;
using System.Collections.Generic;

namespace RecipeWeb.Data;

public partial class User
{
    public int UserId { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public bool? IsBanned { get; set; }

    public string? Avatar { get; set; }

    public virtual ICollection<Favourite> Favourites { get; set; } = new List<Favourite>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
