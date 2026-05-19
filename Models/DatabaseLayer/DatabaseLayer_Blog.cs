using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
   public partial interface IDatabaseLayer
    {
        Task<List<Blogmodel>> GetAllBlogs();
        Task<Blogmodel> AddBlog([FromForm] Blogmodel model);

        Task<Blogmodel> EditBlog(int id, [FromForm] Blogmodel model);
        Task<Blogmodel> DeleteBlog(int id);
        Task<Blogmodel> GetBlogById(int id);

    }
    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<Blogmodel>> GetAllBlogs()
        {
            var list = new List<Blogmodel>();

            using var con = new NpgsqlConnection(this.DbConnection);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
        SELECT 
            id,
         
            title,
            slug,
            content,
            featured_image,
            status,
            views,
            created_at,
            updated_at
        FROM blogs
        ORDER BY created_at DESC
    ", con);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Blogmodel
                {
                    Id = reader.GetInt32(0),
                    Title = reader["title"]?.ToString(),
                    Slug = reader["slug"]?.ToString(),
                    Content = reader["content"]?.ToString(),
                    FeaturedImage = reader["featured_image"]?.ToString(),
                    Status = reader["status"]?.ToString(),
                    Views = reader.GetInt32(6),
                    CreatedAt = reader.GetDateTime(7),
                    UpdatedAt = reader.GetDateTime(8)
                });
            }

            return list;
        }

        public async Task<Blogmodel> AddBlog(Blogmodel model)
        {
            using var con = new NpgsqlConnection(this.DbConnection);

            await con.OpenAsync();

            // ✅ Auto Generate Slug
            model.Slug = model.Title?
                .Trim()
                .ToLower()
                .Replace(" ", "-");

            using var cmd = new NpgsqlCommand(@"
        INSERT INTO blogs
        (
          
            title,
            slug,
            content,
            featured_image,
            status,
            views,
            created_at,
            updated_at
        )
        VALUES
        (
          
            @title,
            @slug,
            @content,
            @featured_image,
            @status,
            @views,
            @created_at,
            @updated_at
        )
        RETURNING id
    ", con);


            cmd.Parameters.AddWithValue("@title", model.Title ?? "");
            cmd.Parameters.AddWithValue("@slug", model.Slug ?? "");
            cmd.Parameters.AddWithValue("@content", model.Content ?? "");
            cmd.Parameters.AddWithValue("@featured_image",
                (object?)model.FeaturedImage ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@status",
                model.Status ?? "draft");

            cmd.Parameters.AddWithValue("@views", model.Views);

            cmd.Parameters.AddWithValue("@created_at",
                model.CreatedAt == default
                    ? DateTime.UtcNow
                    : model.CreatedAt);

            cmd.Parameters.AddWithValue("@updated_at",
                model.UpdatedAt == default
                    ? DateTime.UtcNow
                    : model.UpdatedAt);

            var newId = await cmd.ExecuteScalarAsync();

            model.Id = Convert.ToInt32(newId);

            return model;
        }




        public async Task<Blogmodel?> EditBlog(int id, Blogmodel model)
        {
            using var con = new NpgsqlConnection(this.DbConnection);

            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
        UPDATE blogs
        SET
            title = @title,
            slug = @slug,
            content = @content,
            featured_image = @featured_image,
            status = @status,
            updated_at = @updated_at
        WHERE id = @id
    ", con);

            cmd.Parameters.AddWithValue("@id", id);

            cmd.Parameters.AddWithValue("@title", model.Title ?? "");

            cmd.Parameters.AddWithValue("@slug",
                model.Title?
                .Trim()
                .ToLower()
                .Replace(" ", "-") ?? "");

            cmd.Parameters.AddWithValue("@content", model.Content ?? "");

            cmd.Parameters.AddWithValue(
                "@featured_image",
                (object?)model.FeaturedImage ?? DBNull.Value
            );

            cmd.Parameters.AddWithValue("@status",
                model.Status ?? "draft");

            cmd.Parameters.AddWithValue("@updated_at",
                DateTime.UtcNow);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return null;

            model.Id = id;

            return model;
        }





        public async Task<Blogmodel> DeleteBlog(int id)
        {
            using var con = new NpgsqlConnection(this.DbConnection);

            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "DELETE FROM blogs WHERE id = @id",
                con);

            cmd.Parameters.AddWithValue("@id", id);

            // ✅ Execute delete query
            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
            {
                return null;
            }

            return new Blogmodel
            {
                Id = id
            };
        }



        public async Task<Blogmodel> GetBlogById(int id)
        {
            using var con = new NpgsqlConnection(this.DbConnection);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(@" 
                SELECT *
                FROM blogs
                WHERE id = @id
            ", con);

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var model = new Blogmodel
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Slug = reader.GetString(reader.GetOrdinal("slug")),
                Content = reader.GetString(reader.GetOrdinal("content")),
                FeaturedImage = reader.GetString(reader.GetOrdinal("featured_image")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                Views = reader.GetInt32(reader.GetOrdinal("views")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };

            return model;
        }
    }

}
