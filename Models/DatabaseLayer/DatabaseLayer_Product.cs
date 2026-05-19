using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Npgsql;
using NpgsqlTypes;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<List<Productmodel>> GetProduct();
        Task<IActionResult> AddProduct(Productmodel product);
        Task<Productmodel?> GetProductById(int id);
        Task<IActionResult> UpdateProduct(int id, Productmodel product);
        Task<IActionResult> DeleteProduct(int id);
        Task<List<Productmodel>> FilterProducts(ProductFilterModel filter);



    }
    public partial interface IDatabaseLayer
    {
    }


    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<List<Productmodel>> GetProduct()
        {
            List<Productmodel> products = new List<Productmodel>();

            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                // ✅ 1. GET PRODUCT
                using (var command = new NpgsqlCommand(@"
        SELECT 
            p.*,
            c.""Name"" AS categoryname,
            sc.""SubCategoryName"" AS subcategoryname,
            cc.""ChildCategoryName"" AS childcategoryname,
            b.brandname AS brandname,

            ARRAY(
                SELECT s.size_name 
                FROM sizes s 
                WHERE s.id = ANY(p.sizeids)
            ) AS sizenames,

            ARRAY(
                SELECT col.colorname 
                FROM color col 
                WHERE col.id = ANY(p.colorids)
            ) AS colornames

        FROM product p
        LEFT JOIN category c ON p.categoryid = c.""Id""
        LEFT JOIN subcategory sc ON p.subcategoryid = sc.""Id""
        LEFT JOIN childcategory cc ON p.childcategoryid = cc.""Id""
        LEFT JOIN brand b ON p.brandid = b.id
        ", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new Productmodel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                ProductName = reader["productname"]?.ToString(),
                                Slug = reader["slug"]?.ToString(),
                                Sku = reader["sku"]?.ToString(),
                                ShortDescription = reader["shortdescription"]?.ToString(),
                                Description = reader["description"]?.ToString(),
                                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                                DiscountPrice = reader.IsDBNull(reader.GetOrdinal("discountprice"))
                                                    ? null : reader.GetDecimal(reader.GetOrdinal("discountprice")),
                                Stock = reader.GetInt32(reader.GetOrdinal("stock")),
                                CategoryId = reader.GetInt32(reader.GetOrdinal("categoryid")),
                                SubCategoryId = reader.GetInt32(reader.GetOrdinal("subcategoryid")),
                                ChildCategoryId = reader.IsDBNull(reader.GetOrdinal("childcategoryid"))
                                                    ? null : reader.GetInt32(reader.GetOrdinal("childcategoryid")),
                                BrandId = reader.IsDBNull(reader.GetOrdinal("brandid"))
                                                    ? null : reader.GetInt32(reader.GetOrdinal("brandid")),
                                SizeIds = reader["sizeids"] as int[],
                                ColorIds = reader["colorids"] as int[],
                                Image = reader["image"]?.ToString(),
                                ImageGallery = reader["imagegallery"] as string[],
                                IsActive = reader.GetBoolean(reader.GetOrdinal("isactive")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),

                                CategoryName = reader["categoryname"]?.ToString(),
                                SubCategoryName = reader["subcategoryname"]?.ToString(),
                                ChildCategoryName = reader["childcategoryname"]?.ToString(),
                                BrandName = reader["brandname"]?.ToString(),

                                SizeNames = reader["sizenames"] != DBNull.Value
                                                ? ((string[])reader["sizenames"]).ToList()
                                                : null,

                                ColorNames = reader["colornames"] != DBNull.Value
                                                ? ((string[])reader["colornames"]).ToList()
                                                : null,

                                Variants = new List<Variantmodel>() // 👈 important
                            });
                        }
                    }
                }

                // ✅ 2. GET VARIANTS (ALL)
                using (var variantCmd = new NpgsqlCommand(@"
        SELECT 
            v.*,

            ARRAY(
                SELECT s.size_name 
                FROM sizes s 
                WHERE s.id = ANY(v.sizeid)
            ) AS sizenames,

            ARRAY(
                SELECT c.colorname 
                FROM color c 
                WHERE c.id = ANY(v.colorid)
            ) AS colornames

        FROM variant v
        ", connection))
                {
                    using (var reader = await variantCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var variant = new Variantmodel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                VariantName = reader["variantname"]?.ToString(),
                                ProductId = reader.GetInt32(reader.GetOrdinal("productid")),
                                SizeId = reader["sizeid"] as int[],
                                ColorId = reader["colorid"] as int[],
                                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                                Stock = reader.GetInt32(reader.GetOrdinal("stock")),
                                Sku = reader["sku"]?.ToString(),
                                Image = reader["image"]?.ToString(),
                                ImageGallery = reader["imagegallery"] as string[],
                                IsActive = reader.GetBoolean(reader.GetOrdinal("isactive")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),

                                SizeNames = reader["sizenames"] as string[],
                                ColorNames = reader["colornames"] as string[]
                            };

                            // ✅ MATCH PRODUCT & ADD VARIANT
                            var product = products.FirstOrDefault(p => p.Id == variant.ProductId);
                            if (product != null)
                            {
                                product.Variants.Add(variant);
                            }
                        }
                    }
                }
            }

            return products;
        }

      

public async Task<List<Productmodel>> FilterProducts(ProductFilterModel filter)
    {
        List<Productmodel> products = new List<Productmodel>();

        using (var connection = new NpgsqlConnection(this.DbConnection))
        {
            await connection.OpenAsync();

            var query = @"SELECT * FROM product WHERE isactive = true";

            // 🔥 CATEGORY (MULTIPLE)
            if (filter.CategoryIds != null && filter.CategoryIds.Length > 0)
                query += " AND categoryid = ANY(@categoryids)";

            if (filter.SubCategoryIds != null && filter.SubCategoryIds.Length > 0)
                query += " AND subcategoryid = ANY(@subcategoryids)";

            if (filter.ChildCategoryIds != null && filter.ChildCategoryIds.Length > 0)
                query += " AND childcategoryid = ANY(@childcategoryids)";

            // 🔥 BRAND
            if (filter.BrandId.HasValue)
                query += " AND brandid = @brandid";

            // 🔥 SIZE + COLOR (OR CONDITION)
            List<string> orConditions = new List<string>();

            if (filter.SizeIds != null && filter.SizeIds.Length > 0)
                orConditions.Add("sizeids && @sizeids");

            if (filter.ColorIds != null && filter.ColorIds.Length > 0)
                orConditions.Add("colorids && @colorids");

            if (orConditions.Count > 0)
                query += " AND (" + string.Join(" OR ", orConditions) + ")";

            // 🔥 PRICE FILTER (FINAL FIX)
            if (filter.MinPrice.HasValue)
                query += " AND COALESCE(discountprice, price) >= @minprice";

            if (filter.MaxPrice.HasValue)
                query += " AND COALESCE(discountprice, price) <= @maxprice";

            // 🔥 SEARCH
            if (!string.IsNullOrEmpty(filter.Search))
                query += " AND LOWER(productname) LIKE LOWER(@search)";

            // 🔥 SORT (FINAL PRICE)
            query += " ORDER BY COALESCE(discountprice, price) ASC";

            using (var cmd = new NpgsqlCommand(query, connection))
            {
                // ✅ PARAMETERS

                if (filter.CategoryIds != null && filter.CategoryIds.Length > 0)
                    cmd.Parameters.AddWithValue("categoryids", filter.CategoryIds);

                if (filter.SubCategoryIds != null && filter.SubCategoryIds.Length > 0)
                    cmd.Parameters.AddWithValue("subcategoryids", filter.SubCategoryIds);

                if (filter.ChildCategoryIds != null && filter.ChildCategoryIds.Length > 0)
                    cmd.Parameters.AddWithValue("childcategoryids", filter.ChildCategoryIds);

                if (filter.BrandId.HasValue)
                    cmd.Parameters.AddWithValue("brandid", filter.BrandId.Value);

                if (filter.SizeIds != null && filter.SizeIds.Length > 0)
                    cmd.Parameters.AddWithValue("sizeids", filter.SizeIds);

                if (filter.ColorIds != null && filter.ColorIds.Length > 0)
                    cmd.Parameters.AddWithValue("colorids", filter.ColorIds);

                // 🔥 PRICE PARAM FIX (NO ERROR)
                if (filter.MinPrice.HasValue)
                    cmd.Parameters.Add("minprice", NpgsqlDbType.Numeric).Value = filter.MinPrice.Value;

                if (filter.MaxPrice.HasValue)
                    cmd.Parameters.Add("maxprice", NpgsqlDbType.Numeric).Value = filter.MaxPrice.Value;

                if (!string.IsNullOrEmpty(filter.Search))
                    cmd.Parameters.AddWithValue("search", "%" + filter.Search + "%");

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        products.Add(new Productmodel
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            ProductName = reader["productname"]?.ToString(),
                            Slug = reader["slug"]?.ToString(),
                            Sku = reader["sku"]?.ToString(),
                            Price = Convert.ToDecimal(reader["price"]),
                            DiscountPrice = reader["discountprice"] as decimal?,
                            Stock = Convert.ToInt32(reader["stock"]),
                            CategoryId = Convert.ToInt32(reader["categoryid"]),
                            SubCategoryId = Convert.ToInt32(reader["subcategoryid"]),
                            ChildCategoryId = reader["childcategoryid"] as int?,
                            BrandId = reader["brandid"] as int?,
                            Image = reader["image"]?.ToString(),
                            ImageGallery = reader["imagegallery"] as string[],
                            IsActive = Convert.ToBoolean(reader["isactive"]),
                            CreatedAt = Convert.ToDateTime(reader["createdat"])
                        });
                    }
                }
            }
        }

        return products;
    }





    public async Task<IActionResult> AddProduct(Productmodel product)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                // 🔥 OPTIONAL: slug duplicate check
                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM product WHERE slug=@slug", connection);
                checkCmd.Parameters.AddWithValue("@slug", product.Slug);
                var count = (long)await checkCmd.ExecuteScalarAsync();

                if (count > 0)
                {
                    product.Slug = product.Slug + "-" + Guid.NewGuid().ToString().Substring(0, 5);
                }

                using (var command = new NpgsqlCommand(@"
            INSERT INTO product 
            (productname, slug, sku, shortdescription, description, price, discountprice,   stock, categoryid, subcategoryid, childcategoryid, brandid, sizeids, colorids, image, imagegallery, isactive, createdat) 
            VALUES 
            (@productname,@slug, @sku, @shortdescription, @description, @price, @discountprice, @stock, @categoryid, @subcategoryid, @childcategoryid, @brandid, @sizeids, @colorids, @image, @imagegallery, @isactive, @createdat)", connection))
                {
                    command.Parameters.AddWithValue("@productname", product.ProductName);
                    command.Parameters.AddWithValue("@slug", product.Slug); // ✅ FIXED
                    command.Parameters.AddWithValue("@sku", (object)product.Sku ?? DBNull.Value);
                    command.Parameters.AddWithValue("@shortdescription", (object)product.ShortDescription ?? DBNull.Value);
                    command.Parameters.AddWithValue("@description", (object)product.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@price", product.Price);
                    command.Parameters.AddWithValue("@discountprice", (object?)product.DiscountPrice ?? DBNull.Value);
                    command.Parameters.AddWithValue("@stock", product.Stock);
                    command.Parameters.AddWithValue("@categoryid", product.CategoryId);
                    command.Parameters.AddWithValue("@subcategoryid", product.SubCategoryId);
                    command.Parameters.AddWithValue("@childcategoryid", (object?)product.ChildCategoryId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@brandid", (object?)product.BrandId ?? DBNull.Value);

                    command.Parameters.Add(new NpgsqlParameter("@sizeids", NpgsqlDbType.Array | NpgsqlDbType.Integer)
                    {
                        Value = (object?)product.SizeIds ?? DBNull.Value
                    });

                    command.Parameters.Add(new NpgsqlParameter("@colorids", NpgsqlDbType.Array | NpgsqlDbType.Integer)
                    {
                        Value = (object?)product.ColorIds ?? DBNull.Value
                    });

                    command.Parameters.AddWithValue("@image", (object?)product.Image ?? DBNull.Value);

                    command.Parameters.Add(new NpgsqlParameter("@imagegallery", NpgsqlDbType.Array | NpgsqlDbType.Text)
                    {
                        Value = (object?)product.ImageGallery ?? DBNull.Value
                    });

                    command.Parameters.AddWithValue("@isactive", product.IsActive);
                    command.Parameters.AddWithValue("@createdat", DateTime.UtcNow);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return new OkObjectResult(new { status = true, message = "Product added successfully" });
                    else
                        return new BadRequestObjectResult(new { status = false, message = "Failed to add product" });
                }
            }
        }



        public async Task<IActionResult> UpdateProduct(int id, Productmodel product)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(@"
            UPDATE product SET 
            productname = @productname, 
slug = @slug,
sku = @sku,
shortdescription = @shortdescription,
            description = @description, 
            price = @price, 
            discountprice = @discountprice,
            stock = @stock, 
            categoryid = @categoryid,
            subcategoryid = @subcategoryid,
            childcategoryid = @childcategoryid,
            brandid = @brandid,
            sizeids = @sizeids,
            colorids = @colorids,
            image = @image,
            imagegallery = @imagegallery,
            isactive = @isactive
            WHERE id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@productname", product.ProductName);
                    command.Parameters.AddWithValue("@slug", product.Slug ?? Guid.NewGuid().ToString()); // Generate slug if not provided
                    command.Parameters.AddWithValue("@sku", (object?)product.Sku ?? DBNull.Value);
                    command.Parameters.AddWithValue("@shortdescription", (object?)product.ShortDescription ?? DBNull.Value);
                    command.Parameters.AddWithValue("@description", (object?)product.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@price", product.Price);
                    command.Parameters.AddWithValue("@discountprice", (object?)product.DiscountPrice ?? DBNull.Value);
                    command.Parameters.AddWithValue("@stock", product.Stock);
                    command.Parameters.AddWithValue("@categoryid", product.CategoryId);        // ✅ fix
                    command.Parameters.AddWithValue("@subcategoryid", product.SubCategoryId);
                    command.Parameters.AddWithValue("@childcategoryid", (object?)product.ChildCategoryId ?? DBNull.Value); // ✅ fix
                    command.Parameters.AddWithValue("@brandid", (object?)product.BrandId ?? DBNull.Value);

                    command.Parameters.Add(new NpgsqlParameter("@sizeids", NpgsqlDbType.Array | NpgsqlDbType.Integer)
                    {
                        Value = (object?)product.SizeIds ?? DBNull.Value
                    });

                    command.Parameters.Add(new NpgsqlParameter("@colorids", NpgsqlDbType.Array | NpgsqlDbType.Integer)
                    {
                        Value = (object?)product.ColorIds ?? DBNull.Value
                    });

                    command.Parameters.AddWithValue("@image", (object?)product.Image ?? DBNull.Value);

                    command.Parameters.Add(new NpgsqlParameter("@imagegallery", NpgsqlDbType.Array | NpgsqlDbType.Text)
                    {
                        Value = (object?)product.ImageGallery ?? DBNull.Value
                    });

                    command.Parameters.AddWithValue("@isactive", product.IsActive);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return new OkObjectResult(new { status = true, message = "Product updated successfully" });
                    else
                        return new NotFoundObjectResult(new { status = false, message = "Product not found" });
                }
            }
        }


        // Implementation
        public async Task<Productmodel?> GetProductById(int id)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand(@"
            SELECT 
                p.id, p.productname, p.slug, p.sku, p.shortdescription, p.description,
                p.price, p.discountprice, p.stock,
                p.categoryid, p.subcategoryid, p.childcategoryid,
                p.brandid, p.sizeids, p.colorids,
                p.image, p.imagegallery, p.isactive, p.createdat,

                c.""Name""                AS categoryname,
                sc.""SubCategoryName""    AS subcategoryname,
                cc.""ChildCategoryName""  AS childcategoryname,
                b.brandname              AS brandname,

                ARRAY(
                    SELECT s.size_name 
                    FROM sizes s 
                    WHERE s.id = ANY(p.sizeids)
                ) AS sizenames,

                ARRAY(
                    SELECT col.colorname 
                    FROM color col 
                    WHERE col.id = ANY(p.colorids)
                ) AS colornames

            FROM product p
            LEFT JOIN category c       ON p.categoryid = c.""Id""
            LEFT JOIN subcategory sc   ON p.subcategoryid = sc.""Id""
            LEFT JOIN childcategory cc ON p.childcategoryid = cc.""Id""
            LEFT JOIN brand b          ON p.brandid = b.id
            WHERE p.id = @id
        ", connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Productmodel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                ProductName = reader.GetFieldValue<string>(reader.GetOrdinal("productname")),
                                Slug = reader.IsDBNull(reader.GetOrdinal("slug")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("slug")),
                                Sku = reader.IsDBNull(reader.GetOrdinal("sku")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("sku")),
                                ShortDescription = reader.IsDBNull(reader.GetOrdinal("shortdescription")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("shortdescription")),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("description")),
                                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                                DiscountPrice = reader.IsDBNull(reader.GetOrdinal("discountprice")) ? null
                                        : reader.GetDecimal(reader.GetOrdinal("discountprice")),
                                Stock = reader.GetInt32(reader.GetOrdinal("stock")),

                                CategoryId = reader.GetInt32(reader.GetOrdinal("categoryid")),
                                SubCategoryId = reader.GetInt32(reader.GetOrdinal("subcategoryid")),
                                ChildCategoryId = reader.IsDBNull(reader.GetOrdinal("childcategoryid")) ? null
                                        : reader.GetInt32(reader.GetOrdinal("childcategoryid")),
                                BrandId = reader.IsDBNull(reader.GetOrdinal("brandid")) ? null
                                        : reader.GetInt32(reader.GetOrdinal("brandid")),

                                SizeIds = reader.IsDBNull(reader.GetOrdinal("sizeids")) ? null
                                        : reader.GetFieldValue<int[]>(reader.GetOrdinal("sizeids")),
                                ColorIds = reader.IsDBNull(reader.GetOrdinal("colorids")) ? null
                                        : reader.GetFieldValue<int[]>(reader.GetOrdinal("colorids")),

                                Image = reader.IsDBNull(reader.GetOrdinal("image")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("image")),
                                ImageGallery = reader.IsDBNull(reader.GetOrdinal("imagegallery")) ? null
                                        : reader.GetFieldValue<string[]>(reader.GetOrdinal("imagegallery")),

                                IsActive = reader.GetBoolean(reader.GetOrdinal("isactive")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),

                                CategoryName = reader.IsDBNull(reader.GetOrdinal("categoryname")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("categoryname")),
                                SubCategoryName = reader.IsDBNull(reader.GetOrdinal("subcategoryname")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("subcategoryname")),
                                ChildCategoryName = reader.IsDBNull(reader.GetOrdinal("childcategoryname")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("childcategoryname")),
                                BrandName = reader.IsDBNull(reader.GetOrdinal("brandname")) ? null
                                        : reader.GetFieldValue<string>(reader.GetOrdinal("brandname")),

                                SizeNames = reader.IsDBNull(reader.GetOrdinal("sizenames")) ? null
                                        : reader.GetFieldValue<string[]>(reader.GetOrdinal("sizenames")).ToList(),
                                ColorNames = reader.IsDBNull(reader.GetOrdinal("colornames")) ? null
                                        : reader.GetFieldValue<string[]>(reader.GetOrdinal("colornames")).ToList(),
                            };
                        }
                    }
                }
            }
            return null;
        }



        public async Task<IActionResult> DeleteProduct(int id)
        {
            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand("DELETE FROM product WHERE id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                        return new OkObjectResult(new { status = true, message = "Product deleted successfully" });
                    else
                        return new NotFoundObjectResult(new { status = false, message = "Product not found" });
                }
            }


        }
    }
}