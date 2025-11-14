using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers_POE3_.Data
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductId { get; set; } = string.Empty; // Original ID from Table Storage

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int StockAvailable { get; set; } = 0;

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Cart> CartItems { get; set; } = new List<Cart>();
    }
}

