using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DbTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=.;Database=SmartJobPortal;Trusted_Connection=True;TrustServerCertificate=True;";
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    Console.WriteLine("Attempting to connect to the database...");
                    connection.Open();
                    Console.WriteLine("Connection successful!");

                    using (var command = new SqlCommand("SELECT COUNT(*) FROM Users", connection))
                    {
                        int userCount = (int)command.ExecuteScalar();
                        Console.WriteLine($"Number of users in the database: {userCount}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection failed!");
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
            }
        }
    }
}
