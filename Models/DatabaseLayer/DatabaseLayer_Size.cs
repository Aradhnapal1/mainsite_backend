using Microsoft.AspNetCore.Mvc;

using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
   public  partial interface IDatabaseLayer
    {
        Task<List<Size>> GetSize();
        Task<IActionResult> AddSize([FromForm] Size size);
        Task<IActionResult> EditSize(int id, [FromForm] Size size);
        Task<IActionResult> DeleteSize(int id);

    }
    public partial interface IDatabaseLayer
    {

    }
    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<Size>> GetSize()
        {
            List<Size> sizes = new List<Size>();

            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand(
                    "SELECT id, size_name, description, is_active FROM sizes ",
                    connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Size size = new Size
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                SizeName = reader["size_name"]?.ToString(),
                                Description = reader["description"]?.ToString(),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
                            };

                            sizes.Add(size);
                        }
                    }
                }
            }

            return sizes;
        }

        public async Task<IActionResult> AddSize([FromForm] Size size)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "INSERT INTO sizes (size_name, description, is_active) VALUES (@size_name, @description, @is_active) RETURNING id",
                    connection))
                {
                    command.Parameters.AddWithValue("@size_name", size.SizeName);
                    command.Parameters.AddWithValue("@description", (object)size.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@is_active", size.IsActive);
                    var newId = await command.ExecuteScalarAsync();
                    if (newId != null)
                    {
                        size.Id = Convert.ToInt32(newId);
                        return new OkObjectResult(size);
                    }
                    else
                    {
                        return new BadRequestObjectResult("Failed to add size.");
                    }
                }
            }
        }


        public async Task<IActionResult> EditSize(int id, [FromForm] Size size)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "UPDATE sizes SET size_name = @size_name, description = @description, is_active = @is_active WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@size_name", size.SizeName);
                    command.Parameters.AddWithValue("@description", (object)size.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@is_active", size.IsActive);
                    command.Parameters.AddWithValue("@id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        return new OkObjectResult(size);
                    }
                    else
                    {
                        return new NotFoundObjectResult("Size not found.");
                    }
                }
            }
        }



        public async Task<IActionResult> DeleteSize(int id)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "DELETE FROM sizes WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        return new OkObjectResult("Size deleted successfully.");
                    }
                    else
                    {
                        return new NotFoundObjectResult("Size not found.");
                    }
                }
            }
        }

        }
}
