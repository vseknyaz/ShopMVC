using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class OrderProduct : Entity
{
    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
