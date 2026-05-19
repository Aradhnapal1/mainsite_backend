using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<List<customermodel>> GetCustomerLogo();
        Task<customermodel> Add(customermodel model);
        Task<customermodel> Edit(int id, [FromForm] customermodel model);
        Task<customermodel> DeleteCustomerLogo(int id);
        Task<customermodel> GetCustomerLogoById(int id);
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<customermodel>> GetCustomerLogo()
        {
            List<customermodel> customerLogos = new List<customermodel>();

            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand(
                    "SELECT id, customername, customerimage, status, createdat FROM customerlogo",
                    connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            customermodel customerLogo = new customermodel
                            {
                                id = reader.GetInt32(reader.GetOrdinal("id")),
                                customername = reader["customername"]?.ToString(),
                                customerimage = reader["customerimage"]?.ToString(),
                                status = reader.GetBoolean(reader.GetOrdinal("status")),
                                createdat = reader.IsDBNull(reader.GetOrdinal("createdat"))
                                    ? DateTime.MinValue
                                    : reader.GetDateTime(reader.GetOrdinal("createdat"))
                            };

                            customerLogos.Add(customerLogo);
                        }
                    }
                }
            }

            return customerLogos;
        }

        public async Task<customermodel> Add(customermodel model)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand(
                    @"INSERT INTO customerlogo 
              (customername, customerimage, status, createdat) 
              VALUES (@customername, @customerimage, @status, @createdat) 
              RETURNING id",
                    connection))
                {
                    command.Parameters.AddWithValue("@customername", model.customername ?? (object)DBNull.Value);

                    // ✅ Handle NULL image
                    command.Parameters.AddWithValue(
                        "@customerimage",
                        string.IsNullOrEmpty(model.customerimage)
                            ? (object)DBNull.Value
                            : model.customerimage
                    );

                    command.Parameters.AddWithValue("@status", model.status);
                    command.Parameters.AddWithValue("@createdat", DateTime.UtcNow);

                    var id = (int)await command.ExecuteScalarAsync();
                    model.id = id;
                }
            }

            return model;
        }

        public async Task<customermodel> Edit(int id, customermodel model)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand(
                    @"UPDATE customerlogo 
              SET customername = @customername, 
                  customerimage = @customerimage, 
                  status = @status 
              WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@customername", model.customername ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@customerimage",
                        string.IsNullOrEmpty(model.customerimage)
                            ? (object)DBNull.Value
                            : model.customerimage
                    );

                    command.Parameters.AddWithValue("@status", model.status);
                    command.Parameters.AddWithValue("@id", id);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return model;
                    else
                        return null;
                }
            }
        }
        public async Task<customermodel?> GetCustomerLogoById(int id)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand(
                    @"SELECT id, customername, customerimage, status, createdat 
              FROM customerlogo 
              WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new customermodel
                            {
                                id = reader.GetInt32(reader.GetOrdinal("id")),
                                customername = reader["customername"]?.ToString(),
                                customerimage = reader["customerimage"]?.ToString(),
                                status = reader.GetBoolean(reader.GetOrdinal("status")),
                                createdat = reader.IsDBNull(reader.GetOrdinal("createdat"))
                                    ? DateTime.MinValue
                                    : reader.GetDateTime(reader.GetOrdinal("createdat"))
                            };
                        }
                    }
                }
            }

            return null; // not found
        }

        public async Task<customermodel> DeleteCustomerLogo(int id)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand(
                    "DELETE FROM customerlogo WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return new customermodel { id = id };
                    else
                        return null;
                }
            }
        }
    }
}