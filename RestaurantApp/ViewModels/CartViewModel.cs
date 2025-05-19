using RestaurantApp.Models;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;
using System.Windows.Input;

public class CartViewModel : BaseViewModel
{
    private readonly UserService _userService;
    private readonly OrderService _orderService;
    private ShoppingCart _cart;
    private string _message;

    public event EventHandler CartChanged;

    public ShoppingCart Cart
    {
        get { return _cart; }
        set { SetProperty(ref _cart, value); }
    }

    public string Message
    {
        get { return _message; }
        set { SetProperty(ref _message, value); }
    }

    public bool CanCheckout => _userService.IsAuthenticated && Cart.Items.Count > 0;

    public ICommand CheckoutCommand { get; }
    public ICommand UpdateQuantityCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand ClearCartCommand { get; }

    public CartViewModel(UserService userService, OrderService orderService, ShoppingCart cart)
    {
        _userService = userService;
        _orderService = orderService;
        Cart = cart;

        if (_userService.IsAuthenticated)
        {
            // Calculate costs based on user's order history
            _orderService.CalculateOrderCosts(Cart, _userService.CurrentUser);
        }

        CheckoutCommand = new RelayCommand(ExecuteCheckout, CanExecuteCheckout);
        UpdateQuantityCommand = new RelayCommand<Tuple<int, int>>(ExecuteUpdateQuantity);
        RemoveItemCommand = new RelayCommand<int>(ExecuteRemoveItem);
        ClearCartCommand = new RelayCommand(_ => { Cart.Clear(); OnCartChanged(); });
    }

    private bool CanExecuteCheckout(object parameter)
    {
        return CanCheckout;
    }

    private void ExecuteCheckout(object parameter)
    {
        try
        {
            if (!_userService.IsAuthenticated)
            {
                Message = "Please log in to checkout.";
                return;
            }

            int orderId = _orderService.PlaceOrder(Cart, _userService.CurrentUser);

            if (orderId > 0)
            {
                Message = $"Order placed successfully! Your order ID is: {orderId}";
                Cart.Clear();
                OnCartChanged();
            }
        }
        catch (Exception ex)
        {
            Message = $"Error placing order: {ex.Message}";
        }
    }

    private void ExecuteUpdateQuantity(Tuple<int, int> param)
    {
        if (param != null)
        {
            int productId = param.Item1;
            int change = param.Item2;

            var item = Cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                int newQuantity = item.Quantity + change;
                Cart.UpdateItemQuantity(productId, newQuantity);
                OnCartChanged();

                if (_userService.IsAuthenticated)
                {
                    // Recalculate costs
                    _orderService.CalculateOrderCosts(Cart, _userService.CurrentUser);
                }

                OnPropertyChanged(nameof(CanCheckout));
            }
        }
    }

    private void ExecuteRemoveItem(int productId)
    {
        Cart.RemoveItem(productId);
        OnCartChanged();

        if (_userService.IsAuthenticated)
        {
            // Recalculate costs
            _orderService.CalculateOrderCosts(Cart, _userService.CurrentUser);
        }

        OnPropertyChanged(nameof(CanCheckout));
    }

    protected virtual void OnCartChanged()
    {
        CartChanged?.Invoke(this, EventArgs.Empty);
    }
}