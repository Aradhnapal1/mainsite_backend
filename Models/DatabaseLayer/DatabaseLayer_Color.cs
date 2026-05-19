using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Drawing;
using System.Linq;



namespace firstproject.Models.DatabaseLayer
{

    public partial interface IDatabaseLayer
    {
        Task<List<Colormodel>> GetColor();
        Task<IActionResult> AddColor([FromForm] Colormodel color);

        Task<IActionResult> EditColor(int id, [FromForm] Colormodel color);
        Task<IActionResult> DeleteColor(int id);


    }
    public partial interface IDatabaseLayer
    {
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<Colormodel>> GetColor()
        {
            List<Colormodel> colors = new List<Colormodel>();
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "SELECT id, colorname, colorcode, isactive FROM color ",
                    connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Colormodel color = new Colormodel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Colorname = reader["colorname"]?.ToString(),
                                Colorcode = reader["colorcode"]?.ToString(),

                                Isactive = reader.GetBoolean(reader.GetOrdinal("isactive"))
                            };
                            colors.Add(color);
                        }
                    }
                }
            }
            return colors;
        }

        public async Task<IActionResult> AddColor([FromForm] Colormodel color)
        {
            if (!string.IsNullOrEmpty(color.Colorname) && string.IsNullOrEmpty(color.Colorcode))
            {
                var knownColors = Enum.GetValues(typeof(KnownColor))
                                      .Cast<KnownColor>()
                                      .Select(c => Color.FromKnownColor(c));

                var match = knownColors.FirstOrDefault(c =>
                    c.Name.Equals(color.Colorname, StringComparison.OrdinalIgnoreCase));

                if (match.IsEmpty)
                {
                    match = knownColors.FirstOrDefault(c =>
                        c.Name.ToLower().Contains(color.Colorname.ToLower()) ||
                        color.Colorname.ToLower().Contains(c.Name.ToLower()));
                }

                if (!match.IsEmpty)
                {
                    color.Colorcode = $"#{match.R:X2}{match.G:X2}{match.B:X2}";
                }
                else
                {
                    // 👉 fallback (default color de do instead of error)
                    color.Colorcode = "#000000"; // black default
                }
            }

            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "INSERT INTO color (colorname, colorcode, isactive) VALUES (@colorname, @colorcode, @isactive) RETURNING id",
                    connection))
                {
                    command.Parameters.AddWithValue("@colorname", color.Colorname ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@colorcode", color.Colorcode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@isactive", color.Isactive);

                    var result = await command.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int newId))
                    {
                        color.Id = newId;
                        return new JsonResult(color) { StatusCode = 201 };
                    }
                    else
                    {
                        return new JsonResult(new { error = "Failed to add color" }) { StatusCode = 500 };
                    }
                }
            }
        }


        public async Task<IActionResult> EditColor(int id, [FromForm] Colormodel color)
        {
            // ✅ Auto set colorcode from colorname (same as Add)
            if (!string.IsNullOrEmpty(color.Colorname) && string.IsNullOrEmpty(color.Colorcode))
            {
                var knownColors = Enum.GetValues(typeof(KnownColor))
                                      .Cast<KnownColor>()
                                      .Select(c => Color.FromKnownColor(c));

                var match = knownColors.FirstOrDefault(c =>
                    c.Name.Equals(color.Colorname, StringComparison.OrdinalIgnoreCase));

                if (match.IsEmpty)
                {
                    match = knownColors.FirstOrDefault(c =>
                        c.Name.ToLower().Contains(color.Colorname.ToLower()) ||
                        color.Colorname.ToLower().Contains(c.Name.ToLower()));
                }

                if (!match.IsEmpty)
                {
                    color.Colorcode = $"#{match.R:X2}{match.G:X2}{match.B:X2}";
                }
                else
                {
                    // ✅ Better fallback (unique color)
                    int hash = color.Colorname.GetHashCode();
                    var random = new Random(hash);
                    color.Colorcode = $"#{random.Next(0x1000000):X6}";
                }
            }

            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "UPDATE color SET colorname = @colorname, colorcode = @colorcode, isactive = @isactive WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@colorname", color.Colorname ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@colorcode", color.Colorcode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@isactive", color.Isactive);
                    command.Parameters.AddWithValue("@id", id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        color.Id = id; // optional but useful
                        return new JsonResult(color) { StatusCode = 200 };
                    }
                    else
                    {
                        return new JsonResult(new { error = "Failed to update color" }) { StatusCode = 500 };
                    }
                }
            }
        }
        public async Task<IActionResult> DeleteColor(int id)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "DELETE FROM color WHERE id = @id",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        return new JsonResult(new { message = "Color deleted successfully" }) { StatusCode = 200 };
                    }
                    else
                    {
                        return new JsonResult(new { error = "Failed to delete color" }) { StatusCode = 500 };
                    }
                }
            }



        }
    }
}
