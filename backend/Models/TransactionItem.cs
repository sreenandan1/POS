using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend.Models
{
    public class TransactionItem
    {
        public int Id { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [JsonIgnore] // Avoid infinite reference loops during API JSON serialization!
        [ForeignKey("TransactionId")]
        public Transaction? Transaction { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CostPrice { get; set; } // Saved historically at sale moment for accurate margin audits!

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal { get; set; }
    }
}
