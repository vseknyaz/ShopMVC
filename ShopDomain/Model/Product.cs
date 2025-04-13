using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopDomain.Model;

public partial class Product : Entity
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty; // Замість null! задаємо порожній рядок

    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int Stock { get; set; }

    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    public int GenderId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Gender? Gender { get; set; } // Прибрано = null!, Gender може бути null, якщо не завантажено

    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

    public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
}