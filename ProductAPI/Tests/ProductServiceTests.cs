using FluentAssertions;
using Moq;
using NUnit.Framework;
using ProductAPI.Models;
using ProductAPI.Services;

namespace ProductAPI.Tests
{
    [TestFixture]
    public class ProductServiceTests
    {
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Mock<ILogger<ProductService>> _loggerMock;
        private Mock<IConfiguration> _configurationMock;
        private ProductService _productService;
        private HttpClient _httpClient;

        [SetUp]
        public void SetUp()
        {
            // Setup for HttpMessageHandler
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<ProductService>>();
            _configurationMock = new Mock<IConfiguration>();

            // Setup the product URL in the mock configuration
            _configurationMock.Setup(x => x["ProductApiSettings:ProductUrl"])
                              .Returns("http://fakeurl.com/products");

            // Setup HttpClient with the mocked handler
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _productService = new ProductService(_httpClient, _loggerMock.Object, _configurationMock.Object);
        }


        [Test]
        public void FilterProducts_Should_Filter_By_Price_And_Size()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Title = "Product 1", Price = 10, Sizes = new List<string> { "S" } },
                new Product { Title = "Product 2", Price = 20, Sizes = new List<string> { "M", "L" } },
                new Product { Title = "Product 3", Price = 30, Sizes = new List<string> { "L" } }
            };

            // Act
            var filtered = _productService.FilterProducts(products, minPrice: 15, maxPrice: 25, size: "M,L");

            // Assert
            filtered.Should().HaveCount(1);
            filtered.Should().ContainSingle(p => p.Title == "Product 2");
        }

        [Test]
        public void HighlightWords_Should_Highlight_Correctly()
        {
            // Arrange
            var description = "This is a test product description.";
            var highlightWords = new[] { "test", "description" };

            // Act
            var highlighted = _productService.HighlightWords(description, highlightWords);

            // Assert
            highlighted.Should().Contain("<em>test</em>");
            highlighted.Should().Contain("<em>description</em>");
        }

        [Test]
        public void GenerateFilterMetadata_Should_Return_Correct_Metadata()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Title = "Product 1", Price = 10, Sizes = new List<string> { "S" } },
                new Product { Title = "Product 2", Price = 20, Sizes = new List<string> { "M", "L" } },
                new Product { Title = "Product 3", Price = 30, Sizes = new List<string> { "L" } }
            };

            // Act
            var filterMetadata = _productService.GenerateFilterMetadata(products);

            // Assert
            filterMetadata.MinPrice.Should().Be(10);
            filterMetadata.MaxPrice.Should().Be(30);
            filterMetadata.Sizes.Should().Contain("S").And.Contain("M").And.Contain("L");
        }
    }
}
