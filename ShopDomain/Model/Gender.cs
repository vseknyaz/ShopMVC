using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class Gender : Entity
{
    public string Name { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
