using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;
using RestaurantApp.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System;
using System.Collections.Specialized;

namespace RestaurantApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly CategoryService _categoryService;
        private readonly ProductService _productService;
        private readonly UserService _userService;
        private readonly OrderService _orderService;

        private ShoppingCart _shoppingCart = new ShoppingCart();

        public int ShoppingCartItemCount => _shoppingCart?.Items.Count ?? 0;
        public ICommand CartClickCommand { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _databaseService = new DatabaseService();
            _categoryService = new CategoryService(_databaseService);
            _productService = new ProductService(_databaseService);
            _userService = new UserService(_databaseService);
            _orderService = new OrderService(_databaseService, _productService);

            CartClickCommand = new RelayCommand(_ => CartButton_Click(null, null));
            _shoppingCart.Items.CollectionChanged += ShoppingCart_CollectionChanged;

            // Navigate to menu by default
            MenuButton_Click(null, null);
            UpdateNavigationBar();
        }

        private void ShoppingCart_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ShoppingCartItemCount));
        }

        private void CategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            var categoryViewModel = new CategoryViewModel(_categoryService);
            var categoryView = new CategoryView();
            categoryView.DataContext = categoryViewModel;
            MainContent.Content = categoryView;
        }

        private void ProductsButton_Click(object sender, RoutedEventArgs e)
        {
            var productViewModel = new ProductViewModel(_productService);
            var productView = new ProductView();
            productView.DataContext = productViewModel;
            MainContent.Content = productView;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchViewModel = new SearchViewModel(_productService);
            var searchView = new SearchView();
            searchView.DataContext = searchViewModel;
            MainContent.Content = searchView;
        }

        private void NavigateToLogin()
        {
            var loginViewModel = new LoginViewModel(_userService, NavigateToRegister, RefreshMainWindow);
            var loginView = new LoginView();
            loginView.DataContext = loginViewModel;
            MainContent.Content = loginView;
            UpdateNavigationBar();
        }

        private void NavigateToRegister()
        {
            var registerViewModel = new RegisterViewModel(_userService, NavigateToLogin, RefreshMainWindow);
            var registerView = new RegisterView();
            registerView.DataContext = registerViewModel;
            MainContent.Content = registerView;
            UpdateNavigationBar();
        }

        private void RefreshMainWindow()
        {
            // Navigate to the menu view by default
            MenuButton_Click(null, null);
            UpdateNavigationBar();
        }

        private void UpdateNavigationBar()
        {
            // Show/hide buttons based on authentication status
            btnLogin.Visibility = _userService.IsAuthenticated ? Visibility.Collapsed : Visibility.Visible;
            btnLogout.Visibility = _userService.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;
            btnOrders.Visibility = _userService.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;

            // Additional admin functions
            adminPanel.Visibility = _userService.IsEmployee ? Visibility.Visible : Visibility.Collapsed;

            // Update user info display
            userInfoText.Text = _userService.IsAuthenticated ? $"Welcome, {_userService.CurrentUser.FullName}" : string.Empty;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToLogin();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _userService.Logout();
            RefreshMainWindow();
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            var cartViewModel = new CartViewModel(_userService, _orderService, _shoppingCart);
            var cartView = new CartView();
            cartView.DataContext = cartViewModel;
            MainContent.Content = cartView;
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            var ordersViewModel = new UserOrdersViewModel(_userService, _orderService);
            var ordersView = new UserOrdersView();
            ordersView.DataContext = ordersViewModel;
            MainContent.Content = ordersView;
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menuViewModel = new MenuViewModel(_categoryService, _productService, _shoppingCart);
            var menuView = new MenuView();
            menuView.DataContext = menuViewModel;
            MainContent.Content = menuView;
        }

        private void AllOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            var adminOrdersViewModel = new AdminOrdersViewModel(_orderService);
            var adminOrdersView = new AdminOrdersView();
            adminOrdersView.DataContext = adminOrdersViewModel;
            MainContent.Content = adminOrdersView;
        }

        private void StockAlertButton_Click(object sender, RoutedEventArgs e)
        {
            var stockAlertViewModel = new StockAlertViewModel(_productService);
            var stockAlertView = new StockAlertView();
            stockAlertView.DataContext = stockAlertViewModel;
            MainContent.Content = stockAlertView;
        }

        private void AddCheeseburger_Click(object sender, RoutedEventArgs e)
        {
            // Get the category ID for Main Course
            int categoryId = _categoryService.GetCategoryByName("Main Course")?.CategoryId ?? 0;

            // If the category doesn't exist, create it
            if (categoryId == 0)
            {
                var category = new Category { Name = "Main Course", Description = "Main dish options" };
                _categoryService.AddCategory(category);
                categoryId = _categoryService.GetCategoryByName("Main Course").CategoryId;
            }

            // Create a cheeseburger product
            var cheeseburger = new Product
            {
                Name = "Cheeseburger",
                Price = 12.99m,
                PortionSize = 350,
                TotalQuantity = 10000,
                CategoryId = categoryId,
                IsAvailable = true
            };

            // Add the cheeseburger to the database
            bool success = _productService.AddProduct(cheeseburger);

            if (success)
            {
                MessageBox.Show("Cheeseburger added successfully!");

                // If you have a product view open, refresh it
                if (MainContent.Content is ProductView)
                {
                    ProductsButton_Click(null, null);
                }
            }
            else
            {
                MessageBox.Show("Failed to add cheeseburger.");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}