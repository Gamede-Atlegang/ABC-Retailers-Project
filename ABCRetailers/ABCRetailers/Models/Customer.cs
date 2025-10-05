using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Customer : ITableEntity
	{
	public string PartitionKey { get; set; } = "Customer";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    [Display(Name = "Customer ID")]
    public string CustomerId => RowKey;

    [Required, Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Required, Display(Name = "Surname")]
    public string Surname { get; set; } = "";

    [Required, Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Display(Name = "Shipping Address")]
    public string ShippingAddress { get; set; } = "";

    }
}
