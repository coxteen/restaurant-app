using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class ProductViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private ObservableCollection<Product> _products;
        private Product _selectedProduct;

        public ObservableCollection<Product> Products
        {
            get { return _products; }
            set { SetProperty(ref _products, value); }
        }

        public Product SelectedProduct
        {
            get { return _selectedProduct; }
            set { SetProperty(ref _selectedProduct, value); }
        }

        public ICommand RefreshCommand { get; }

        public ProductViewModel(ProductService productService)
        {
            _productService = productService;

            RefreshCommand = new RelayCommand(_ => LoadProducts());

            LoadProducts();
        }

        private void LoadProducts()
        {
            var productList = _productService.GetAllProducts();
            Products = new ObservableCollection<Product>(productList);
        }
    }
}