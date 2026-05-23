using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<int> GetReport();
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        public async Task<int> GetReport()
        {
            int totalUsers = 0;

            using (var connection = new NpgsqlConnection(this.DbConnection))
            {
                await connection.OpenAsync();

                using (var command = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM users",
                    connection))
                {
                    totalUsers = Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }

            return totalUsers;
        }
    }
}
