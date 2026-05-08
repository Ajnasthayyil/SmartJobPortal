using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SmartJobPortal.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _config;

    public DbConnectionFactory(IConfiguration config)
    {
        _config = config;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_config.GetConnectionString("Default"));
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            return connection;
        }
        catch (Exception ex)
        {
            connection.Dispose();
            Console.WriteLine($"[Critical] Database Connection Failed: {ex.Message}");
            throw;
        }
    }
}