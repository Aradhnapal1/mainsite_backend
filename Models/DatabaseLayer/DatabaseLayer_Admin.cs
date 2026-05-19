using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<List<AdminModel>> GetAllAdmins();
        Task<AdminModel> Add(AdminModel model);
        Task<AdminModel> Edit(int id, AdminModel model);
        Task<IActionResult> Delete(int id);

        // ✅ NEW: Login
        Task<AdminModel> GetAdminByEmail(string email);
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<AdminModel>> GetAllAdmins()
        {
            var admins = new List<AdminModel>();

            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    @"SELECT ""Id"", ""FirstName"", ""LastName"", ""Phone"", ""Email"", ""PasswordHash"", ""CreatedAt"" 
                      FROM ""admin"" 
                      ORDER BY ""Id"" DESC", connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        admins.Add(new AdminModel
                        {
                            Id = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Email = reader.GetString(4),
                            PasswordHash = reader.GetString(5),
                            CreatedAt = reader.GetDateTime(6)
                        });
                    }
                }
            }

            return admins;
        }

        public async Task<AdminModel> Add(AdminModel model)
        {
            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

                var command = new NpgsqlCommand(
                    @"INSERT INTO ""admin"" 
                      (""FirstName"", ""LastName"", ""Phone"", ""Email"", ""PasswordHash"", ""CreatedAt"") 
                      VALUES 
                      (@FirstName, @LastName, @Phone, @Email, @PasswordHash, @CreatedAt) 
                      RETURNING ""Id""", connection);

                command.Parameters.AddWithValue("@FirstName", model.FirstName);
                command.Parameters.AddWithValue("@LastName", model.LastName);
                command.Parameters.AddWithValue("@Phone", (object)model.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", model.Email);
                command.Parameters.AddWithValue("@PasswordHash", model.PasswordHash);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                var newId = (int)await command.ExecuteScalarAsync();
                model.Id = newId;
                model.PasswordHash = null;
            }

            return model;
        }

        public async Task<AdminModel> Edit(int id, AdminModel model)
        {
            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                string query;

                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

                    query = @"UPDATE ""admin"" 
                              SET ""FirstName""=@FirstName,
                                  ""LastName""=@LastName,
                                  ""Phone""=@Phone,
                                  ""Email""=@Email,
                                  ""PasswordHash""=@PasswordHash
                              WHERE ""Id""=@Id";
                }
                else
                {
                    query = @"UPDATE ""admin"" 
                              SET ""FirstName""=@FirstName,
                                  ""LastName""=@LastName,
                                  ""Phone""=@Phone,
                                  ""Email""=@Email
                              WHERE ""Id""=@Id";
                }

                var command = new NpgsqlCommand(query, connection);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@FirstName", model.FirstName);
                command.Parameters.AddWithValue("@LastName", model.LastName);
                command.Parameters.AddWithValue("@Phone", (object)model.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", model.Email);

                if (!string.IsNullOrEmpty(model.PasswordHash))
                    command.Parameters.AddWithValue("@PasswordHash", model.PasswordHash);

                var rows = await command.ExecuteNonQueryAsync();
                if (rows == 0) return null;

                model.Id = id;
                model.PasswordHash = null;
            }

            return model;
        }

        public async Task<IActionResult> Delete(int id)
        {
            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();
                var command = new NpgsqlCommand(
                    @"DELETE FROM ""admin"" WHERE ""Id""=@Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                var rows = await command.ExecuteNonQueryAsync();

                if (rows == 0)
                    return new NotFoundObjectResult(new { status = false, message = "Record not found" });

                return new OkObjectResult(new { status = true, message = "Record deleted successfully" });
            }
        }

        // ✅ NEW: Email se admin fetch karo (login ke liye)
        public async Task<AdminModel> GetAdminByEmail(string email)
        {
            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    @"SELECT ""Id"", ""FirstName"", ""LastName"", ""Phone"", ""Email"", ""PasswordHash"", ""CreatedAt"" 
                      FROM ""admin"" 
                      WHERE ""Email"" = @Email 
                      LIMIT 1", connection);

                command.Parameters.AddWithValue("@Email", email);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new AdminModel
                        {
                            Id = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Email = reader.GetString(4),
                            PasswordHash = reader.GetString(5),
                            CreatedAt = reader.GetDateTime(6)
                        };
                    }
                }
            }

            return null;
        }
    }
}