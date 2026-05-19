using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<List<Brandmodel>> GetBrand();
        Task<Brandmodel> Add(Brandmodel model);
        Task<bool> Edit(int id, Brandmodel model);
        Task<bool> DeleteBrand(int id);
        Task<Brandmodel> GetBrandById(int id);

        Task UpdateBrandImage(int id, Brandmodel model);
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        // ✅ GET ALL

        public async Task UpdateBrandImage(int id, Brandmodel model)
        {
            using var con = new NpgsqlConnection(this.DbConnection);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(
                @"UPDATE brand 
          SET brandimage=@image, publicid=@publicid 
          WHERE id=@id", con);

            cmd.Parameters.AddWithValue("@image", model.BrandImage ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@publicid", model.PublicId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<List<Brandmodel>> GetBrand()
        {
            var list = new List<Brandmodel>();

            using var con = new NpgsqlConnection(this.DbConnection);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT id, brandname, brandimage, publicid, isactive FROM brand",
                con);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Brandmodel
                {
                    Id = reader.GetInt32(0),
                    BrandName = reader["brandname"]?.ToString(),
                    BrandImage = reader["brandimage"]?.ToString(),
                    PublicId = reader["publicid"]?.ToString(), // 🔥 FIX
                    IsActive = reader.GetBoolean(4)
                });
            }

            return list;
        }

        // ✅ ADD (ONLY BASIC DATA)
        public async Task<Brandmodel> Add(Brandmodel model)
        {
            using var con = new NpgsqlConnection(this.DbConnection);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO brand (brandname, isactive) VALUES (@name, @active) RETURNING id",
                con);

            cmd.Parameters.AddWithValue("@name", model.BrandName);
            cmd.Parameters.AddWithValue("@active", model.IsActive);

            model.Id = (int)await cmd.ExecuteScalarAsync();

            return model;
        }

        // ✅ EDIT (🔥 MOST IMPORTANT FIX)
        public async Task<bool> Edit(int id, Brandmodel model)
        {
            using var con = new NpgsqlConnection(this.DbConnection);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(
                @"UPDATE brand 
                  SET brandname=@name, 
                      brandimage=@image,
                      publicid=@publicid,
                      isactive=@active 
                  WHERE id=@id",
                con);

            cmd.Parameters.AddWithValue("@name", model.BrandName);
            cmd.Parameters.AddWithValue("@image", (object?)model.BrandImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@publicid", (object?)model.PublicId ?? DBNull.Value); // 🔥 FIX
            cmd.Parameters.AddWithValue("@active", model.IsActive);
            cmd.Parameters.AddWithValue("@id", id);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ✅ GET BY ID
        public async Task<Brandmodel> GetBrandById(int id)
        {
            using var con = new NpgsqlConnection(this.DbConnection);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT id, brandname, brandimage, publicid, isactive FROM brand WHERE id=@id",
                con);

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Brandmodel
                {
                    Id = reader.GetInt32(0),
                    BrandName = reader["brandname"]?.ToString(),
                    BrandImage = reader["brandimage"]?.ToString(),
                    PublicId = reader["publicid"]?.ToString(), // 🔥 FIX
                    IsActive = reader.GetBoolean(4)
                };
            }

            return null;
        }

        // ✅ DELETE
        public async Task<bool> DeleteBrand(int id)
        {
            using var con = new NpgsqlConnection(this.DbConnection);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "DELETE FROM brand WHERE id=@id",
                con);

            cmd.Parameters.AddWithValue("@id", id);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}