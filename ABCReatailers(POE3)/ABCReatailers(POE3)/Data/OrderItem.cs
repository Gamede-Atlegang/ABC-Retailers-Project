using System.ComponentModel.DataAnnotations.Schema;
using ABCRetailers_POE3_.Models;
using Microsoft.Build.Framework;

namespace ABCRetailers_POE3_.Data
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; } // Foreign key to Orders

        [Required]
        public int ProductId { get; set; } // Foreign key to Products

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
