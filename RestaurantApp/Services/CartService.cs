using Npgsql;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;

namespace RestaurantApp.Services
{
    public class CartService
    {
        private readonly DatabaseService _databaseService;
        private readonly ProductService _productService;

        public CartService(DatabaseService databaseService, ProductService productService)
        {
            _databaseService = databaseService;
            _productService = productService;
        }

        // Save the current cart items for a user
        public bool SaveCartItems(int userId, ShoppingCart cart)
        {
            // If cart is empty, clear any existing items
            if (cart == null || cart.Items.Count == 0)
            {
                return ClearCartItems(userId);
            }

            try
            {
                using (NpgsqlConnection connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    using (NpgsqlTransaction transaction = connection.BeginTransaction())
                    {
                        // First, clear existing cart items for this user
                        string clearQuery = "DELETE FROM cart_items WHERE userid = @UserId";
                        using (NpgsqlCommand command = new NpgsqlCommand(clearQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            command.ExecuteNonQuery();
                        }

                        // Then, insert the current cart items
                        foreach (var item in cart.Items)
                        {
                            string insertQuery = @"
                                INSERT INTO cart_items (userid, productid, quantity)
                                VALUES (@UserId, @ProductId, @Quantity)";

                            using (NpgsqlCommand command = new NpgsqlCommand(insertQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Npgsql.PostgresException pex) when (pex.SqlState == "42P01")
            {
                // Missing table - do not crash the app, log and return false
                Console.WriteLine($"Cart table missing when saving cart items: {pex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving cart items: {ex.Message}");
                return false;
            }
        }

        // Load cart items for a user
        public ShoppingCart LoadCartItems(int userId)
        {
            var cart = new ShoppingCart();

            try
            {
                using (NpgsqlConnection connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    string query = @"
                    SELECT ci.productid, ci.quantity, p.name, p.price, p.isavailable
                    FROM cart_items ci
                    JOIN products p ON ci.productid = p.productid
                    WHERE ci.userid = @UserId";

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int productId = reader.GetInt32(0);
                                int quantity = reader.GetInt32(1);
                                string productName = reader.GetString(2);
                                decimal price = reader.GetDecimal(3);
                                bool isAvailable = reader.GetBoolean(4);

                                // Only add to cart if product is still available
                                if (isAvailable)
                                {
                                    cart.Items.Add(new CartItem
                                    {
                                        ProductId = productId,
                                        ProductName = productName,
                                        UnitPrice = price,
                                        Quantity = quantity
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Npgsql.PostgresException pex) when (pex.SqlState == "42P01")
            {
                // Table missing - return empty cart instead of throwing
                Console.WriteLine($"Cart table missing when loading cart items: {pex.Message}");
                return cart;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cart items: {ex.Message}");
                return cart;
            }

            return cart;
        }

        // Clear all cart items for a user
        public bool ClearCartItems(int userId)
        {
            try
            {
                using (NpgsqlConnection connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM cart_items WHERE userid = @UserId";

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        int rowsAffected = command.ExecuteNonQuery();
                        return true; // Return true even if no rows affected
                    }
                }
            }
            catch (Npgsql.PostgresException pex) when (pex.SqlState == "42P01")
            {
                Console.WriteLine($"Cart table missing when clearing cart items: {pex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cart items: {ex.Message}");
                return false;
            }
        }
    }
}