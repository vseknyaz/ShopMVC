using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class Product : Entity
{

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public int ShopId { get; set; }

    public int CategoryId { get; set; }

    public int GenderId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Gender Gender { get; set; } = null!;

    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

    public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();

    public virtual Shop Shop { get; set; } = null!;
}
