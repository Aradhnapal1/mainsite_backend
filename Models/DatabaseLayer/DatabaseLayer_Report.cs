using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<object> GetReport();
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<object> GetReport()
        {
            try
            {
                int totalUsers = 0;
                int totalProducts = 0;
                int totalVariants = 0;
                int totalBlogs = 0;
                int totalBrands = 0;
                int totalCategories = 0;
                int totalSubCategories = 0;
                int totalColors = 0;
                int totalSizes = 0;
                int totalCustomerLogos = 0;
                int totalChildCategories = 0;


                using (var connection = new NpgsqlConnection(this.DbConnection))
                {
                    await connection.OpenAsync();

                    // ✅ Total Users
                    using (var userCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM users",
                        connection))
                    {
                        totalUsers = Convert.ToInt32(await userCommand.ExecuteScalarAsync());
                    }

                    // ✅ Total Products
                    using (var productCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM product",
                        connection))
                    {
                        totalProducts = Convert.ToInt32(await productCommand.ExecuteScalarAsync());
                    }

                    // ✅ Total Variants
                    using (var variantCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM variant",
                        connection))
                    {
                        totalVariants = Convert.ToInt32(await variantCommand.ExecuteScalarAsync());
                    }

                    // ✅ Total Blogs
                    using (var blogCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM blogs",
                        connection))
                    {
                        totalBlogs = Convert.ToInt32(await blogCommand.ExecuteScalarAsync());
                    }

                    // ✅ Total Brands
                    using (var brandCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM brand",
                        connection))
                    {
                        totalBrands = Convert.ToInt32(await brandCommand.ExecuteScalarAsync());
                    }

                    // ✅ Total Categories
                    using (var categoryCommand = new NpgsqlCommand(
                        @"SELECT COUNT(*) FROM ""category""",
                        connection))
                    {
                        totalCategories = Convert.ToInt32(await categoryCommand.ExecuteScalarAsync());
                    }

                    // ✅ Total SubCategories
                    using (var subCategoryCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM subcategory",
                        connection))
                    {
                        totalSubCategories = Convert.ToInt32(await subCategoryCommand.ExecuteScalarAsync());
                    }

                    // ✅ Total Colors
                    using (var colorCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM color",
                        connection))
                    {
                        totalColors = Convert.ToInt32(await colorCommand.ExecuteScalarAsync());
                    }
                    // ✅ Total Sizes
                    using (var sizeCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM sizes",
                        connection))
                    {
                        totalSizes = Convert.ToInt32(await sizeCommand.ExecuteScalarAsync());
                    }
                    // ✅ Total Customer Logos
                    using (var customerLogoCommand = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM customerlogo",
                        connection))
                    {
                        totalCustomerLogos = Convert.ToInt32(await customerLogoCommand.ExecuteScalarAsync());
                    }
                    // ✅ Total Child Categories
                    using (var childCategoryCommand = new NpgsqlCommand(
                        @"SELECT COUNT(*) FROM childCategory",
                        connection))
                    {
                        totalChildCategories = Convert.ToInt32(
                            await childCategoryCommand.ExecuteScalarAsync()
                        );
                    }


                }

                return new
                {
                    totalUsers = totalUsers,
                    totalProducts = totalProducts,
                    totalVariants = totalVariants,
                    totalBlogs = totalBlogs,
                    totalBrands = totalBrands,
                    totalCategories = totalCategories,
                    totalSubCategories = totalSubCategories,
                    totalColors = totalColors,
                    totalSizes = totalSizes,
                    totalCustomerLogos = totalCustomerLogos,
                    totalChildCategories = totalChildCategories

                };
            }
            catch (Exception ex)
            {
                return new
                {
                    message = ex.Message
                };
            }
        }
    }
}
