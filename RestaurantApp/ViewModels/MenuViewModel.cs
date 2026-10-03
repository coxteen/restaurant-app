using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
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
        private string _message;

        public ObservableCollection<CategoryWithProducts> Categories
        {
            get { return _categories; }
            set { SetProperty(ref _categories, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ICommand AddToCartCommand { get; }

        public MenuViewModel(CategoryService categoryService, ProductService productService, ShoppingCart cart)
        {
            _categoryService = categoryService;
            _productService = productService;
            _cart = cart;

            AddToCartCommand = new RelayCommand(ExecuteAddToCart, CanExecuteAddToCart);

            LoadMenu();
        }

        private void LoadMenu()
        {
            try
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
                Message = string.Empty;
            }
            catch (System.Exception ex)
            {
                // If the database is down or unreachable, show a user-friendly message and keep the UI usable.
                Categories = new ObservableCollection<CategoryWithProducts>();
                Message = "Unable to load menu: database unavailable. Run PostgreSQL or check connection settings.";
                try
                {
                    var logPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "db-errors.log");
                    var logLine = $"[{System.DateTime.Now:O}] {ex}{System.Environment.NewLine}";
                    System.IO.File.AppendAllText(logPath, logLine);
                }
                catch
                {
                    // Swallow any logging errors to avoid impacting the UI.
                }
            }
        }

        private bool CanExecuteAddToCart(object parameter)
        {
            if (parameter is Product product)
            {
                return product.IsAvailable && product.TotalQuantity > 0;
            }
            return false;
        }

        private void ExecuteAddToCart(object parameter)
        {
            if (parameter is Product product)
            {
                _cart.AddItem(product);
                Message = $"Added {product.Name} to cart";

                // Clear the message after 3 seconds
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                {
                    if (Message == $"Added {product.Name} to cart")
                    {
                        Message = string.Empty;
                        OnPropertyChanged(nameof(Message));
                    }
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }

    public class CategoryWithProducts : Category
    {
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
    }
}