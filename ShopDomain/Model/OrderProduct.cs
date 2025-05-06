using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class OrderProduct : Entity
{
    public int OrderId { get; set; }
    public int ProductSizeId { get; set; } // Змінюємо на ProductSizeId
    public int Quantity { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual ProductSize ProductSize { get; set; } = null!; // Змінюємо на ProductSize
}
