using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class Status : Entity
{
    public string Name { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
