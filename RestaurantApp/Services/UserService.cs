using Npgsql;
using RestaurantApp.Helpers;
using RestaurantApp.Models;
using System;

namespace RestaurantApp.Services
{
    public class UserService
    {
        private readonly DatabaseService _databaseService;
        private User _currentUser;

        public User CurrentUser
        {
            get { return _currentUser; }
            private set { _currentUser = value; }
        }

        public bool IsAuthenticated => CurrentUser != null;
        public bool IsEmployee => IsAuthenticated && CurrentUser.UserType == "Employee";
        public bool IsCustomer => IsAuthenticated && CurrentUser.UserType == "Customer";

        public UserService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public bool Register(User user, string password)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                // Check if email already exists
                string checkQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";
                using (NpgsqlCommand checkCommand = new NpgsqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Email", user.Email);
                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (count > 0)
                        return false; // Email already exists
                }

                // Insert new user
                string query = @"
                    INSERT INTO users (firstname, lastname, email, passwordhash, phonenumber, deliveryaddress, usertype)
                    VALUES (@FirstName, @LastName, @Email, @PasswordHash, @PhoneNumber, @DeliveryAddress, @UserType)
                    RETURNING userid";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@LastName", user.LastName);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@PasswordHash", PasswordHasher.HashPassword(password));
                    command.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrEmpty(user.PhoneNumber) ? DBNull.Value : (object)user.PhoneNumber);
                    command.Parameters.AddWithValue("@DeliveryAddress", string.IsNullOrEmpty(user.DeliveryAddress) ? DBNull.Value : (object)user.DeliveryAddress);
                    command.Parameters.AddWithValue("@UserType", user.UserType);

                    user.UserId = Convert.ToInt32(command.ExecuteScalar());
                    return user.UserId > 0;
                }
            }
        }

        public bool Login(string email, string password)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                string query = "SELECT * FROM users WHERE email = @Email";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader.GetString(reader.GetOrdinal("passwordhash"));

                            if (PasswordHasher.VerifyPassword(password, storedHash))
                            {
                                // Login successful
                                CurrentUser = new User
                                {
                                    UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                                    FirstName = reader.GetString(reader.GetOrdinal("firstname")),
                                    LastName = reader.GetString(reader.GetOrdinal("lastname")),
                                    Email = reader.GetString(reader.GetOrdinal("email")),
                                    PasswordHash = storedHash,
                                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phonenumber")) ? null : reader.GetString(reader.GetOrdinal("phonenumber")),
                                    DeliveryAddress = reader.IsDBNull(reader.GetOrdinal("deliveryaddress")) ? null : reader.GetString(reader.GetOrdinal("deliveryaddress")),
                                    UserType = reader.GetString(reader.GetOrdinal("usertype"))
                                };

                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}