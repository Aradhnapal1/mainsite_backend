using firstproject.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<List<categoryModel>> GetAllCategory();
        Task<categoryModel> Add(categoryModel model);
        Task<categoryModel> Edit(int id, categoryModel model);
        Task<bool> DeleteCategory(int id);
        Task<categoryModel?> GetCategoryById(int id);
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<categoryModel>> GetAllCategory()
        {
            var categories = new List<categoryModel>();

            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    @"SELECT ""Id"", ""Name"", ""Status"", ""ImageUrl"", ""CreatedAt""
                      FROM ""category""
                      ORDER BY ""Id"" DESC",
                    connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var category = new categoryModel
                        {
                            id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                            ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("ImageUrl")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                        };

                        categories.Add(category);
                    }
                }
            }

            return categories;
        }

        public async Task<categoryModel> Add(categoryModel model)
        {
            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    @"INSERT INTO ""category"" (""Name"", ""ImageUrl"", ""Status"") 
                      VALUES (@Name, @ImageUrl, @Status)
                      RETURNING ""Id"", ""CreatedAt"";",
                    connection);

                command.Parameters.AddWithValue("@Name", model.Name);
                command.Parameters.AddWithValue("@ImageUrl", (object?)model.ImageUrl ?? DBNull.Value);
                command.Parameters.AddWithValue("@Status", model.Status);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        model.id = reader.GetInt32(0);
                        model.CreatedAt = reader.GetDateTime(1);
                    }
                }
            }

            return model;
        }

        public async Task<categoryModel> Edit(int id, categoryModel model)
        {
            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    @"UPDATE ""category"" 
                      SET ""Name"" = @Name,
                          ""ImageUrl"" = COALESCE(@ImageUrl, ""ImageUrl""),
                          ""Status"" = @Status
                      WHERE ""Id"" = @Id
                      RETURNING ""Id"", ""Name"", ""ImageUrl"", ""Status"";",
                    connection);

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", model.Status);
                command.Parameters.AddWithValue("@ImageUrl",
                    string.IsNullOrEmpty(model.ImageUrl) ? (object)DBNull.Value : model.ImageUrl);

                await command.ExecuteNonQueryAsync();

                return model;
            }
        }

        public async Task<bool> DeleteCategory(int id)
        {
            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    @"DELETE FROM ""category"" 
                      WHERE ""Id"" = @Id",
                    connection);

                command.Parameters.AddWithValue("@Id", id);

                var rows = await command.ExecuteNonQueryAsync();

                return rows > 0;
            }
        }

        public async Task<categoryModel?> GetCategoryById(int id)
        {
            using (var connection = new NpgsqlConnection(DbConnection))
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    @"SELECT ""Id"", ""ImageUrl"" FROM ""category"" WHERE ""Id"" = @Id",
                    connection);

                command.Parameters.AddWithValue("@Id", id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new categoryModel
                        {
                            id = reader.GetInt32(reader.GetOrdinal("Id")),
                            ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("ImageUrl"))
                        };
                    }
                }

                return null;
            }
        }
    }
}