using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class Shop : Entity
{

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
