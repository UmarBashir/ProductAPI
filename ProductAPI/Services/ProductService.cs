using Newtonsoft.Json;
using ProductAPI.Models;
using System.Text.RegularExpressions;

namespace ProductAPI.Services
{
    // ProductService handles operations related to fetching and processing product data
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;
        private readonly string _productUrl;

        public ProductService(HttpClient httpClient, ILogger<ProductService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Load the product URL from configuration settings
            _productUrl = configuration["ProductApiSettings:ProductUrl"] ?? throw new ArgumentNullException("Product API URL cannot be null.");
        }

        // Retrieves a list of products from the external API
        public async Task<List<Product>?> GetProductsAsync()
        {
            _logger.LogInformation("Fetching products from external source.");

            try
            {
                // Make a GET request to the product URL
                var response = await _httpClient.GetStringAsync(_productUrl);
                _logger.LogInformation("Response from external API: {response}", response);

                // Deserialize the JSON response into a ProductResponse object
                var productResponse = JsonConvert.DeserializeObject<ProductResponse>(response);

                if (productResponse?.Products == null || !productResponse.Products.Any())
                {
                    _logger.LogWarning("No products found in the response.");
                }

                return productResponse?.Products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching products.");
                throw; 
            }
        }

        // Filters the product list based on price range and size
        public List<Product> FilterProducts(List<Product> products, decimal? minPrice, decimal? maxPrice, string size)
        {
            _logger.LogInformation("Filtering products with MinPrice: {minPrice}, MaxPrice: {maxPrice}, Sizes: {size}", minPrice, maxPrice, size);
            var sizes = string.IsNullOrEmpty(size) ? Array.Empty<string>() : size.Split(',');

            var filteredProducts = products.Where(p =>
                (!minPrice.HasValue || p.Price >= minPrice.Value) &&
                (!maxPrice.HasValue || p.Price <= maxPrice.Value) &&
                (sizes.Length == 0 || p.Sizes.Any(s => sizes.Contains(s, StringComparer.OrdinalIgnoreCase)))
            ).ToList();

            _logger.LogInformation("Filtered products count: {count}", filteredProducts.Count);
            return filteredProducts;
        }

        // Highlights specified words in the product description
        public string HighlightWords(string description, string[]? highlightWords)
        {
            // If no words to highlight, return the original description
            if (highlightWords == null || highlightWords.Length == 0)
            {
                _logger.LogInformation("No words to highlight in the description.");
                return description ?? string.Empty; 
            }

            // Loop through each word to highlight
            foreach (var word in highlightWords)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    // Create a regex to find the word in the description and replace it with highlighted version
                    var regex = new Regex($@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
                    description = regex.Replace(description ?? string.Empty, $"<em>{word}</em>");
                }
            }

            _logger.LogInformation("Highlighted words in the description.");
            return description ?? string.Empty;
        }

        // Generates metadata for filtering products, including price range, available sizes, and common words
        public Filter GenerateFilterMetadata(List<Product>? products)
        {
            if (products == null || !products.Any())
            {
                _logger.LogWarning("Cannot generate filter metadata; no products available.");
                return new Filter(); 
            }

            var filter = new Filter
            {
                MinPrice = products.Min(p => p.Price),
                MaxPrice = products.Max(p => p.Price),
                Sizes = products.SelectMany(p => p.Sizes).Distinct().ToList(),
                CommonWords = GetMostCommonWords(products, 10, 5)
            };

            _logger.LogInformation("Generated filter metadata with MinPrice: {minPrice}, MaxPrice: {maxPrice}, Sizes: {sizeCount}, CommonWords: {wordCount}",
                filter.MinPrice, filter.MaxPrice, filter.Sizes.Count, filter.CommonWords.Count);

            return filter;
        }

        // Extracts the most common words from product descriptions
        private List<string> GetMostCommonWords(List<Product>? products, int topCount, int excludeTop)
        {
            // Check for null or empty product list
            if (products == null || !products.Any())
            {
                _logger.LogWarning("Product list is empty or null; returning empty common words list.");
                return new List<string>();
            }

            // Split descriptions into words, count occurrences, and select the most common words
            var allWords = products.SelectMany(p =>
                string.IsNullOrWhiteSpace(p.Description)
                ? Enumerable.Empty<string>()
                : Regex.Split(p.Description.ToLower(), @"\W+"))
                .Where(w => !string.IsNullOrEmpty(w)) 
                .GroupBy(w => w)  
                .OrderByDescending(g => g.Count()) 
                .Select(g => g.Key)  
                .Skip(excludeTop) 
                .Take(topCount)  
                .ToList();

            _logger.LogInformation("Extracted {count} common words from product descriptions.", allWords.Count);
            return allWords;
        }
    }
}
