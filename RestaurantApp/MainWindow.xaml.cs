using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;
using RestaurantApp.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace RestaurantApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly CategoryService _categoryService;
        private readonly ProductService _productService;
        private readonly UserService _userService;
        private readonly OrderService _orderService;
        private readonly CartPersistenceService _cartPersistenceService;

        private ShoppingCart _shoppingCart = new ShoppingCart();

        public int ShoppingCartItemCount => _shoppingCart?.Items.Count ?? 0;
        public ICommand CartClickCommand { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            _databaseService = new DatabaseService();
            _categoryService = new CategoryService(_databaseService);
            _productService = new ProductService(_databaseService);
            _userService = new UserService(_databaseService);
            _orderService = new OrderService(_databaseService, _productService);
            _cartPersistenceService = new CartPersistenceService(_databaseService, _productService);

            CartClickCommand = new RelayCommand(_ => CartButton_Click(null, null));
            _shoppingCart.Items.CollectionChanged += ShoppingCart_CollectionChanged;

            _cartPersistenceService.EnsureCartTableExists();
        }

        private void ShoppingCart_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            var ordersViewModel = new UserOrdersViewModel(_userService, _orderService);
            var ordersView = new UserOrdersView();
            ordersView.DataContext = ordersViewModel;
            MainContent.Content = ordersView;
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

        private void HandleUserSessionChange()
        {
            if (_userService.IsAuthenticated)
            {
                // Save current anonymous cart items if any
                var anonymousCartItems = new List<CartItem>(_shoppingCart.Items);

                // Load user's saved cart
                _shoppingCart = _cartPersistenceService.LoadCart(_userService.CurrentUser.UserId);

                // Merge anonymous cart items into user's cart if there were any
                if (anonymousCartItems.Count > 0)
                {
                    foreach (var item in anonymousCartItems)
                    {
                        var existingItem = _shoppingCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                        if (existingItem != null)
                        {
                            existingItem.Quantity += item.Quantity;
                        }
                        else
                        {
                            _shoppingCart.Items.Add(item);
                        }
                    }

                    // Save the merged cart
                    _cartPersistenceService.SaveCart(_userService.CurrentUser.UserId, _shoppingCart);
                }
            }

            // Note: When logging out, we keep the cart items in memory but don't clear them

            // Update cart button display
            OnPropertyChanged(nameof(ShoppingCartItemCount));
        }

        // Update the RefreshMainWindow method to handle session changes
        private void RefreshMainWindow()
        {
            // Handle cart persistence
            HandleUserSessionChange();

            // Navigate to the menu view by default
            MenuButton_Click(null, null);
            UpdateNavigationBar();
        }

        // Add save cart method
        private void SaveCurrentCart()
        {
            if (_userService.IsAuthenticated && _shoppingCart.Items.Count > 0)
            {
                _cartPersistenceService.SaveCart(_userService.CurrentUser.UserId, _shoppingCart);
            }
        }

        // Update MenuViewModel creation to handle adding products to cart
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menuViewModel = new MenuViewModel(_categoryService, _productService, _shoppingCart);
            menuViewModel.ProductAddedToCart += (s, args) => SaveCurrentCart(); // Save cart when product added
            var menuView = new MenuView();
            menuView.DataContext = menuViewModel;
            MainContent.Content = menuView;
        }

        // Update CartButton_Click to save cart on changes
        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            var cartViewModel = new CartViewModel(_userService, _orderService, _shoppingCart);
            cartViewModel.CartChanged += (s, args) => SaveCurrentCart(); // Save when cart changed
            var cartView = new CartView();
            cartView.DataContext = cartViewModel;
            MainContent.Content = cartView;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}