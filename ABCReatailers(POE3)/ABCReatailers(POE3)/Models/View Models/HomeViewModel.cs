namespace ABCRetailers_POE3_.Models.View_Models;

public class HomeViewModel
{
    public int CustomerCount { get; set; }
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
    public List<ABCRetailers_POE3_.Data.Product> FeaturedProducts { get; set; } = new();
}