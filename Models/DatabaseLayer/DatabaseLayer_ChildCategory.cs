using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<List<childCategoryModel>> GetAllChildCategory();
        Task<childCategoryModel> Add(childCategoryModel model);
        Task<childCategoryModel> Edit(int id, childCategoryModel model);
        Task<childCategoryModel?> GetChildCategoryById(int id);
        Task<bool> DeleteChildCategory(int id);
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<childCategoryModel>> GetAllChildCategory()
        {
            var childCategories = new List<childCategoryModel>();

            using var connection = new NpgsqlConnection(DbConnection);
            {
                await connection.OpenAsync();
                var command = new NpgsqlCommand(@"
    SELECT 
        cc.""Id"",
        cc.""ChildCategoryName"",
        cc.""ChildCategoryImageUrl"",
        cc.""SubCategoryId"",
        sc.""SubCategoryName"",
        sc.""CategoryId"",
        c.""Name"" AS ""CategoryName"",
        cc.""CreatedAt"",
        cc.""Status""
    FROM childCategory cc
    INNER JOIN ""subcategory"" sc 
        ON cc.""SubCategoryId"" = sc.""Id""
    INNER JOIN ""category"" c 
        ON sc.""CategoryId"" = c.""Id""
", connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var childCategory = new childCategoryModel
                    {
                        Id = reader.GetInt32(0),
                        ChildCategoryName = reader.GetString(1),
                        ChildCategoryImageUrl = reader.GetString(2),
                        SubCategoryId = reader.GetInt32(3),
                        SubCategoryName = reader.GetString(4),
                        CategoryId = reader.GetInt32(5),
                        CategoryName = reader.GetString(6),
                        CreatedAt = reader.GetDateTime(7),
                        Status = reader.GetBoolean(8)
                    };
                    childCategories.Add(childCategory);
                }
            }
            return childCategories;
        }

        public async Task<childCategoryModel> Add(childCategoryModel model)
        {
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(@"
        INSERT INTO childCategory 
        (""ChildCategoryName"", ""ChildCategoryImageUrl"", ""CategoryId"", ""SubCategoryId"", ""Status"")
        VALUES 
        (@ChildCategoryName, @ChildCategoryImageUrl, @CategoryId, @SubCategoryId, @Status)
        RETURNING ""Id"", ""CreatedAt"";
    ", connection);

            command.Parameters.AddWithValue("@ChildCategoryName", model.ChildCategoryName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ChildCategoryImageUrl", model.ChildCategoryImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", model.CategoryId);
            command.Parameters.AddWithValue("@SubCategoryId", model.SubCategoryId);
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

        public async Task<childCategoryModel> Edit(int id, childCategoryModel model)
        {
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@"UPDATE childCategory
             SET ""ChildCategoryName"" = @ChildCategoryName,
                 ""ChildCategoryImageUrl"" = @ChildCategoryImageUrl,
                 ""CategoryId"" = @CategoryId,
                 ""SubCategoryId"" = @SubCategoryId,
                 ""Status"" = @Status
             WHERE ""Id"" = @Id
             RETURNING ""Id"", ""ChildCategoryName"", ""ChildCategoryImageUrl"", ""CategoryId"", ""SubCategoryId"", ""Status"", ""CreatedAt"";", connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@ChildCategoryName", model.ChildCategoryName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ChildCategoryImageUrl", model.ChildCategoryImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", model.CategoryId);
            command.Parameters.AddWithValue("@SubCategoryId", model.SubCategoryId);
            command.Parameters.AddWithValue("@Status", model.Status);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                model.Id = reader.GetInt32(0);
                model.ChildCategoryName = reader.GetString(1);
                model.ChildCategoryImageUrl = reader.GetString(2);
                model.CategoryId = reader.GetInt32(3);
                model.SubCategoryId = reader.GetInt32(4);
                model.Status = reader.GetBoolean(5);
                model.CreatedAt = reader.GetDateTime(6);
            }
            return model;
        }

        public async Task<childCategoryModel?> GetChildCategoryById(int id)
        {
            childCategoryModel? childCategory = null;
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@"SELECT ""Id"", ""ChildCategoryName"", ""ChildCategoryImageUrl"", ""CategoryId"", ""SubCategoryId"", ""Status"", ""CreatedAt""
                                              FROM childCategory
                                              WHERE ""Id"" = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                childCategory = new childCategoryModel
                {
                    Id = reader.GetInt32(0),
                    ChildCategoryName = reader.GetString(1),
                    ChildCategoryImageUrl = reader.GetString(2),
                    CategoryId = reader.GetInt32(3),
                    SubCategoryId = reader.GetInt32(4),
                    Status = reader.GetBoolean(5),
                    CreatedAt = reader.GetDateTime(6)
                };
            }
            return childCategory;
        }

        public async Task<bool> DeleteChildCategory(int id)
        {
            using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@"DELETE FROM childCategory WHERE ""Id"" = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}
