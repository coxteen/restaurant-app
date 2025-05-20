using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;
using RestaurantApp.Views;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly Window _window;
        private readonly DatabaseService _databaseService;
        private readonly CategoryService _categoryService;
        private readonly ProductService _productService;
        private readonly UserService _userService;
        private readonly OrderService _orderService;
        private readonly CartService _cartService;
        private ShoppingCart _shoppingCart = new ShoppingCart();
        private ContentControl _mainContent;
        private string _userInfoText = string.Empty;
        private Visibility _loginButtonVisibility = Visibility.Visible;
        private Visibility _logoutButtonVisibility = Visibility.Collapsed;
        private Visibility _ordersButtonVisibility = Visibility.Collapsed;
        private Visibility _adminPanelVisibility = Visibility.Collapsed;
        private Visibility _contactOverlayVisibility = Visibility.Collapsed;
        #endregion

        #region Public Properties
        public int ShoppingCartItemCount => _shoppingCart?.Items.Count ?? 0;

        public string UserInfoText
        {
            get { return _userInfoText; }
            set
            {
                _userInfoText = value;
                OnPropertyChanged();
            }
        }

        public Visibility LoginButtonVisibility
        {
            get { return _loginButtonVisibility; }
            set
            {
                _loginButtonVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility LogoutButtonVisibility
        {
            get { return _logoutButtonVisibility; }
            set
            {
                _logoutButtonVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility OrdersButtonVisibility
        {
            get { return _ordersButtonVisibility; }
            set
            {
                _ordersButtonVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility AdminPanelVisibility
        {
            get { return _adminPanelVisibility; }
            set
            {
                _adminPanelVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility ContactOverlayVisibility
        {
            get { return _contactOverlayVisibility; }
            set
            {
                _contactOverlayVisibility = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand LoginCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
        public ICommand CartCommand { get; private set; }
        public ICommand ContactCommand { get; private set; }
        public ICommand CloseContactOverlayCommand { get; private set; }
        public ICommand MenuCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand OrdersCommand { get; private set; }
        public ICommand CategoriesCommand { get; private set; }
        public ICommand ProductsCommand { get; private set; }
        public ICommand AllOrdersCommand { get; private set; }
        public ICommand StockAlertCommand { get; private set; }
        #endregion

        #region Constructor
        public MainWindowViewModel(Window window, ContentControl mainContent)
        {
            _window = window;
            _mainContent = mainContent;

            // Initialize services
            _databaseService = new DatabaseService();
            _categoryService = new CategoryService(_databaseService);
            _productService = new ProductService(_databaseService);
            _userService = new UserService(_databaseService);
            _cartService = new CartService(_databaseService, _productService);
            _orderService = new OrderService(_databaseService, _productService, _cartService);

            // Initialize commands
            LoginCommand = new RelayCommand(_ => NavigateToLogin());
            LogoutCommand = new RelayCommand(_ => Logout());
            CartCommand = new RelayCommand(_ => NavigateToCart());
            ContactCommand = new RelayCommand(_ => ShowContactOverlay());
            CloseContactOverlayCommand = new RelayCommand(_ => HideContactOverlay());
            MenuCommand = new RelayCommand(_ => NavigateToMenu());
            SearchCommand = new RelayCommand(_ => NavigateToSearch());
            OrdersCommand = new RelayCommand(_ => NavigateToOrders());
            CategoriesCommand = new RelayCommand(_ => NavigateToCategories());
            ProductsCommand = new RelayCommand(_ => NavigateToProducts());
            AllOrdersCommand = new RelayCommand(_ => NavigateToAllOrders());
            StockAlertCommand = new RelayCommand(_ => NavigateToStockAlert());

            // Subscribe to cart changes
            _shoppingCart.Items.CollectionChanged += ShoppingCart_CollectionChanged;

            // Navigate to menu view by default
            NavigateToMenu();
        }
        #endregion

        #region Navigation Methods
        private void NavigateToLogin()
        {
            var loginViewModel = new LoginViewModel(_userService, NavigateToRegister, OnLoginSuccess);
            var loginView = new LoginView();
            loginView.DataContext = loginViewModel;
            _mainContent.Content = loginView;
        }

        private void NavigateToRegister()
        {
            var registerViewModel = new RegisterViewModel(_userService, NavigateToLogin, OnLoginSuccess);
            var registerView = new RegisterView();
            registerView.DataContext = registerViewModel;
            _mainContent.Content = registerView;
        }

        private void OnLoginSuccess()
        {
            // Load the user's saved cart
            bool isLoadingCart = true;
            try
            {
                ShoppingCart savedCart = _cartService.LoadCartItems(_userService.CurrentUser.UserId);
                _shoppingCart.ReplaceWith(savedCart);
            }
            finally
            {
                isLoadingCart = false;
            }

            // Navigate to the menu view
            RefreshMainWindow();
        }

        private void Logout()
        {
            // Save cart to database before logout if user is authenticated
            if (_userService.IsAuthenticated)
            {
                _cartService.SaveCartItems(_userService.CurrentUser.UserId, _shoppingCart);
            }

            _userService.Logout();
            _shoppingCart.Clear(); // Clear the cart in memory
            RefreshMainWindow();
        }

        private void RefreshMainWindow()
        {
            // Navigate to the menu view by default
            NavigateToMenu();
            UpdateNavigationBar();
        }

        private void UpdateNavigationBar()
        {
            // Show/hide buttons based on authentication status
            LoginButtonVisibility = _userService.IsAuthenticated ? Visibility.Collapsed : Visibility.Visible;
            LogoutButtonVisibility = _userService.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;
            OrdersButtonVisibility = _userService.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;

            // Additional admin functions
            AdminPanelVisibility = _userService.IsEmployee ? Visibility.Visible : Visibility.Collapsed;

            // Update user info display
            UserInfoText = _userService.IsAuthenticated ? $"Welcome, {_userService.CurrentUser.FullName}" : string.Empty;
        }

        private void NavigateToCart()
        {
            var cartViewModel = new CartViewModel(_userService, _orderService, _shoppingCart);
            var cartView = new CartView();
            cartView.DataContext = cartViewModel;
            _mainContent.Content = cartView;
        }

        private void NavigateToMenu()
        {
            var menuViewModel = new MenuViewModel(_categoryService, _productService, _shoppingCart);
            var menuView = new MenuView();
            menuView.DataContext = menuViewModel;
            _mainContent.Content = menuView;
        }

        private void NavigateToSearch()
        {
            var searchViewModel = new SearchViewModel(_productService, _shoppingCart);
            var searchView = new SearchView();
            searchView.DataContext = searchViewModel;
            _mainContent.Content = searchView;
        }

        private void NavigateToOrders()
        {
            var ordersViewModel = new UserOrdersViewModel(_userService, _orderService);
            var ordersView = new UserOrdersView();
            ordersView.DataContext = ordersViewModel;
            _mainContent.Content = ordersView;
        }

        private void NavigateToCategories()
        {
            var categoryViewModel = new CategoryViewModel(_categoryService);
            var categoryView = new CategoryView();
            categoryView.DataContext = categoryViewModel;
            _mainContent.Content = categoryView;
        }

        private void NavigateToProducts()
        {
            var productViewModel = new ProductViewModel(_productService, _shoppingCart);
            var productView = new ProductView();
            productView.DataContext = productViewModel;
            _mainContent.Content = productView;
        }

        private void NavigateToAllOrders()
        {
            var adminOrdersViewModel = new AdminOrdersViewModel(_orderService);
            var adminOrdersView = new AdminOrdersView();
            adminOrdersView.DataContext = adminOrdersViewModel;
            _mainContent.Content = adminOrdersView;
        }

        private void NavigateToStockAlert()
        {
            var stockAlertViewModel = new StockAlertViewModel(_productService);
            var stockAlertView = new StockAlertView();
            stockAlertView.DataContext = stockAlertViewModel;
            _mainContent.Content = stockAlertView;
        }
        #endregion

        #region Contact Methods
        private void ShowContactOverlay()
        {
            ContactOverlayVisibility = Visibility.Visible;
        }

        private void HideContactOverlay()
        {
            ContactOverlayVisibility = Visibility.Collapsed;
        }
        #endregion

        #region Event Handlers
        private void ShoppingCart_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ShoppingCartItemCount));

            // Save cart changes if the user is logged in
            if (_userService.IsAuthenticated)
            {
                _cartService.SaveCartItems(_userService.CurrentUser.UserId, _shoppingCart);
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}