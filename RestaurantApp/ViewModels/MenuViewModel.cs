using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class MenuViewModel : BaseViewModel
    {
        private readonly CategoryService _categoryService;
        private readonly ProductService _productService;
        private readonly ShoppingCart _cart;
        private ObservableCollection<CategoryWithProducts> _categories;

        public event EventHandler ProductAddedToCart;

        public ObservableCollection<CategoryWithProducts> Categories
        {
            get { return _categories; }
            set { SetProperty(ref _categories, value); }
        }

        public ICommand AddToCartCommand { get; }

        public MenuViewModel(CategoryService categoryService, ProductService productService, ShoppingCart cart)
        {
            _categoryService = categoryService;
            _productService = productService;
            _cart = cart;

            AddToCartCommand = new RelayCommand<Product>(ExecuteAddToCart);

            LoadMenu();
        }

        private void LoadMenu()
        {
            var categories = _categoryService.GetAllCategories();
            var products = _productService.GetAllProducts();

            var categoriesWithProducts = categories.Select(c => new CategoryWithProducts
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                Products = new ObservableCollection<Product>(
                    products.Where(p => p.CategoryId == c.CategoryId).ToList())
            }).ToList();

            Categories = new ObservableCollection<CategoryWithProducts>(categoriesWithProducts);
        }

        private void ExecuteAddToCart(Product product)
        {
            if (product != null && product.IsAvailable)
            {
                _cart.AddItem(product, 1);
                OnProductAddedToCart();
            }
        }

        protected virtual void OnProductAddedToCart()
        {
            ProductAddedToCart?.Invoke(this, EventArgs.Empty);
        }
    }

    public class CategoryWithProducts : Category
    {
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
    }
}