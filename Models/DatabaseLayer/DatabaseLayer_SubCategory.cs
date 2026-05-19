using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<List<SubCategoryModel>> GetAllSubCategory();
        Task<SubCategoryModel> Add(SubCategoryModel model);
        Task<SubCategoryModel> Edit(int id, SubCategoryModel model);
        Task<SubCategoryModel?> GetSubCategoryById(int id);
        Task<bool> DeleteSubCategory(int id);
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<SubCategoryModel>> GetAllSubCategory()
        {
            var subCategories = new List<SubCategoryModel>();

            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(@"
                    SELECT 
                        sc.""Id"",
                        sc.""CategoryId"",
                        sc.""SubCategoryName"",
                        sc.""SubCategoryImageUrl"",
                        sc.""Status"",
                        sc.""CreatedAt"",
                        c.""Name"" AS ""CategoryName""
                    FROM subcategory sc
                    INNER JOIN category c 
                        ON sc.""CategoryId"" = c.""Id""
                    ORDER BY sc.""Id"" DESC
                ", connection);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var subCategory = new SubCategoryModel()
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),

                        SubCategoryName = reader.IsDBNull(reader.GetOrdinal("SubCategoryName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("SubCategoryName")),

                        SubCategoryImageUrl = reader.IsDBNull(reader.GetOrdinal("SubCategoryImageUrl"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("SubCategoryImageUrl")),

                        CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),

                        CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("CategoryName")),

                        Status = reader.GetBoolean(reader.GetOrdinal("Status"))
                    };

                    subCategories.Add(subCategory);
                }
            }

            return subCategories;
        }

        public async Task<SubCategoryModel> Add(SubCategoryModel model)
        {
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(@"
        INSERT INTO subCategory (""SubCategoryName"", ""SubCategoryImageUrl"", ""CategoryId"", ""Status"")
        VALUES (@SubCategoryName, @SubCategoryImageUrl, @CategoryId, @Status)
        RETURNING ""Id"", ""CreatedAt"";
    ", connection);

            command.Parameters.AddWithValue("@SubCategoryName", model.SubCategoryName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@SubCategoryImageUrl", model.SubCategoryImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", model.CategoryId);
            command.Parameters.AddWithValue("@Status", model.Status);

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    model.Id = reader.GetInt32(0);
                    model.CreatedAt = reader.GetDateTime(1);
                }
            }

            return model;
        }

        public async Task<SubCategoryModel> Edit(int id, SubCategoryModel model)
        {
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@"
    UPDATE subcategory
    SET ""SubCategoryName"" = @SubCategoryName,
        ""SubCategoryImageUrl"" = @SubCategoryImageUrl,
        ""CategoryId"" = @CategoryId,
        ""Status"" = @Status
    WHERE ""Id"" = @Id
    RETURNING ""Id"", ""CreatedAt"";
", connection);

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@SubCategoryName", model.SubCategoryName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@SubCategoryImageUrl", model.SubCategoryImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", model.CategoryId);
            command.Parameters.AddWithValue("@Status", model.Status);

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    model.Id = reader.GetInt32(0);
                    model.CreatedAt = reader.GetDateTime(1);
                }
            }

            return model;
        }

        public async Task<SubCategoryModel?> GetSubCategoryById(int id)
        {
            SubCategoryModel? subCategory = null;
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@"
                SELECT 
                    sc.""Id"",
                    sc.""CategoryId"",
                    sc.""SubCategoryName"",
                    sc.""SubCategoryImageUrl"",
                    sc.""Status"",
                    sc.""CreatedAt"",
                    c.""Name"" AS ""CategoryName""
                FROM subcategory sc
                INNER JOIN category c 
                    ON sc.""CategoryId"" = c.""Id""
                WHERE sc.""Id"" = @Id
            ", connection);
            command.Parameters.AddWithValue("@Id", id);
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    subCategory = new SubCategoryModel()
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        SubCategoryName = reader.IsDBNull(reader.GetOrdinal("SubCategoryName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("SubCategoryName")),
                        SubCategoryImageUrl = reader.IsDBNull(reader.GetOrdinal("SubCategoryImageUrl"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("SubCategoryImageUrl")),
                        CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                        CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("CategoryName")),
                        Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                }
            }
            return subCategory;
        }

        public async Task<bool> DeleteSubCategory(int id)
        {
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@"
                DELETE FROM subcategory
                WHERE ""Id"" = @Id
            ", connection);
            command.Parameters.AddWithValue("@Id", id);
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}