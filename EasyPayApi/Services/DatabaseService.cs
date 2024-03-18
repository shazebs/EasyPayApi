using Azure.Core;
using Azure.Storage.Blobs.Models;
using Microsoft.Data.SqlClient; 

namespace EasyPayApi.Services
{
    public class DatabaseService
    {
        public ILogger logger;
        private readonly RSAEncryptionService rsa = new RSAEncryptionService();

        // dev
        private static readonly string connectionString = @"Data Source=(localdb)\ProjectModels;Initial Catalog=EasyPay;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

        // prod
        //private static readonly string connectionString = @"";

        public DatabaseService(ILogger<DatabaseService> _logger)
        {
            logger = _logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="salesOrderForm"></param>
        /// <returns></returns>
        public List<SalesOrderForm> AddToCatalog(SalesOrderForm salesOrderForm)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "INSERT INTO easypay_catalog (username, item_name, price, currency, image_url) VALUES (@username, @name, @price, @currency, @image);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", salesOrderForm.username);
                    command.Parameters.AddWithValue("@name", salesOrderForm.name);
                    command.Parameters.AddWithValue("@price", salesOrderForm.price);
                    command.Parameters.AddWithValue("@currency", salesOrderForm.currency);
                    command.Parameters.AddWithValue("@image", salesOrderForm.image);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            return GetCatalogByUsername(salesOrderForm.username); 
        }

        /// <summary>
        /// Check if username already exists in the database.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool CheckUsernameExists(string username)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "SELECT COUNT(*) FROM Accounts WHERE username = @username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="salesOrderForm"></param>
        /// <param name="catalog"></param>
        /// <returns></returns>
        public bool DeleteCatalogID(SalesOrderForm salesOrderForm, out List<SalesOrderForm> catalog)
        {
            bool result = false;
            catalog = null; 

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "DELETE FROM easypay_catalog WHERE id = @id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", salesOrderForm.id);
                    connection.Open();

                    if (command.ExecuteNonQuery() > 0)
                    {
                        result = true;
                        catalog = GetCatalogByUsername(salesOrderForm.username);
                    }
                }
            }
            return result; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public List<SalesOrderForm> GetCatalogByUsername(string username)
        {
            List<SalesOrderForm> catalog = new();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "SELECT * FROM easypay_catalog WHERE username = @username ORDER BY id DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            catalog.Add(new SalesOrderForm
                            {
                                id = (int)reader["id"],
                                username = (string)reader["username"],
                                name = (string)reader["item_name"],
                                price = (decimal)reader["price"],
                                currency = (string)reader["currency"],
                                image = (string)reader["image_url"]
                            });
                        }
                    }
                }
            }
            return catalog;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GetPasswordByUsername(string username)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "SELECT pass_word FROM Accounts WHERE username = @username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return rsa.Decrypt((string)reader["pass_word"]);
                        }
                    }
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GetStripeKey(string username)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "SELECT stripe_key FROM Accounts WHERE username = @username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return rsa.Decrypt((string)reader["stripe_key"]);
                        }
                    }
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginForm"></param>
        /// <returns></returns>
        public Account? LoginAccount(LoginForm loginForm)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    //var query = "SELECT email, username FROM Accounts WHERE username = @username AND pass_word COLLATE Latin1_General_CS_AS = @password";
                    var query = "SELECT email, username, pass_word FROM Accounts WHERE username = @username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", loginForm.username);
                        //command.Parameters.AddWithValue("@password", loginForm.password);

                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reader.Read();

                                if (rsa.Decrypt((string)reader["pass_word"]).Equals(loginForm.password))
                                {
                                    return new Account()
                                    {
                                        email = (string)reader["email"],
                                        username = (string)reader["username"],
                                    };
                                }
                            }
                            else
                            {
                                // No records found
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message, "Error occurred within LoginAccount");
            }
            return null;
        }

        /// <summary>
        /// Attempt to save account to database. 
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool RegisterAccount(Account account)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "INSERT INTO Accounts (email, username, pass_word, stripe_key) VALUES (@email, @username, @password, @stripe_key);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", account.email);
                    command.Parameters.AddWithValue("@username", account.username);
                    command.Parameters.AddWithValue("@password", rsa.Encrypt(account.password));
                    command.Parameters.AddWithValue("@stripe_key", rsa.Encrypt(account.stripe_key));
                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message, "Account not registerd.");
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public bool UpdateEmail(UpdateEmailRequest request)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE Accounts SET email = @new_email WHERE username = @username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@new_email", request.new_email);
                    command.Parameters.AddWithValue("@username", request.username);
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public bool UpdatePassword(UpdatePasswordRequest request)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE Accounts SET pass_word = @new_password WHERE username = @username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@new_password", rsa.Encrypt(request.new_password));
                    command.Parameters.AddWithValue("@username", request.username);
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public bool UpdateStripeKey(UpdateStripeKeyRequest request)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE Accounts SET stripe_key = @stripe_key WHERE username = @username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", request.username);
                    command.Parameters.AddWithValue("@stripe_key", rsa.Encrypt(request.stripe_key));
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_username"></param>
        /// <param name="new_username"></param>
        /// <returns></returns>
        public bool UpdateUsername(UpdateUsernameRequest request)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE Accounts SET username = @new_username WHERE username = @current_username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@new_username", request.new_username);
                    command.Parameters.AddWithValue("@current_username", request.current_username);
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        /// <summary>
        /// Update catalog items with new username.
        /// </summary>
        /// <param name="current_username"></param>
        /// <param name="new_username"></param>
        /// <returns></returns>
        public bool UpdateUsernameInCatalog(string current_username, string new_username)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE easypay_catalog SET username = @new_username WHERE username = @current_username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@new_username", new_username);
                    command.Parameters.AddWithValue("@current_username", current_username);
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

    }
}
