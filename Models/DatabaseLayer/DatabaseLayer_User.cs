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
        Task EnsureUserPasswordResetSchema();
        Task SaveUserPasswordResetOtp(int userId, string otp, DateTime expiresAtUtc);
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
                    "SELECT id, firstname, lastname, email, password, role, isactive, createdat FROM users WHERE LOWER(email) = LOWER(@Email)",
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







        public async Task EnsureUserPasswordResetSchema()
        {
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            const string sql = @"
CREATE TABLE IF NOT EXISTS user_password_reset (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    otp TEXT NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);";
            using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveUserPasswordResetOtp(int userId, string otp, DateTime expiresAtUtc)
        {
            await EnsureUserPasswordResetSchema();
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            const string sql = @"
INSERT INTO user_password_reset (user_id, otp, expires_at)
VALUES (@userId, @otp, @expiresAt);";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@otp", otp);
            cmd.Parameters.AddWithValue("@expiresAt", expiresAtUtc);
            await cmd.ExecuteNonQueryAsync();
        }











    }
}