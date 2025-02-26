using System;
using System.Collections.Generic;

namespace RecipeWeb.Data;

public partial class Rating
{
    public int RatingId { get; set; }

    public int? RatingScore { get; set; }

    public string? Commnent { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? UserId { get; set; }

    public int? RecipeId { get; set; }

    public virtual Recipe? Recipe { get; set; }

    public virtual User? User { get; set; }
}
