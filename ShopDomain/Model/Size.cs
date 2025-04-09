using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class Size : Entity
{
    public string Name { get; set; } = null!;

    public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
}
