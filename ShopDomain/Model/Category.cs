using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopDomain.Model;

public partial class Category: Entity
{
    [Required(ErrorMessage = "Введіть назву категорії")]
    [Display(Name = "Категорія")]
    public string Name { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
