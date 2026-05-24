using System;

namespace backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Cashier"; // SuperAdmin, Owner, Manager, Waiter, Cashier
        public int? RestaurantId { get; set; }
        public int? ParentOwnerId { get; set; } // The ID of the Owner or SuperAdmin who added this user
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
