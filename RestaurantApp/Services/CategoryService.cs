using Npgsql;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace RestaurantApp.Services
{
    public class CategoryService
    {
        private readonly DatabaseService _databaseService;

        public CategoryService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public List<Category> GetAllCategories()
        {
            List<Category> categories = new List<Category>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                string query = "SELECT categoryid, name, description FROM categories ORDER BY name";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                CategoryId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return categories;
        }

        public bool AddCategory(Category category)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = @"
                    INSERT INTO categories (name, description)
                    VALUES (@Name, @Description)";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", category.Name);
                    command.Parameters.AddWithValue("@Description",
                        string.IsNullOrEmpty(category.Description) ? DBNull.Value : (object)category.Description);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public bool UpdateCategory(Category category)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = @"
                    UPDATE categories
                    SET name = @Name, description = @Description
                    WHERE categoryid = @CategoryId";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CategoryId", category.CategoryId);
                    command.Parameters.AddWithValue("@Name", category.Name);
                    command.Parameters.AddWithValue("@Description",
                        string.IsNullOrEmpty(category.Description) ? DBNull.Value : (object)category.Description);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public bool DeleteCategory(int categoryId)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                // Check if the category is in use by products
                string checkQuery = "SELECT COUNT(*) FROM products WHERE categoryid = @CategoryId";
                using (NpgsqlCommand checkCommand = new NpgsqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@CategoryId", categoryId);
                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (count > 0)
                        return false; // Category is in use
                }

                string query = "DELETE FROM categories WHERE categoryid = @CategoryId";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CategoryId", categoryId);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public Category GetCategoryByName(string name)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                string query = "SELECT categoryid, name, description FROM categories WHERE name = @Name";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Category
                            {
                                CategoryId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}