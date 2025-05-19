using Npgsql;
using RestaurantApp.Helpers;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace RestaurantApp.Services
{
    public class OrderService
    {
        private readonly DatabaseService _databaseService;
        private readonly ProductService _productService;
        private readonly CartService _cartService;

        public OrderService(DatabaseService databaseService, ProductService productService, CartService cartService)
        {
            _databaseService = databaseService;
            _productService = productService;
            _cartService = cartService;
        }

        public ShoppingCart CalculateOrderCosts(ShoppingCart cart, User user)
        {
            // Apply delivery fee if subtotal is less than the minimum required for free delivery
            cart.DeliveryFee = cart.Subtotal < AppSettings.MinimumOrderForFreeDelivery ? AppSettings.DeliveryFee : 0;

            // Apply discount if subtotal is greater than minimum required for discount
            if (cart.Subtotal >= AppSettings.MinimumOrderForDiscount)
            {
                cart.Discount = cart.Subtotal * (AppSettings.OrderDiscountPercentage / 100);
            }
            else
            {
                // Check for loyalty discount
                int recentOrderCount = GetRecentOrderCount(user.UserId, AppSettings.DaysForLoyaltyDiscountPeriod);

                if (recentOrderCount >= AppSettings.OrderCountForLoyaltyDiscount)
                {
                    cart.Discount = cart.Subtotal * (AppSettings.OrderDiscountPercentage / 100);
                }
                else
                {
                    cart.Discount = 0;
                }
            }

            return cart;
        }

        private int GetRecentOrderCount(int userId, int days)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT COUNT(*) 
                    FROM orders 
                    WHERE userid = @UserId 
                      AND orderdate >= @StartDate 
                      AND status != 'Cancelled'";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@StartDate", DateTime.Now.AddDays(-days));

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public int PlaceOrder(ShoppingCart cart, User user)
        {
            // Calculate costs first
            CalculateOrderCosts(cart, user);

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                // Begin transaction
                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Create order
                        string orderQuery = @"
                            INSERT INTO orders (userid, totalamount, deliveryfee, discount, status, estimateddeliverytime, deliveryaddress)
                            VALUES (@UserId, @TotalAmount, @DeliveryFee, @Discount, @Status, @EstimatedDeliveryTime, @DeliveryAddress)
                            RETURNING orderid";

                        int orderId;
                        using (NpgsqlCommand command = new NpgsqlCommand(orderQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", user.UserId);
                            command.Parameters.AddWithValue("@TotalAmount", cart.Total);
                            command.Parameters.AddWithValue("@DeliveryFee", cart.DeliveryFee);
                            command.Parameters.AddWithValue("@Discount", cart.Discount);
                            command.Parameters.AddWithValue("@Status", "Registered");
                            command.Parameters.AddWithValue("@EstimatedDeliveryTime", DateTime.Now.AddHours(1));
                            command.Parameters.AddWithValue("@DeliveryAddress", user.DeliveryAddress);

                            orderId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        // Add order items
                        foreach (var item in cart.Items)
                        {
                            string itemQuery = @"
                                INSERT INTO order_items (orderid, productid, quantity, unitprice)
                                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";

                            using (NpgsqlCommand command = new NpgsqlCommand(itemQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@OrderId", orderId);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);

                                command.ExecuteNonQuery();
                            }
                        }
                        _cartService.ClearCartItems(user.UserId);
                        transaction.Commit();
                        return orderId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<Order> GetUserOrders(int userId)
        {
            List<Order> orders = new List<Order>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT * 
                    FROM orders 
                    WHERE userid = @UserId 
                    ORDER BY orderdate DESC";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new Order
                            {
                                OrderId = reader.GetInt32(reader.GetOrdinal("orderid")),
                                UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("orderdate")),
                                TotalAmount = reader.GetDecimal(reader.GetOrdinal("totalamount")),
                                DeliveryFee = reader.GetDecimal(reader.GetOrdinal("deliveryfee")),
                                Discount = reader.GetDecimal(reader.GetOrdinal("discount")),
                                Status = reader.GetString(reader.GetOrdinal("status")),
                                EstimatedDeliveryTime = reader.IsDBNull(reader.GetOrdinal("estimateddeliverytime")) ?
                                    null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("estimateddeliverytime")),
                                DeliveryAddress = reader.GetString(reader.GetOrdinal("deliveryaddress"))
                            });
                        }
                    }
                }

                // Load order items for each order
                foreach (var order in orders)
                {
                    order.OrderItems = GetOrderItems(order.OrderId);
                }
            }

            return orders;
        }

        public List<Order> GetAllOrders(bool activeOnly = false)
        {
            List<Order> orders = new List<Order>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT o.*, u.firstname, u.lastname, u.phonenumber
                    FROM orders o
                    JOIN users u ON o.userid = u.userid";

                if (activeOnly)
                {
                    query += " WHERE o.status NOT IN ('Delivered', 'Cancelled')";
                }

                query += " ORDER BY o.orderdate DESC";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new Order
                            {
                                OrderId = reader.GetInt32(reader.GetOrdinal("orderid")),
                                UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("orderdate")),
                                TotalAmount = reader.GetDecimal(reader.GetOrdinal("totalamount")),
                                DeliveryFee = reader.GetDecimal(reader.GetOrdinal("deliveryfee")),
                                Discount = reader.GetDecimal(reader.GetOrdinal("discount")),
                                Status = reader.GetString(reader.GetOrdinal("status")),
                                EstimatedDeliveryTime = reader.IsDBNull(reader.GetOrdinal("estimateddeliverytime")) ?
                                    null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("estimateddeliverytime")),
                                DeliveryAddress = reader.GetString(reader.GetOrdinal("deliveryaddress")),
                                CustomerName = $"{reader.GetString(reader.GetOrdinal("firstname"))} {reader.GetString(reader.GetOrdinal("lastname"))}",
                                CustomerPhone = reader.IsDBNull(reader.GetOrdinal("phonenumber")) ?
                                    string.Empty : reader.GetString(reader.GetOrdinal("phonenumber"))
                            });
                        }
                    }
                }

                // Load order items for each order
                foreach (var order in orders)
                {
                    order.OrderItems = GetOrderItems(order.OrderId);
                }
            }

            return orders;
        }

        private List<OrderItem> GetOrderItems(int orderId)
        {
            List<OrderItem> items = new List<OrderItem>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT oi.*, p.name AS productname
                    FROM order_items oi
                    JOIN products p ON oi.productid = p.productid
                    WHERE oi.orderid = @OrderId";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new OrderItem
                            {
                                OrderItemId = reader.GetInt32(reader.GetOrdinal("orderitemid")),
                                OrderId = reader.GetInt32(reader.GetOrdinal("orderid")),
                                ProductId = reader.GetInt32(reader.GetOrdinal("productid")),
                                Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                                UnitPrice = reader.GetDecimal(reader.GetOrdinal("unitprice")),
                                ProductName = reader.GetString(reader.GetOrdinal("productname"))
                            });
                        }
                    }
                }
            }

            return items;
        }

        public bool UpdateOrderStatus(int orderId, string status)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = @"
                    UPDATE orders
                    SET status = @Status
                    WHERE orderid = @OrderId";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@Status", status);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        // Method to update product quantities when an order status changes to 'Preparing'
        public bool UpdateProductQuantities(int orderId)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                // Begin transaction
                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Use the stored procedure instead of manual updates
                        string query = "SELECT update_product_quantities_for_order(@OrderId)";

                        using (NpgsqlCommand command = new NpgsqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@OrderId", orderId);
                            bool success = (bool)command.ExecuteScalar();

                            if (!success)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}