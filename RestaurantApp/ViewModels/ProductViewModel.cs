using RestaurantApp.Models;
using RestaurantApp.Services;
using System.Collections.ObjectModel;

namespace RestaurantApp.ViewModels
{
    public class ProductViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private ObservableCollection<Product> _products;

        public ObservableCollection<Product> Products
        {
            get { return _products; }
            set { SetProperty(ref _products, value); }
        }

        public ProductViewModel(ProductService productService)
        {
            _productService = productService;
            LoadProducts();
        }

        private void LoadProducts()
        {
            var productList = _productService.GetAllProducts();
            Products = new ObservableCollection<Product>(productList);
        }
    }
}