using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class FileUploadModel
    {
        [Display(Name = "Order ID")]
        public string? OrderId { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "Please select a file")]
        [Display(Name = "Proof of Payment")]
        public IFormFile? ProofOfPayment { get; set; }
    }
}