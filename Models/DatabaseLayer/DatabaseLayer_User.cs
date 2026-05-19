using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<List<Usermodel>> GetUsers();
        Task<IActionResult> AddUser([FromForm] Usermodel model);
        Task<IActionResult> UpdateUser(int id, [FromForm] Usermodel model);
        Task<IActionResult> DeleteUser(int id);
        Task<Usermodel> GetUserByEmail(string email);
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<Usermodel>> GetUsers()
        {
            List<Usermodel> users = new List<Usermodel>();
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "SELECT id, firstname, lastname, email, password, role, isactive, createdat FROM users",
                    connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Usermodel user = new Usermodel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Firstname = reader["firstname"]?.ToString(),
                                Lastname = reader["lastname"]?.ToString(),
                                Email = reader["email"]?.ToString(),
                                Password = reader.IsDBNull(reader.GetOrdinal("password"))
                                           ? null
                                           : reader["password"].ToString(),
                                Role = reader["role"]?.ToString(),
                                Isactive = reader.GetBoolean(reader.GetOrdinal("isactive")),
                                Createdat = reader.GetDateTime(reader.GetOrdinal("createdat"))
                            };
                            users.Add(user);
                        }
                    }
                }
            }
            return users;
        }

        public async Task<IActionResult> AddUser([FromForm] Usermodel model)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "INSERT INTO users (firstname, lastname, email, password, role, isactive, createdat) VALUES (@firstname, @lastname, @email, @password, @role, @isactive, @createdat)",
                    connection))
                {
                    command.Parameters.AddWithValue("@firstname", model.Firstname ?? string.Empty);
                    command.Parameters.AddWithValue("@lastname", model.Lastname ?? string.Empty);
                    command.Parameters.AddWithValue("@email", model.Email ?? string.Empty);
                    command.Parameters.AddWithValue("@password", model.Password ?? string.Empty);
                    command.Parameters.AddWithValue("@role", model.Role ?? "User");
                    command.Parameters.AddWithValue("@isactive", model.Isactive);
                    command.Parameters.AddWithValue("@createdat", DateTime.UtcNow);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                        return new OkObjectResult("User added successfully.");
                    else
                        return new BadRequestObjectResult("Failed to add user.");
                }
            }
        }

        public async Task<IActionResult> UpdateUser(int id, [FromForm] Usermodel model)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "UPDATE users SET firstname = @firstname, lastname = @lastname, email = @email, password = @password, role = @role, isactive = @isactive WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@firstname", model.Firstname ?? string.Empty);
                    command.Parameters.AddWithValue("@lastname", model.Lastname ?? string.Empty);
                    command.Parameters.AddWithValue("@email", model.Email ?? string.Empty);
                    command.Parameters.AddWithValue("@password", model.Password ?? string.Empty);
                    command.Parameters.AddWithValue("@role", model.Role ?? "User");
                    command.Parameters.AddWithValue("@isactive", model.Isactive);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                        return new OkObjectResult("User updated successfully.");
                    else
                        return new BadRequestObjectResult("Failed to update user.");
                }
            }
        }

        public async Task<IActionResult> DeleteUser(int id)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "DELETE FROM users WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                        return new OkObjectResult("User deleted successfully.");
                    else
                        return new BadRequestObjectResult("Failed to delete user.");
                }
            }
        }

        public async Task<Usermodel> GetUserByEmail(string email)
        {
            Usermodel user = null;
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "SELECT id, firstname, lastname, email, password, role, isactive, createdat FROM users WHERE email = @Email",
                    connection))
                {
                    command.Parameters.AddWithValue("@Email", email ?? string.Empty);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new Usermodel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Firstname = reader["firstname"]?.ToString(),
                                Lastname = reader["lastname"]?.ToString(),
                                Email = reader["email"]?.ToString(),
                                // ✅ Password null safe
                                Password = reader.IsDBNull(reader.GetOrdinal("password"))
                                           ? null
                                           : reader["password"].ToString(),
                                Role = reader["role"]?.ToString(),
                                Isactive = reader.GetBoolean(reader.GetOrdinal("isactive")),
                                Createdat = reader.GetDateTime(reader.GetOrdinal("createdat"))
                            };
                        }
                    }
                }
            }
            return user;
        }
    }
}