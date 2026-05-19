using firstproject.Models;
using firstproject.S3Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        //================================================================Task MicroSite Task ============================================================== 
        Task<List<MicrositeModel>> GetMicrosite();
        Task<MicrositeModel> GetMicrositeById(long id);
        Task<MicrositeModel?> GetMicrositeByUniqueId(string micrositeId);
        Task<MicrositeModel> CreateMicrosite(MicrositeModel model);
        Task<string> UpdateMicrosite(long id, MicrositeModel model);
        Task<string> DeleteMicrosite(long id);

        ////================================================================Assign Products MicroSites Task Start ===============================================================
        Task<bool> AssignProduct(long micrositeId, long productId);
        Task<List<object>> GetAssignedProducts();
        Task<bool> UpdateAssignedProduct(long id, long micrositeId, long productId, bool status);
        Task<bool> DeleteAssignedProduct(long id);
        Task<bool> UpdateMicrositeOrderStatus(long micrositeId, int orderId, string status);
        Task<bool> DeleteMicrositeOrder(long micrositeId, int orderId);
    }

    public partial class DatabaseLayer
    {
        //private readonly object _config;
        //========================== MicroSite =================================== 
        private async Task EnsureAssignProductSchema()
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            const string sql = @"
CREATE TABLE IF NOT EXISTS assign_product (
    id SERIAL PRIMARY KEY,
    microsite_id BIGINT NOT NULL REFERENCES microsites(id) ON DELETE CASCADE,
    product_id INT NOT NULL REFERENCES product(id) ON DELETE CASCADE,
    status BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (microsite_id, product_id)
);";

            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<MicrositeModel>> GetMicrosite()
        {

            var list = new List<MicrositeModel>();

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string sql = @"SELECT 
                    m.id,
                    m.name,
                    m.slug,
                    m.heading,
                    m.content,
                    m.address,
                    m.email,
                    m.mobile,
                    m.logo_image,
                    m.banner_image,
                    m.favicon,
                    m.start_date,
                    m.end_date,
                    m.status,                
                    m.url,
                    m.unique_id,

                    d.domain,

                    t.header_color,
                    t.text_color,
                    t.background_color,
                    t.button_color,
                    t.button_text_color,
                    t.footer_color,
                    t.footer_text_color,
                    t.font_family,

                    s.meta_title,
                    s.meta_description,
                    s.meta_keywords,
                    s.og_image

                FROM microsites m
                LEFT JOIN microsite_domains d ON m.id = d.microsite_id
                LEFT JOIN microsite_themes t ON m.id = t.microsite_id
                LEFT JOIN microsite_seo s ON m.id = s.microsite_id
                ORDER BY m.id";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var micrositeDict = new Dictionary<long, MicrositeModel>();

            while (await reader.ReadAsync())
            {
                var id = reader.GetInt64(reader.GetOrdinal("id"));

                if (!micrositeDict.ContainsKey(id))
                {
                    var uniqueId = reader["unique_id"]?.ToString();
                    var dbUrl = reader["url"]?.ToString();

                    var finalUrl = BuildMicrositeRuntimeUrl(uniqueId, dbUrl);

                    micrositeDict[id] = new MicrositeModel
                    {
                        Id = id,
                        Name = reader["name"]?.ToString(),
                        Slug = reader["slug"]?.ToString(),
                        Heading = reader["heading"]?.ToString(),
                        Content = reader["content"]?.ToString(),
                        Address = reader["address"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        Mobile = reader["mobile"]?.ToString(),

                        Url = finalUrl,                 // ✅ always correct
                        UniqueId = uniqueId,

                        LogoImage = reader["logo_image"]?.ToString(),
                        BannerImage = reader["banner_image"]?.ToString(),
                        Favicon = reader["favicon"]?.ToString(),

                        StartDate = reader.IsDBNull(reader.GetOrdinal("start_date"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("start_date")),

                        EndDate = reader.IsDBNull(reader.GetOrdinal("end_date"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("end_date")),

                        Status = reader.GetBoolean(reader.GetOrdinal("status")),

                        Domains = new List<string>(),

                        Theme = new MicrositeTheme
                        {
                            HeaderColor = reader["header_color"]?.ToString(),
                            TextColor = reader["text_color"]?.ToString(),
                            BackgroundColor = reader["background_color"]?.ToString(),
                            ButtonColor = reader["button_color"]?.ToString(),
                            ButtonTextColor = reader["button_text_color"]?.ToString(),
                            FooterColor = reader["footer_color"]?.ToString(),
                            FooterTextColor = reader["footer_text_color"]?.ToString(),
                            FontFamily = reader["font_family"]?.ToString()
                        },

                        Seo = new MicrositeSeo
                        {
                            MetaTitle = reader["meta_title"]?.ToString(),
                            MetaDescription = reader["meta_description"]?.ToString(),
                            MetaKeywords = reader["meta_keywords"]?.ToString(),
                            OgImage = reader["og_image"]?.ToString()
                        }
                    };
                }

                // ✅ Handle multiple domains
                if (!reader.IsDBNull(reader.GetOrdinal("domain")))
                {
                    var domain = reader["domain"].ToString();

                    if (!micrositeDict[id].Domains.Contains(domain))
                    {
                        micrositeDict[id].Domains.Add(domain);
                    }
                }
            }

            list = micrositeDict.Values.ToList();

            return list;
        }

        public async Task<MicrositeModel> GetMicrositeById(long id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string sql = @"SELECT 
                m.id,
                m.name,
                m.slug,
                m.heading,
                m.content,
                m.address,
                m.email,
                m.mobile,
                m.logo_image,
                m.banner_image,
                m.favicon,
                m.url,
                m.unique_id,
                m.start_date,
                m.end_date,
                m.status,

                d.domain,

                t.header_color,
                t.text_color,
                t.background_color,
                t.button_color,
                t.button_text_color,
                t.footer_color,
                t.footer_text_color,
                t.font_family,

                s.meta_title,
                s.meta_description,
                s.meta_keywords,
                s.og_image

            FROM microsites m
            LEFT JOIN microsite_domains d ON m.id = d.microsite_id
            LEFT JOIN microsite_themes t ON m.id = t.microsite_id
            LEFT JOIN microsite_seo s ON m.id = s.microsite_id
            WHERE m.id = @id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            MicrositeModel microsite = null;

            while (await reader.ReadAsync())
            {
                if (microsite == null)
                {
                    var uniqueId = reader["unique_id"]?.ToString();
                    var dbUrl = reader["url"]?.ToString();

                    microsite = new MicrositeModel
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("id")),
                        Name = reader["name"]?.ToString(),
                        Slug = reader["slug"]?.ToString(),
                        Heading = reader["heading"]?.ToString(),
                        Content = reader["content"]?.ToString(),
                        Address = reader["address"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        Mobile = reader["mobile"]?.ToString(),
                        LogoImage = reader["logo_image"]?.ToString(),
                        BannerImage = reader["banner_image"]?.ToString(),
                        Favicon = reader["favicon"]?.ToString(),
                        UniqueId = uniqueId,
                        Url = BuildMicrositeRuntimeUrl(uniqueId, dbUrl),

                        StartDate = reader.IsDBNull(reader.GetOrdinal("start_date"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("start_date")),

                        EndDate = reader.IsDBNull(reader.GetOrdinal("end_date"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("end_date")),

                        Status = reader.GetBoolean(reader.GetOrdinal("status")),

                        Domains = new List<string>(),

                        Theme = new MicrositeTheme
                        {
                            HeaderColor = reader["header_color"]?.ToString(),
                            TextColor = reader["text_color"]?.ToString(),
                            BackgroundColor = reader["background_color"]?.ToString(),
                            ButtonColor = reader["button_color"]?.ToString(),
                            ButtonTextColor = reader["button_text_color"]?.ToString(),
                            FooterColor = reader["footer_color"]?.ToString(),
                            FooterTextColor = reader["footer_text_color"]?.ToString(),
                            FontFamily = reader["font_family"]?.ToString()
                        },

                        Seo = new MicrositeSeo
                        {
                            MetaTitle = reader["meta_title"]?.ToString(),
                            MetaDescription = reader["meta_description"]?.ToString(),
                            MetaKeywords = reader["meta_keywords"]?.ToString(),
                            OgImage = reader["og_image"]?.ToString()
                        }
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("domain")))
                {
                    microsite.Domains.Add(reader["domain"].ToString());
                }
            }

            return microsite;
        }

        public async Task<MicrositeModel?> GetMicrositeByUniqueId(string micrositeId)
        {
            if (string.IsNullOrWhiteSpace(micrositeId))
                return null;

            var normalizedId = micrositeId.Trim().Replace("-", "").ToLowerInvariant();

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            const string sql = @"SELECT 
                m.id,
                m.name,
                m.slug,
                m.heading,
                m.content,
                m.address,
                m.email,
                m.mobile,
                m.logo_image,
                m.banner_image,
                m.favicon,
                m.url,
                m.unique_id,
                m.start_date,
                m.end_date,
                m.status,
                d.domain,
                t.header_color,
                t.text_color,
                t.background_color,
                t.button_color,
                t.button_text_color,
                t.footer_color,
                t.footer_text_color,
                t.font_family,
                s.meta_title,
                s.meta_description,
                s.meta_keywords,
                s.og_image
            FROM microsites m
            LEFT JOIN microsite_domains d ON m.id = d.microsite_id
            LEFT JOIN microsite_themes t ON m.id = t.microsite_id
            LEFT JOIN microsite_seo s ON m.id = s.microsite_id
            WHERE LOWER(REPLACE(m.unique_id::text, '-', '')) = @uid AND m.status = true";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", normalizedId);

            using var reader = await cmd.ExecuteReaderAsync();

            MicrositeModel? microsite = null;

            while (await reader.ReadAsync())
            {
                if (microsite == null)
                {
                    var uniqueId = reader["unique_id"]?.ToString();
                    var dbUrl = reader["url"]?.ToString();

                    microsite = new MicrositeModel
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("id")),
                        Name = reader["name"]?.ToString(),
                        Slug = reader["slug"]?.ToString(),
                        Heading = reader["heading"]?.ToString(),
                        Content = reader["content"]?.ToString(),
                        Address = reader["address"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        Mobile = reader["mobile"]?.ToString(),
                        LogoImage = reader["logo_image"]?.ToString(),
                        BannerImage = reader["banner_image"]?.ToString(),
                        Favicon = reader["favicon"]?.ToString(),
                        UniqueId = uniqueId,
                        Url = BuildMicrositeRuntimeUrl(uniqueId, dbUrl),
                        StartDate = reader.IsDBNull(reader.GetOrdinal("start_date"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("start_date")),
                        EndDate = reader.IsDBNull(reader.GetOrdinal("end_date"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("end_date")),
                        Status = reader.GetBoolean(reader.GetOrdinal("status")),
                        Domains = new List<string>(),
                        Theme = new MicrositeTheme
                        {
                            HeaderColor = reader["header_color"]?.ToString(),
                            TextColor = reader["text_color"]?.ToString(),
                            BackgroundColor = reader["background_color"]?.ToString(),
                            ButtonColor = reader["button_color"]?.ToString(),
                            ButtonTextColor = reader["button_text_color"]?.ToString(),
                            FooterColor = reader["footer_color"]?.ToString(),
                            FooterTextColor = reader["footer_text_color"]?.ToString(),
                            FontFamily = reader["font_family"]?.ToString()
                        },
                        Seo = new MicrositeSeo
                        {
                            MetaTitle = reader["meta_title"]?.ToString(),
                            MetaDescription = reader["meta_description"]?.ToString(),
                            MetaKeywords = reader["meta_keywords"]?.ToString(),
                            OgImage = reader["og_image"]?.ToString()
                        }
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("domain")))
                {
                    var domainValue = reader["domain"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(domainValue))
                        microsite!.Domains.Add(domainValue);
                }
            }

            return microsite;
        }

        public async Task<MicrositeModel> CreateMicrosite(MicrositeModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Use deterministic UUID and generated microsite URL for this project format.
                var micrositeUniqueId = Guid.NewGuid();
                var uniqueIdText = micrositeUniqueId.ToString("N");
                model.UniqueId = uniqueIdText;
                model.Url = BuildMicrositeRuntimeUrl(uniqueIdText, null);

                // ================= IMAGE UPLOAD (S3) =================

                // LOGO
                if (model.LogoFile != null && model.LogoFile.Length > 0)
                {
                    model.LogoImage = await S3StorageHelper.UploadFileAsync(model.LogoFile, "microsites/logo");
                }

                // BANNER
                if (model.BannerFile != null && model.BannerFile.Length > 0)
                {
                    model.BannerImage = await S3StorageHelper.UploadFileAsync(model.BannerFile, "microsites/banner");
                }

                // FAVICON
                if (model.FaviconFile != null && model.FaviconFile.Length > 0)
                {
                    model.Favicon = await S3StorageHelper.UploadFileAsync(model.FaviconFile, "microsites/favicon");
                }
                // ================= INSERT MICROSITE =================

                string sql = @"INSERT INTO microsites
                (name,slug,heading,content,address,email,mobile,
                logo_image,banner_image,favicon,
                unique_id,start_date,end_date,status,url)
                VALUES
                (@name,@slug,@heading,@content,@address,@email,@mobile,
                @logo,@banner,@favicon,
                @unique_id,@start,@end,@status,@url)
                RETURNING id, unique_id::text, url";

                using var cmd = new NpgsqlCommand(sql, conn, transaction);

                cmd.Parameters.AddWithValue("@name", model.Name ?? "");
                cmd.Parameters.AddWithValue("@slug", model.Slug ?? "");
                cmd.Parameters.AddWithValue("@heading", model.Heading ?? "");
                cmd.Parameters.AddWithValue("@content", model.Content ?? "");
                cmd.Parameters.AddWithValue("@address", model.Address ?? "");
                cmd.Parameters.AddWithValue("@email", model.Email ?? "");
                cmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                cmd.Parameters.AddWithValue("@logo", model.LogoImage ?? "");
                cmd.Parameters.AddWithValue("@banner", model.BannerImage ?? "");
                cmd.Parameters.AddWithValue("@favicon", model.Favicon ?? "");
                cmd.Parameters.AddWithValue("@unique_id", micrositeUniqueId);
                cmd.Parameters.AddWithValue("@start", model.StartDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@end", model.EndDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", model.Status);
                cmd.Parameters.AddWithValue("@url", model.Url ?? "");

                long micrositeId;

                using (var insertReader = await cmd.ExecuteReaderAsync())
                {
                    await insertReader.ReadAsync();
                    micrositeId = insertReader.GetInt64(0);
                    model.UniqueId = insertReader.GetString(1).Replace("-", "");
                    model.Url = insertReader.GetString(2);
                }

                // ================= DOMAINS =================

                if (model.Domains != null && model.Domains.Count > 0)
                {
                    foreach (var domain in model.Domains)
                    {
                        string domainSql = @"INSERT INTO microsite_domains (microsite_id,domain)
                                    VALUES(@mid,@domain)";

                        using var domainCmd = new NpgsqlCommand(domainSql, conn, transaction);
                        domainCmd.Parameters.AddWithValue("@mid", micrositeId);
                        domainCmd.Parameters.AddWithValue("@domain", domain);

                        await domainCmd.ExecuteNonQueryAsync();
                    }
                }

                // ================= THEME =================

                if (model.Theme != null)
                {
                    string themeSql = @"INSERT INTO microsite_themes
            (microsite_id,header_color,text_color,background_color,
             button_color,button_text_color,footer_color,footer_text_color,font_family)
            VALUES
            (@mid,@header,@text,@bg,@btn,@btnText,@footer,@footerText,@font)";

                    using var themeCmd = new NpgsqlCommand(themeSql, conn, transaction);

                    themeCmd.Parameters.AddWithValue("@mid", micrositeId);
                    themeCmd.Parameters.AddWithValue("@header", model.Theme.HeaderColor ?? "");
                    themeCmd.Parameters.AddWithValue("@text", model.Theme.TextColor ?? "");
                    themeCmd.Parameters.AddWithValue("@bg", model.Theme.BackgroundColor ?? "");
                    themeCmd.Parameters.AddWithValue("@btn", model.Theme.ButtonColor ?? "");
                    themeCmd.Parameters.AddWithValue("@btnText", model.Theme.ButtonTextColor ?? "");
                    themeCmd.Parameters.AddWithValue("@footer", model.Theme.FooterColor ?? "");
                    themeCmd.Parameters.AddWithValue("@footerText", model.Theme.FooterTextColor ?? "");
                    themeCmd.Parameters.AddWithValue("@font", model.Theme.FontFamily ?? "");

                    await themeCmd.ExecuteNonQueryAsync();
                }

                // ================= SEO =================

                if (model.Seo != null)
                {
                    string seoSql = @"INSERT INTO microsite_seo
            (microsite_id,meta_title,meta_description,meta_keywords,og_image)
            VALUES
            (@mid,@title,@desc,@keywords,@og)";

                    using var seoCmd = new NpgsqlCommand(seoSql, conn, transaction);

                    seoCmd.Parameters.AddWithValue("@mid", micrositeId);
                    seoCmd.Parameters.AddWithValue("@title", model.Seo.MetaTitle ?? "");
                    seoCmd.Parameters.AddWithValue("@desc", model.Seo.MetaDescription ?? "");
                    seoCmd.Parameters.AddWithValue("@keywords", model.Seo.MetaKeywords ?? "");
                    seoCmd.Parameters.AddWithValue("@og", model.Seo.OgImage ?? "");

                    await seoCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                model.Id = micrositeId;

                return new MicrositeModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    Slug = model.Slug,
                    UniqueId = model.UniqueId,
                    Url = model.Url,
                    Status = model.Status
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        //public async Task<string> UpdateMicrosite(long id, MicrositeModel model)
        //{
        //    using var conn = new NpgsqlConnection(DbConnection);
        //    await conn.OpenAsync();

        //    using var transaction = await conn.BeginTransactionAsync();

        //    try
        //    {
        //        // ================= IMAGE UPDATE =================
        //        string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/micro_banner_logo_image");

        //        if (!Directory.Exists(folder))
        //            Directory.CreateDirectory(folder);

        //        // LOGO
        //        if (model.LogoFile != null)
        //        {
        //            var fileName = Guid.NewGuid() + Path.GetExtension(model.LogoFile.FileName);
        //            var path = Path.Combine(folder, fileName);

        //            using var stream = new FileStream(path, FileMode.Create);
        //            await model.LogoFile.CopyToAsync(stream);

        //            model.LogoImage = "/micro_banner_logo_image/" + fileName;
        //        }

        //        // ================= UPDATE MAIN =================
        //        string sql = @"UPDATE microsites
        //SET name=@name,
        //    heading=@heading,
        //    content=@content,
        //    address=@address,
        //    email=@email,
        //    mobile=@mobile,
        //    logo_image = COALESCE(@logo, logo_image),
        //    banner_image = COALESCE(@banner, banner_image),
        //    favicon = COALESCE(@favicon, favicon),
        //    start_date=@start,
        //    end_date=@end,
        //    status=@status
        //WHERE id=@id";

        //        using var cmd = new NpgsqlCommand(sql, conn, transaction);

        //        cmd.Parameters.AddWithValue("@id", id);
        //        cmd.Parameters.AddWithValue("@name", model.Name);
        //        cmd.Parameters.AddWithValue("@heading", model.Heading ?? "");
        //        cmd.Parameters.AddWithValue("@content", model.Content ?? "");
        //        cmd.Parameters.AddWithValue("@address", model.Address ?? "");
        //        cmd.Parameters.AddWithValue("@email", model.Email ?? "");
        //        cmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
        //        cmd.Parameters.AddWithValue("@logo", (object?)model.LogoImage ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@banner", (object?)model.BannerImage ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@favicon", (object?)model.Favicon ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@start", model.StartDate ?? (object)DBNull.Value);
        //        cmd.Parameters.AddWithValue("@end", model.EndDate ?? (object)DBNull.Value);
        //        cmd.Parameters.AddWithValue("@status", model.Status);

        //        await cmd.ExecuteNonQueryAsync();

        //        // ================= DOMAINS =================
        //        string deleteDomainSql = @"DELETE FROM microsite_domains WHERE microsite_id=@id";
        //        using var delCmd = new NpgsqlCommand(deleteDomainSql, conn, transaction);
        //        delCmd.Parameters.AddWithValue("@id", id);
        //        await delCmd.ExecuteNonQueryAsync();

        //        if (model.Domains != null && model.Domains.Count > 0)
        //        {
        //            foreach (var domain in model.Domains)
        //            {
        //                string domainSql = @"INSERT INTO microsite_domains (microsite_id,domain)
        //                            VALUES(@mid,@domain)";

        //                using var domainCmd = new NpgsqlCommand(domainSql, conn, transaction);
        //                domainCmd.Parameters.AddWithValue("@mid", id);
        //                domainCmd.Parameters.AddWithValue("@domain", domain);

        //                await domainCmd.ExecuteNonQueryAsync();
        //            }
        //        }

        //        // ================= THEME UPDATE =================
        //        if (model.Theme != null)
        //        {
        //            string deleteTheme = "DELETE FROM microsite_themes WHERE microsite_id=@id";
        //            using var delThemeCmd = new NpgsqlCommand(deleteTheme, conn, transaction);
        //            delThemeCmd.Parameters.AddWithValue("@id", id);
        //            await delThemeCmd.ExecuteNonQueryAsync();

        //            string themeSql = @"INSERT INTO microsite_themes
        //    (microsite_id,header_color,text_color,background_color,
        //     button_color,button_text_color,footer_color,footer_text_color,font_family)
        //    VALUES
        //    (@mid,@header,@text,@bg,@btn,@btnText,@footer,@footerText,@font)";

        //            using var themeCmd = new NpgsqlCommand(themeSql, conn, transaction);

        //            themeCmd.Parameters.AddWithValue("@mid", id);
        //            themeCmd.Parameters.AddWithValue("@header", model.Theme.HeaderColor ?? "");
        //            themeCmd.Parameters.AddWithValue("@text", model.Theme.TextColor ?? "");
        //            themeCmd.Parameters.AddWithValue("@bg", model.Theme.BackgroundColor ?? "");
        //            themeCmd.Parameters.AddWithValue("@btn", model.Theme.ButtonColor ?? "");
        //            themeCmd.Parameters.AddWithValue("@btnText", model.Theme.ButtonTextColor ?? "");
        //            themeCmd.Parameters.AddWithValue("@footer", model.Theme.FooterColor ?? "");
        //            themeCmd.Parameters.AddWithValue("@footerText", model.Theme.FooterTextColor ?? "");
        //            themeCmd.Parameters.AddWithValue("@font", model.Theme.FontFamily ?? "");

        //            await themeCmd.ExecuteNonQueryAsync();
        //        }

        //        // ================= SEO UPDATE =================
        //        if (model.Seo != null)
        //        {
        //            string deleteSeo = "DELETE FROM microsite_seo WHERE microsite_id=@id";
        //            using var delSeoCmd = new NpgsqlCommand(deleteSeo, conn, transaction);
        //            delSeoCmd.Parameters.AddWithValue("@id", id);
        //            await delSeoCmd.ExecuteNonQueryAsync();

        //            string seoSql = @"INSERT INTO microsite_seo
        //    (microsite_id,meta_title,meta_description,meta_keywords,og_image)
        //    VALUES
        //    (@mid,@title,@desc,@keywords,@og)";

        //            using var seoCmd = new NpgsqlCommand(seoSql, conn, transaction);

        //            seoCmd.Parameters.AddWithValue("@mid", id);
        //            seoCmd.Parameters.AddWithValue("@title", model.Seo.MetaTitle ?? "");
        //            seoCmd.Parameters.AddWithValue("@desc", model.Seo.MetaDescription ?? "");
        //            seoCmd.Parameters.AddWithValue("@keywords", model.Seo.MetaKeywords ?? "");
        //            seoCmd.Parameters.AddWithValue("@og", model.Seo.OgImage ?? "");

        //            await seoCmd.ExecuteNonQueryAsync();
        //        }

        //        await transaction.CommitAsync();

        //        return "Microsite Updated Successfully";
        //    }
        //    catch
        //    {
        //        await transaction.RollbackAsync();
        //        throw;
        //    }
        //}

        public async Task<string> UpdateMicrosite(long id, MicrositeModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // ================= CHECK EXIST =================
                var checkCmd = new NpgsqlCommand("SELECT COUNT(1) FROM microsites WHERE id=@id", conn, transaction);
                checkCmd.Parameters.AddWithValue("@id", id);

                var exists = (long)await checkCmd.ExecuteScalarAsync();
                if (exists == 0)
                    throw new Exception("Microsite not found");

                // ================= GET OLD IMAGES =================
                string oldLogo = null, oldBanner = null, oldFavicon = null, oldOg = null;

                using (var cmdOld = new NpgsqlCommand(
                    "SELECT logo_image, banner_image, favicon FROM microsites WHERE id=@id",
                    conn, transaction))
                {
                    cmdOld.Parameters.AddWithValue("@id", id);

                    using var reader = await cmdOld.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        oldLogo = reader["logo_image"]?.ToString();
                        oldBanner = reader["banner_image"]?.ToString();
                        oldFavicon = reader["favicon"]?.ToString();
                    }
                }

                using (var seoOldCmd = new NpgsqlCommand(
                    "SELECT og_image FROM microsite_seo WHERE microsite_id=@id",
                    conn, transaction))
                {
                    seoOldCmd.Parameters.AddWithValue("@id", id);
                    var res = await seoOldCmd.ExecuteScalarAsync();
                    if (res != null) oldOg = res.ToString();
                }

                // ================= IMAGE UPLOAD (S3) =================
                model.LogoImage = await ReplaceMicrositeFileAsync(model.LogoFile, oldLogo, "microsites/logo");
                model.BannerImage = await ReplaceMicrositeFileAsync(model.BannerFile, oldBanner, "microsites/banner");
                model.Favicon = await ReplaceMicrositeFileAsync(model.FaviconFile, oldFavicon, "microsites/favicon");



                // ================= UPDATE MAIN =================
                string updateSql = @"UPDATE microsites
        SET name = COALESCE(@name, name),
            heading = COALESCE(@heading, heading),
            content = COALESCE(@content, content),
            address = COALESCE(@address, address),
            email = COALESCE(@email, email),
            mobile = COALESCE(@mobile, mobile),
            logo_image = @logo,
            banner_image = @banner,
            favicon = @favicon,
            start_date = @start,
            end_date = @end,
            status = @status
        WHERE id=@id";

                using (var updateCmd = new NpgsqlCommand(updateSql, conn, transaction))
                {
                    updateCmd.Parameters.AddWithValue("@id", id);
                    updateCmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(model.Name) ? DBNull.Value : model.Name);
                    updateCmd.Parameters.AddWithValue("@heading", string.IsNullOrWhiteSpace(model.Heading) ? DBNull.Value : model.Heading);
                    updateCmd.Parameters.AddWithValue("@content", string.IsNullOrWhiteSpace(model.Content) ? DBNull.Value : model.Content);
                    updateCmd.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(model.Address) ? DBNull.Value : model.Address);
                    updateCmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(model.Email) ? DBNull.Value : model.Email);
                    updateCmd.Parameters.AddWithValue("@mobile", string.IsNullOrWhiteSpace(model.Mobile) ? DBNull.Value : model.Mobile);
                    updateCmd.Parameters.AddWithValue("@logo", model.LogoImage ?? "");
                    updateCmd.Parameters.AddWithValue("@banner", model.BannerImage ?? "");
                    updateCmd.Parameters.AddWithValue("@favicon", model.Favicon ?? "");
                    updateCmd.Parameters.AddWithValue("@start", model.StartDate ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@end", model.EndDate ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@status", model.Status);

                    var rows = await updateCmd.ExecuteNonQueryAsync();
                    if (rows == 0)
                        throw new Exception("Update failed");
                }

                // ================= DOMAINS (omit if null so partial form updates do not wipe rows) =================
                if (model.Domains != null)
                {
                    await new NpgsqlCommand("DELETE FROM microsite_domains WHERE microsite_id=@id", conn, transaction)
                    {
                        Parameters = { new NpgsqlParameter("@id", id) }
                    }.ExecuteNonQueryAsync();

                    foreach (var domain in model.Domains)
                    {
                        using var domainCmd = new NpgsqlCommand(
                            "INSERT INTO microsite_domains (microsite_id,domain) VALUES(@mid,@domain)",
                            conn, transaction);

                        domainCmd.Parameters.AddWithValue("@mid", id);
                        domainCmd.Parameters.AddWithValue("@domain", domain);

                        await domainCmd.ExecuteNonQueryAsync();
                    }
                }

                // ================= THEME =================
                if (model.Theme != null)
                {
                    await new NpgsqlCommand("DELETE FROM microsite_themes WHERE microsite_id=@id", conn, transaction)
                    {
                        Parameters = { new NpgsqlParameter("@id", id) }
                    }.ExecuteNonQueryAsync();

                    using var themeCmd = new NpgsqlCommand(@"INSERT INTO microsite_themes
            (microsite_id,header_color,text_color,background_color,
             button_color,button_text_color,footer_color,footer_text_color,font_family)
            VALUES (@mid,@header,@text,@bg,@btn,@btnText,@footer,@footerText,@font)",
                    conn, transaction);

                    themeCmd.Parameters.AddWithValue("@mid", id);
                    themeCmd.Parameters.AddWithValue("@header", model.Theme.HeaderColor ?? "");
                    themeCmd.Parameters.AddWithValue("@text", model.Theme.TextColor ?? "");
                    themeCmd.Parameters.AddWithValue("@bg", model.Theme.BackgroundColor ?? "");
                    themeCmd.Parameters.AddWithValue("@btn", model.Theme.ButtonColor ?? "");
                    themeCmd.Parameters.AddWithValue("@btnText", model.Theme.ButtonTextColor ?? "");
                    themeCmd.Parameters.AddWithValue("@footer", model.Theme.FooterColor ?? "");
                    themeCmd.Parameters.AddWithValue("@footerText", model.Theme.FooterTextColor ?? "");
                    themeCmd.Parameters.AddWithValue("@font", model.Theme.FontFamily ?? "");

                    await themeCmd.ExecuteNonQueryAsync();
                }

                // ================= SEO =================
                if (model.Seo != null)
                {
                    var newOg = model.Seo.OgImage ?? "";
                    if (!string.IsNullOrWhiteSpace(oldOg) &&
                        !string.Equals(oldOg, newOg, StringComparison.Ordinal))
                        await S3StorageHelper.DeleteByPathAsync(oldOg);

                    await new NpgsqlCommand("DELETE FROM microsite_seo WHERE microsite_id=@id", conn, transaction)
                    {
                        Parameters = { new NpgsqlParameter("@id", id) }
                    }.ExecuteNonQueryAsync();

                    using var seoCmd = new NpgsqlCommand(@"INSERT INTO microsite_seo
            (microsite_id,meta_title,meta_description,meta_keywords,og_image)
            VALUES (@mid,@title,@desc,@keywords,@og)", conn, transaction);

                    seoCmd.Parameters.AddWithValue("@mid", id);
                    seoCmd.Parameters.AddWithValue("@title", model.Seo.MetaTitle ?? "");
                    seoCmd.Parameters.AddWithValue("@desc", model.Seo.MetaDescription ?? "");
                    seoCmd.Parameters.AddWithValue("@keywords", model.Seo.MetaKeywords ?? "");
                    seoCmd.Parameters.AddWithValue("@og", newOg);

                    await seoCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return "Microsite Updated Successfully";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>Uploads a new file to S3, removes the previous object when replaced, and keeps the old URL if upload fails.</summary>
        private static async Task<string?> ReplaceMicrositeFileAsync(
            IFormFile? file,
            string? previousUrl,
            string s3Folder)
        {
            if (file == null || file.Length == 0)
                return previousUrl;

            var uploaded = await S3StorageHelper.UploadFileAsync(file, s3Folder);
            if (string.IsNullOrWhiteSpace(uploaded))
                return previousUrl;

            var oldNorm = previousUrl?.Trim() ?? "";
            var newNorm = uploaded.Trim();
            if (!string.IsNullOrEmpty(oldNorm) &&
                !string.Equals(oldNorm, newNorm, StringComparison.OrdinalIgnoreCase))
                // Handles S3, MinIO, and legacy wwwroot paths
                await S3StorageHelper.DeleteStoredMediaAsync(oldNorm);

            return uploaded;
        }

        private string BuildMicrositeRuntimeUrl(string? uniqueId, string? dbUrl)
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
            {
                return dbUrl ?? string.Empty;
            }

            var micrositeBaseUrl = _configuration["MicrositePublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(micrositeBaseUrl))
            {
                var backendBase = _configuration["AppUrls:ProductionBackendBaseUrl"]?.TrimEnd('/');
                micrositeBaseUrl = string.IsNullOrWhiteSpace(backendBase)
                    ? "http://localhost/hypercompanyfrontend/mainsite/user_microsite/index.php"
                    : $"{backendBase}/user_microsite/index.html";
            }

            return $"{micrositeBaseUrl.TrimEnd('?')}?microsite_id={uniqueId.Replace("-", "")}";
        }

        public async Task<string> DeleteMicrosite(long id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string? logo = null, banner = null, favicon = null, og = null;

                using (var selectCmd = new NpgsqlCommand(
                    "SELECT logo_image, banner_image, favicon FROM microsites WHERE id=@id",
                    conn, transaction))
                {
                    selectCmd.Parameters.AddWithValue("@id", id);
                    using var reader = await selectCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        logo = reader["logo_image"]?.ToString();
                        banner = reader["banner_image"]?.ToString();
                        favicon = reader["favicon"]?.ToString();
                    }
                }

                using (var ogCmd = new NpgsqlCommand(
                    "SELECT og_image FROM microsite_seo WHERE microsite_id=@id LIMIT 1",
                    conn, transaction))
                {
                    ogCmd.Parameters.AddWithValue("@id", id);
                    var scalar = await ogCmd.ExecuteScalarAsync();
                    if (scalar != null && scalar != DBNull.Value)
                        og = scalar.ToString();
                }

                foreach (var url in new[] { logo, banner, favicon, og })
                {
                    if (!string.IsNullOrWhiteSpace(url))
                        await S3StorageHelper.DeleteByPathAsync(url);
                }

                await new NpgsqlCommand("DELETE FROM assign_product WHERE microsite_id=@id", conn, transaction)
                {
                    Parameters = { new NpgsqlParameter("@id", id) }
                }.ExecuteNonQueryAsync();

                await new NpgsqlCommand("DELETE FROM microsite_domains WHERE microsite_id=@id", conn, transaction)
                {
                    Parameters = { new NpgsqlParameter("@id", id) }
                }.ExecuteNonQueryAsync();

                await new NpgsqlCommand("DELETE FROM microsite_themes WHERE microsite_id=@id", conn, transaction)
                {
                    Parameters = { new NpgsqlParameter("@id", id) }
                }.ExecuteNonQueryAsync();

                await new NpgsqlCommand("DELETE FROM microsite_seo WHERE microsite_id=@id", conn, transaction)
                {
                    Parameters = { new NpgsqlParameter("@id", id) }
                }.ExecuteNonQueryAsync();

                using var delCmd = new NpgsqlCommand("DELETE FROM microsites WHERE id=@id", conn, transaction);
                delCmd.Parameters.AddWithValue("@id", id);
                var rows = await delCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                return rows > 0
                    ? "Microsite Deleted Successfully"
                    : "Microsite Not Found";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        ////================================================================Assign Products MicroSites Start ===============================================================
        public async Task<bool> AssignProduct(long micrositeId, long productId)
        {
            if (micrositeId <= 0 || productId <= 0)
                return false;

            await EnsureAssignProductSchema();
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            const string validateSql = @"
SELECT
    EXISTS(SELECT 1 FROM microsites WHERE id = @micrositeId) AS microsite_exists,
    EXISTS(SELECT 1 FROM product WHERE id = @productId) AS product_exists;";

            using (var validateCmd = new NpgsqlCommand(validateSql, conn))
            {
                validateCmd.Parameters.AddWithValue("@micrositeId", micrositeId);
                validateCmd.Parameters.AddWithValue("@productId", productId);
                using var reader = await validateCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return false;

                var micrositeExists = reader.GetBoolean(0);
                var productExists = reader.GetBoolean(1);
                if (!micrositeExists || !productExists)
                    return false;
            }

            var query = @"INSERT INTO assign_product (microsite_id, product_id)
                  VALUES (@micrositeId, @productId)
                  ON CONFLICT (microsite_id, product_id)
                  DO UPDATE SET status = TRUE";

            try
            {
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@micrositeId", micrositeId);
                cmd.Parameters.AddWithValue("@productId", productId);
                var result = await cmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (PostgresException)
            {
                return false;
            }
        }

        public async Task<List<object>> GetAssignedProducts()
        {
            await EnsureAssignProductSchema();
            var list = new List<object>();

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
SELECT 
    ap.id,
    ap.microsite_id,   -- ✅ FIXED HERE
    m.name AS microsite_name,
    m.unique_id,

    ap.status AS assign_status,
    ap.created_at AS assign_created_at,

    p.id AS product_id,
    p.productname AS product_name,
    p.slug,
    p.description,
    p.price,
    p.discountprice AS discount_price,
    p.stock,
    p.isactive AS product_status,
    p.createdat AS product_created_at,

    b.brandname AS brand_name,
    c.""Name"" AS category_name,

    p.image,
    p.imagegallery

FROM assign_product ap
LEFT JOIN microsites m ON ap.microsite_id = m.id
LEFT JOIN product p ON ap.product_id = p.id
LEFT JOIN brand b ON p.brandid = b.id
LEFT JOIN category c ON p.categoryid = c.""Id""

ORDER BY ap.id DESC
";

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                    MicrositeName = reader["microsite_name"]?.ToString(),
                    UniqueId = reader["unique_id"]?.ToString(),
                    MicrositeID = reader["microsite_id"] != DBNull.Value
    ? Convert.ToInt32(reader["microsite_id"])
    : 0,

                    AssignStatus = reader["assign_status"] != DBNull.Value
                        ? Convert.ToBoolean(reader["assign_status"])
                        : false,

                    AssignCreatedAt = reader["assign_created_at"] != DBNull.Value
                        ? Convert.ToDateTime(reader["assign_created_at"])
                        : DateTime.MinValue,

                    Product = new
                    {
                        ProductId = reader["product_id"] != DBNull.Value ? Convert.ToInt32(reader["product_id"]) : 0,
                        Name = reader["product_name"]?.ToString(),
                        Slug = reader["slug"]?.ToString(),
                        Description = reader["description"]?.ToString(),

                        Price = reader["price"] != DBNull.Value
        ? Convert.ToDecimal(reader["price"])
        : 0,

                        DiscountPrice = reader["discount_price"] != DBNull.Value
        ? Convert.ToDecimal(reader["discount_price"])
        : 0,

                        Stock = reader["stock"] != DBNull.Value
        ? Convert.ToInt32(reader["stock"])
        : 0,

                        Status = reader["product_status"] != DBNull.Value
        ? Convert.ToBoolean(reader["product_status"])
        : false,

                        CreatedAt = reader["product_created_at"] != DBNull.Value
        ? Convert.ToDateTime(reader["product_created_at"])
        : DateTime.MinValue,

                        BrandName = reader["brand_name"]?.ToString(),
                        CategoryName = reader["category_name"]?.ToString(),

                        // ✅ Image + gallery merged for frontend display
                        Images = new List<string>()
                            .Concat(
                                reader["image"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["image"]?.ToString())
                                    ? new List<string> { reader["image"]?.ToString() ?? "" }
                                    : new List<string>())
                            .Concat(
                                reader["imagegallery"] != DBNull.Value
                                    ? ((string[])reader["imagegallery"]).ToList()
                                    : new List<string>())
                            .ToList()
                    }
                });
            }

            return list;
        }

        public async Task<bool> DeleteAssignedProduct(long id)
        {
            await EnsureAssignProductSchema();
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var query = "DELETE FROM assign_product WHERE id=@id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteNonQueryAsync();

            return result > 0;
        }

        public async Task<bool> UpdateAssignedProduct(long id, long micrositeId, long productId, bool status)
        {
            await EnsureAssignProductSchema();
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            const string query = @"
UPDATE assign_product
SET microsite_id = @micrositeId,
    product_id = @productId,
    status = @status
WHERE id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@micrositeId", micrositeId);
            cmd.Parameters.AddWithValue("@productId", productId);
            cmd.Parameters.AddWithValue("@status", status);

            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<bool> UpdateMicrositeOrderStatus(long micrositeId, int orderId, string status)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            const string query = @"
UPDATE microsite_orders
SET status = @status
WHERE id = @orderId AND microsite_id = @micrositeId";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@orderId", orderId);
            cmd.Parameters.AddWithValue("@micrositeId", micrositeId);

            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<bool> DeleteMicrositeOrder(long micrositeId, int orderId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            const string query = "DELETE FROM microsite_orders WHERE id = @orderId AND microsite_id = @micrositeId";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@orderId", orderId);
            cmd.Parameters.AddWithValue("@micrositeId", micrositeId);

            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }
    }
}
