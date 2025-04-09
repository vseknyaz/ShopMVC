using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class ProductSize : Entity
{
    public int ProductId { get; set; }

    public int SizeId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Size Size { get; set; } = null!;
}
