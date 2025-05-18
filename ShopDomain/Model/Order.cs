using System;
using System.Collections.Generic;

namespace ShopDomain.Model;

public partial class Order : Entity
{
    public DateTime OrderDate { get; set; }

    public int? StatusId { get; set; }

    public string UserId { get; set; } // Нове поле для зв’язку з AspNetUsers
    public bool IsDeleted { get; set; }

    
    public virtual Status? Status { get; set; }
    public virtual User User { get; set; } // Навігаційна властивість до користувача
    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

    
}
