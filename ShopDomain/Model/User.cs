﻿using Microsoft.AspNetCore.Identity;

namespace ShopDomain.Model
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}