using Microsoft.AspNetCore.Mvc;
using ProductAPI.Services;

namespace ProductFilterAPI.Controllers
{
    /// <summary>
    /// Product API endpoint for filtering products.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;  
        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Retrieves a filtered list of products based on specified criteria.
        /// </summary>
        /// <param name="minPrice">Optional minimum price filter.</param>
        /// <param name="maxPrice">Optional maximum price filter.</param>
        /// <param name="size">Optional comma-separated list of sizes filter.</param>
        /// <param name="highlight">Optional comma-separated list of words to highlight in product descriptions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the filtered products and filter metadata.</returns>
        [HttpGet("filter")]
        public async Task<IActionResult> GetFilteredProducts(
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string size = null,
            [FromQuery] string highlight = null)
        {
            // Fetch the full list of products from the ProductService
            var products = await _productService.GetProductsAsync();

            // Apply filtering to the products based on provided parameters
            var filteredProducts = _productService.FilterProducts(products, minPrice, maxPrice, size);

            // Split the highlight words by comma, default to an empty array if null
            var highlightWords = highlight?.Split(',') ?? Array.Empty<string>();

            // Highlight the specified words in the description of each filtered product
            filteredProducts.ForEach(p => p.Description = _productService.HighlightWords(p.Description, highlightWords));

            // Generate metadata for filters based on the original list of products
            var filterMetadata = _productService.GenerateFilterMetadata(products);

            // Create a response object containing the filtered products and filter metadata
            var response = new
            {
                Products = filteredProducts,
                Filter = filterMetadata
            };
            return Ok(response);
        }
    }
}
