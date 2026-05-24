using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public int OwnerId { get; set; } // Reference to the Business Owner who owns this shop

        [ForeignKey("OwnerId")]
        public User? Owner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
