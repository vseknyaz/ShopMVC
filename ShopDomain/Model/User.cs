using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class User : Entity
{

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Role { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
