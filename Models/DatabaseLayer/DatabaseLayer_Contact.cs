using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{


    public partial interface IDatabaseLayer
    {
        Task<List<Contactmodel>> GetContact();
        Task<IActionResult> AddContact([FromForm] Contactmodel model);
        Task<IActionResult> UpdateContact(int id, [FromForm] Contactmodel model);
        Task<IActionResult> DeleteContact(int id);
    }

    public partial interface IDatabaseLayer
    {
    }


    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<Contactmodel>> GetContact()
        {
            List<Contactmodel> contacts = new List<Contactmodel>();
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "SELECT id, fullname, email, subject, message, phone, createdat FROM contact ",
                    connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Contactmodel contact = new Contactmodel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                FullName = reader["fullname"]?.ToString(),
                                Email = reader["email"]?.ToString(),
                                Subject = reader["subject"]?.ToString(),
                                Message = reader["message"]?.ToString(),
                                PhoneNumber = reader["phone"]?.ToString(),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat"))
                            };
                            contacts.Add(contact);
                        }
                    }
                }
            }
            return contacts;
        }


        public async Task<IActionResult> AddContact([FromForm] Contactmodel model)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "INSERT INTO contact (fullname, email, subject, message, phone) VALUES (@fullname, @email, @subject, @message, @phone) RETURNING id",
                    connection))
                {
                    command.Parameters.AddWithValue("@fullname", model.FullName);
                    command.Parameters.AddWithValue("@email", model.Email);
                    command.Parameters.AddWithValue("@subject", model.Subject);
                    command.Parameters.AddWithValue("@message", model.Message);
                    command.Parameters.AddWithValue("@phone", model.PhoneNumber);
                    var newId = (int)await command.ExecuteScalarAsync();
                    return new JsonResult(new { status = true, message = "Contact added successfully", id = newId });
                }
            }




        }




        public async Task<IActionResult> UpdateContact(int id, [FromForm] Contactmodel model)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "UPDATE contact SET fullname = @fullname, email = @email, subject = @subject, message = @message, phone = @phone WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@fullname", model.FullName);
                    command.Parameters.AddWithValue("@email", model.Email);
                    command.Parameters.AddWithValue("@subject", model.Subject);
                    command.Parameters.AddWithValue("@message", model.Message);
                    command.Parameters.AddWithValue("@phone", model.PhoneNumber);
                    command.Parameters.AddWithValue("@id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        return new JsonResult(new { status = true, message = "Contact updated successfully" });
                    }
                    else
                    {
                        return new JsonResult(new { status = false, message = "Contact not found" }) { StatusCode = 404 };
                    }
                }
            }
        }




        public async Task<IActionResult> DeleteContact(int id)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "DELETE FROM contact WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        return new JsonResult(new { status = true, message = "Contact deleted successfully" });
                    }
                    else
                    {
                        return new JsonResult(new { status = false, message = "Contact not found" }) { StatusCode = 404 };
                    }
                }
            }
        }

    }
}
    
