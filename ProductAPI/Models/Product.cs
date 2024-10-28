namespace ProductAPI.Models
{
    public class Product
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public List<string>? Sizes { get; set; }
    }

    public class ProductResponse
    {
        public List<Product>? Products { get; set; }
    }
}
