using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task EnsureMicrositePublicSchema();
        Task<MicrositeResolvedData?> ResolveMicrositeByDomain(string domain);
        Task<List<MicrositeAssignedProduct>> GetMicrositeProducts(long micrositeId);
        Task<int> CreateMicrositeOtp(long micrositeId, string email, string otp, DateTime expiresAtUtc);
        Task<bool> VerifyMicrositeOtp(long micrositeId, string email, string otp);
        Task<MicrositePublicUser?> GetMicrositeUserByEmail(long micrositeId, string email);
        Task<MicrositePublicUser> UpsertMicrositeUser(long micrositeId, string email, string? name);
        Task<IActionResult> PlaceMicrositeSingleOrder(int userId, long micrositeId, MicrositeSingleOrderRequest request);
        Task<IActionResult> GetMicrositeOrders(int userId, long micrositeId);
        Task<IActionResult> GetMicrositeOrderDetail(int userId, long micrositeId, int orderId);
        Task<IActionResult> GetAdminMicrositeOrders(long micrositeId);
        Task<IActionResult> GetAdminMicrositeOrderDetail(long micrositeId, int orderId);
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task EnsureMicrositePublicSchema()
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();
            var sql = @"
CREATE TABLE IF NOT EXISTS microsite_users (
    id SERIAL PRIMARY KEY,
    microsite_id BIGINT NOT NULL REFERENCES microsites(id) ON DELETE CASCADE,
    name TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (microsite_id, email)
);

CREATE TABLE IF NOT EXISTS microsite_user_otp (
    id SERIAL PRIMARY KEY,
    microsite_id BIGINT NOT NULL REFERENCES microsites(id) ON DELETE CASCADE,
    email TEXT NOT NULL,
    otp TEXT NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS microsite_orders (
    id SERIAL PRIMARY KEY,
    microsite_id BIGINT NOT NULL REFERENCES microsites(id) ON DELETE CASCADE,
    microsite_user_id INT NOT NULL REFERENCES microsite_users(id) ON DELETE CASCADE,
    product_id INT NOT NULL REFERENCES product(id),
    quantity INT NOT NULL DEFAULT 1,
    first_name TEXT NOT NULL DEFAULT '',
    last_name TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    mobile TEXT NOT NULL DEFAULT '',
    address TEXT NOT NULL DEFAULT '',
    city TEXT NOT NULL DEFAULT '',
    state TEXT NOT NULL DEFAULT '',
    pincode TEXT NOT NULL DEFAULT '',
    country TEXT NOT NULL DEFAULT 'India',
    unit_price NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_price NUMERIC(18,2) NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'Placed',
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS assign_product (
    id SERIAL PRIMARY KEY,
    microsite_id BIGINT NOT NULL REFERENCES microsites(id) ON DELETE CASCADE,
    product_id INT NOT NULL REFERENCES product(id) ON DELETE CASCADE,
    status BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (microsite_id, product_id)
);
";
            using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<MicrositeResolvedData?> ResolveMicrositeByDomain(string domain)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();

            const string sql = @"
SELECT m.id, m.name, m.heading, m.content, m.email, m.mobile, m.logo_image, m.banner_image,
       d.domain,
       t.header_color, t.text_color, t.background_color, t.button_color, t.button_text_color, t.footer_color, t.footer_text_color, t.font_family,
       s.meta_title, s.meta_description, s.meta_keywords, s.og_image
FROM microsites m
JOIN microsite_domains d ON d.microsite_id = m.id
LEFT JOIN microsite_themes t ON t.microsite_id = m.id
LEFT JOIN microsite_seo s ON s.microsite_id = m.id
WHERE LOWER(d.domain) = LOWER(@domain) AND m.status = true
LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@domain", domain.Trim());
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new MicrositeResolvedData
            {
                MicrositeId = Convert.ToInt64(reader["id"]),
                Domain = reader["domain"]?.ToString() ?? domain,
                Name = reader["name"]?.ToString() ?? "",
                Heading = reader["heading"]?.ToString(),
                Content = reader["content"]?.ToString(),
                Email = reader["email"]?.ToString(),
                Mobile = reader["mobile"]?.ToString(),
                LogoImage = reader["logo_image"]?.ToString(),
                BannerImage = reader["banner_image"]?.ToString(),
                Theme = new
                {
                    headerColor = reader["header_color"]?.ToString(),
                    textColor = reader["text_color"]?.ToString(),
                    backgroundColor = reader["background_color"]?.ToString(),
                    buttonColor = reader["button_color"]?.ToString(),
                    buttonTextColor = reader["button_text_color"]?.ToString(),
                    footerColor = reader["footer_color"]?.ToString(),
                    footerTextColor = reader["footer_text_color"]?.ToString(),
                    fontFamily = reader["font_family"]?.ToString()
                },
                Seo = new
                {
                    metaTitle = reader["meta_title"]?.ToString(),
                    metaDescription = reader["meta_description"]?.ToString(),
                    metaKeywords = reader["meta_keywords"]?.ToString(),
                    ogImage = reader["og_image"]?.ToString()
                }
            };
        }

        public async Task<List<MicrositeAssignedProduct>> GetMicrositeProducts(long micrositeId)
        {
            var list = new List<MicrositeAssignedProduct>();
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();

            const string sql = @"
SELECT p.id, p.productname, p.slug, p.description, p.price, p.discountprice, p.stock, p.image, p.imagegallery
FROM assign_product ap
JOIN product p ON p.id = ap.product_id
WHERE ap.microsite_id = @mid AND ap.status = true AND p.isactive = true
ORDER BY ap.id DESC;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@mid", micrositeId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var images = ReadProductImageList(reader);

                list.Add(new MicrositeAssignedProduct
                {
                    ProductId = Convert.ToInt32(reader["id"]),
                    ProductName = reader["productname"]?.ToString() ?? "",
                    Slug = reader["slug"]?.ToString(),
                    Description = reader["description"]?.ToString(),
                    Price = Convert.ToDecimal(reader["price"]),
                    DiscountPrice = reader["discountprice"] == DBNull.Value ? null : Convert.ToDecimal(reader["discountprice"]),
                    Stock = Convert.ToInt32(reader["stock"]),
                    Images = images
                });
            }

            return list;
        }

        public async Task<int> CreateMicrositeOtp(long micrositeId, string email, string otp, DateTime expiresAtUtc)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();
            const string sql = @"INSERT INTO microsite_user_otp (microsite_id, email, otp, expires_at) VALUES (@mid, @email, @otp, @exp) RETURNING id;";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@mid", micrositeId);
            cmd.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@otp", otp);
            cmd.Parameters.AddWithValue("@exp", expiresAtUtc);
            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        public async Task<bool> VerifyMicrositeOtp(long micrositeId, string email, string otp)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            const string selectSql = @"
SELECT id FROM microsite_user_otp
WHERE microsite_id=@mid AND LOWER(email)=LOWER(@email) AND otp=@otp
  AND is_used=false AND expires_at >= NOW()
ORDER BY id DESC LIMIT 1;";
            using var selectCmd = new NpgsqlCommand(selectSql, connection, tx);
            selectCmd.Parameters.AddWithValue("@mid", micrositeId);
            selectCmd.Parameters.AddWithValue("@email", email.Trim());
            selectCmd.Parameters.AddWithValue("@otp", otp.Trim());
            var otpIdObj = await selectCmd.ExecuteScalarAsync();
            if (otpIdObj == null)
            {
                await tx.RollbackAsync();
                return false;
            }

            const string updateSql = "UPDATE microsite_user_otp SET is_used=true WHERE id=@id;";
            using var updateCmd = new NpgsqlCommand(updateSql, connection, tx);
            updateCmd.Parameters.AddWithValue("@id", Convert.ToInt32(otpIdObj));
            await updateCmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            return true;
        }

        public async Task<MicrositePublicUser?> GetMicrositeUserByEmail(long micrositeId, string email)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();
            const string sql = @"SELECT id, microsite_id, name, email, created_at FROM microsite_users WHERE microsite_id=@mid AND LOWER(email)=LOWER(@email) LIMIT 1;";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@mid", micrositeId);
            cmd.Parameters.AddWithValue("@email", email.Trim());
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;
            return new MicrositePublicUser
            {
                Id = Convert.ToInt32(reader["id"]),
                MicrositeId = Convert.ToInt64(reader["microsite_id"]),
                Name = reader["name"]?.ToString() ?? "",
                Email = reader["email"]?.ToString() ?? "",
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            };
        }

        public async Task<MicrositePublicUser> UpsertMicrositeUser(long micrositeId, string email, string? name)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();

            const string sql = @"
INSERT INTO microsite_users (microsite_id, name, email)
VALUES (@mid, @name, @email)
ON CONFLICT (microsite_id, email)
DO UPDATE SET name = CASE WHEN EXCLUDED.name = '' THEN microsite_users.name ELSE EXCLUDED.name END
RETURNING id, microsite_id, name, email, created_at;";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@mid", micrositeId);
            cmd.Parameters.AddWithValue("@name", name?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new MicrositePublicUser
            {
                Id = Convert.ToInt32(reader["id"]),
                MicrositeId = Convert.ToInt64(reader["microsite_id"]),
                Name = reader["name"]?.ToString() ?? "",
                Email = reader["email"]?.ToString() ?? "",
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            };
        }

        public async Task<IActionResult> PlaceMicrositeSingleOrder(int userId, long micrositeId, MicrositeSingleOrderRequest request)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();
            using var tx = await connection.BeginTransactionAsync();

            try
            {
                const string productSql = @"
SELECT p.id, p.productname, p.price, p.discountprice, p.stock
FROM assign_product ap
JOIN product p ON p.id = ap.product_id
WHERE ap.microsite_id = @mid AND ap.product_id = @pid AND ap.status = true AND p.isactive = true
LIMIT 1;";
                using var pCmd = new NpgsqlCommand(productSql, connection, tx);
                pCmd.Parameters.AddWithValue("@mid", micrositeId);
                pCmd.Parameters.AddWithValue("@pid", request.ProductId);
                using var pReader = await pCmd.ExecuteReaderAsync();
                if (!await pReader.ReadAsync())
                    return new NotFoundObjectResult(new { status = false, message = "Product microsite me available nahi hai." });

                var stock = Convert.ToInt32(pReader["stock"]);
                if (stock < request.Quantity)
                    return new BadRequestObjectResult(new { status = false, message = "Stock available nahi hai." });

                var price = pReader["discountprice"] == DBNull.Value
                    ? Convert.ToDecimal(pReader["price"])
                    : Convert.ToDecimal(pReader["discountprice"]);
                var productName = pReader["productname"]?.ToString() ?? "";
                await pReader.CloseAsync();

                var total = price * request.Quantity;
                const string insertSql = @"
INSERT INTO microsite_orders (microsite_id, microsite_user_id, product_id, quantity, first_name, last_name, email, mobile, address, city, state, pincode, country, unit_price, total_price)
VALUES (@mid, @uid, @pid, @qty, @fn, @ln, @email, @mobile, @address, @city, @state, @pincode, @country, @unit, @total)
RETURNING id, created_at;";
                using var insertCmd = new NpgsqlCommand(insertSql, connection, tx);
                insertCmd.Parameters.AddWithValue("@mid", micrositeId);
                insertCmd.Parameters.AddWithValue("@uid", userId);
                insertCmd.Parameters.AddWithValue("@pid", request.ProductId);
                insertCmd.Parameters.AddWithValue("@qty", request.Quantity);
                insertCmd.Parameters.AddWithValue("@fn", request.FirstName.Trim());
                insertCmd.Parameters.AddWithValue("@ln", request.LastName.Trim());
                insertCmd.Parameters.AddWithValue("@email", request.Email.Trim());
                insertCmd.Parameters.AddWithValue("@mobile", request.Mobile.Trim());
                insertCmd.Parameters.AddWithValue("@address", request.Address.Trim());
                insertCmd.Parameters.AddWithValue("@city", request.City.Trim());
                insertCmd.Parameters.AddWithValue("@state", request.State.Trim());
                insertCmd.Parameters.AddWithValue("@pincode", request.Pincode.Trim());
                insertCmd.Parameters.AddWithValue("@country", request.Country.Trim());
                insertCmd.Parameters.AddWithValue("@unit", price);
                insertCmd.Parameters.AddWithValue("@total", total);

                using var orderReader = await insertCmd.ExecuteReaderAsync();
                await orderReader.ReadAsync();
                var orderId = Convert.ToInt32(orderReader["id"]);
                var createdAt = Convert.ToDateTime(orderReader["created_at"]);
                await orderReader.CloseAsync();

                await tx.CommitAsync();
                return new OkObjectResult(new
                {
                    status = true,
                    message = "Order place ho gaya.",
                    data = new
                    {
                        orderId,
                        productName,
                        quantity = request.Quantity,
                        unitPrice = price,
                        totalPrice = total,
                        createdAt
                    }
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new ObjectResult(new { status = false, message = "Order place fail hua.", error = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetMicrositeOrders(int userId, long micrositeId)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();
            var list = new List<object>();
            const string sql = @"
SELECT mo.id, mo.product_id, p.productname, mo.quantity, mo.total_price, mo.status, mo.created_at
FROM microsite_orders mo
JOIN product p ON p.id = mo.product_id
WHERE mo.microsite_user_id = @uid AND mo.microsite_id = @mid
ORDER BY mo.id DESC;";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@mid", micrositeId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = Convert.ToInt32(reader["id"]),
                    productId = Convert.ToInt32(reader["product_id"]),
                    productName = reader["productname"]?.ToString(),
                    quantity = Convert.ToInt32(reader["quantity"]),
                    totalPrice = Convert.ToDecimal(reader["total_price"]),
                    status = reader["status"]?.ToString(),
                    createdAt = Convert.ToDateTime(reader["created_at"])
                });
            }

            return new OkObjectResult(new { status = true, totalOrders = list.Count, data = list });
        }

        public async Task<IActionResult> GetMicrositeOrderDetail(int userId, long micrositeId, int orderId)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();
            const string sql = @"
SELECT mo.id, mo.product_id, p.productname, p.slug, p.description,
       mo.quantity, mo.unit_price, mo.total_price, mo.status, mo.created_at,
       mo.first_name, mo.last_name, mo.email, mo.mobile, mo.address, mo.city, mo.state, mo.pincode, mo.country
FROM microsite_orders mo
JOIN product p ON p.id = mo.product_id
WHERE mo.id = @oid AND mo.microsite_user_id = @uid AND mo.microsite_id = @mid
LIMIT 1;";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@oid", orderId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@mid", micrositeId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return new NotFoundObjectResult(new { status = false, message = "Order detail nahi mila." });

            var data = new
            {
                id = Convert.ToInt32(reader["id"]),
                product = new
                {
                    productId = Convert.ToInt32(reader["product_id"]),
                    name = reader["productname"]?.ToString(),
                    slug = reader["slug"]?.ToString(),
                    description = reader["description"]?.ToString()
                },
                quantity = Convert.ToInt32(reader["quantity"]),
                unitPrice = Convert.ToDecimal(reader["unit_price"]),
                totalPrice = Convert.ToDecimal(reader["total_price"]),
                status = reader["status"]?.ToString(),
                createdAt = Convert.ToDateTime(reader["created_at"]),
                customer = new
                {
                    firstName = reader["first_name"]?.ToString(),
                    lastName = reader["last_name"]?.ToString(),
                    email = reader["email"]?.ToString(),
                    mobile = reader["mobile"]?.ToString(),
                    address = reader["address"]?.ToString(),
                    city = reader["city"]?.ToString(),
                    state = reader["state"]?.ToString(),
                    pincode = reader["pincode"]?.ToString(),
                    country = reader["country"]?.ToString()
                }
            };
            return new OkObjectResult(new { status = true, data });
        }

        public async Task<IActionResult> GetAdminMicrositeOrders(long micrositeId)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();

            var list = new List<object>();
            const string sql = @"
SELECT mo.id, mo.product_id, p.productname, mo.quantity, mo.unit_price, mo.total_price, mo.status, mo.created_at,
       mo.first_name, mo.last_name, mo.email, mo.mobile
FROM microsite_orders mo
JOIN product p ON p.id = mo.product_id
WHERE mo.microsite_id = @mid
ORDER BY mo.id DESC;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@mid", micrositeId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = Convert.ToInt32(reader["id"]),
                    productId = Convert.ToInt32(reader["product_id"]),
                    productName = reader["productname"]?.ToString(),
                    quantity = Convert.ToInt32(reader["quantity"]),
                    unitPrice = Convert.ToDecimal(reader["unit_price"]),
                    totalPrice = Convert.ToDecimal(reader["total_price"]),
                    status = reader["status"]?.ToString(),
                    createdAt = Convert.ToDateTime(reader["created_at"]),
                    customer = new
                    {
                        firstName = reader["first_name"]?.ToString(),
                        lastName = reader["last_name"]?.ToString(),
                        email = reader["email"]?.ToString(),
                        mobile = reader["mobile"]?.ToString()
                    }
                });
            }

            return new OkObjectResult(new { status = true, totalOrders = list.Count, data = list });
        }

        public async Task<IActionResult> GetAdminMicrositeOrderDetail(long micrositeId, int orderId)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();

            const string sql = @"
SELECT mo.id, mo.product_id, p.productname, p.slug, p.description,
       mo.quantity, mo.unit_price, mo.total_price, mo.status, mo.created_at,
       mo.first_name, mo.last_name, mo.email, mo.mobile, mo.address, mo.city, mo.state, mo.pincode, mo.country
FROM microsite_orders mo
JOIN product p ON p.id = mo.product_id
WHERE mo.id = @oid AND mo.microsite_id = @mid
LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@oid", orderId);
            cmd.Parameters.AddWithValue("@mid", micrositeId);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return new NotFoundObjectResult(new { status = false, message = "Order detail nahi mila." });

            var data = new
            {
                id = Convert.ToInt32(reader["id"]),
                product = new
                {
                    productId = Convert.ToInt32(reader["product_id"]),
                    name = reader["productname"]?.ToString(),
                    slug = reader["slug"]?.ToString(),
                    description = reader["description"]?.ToString()
                },
                quantity = Convert.ToInt32(reader["quantity"]),
                unitPrice = Convert.ToDecimal(reader["unit_price"]),
                totalPrice = Convert.ToDecimal(reader["total_price"]),
                status = reader["status"]?.ToString(),
                createdAt = Convert.ToDateTime(reader["created_at"]),
                customer = new
                {
                    firstName = reader["first_name"]?.ToString(),
                    lastName = reader["last_name"]?.ToString(),
                    email = reader["email"]?.ToString(),
                    mobile = reader["mobile"]?.ToString(),
                    address = reader["address"]?.ToString(),
                    city = reader["city"]?.ToString(),
                    state = reader["state"]?.ToString(),
                    pincode = reader["pincode"]?.ToString(),
                    country = reader["country"]?.ToString()
                }
            };

            return new OkObjectResult(new { status = true, data });
        }
    }
}
