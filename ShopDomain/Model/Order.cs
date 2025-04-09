using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class Order : Entity
{
    public DateTime OrderDate { get; set; }

    public int? StatusId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

    public virtual Status? Status { get; set; }
}
