using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeWeb.Data;

public partial class DetailRecipeIngredient
{
    public int? IngredientId { get; set; }

    public int RecipeId { get; set; }

    public string? Amount { get; set; }

    [ValidateNever]
    public virtual Ingredient Ingredient { get; set; } = null!;

    [ValidateNever]
    public virtual Recipe Recipe { get; set; } = null!;

    [NotMapped]
    public string? IngredientName { get; set; }

}