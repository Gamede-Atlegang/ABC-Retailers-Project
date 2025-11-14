
using Microsoft.Build.Framework;

namespace ABCRetailers_POE3_.Data
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; } // Foreign key to Customers

        [Required]
        public int ProductId { get; set; } // Foreign key to Products

        [Required]
        public int Quantity { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Customer Customer { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
