using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Mobile

        [Column(TypeName = "decimal(18, 2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ChangeReturned { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Completed"; // Completed, Refunded

        [Required]
        public int RestaurantId { get; set; }

        [ForeignKey("RestaurantId")]
        public Restaurant? Restaurant { get; set; }

        [Required]
        public int CashierId { get; set; }

        [ForeignKey("CashierId")]
        public User? Cashier { get; set; }

        public List<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
    }
}

