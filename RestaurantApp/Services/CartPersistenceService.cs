using Npgsql;
using RestaurantApp.Models;
using System;
using System.Collections.ObjectModel;

namespace RestaurantApp.Services
{
    public class CartPersistenceService
    {
        private readonly DatabaseService _databaseService;
        private readonly ProductService _productService;

        public CartPersistenceService(DatabaseService databaseService, ProductService productService)
        {
            _databaseService = databaseService;
            _productService = productService;
        }

        // Add a cart_items table to the database if it doesn't exist
        public void EnsureCartTableExists()
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                // Check if the table exists
                string checkTableQuery = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = 'cart_items'
                    )";

                bool tableExists;
                using (NpgsqlCommand command = new NpgsqlCommand(checkTableQuery, connection))
                {
                    tableExists = (bool)command.ExecuteScalar();
                }

                if (!tableExists)
                {
                    // Create the cart_items table
                    string createTableQuery = @"
                        CREATE TABLE cart_items (
                            cartitemid SERIAL PRIMARY KEY,
                            userid INT NOT NULL REFERENCES users(userid) ON DELETE CASCADE,
                            productid INT NOT NULL REFERENCES products(productid) ON DELETE CASCADE,
                            quantity INT NOT NULL,
                            dateadded TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                        )";

                    using (NpgsqlCommand command = new NpgsqlCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Create an index for faster lookups
                    string createIndexQuery = "CREATE INDEX idx_cart_items_user ON cart_items(userid)";
                    using (NpgsqlCommand command = new NpgsqlCommand(createIndexQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        // Save cart items to database
        public void SaveCart(int userId, ShoppingCart cart)
        {
            if (userId <= 0 || cart == null || cart.Items == null || cart.Items.Count == 0)
                return;

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // First, clear existing cart items for this user
                        string clearCartQuery = "DELETE FROM cart_items WHERE userid = @UserId";
                        using (NpgsqlCommand command = new NpgsqlCommand(clearCartQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            command.ExecuteNonQuery();
                        }

                        // Then insert all current cart items
                        string insertItemQuery = @"
                            INSERT INTO cart_items (userid, productid, quantity)
                            VALUES (@UserId, @ProductId, @Quantity)";

                        foreach (var item in cart.Items)
                        {
                            using (NpgsqlCommand command = new NpgsqlCommand(insertItemQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // Load cart items from database
        public ShoppingCart LoadCart(int userId)
        {
            ShoppingCart cart = new ShoppingCart();

            if (userId <= 0)
                return cart;

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT ci.productid, ci.quantity, p.name, p.price
                    FROM cart_items ci
                    JOIN products p ON ci.productid = p.productid
                    WHERE ci.userid = @UserId
                    ORDER BY ci.dateadded";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cart.Items.Add(new CartItem
                            {
                                ProductId = reader.GetInt32(0),
                                Quantity = reader.GetInt32(1),
                                ProductName = reader.GetString(2),
                                UnitPrice = reader.GetDecimal(3)
                            });
                        }
                    }
                }
            }

            return cart;
        }

        // Clear cart for a user
        public void ClearCart(int userId)
        {
            if (userId <= 0)
                return;

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM cart_items WHERE userid = @UserId";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}