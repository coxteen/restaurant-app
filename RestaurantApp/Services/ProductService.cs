using Npgsql;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;

namespace RestaurantApp.Services
{
    public class ProductService
    {
        private readonly DatabaseService _databaseService;

        public ProductService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public List<Product> GetAllProducts()
        {
            List<Product> products = new List<Product>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT p.productid, p.name, p.price, p.portionsize, p.totalquantity, 
                           p.categoryid, c.name as categoryname, p.isavailable
                    FROM products p
                    JOIN categories c ON p.categoryid = c.categoryid
                    ORDER BY p.name";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                PortionSize = reader.GetInt32(3),
                                TotalQuantity = reader.GetInt32(4),
                                CategoryId = reader.GetInt32(5),
                                CategoryName = reader.GetString(6),
                                IsAvailable = reader.GetBoolean(7)
                            });
                        }
                    }
                }

                // Load allergens for each product
                foreach (var product in products)
                {
                    product.Allergens = GetProductAllergens(product.ProductId);
                }
            }

            return products;
        }

        private List<Allergen> GetProductAllergens(int productId)
        {
            List<Allergen> allergens = new List<Allergen>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT a.allergenid, a.name, a.description
                    FROM allergens a
                    JOIN product_allergens pa ON a.allergenid = pa.allergenid
                    WHERE pa.productid = @ProductId";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allergens.Add(new Allergen
                            {
                                AllergenId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return allergens;
        }

        public Product GetProductById(int productId)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT p.productid, p.name, p.price, p.portionsize, p.totalquantity, 
                           p.categoryid, c.name as categoryname, p.isavailable
                    FROM products p
                    JOIN categories c ON p.categoryid = c.categoryid
                    WHERE p.productid = @ProductId";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Product product = new Product
                            {
                                ProductId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                PortionSize = reader.GetInt32(3),
                                TotalQuantity = reader.GetInt32(4),
                                CategoryId = reader.GetInt32(5),
                                CategoryName = reader.GetString(6),
                                IsAvailable = reader.GetBoolean(7)
                            };

                            product.Allergens = GetProductAllergens(product.ProductId);
                            return product;
                        }
                    }
                }

                return null;
            }
        }

        public List<Product> GetLowStockProducts(int threshold)
        {
            List<Product> products = new List<Product>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT p.productid, p.name, p.totalquantity, p.portionsize, c.name as categoryname
                    FROM products p
                    JOIN categories c ON p.categoryid = c.categoryid
                    WHERE p.totalquantity <= @Threshold
                    ORDER BY p.totalquantity ASC";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Threshold", threshold);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32(reader.GetOrdinal("productid")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                TotalQuantity = reader.GetInt32(reader.GetOrdinal("totalquantity")),
                                PortionSize = reader.GetInt32(reader.GetOrdinal("portionsize")),
                                CategoryName = reader.GetString(reader.GetOrdinal("categoryname"))
                            });
                        }
                    }
                }
            }

            return products;
        }

        public bool AddProduct(Product product)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = @"
                    INSERT INTO products (name, price, portionsize, totalquantity, categoryid, isavailable)
                    VALUES (@Name, @Price, @PortionSize, @TotalQuantity, @CategoryId, @IsAvailable)
                    RETURNING productid";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@PortionSize", product.PortionSize);
                    command.Parameters.AddWithValue("@TotalQuantity", product.TotalQuantity);
                    command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
                    command.Parameters.AddWithValue("@IsAvailable", product.IsAvailable);

                    int productId = Convert.ToInt32(command.ExecuteScalar());

                    // Add allergens if any
                    if (product.Allergens != null && product.Allergens.Count > 0)
                    {
                        foreach (var allergen in product.Allergens)
                        {
                            string allergenQuery = @"
                                INSERT INTO product_allergens (productid, allergenid)
                                VALUES (@ProductId, @AllergenId)";

                            using (NpgsqlCommand allergenCommand = new NpgsqlCommand(allergenQuery, connection))
                            {
                                allergenCommand.Parameters.AddWithValue("@ProductId", productId);
                                allergenCommand.Parameters.AddWithValue("@AllergenId", allergen.AllergenId);
                                allergenCommand.ExecuteNonQuery();
                            }
                        }
                    }

                    return productId > 0;
                }
            }
        }
    }
}