using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Exchange
{
    public static class QueryDB
    {
        // private static string dbLocation = "Data Source=Database/trading.db";

        private static string dbLocation = GetDbPath();

        private static string GetDbPath()
        {
            string basePath = Directory.GetCurrentDirectory();

            // Walk up until we find the Database folder
            while (!Directory.Exists(Path.Combine(basePath, "Database")))
            {
                basePath = Directory.GetParent(basePath).FullName;
            }

            string fullPath = Path.Combine(basePath, "Database", "trading.db");

            // Console.WriteLine($"DB Path: {fullPath}");

            return $"Data Source={fullPath}";
        }

        public static void Run()
        {
            Console.WriteLine("=== DATABASE VIEWER ===\n");
            PrintTable("BuyOrders");
            PrintTable("SellOrders");
            PrintTable("Trades");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        private static void PrintTable(string tableName)
        {
            Console.WriteLine($"\n--- {tableName.ToUpper()} ---");

            using (SqliteConnection connection = new SqliteConnection(dbLocation))
            {
                connection.Open();

                string query = "SELECT * FROM " + tableName;

                using (SqliteCommand command = new SqliteCommand(query, connection))
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.WriteLine("No data.");
                        return;
                    }

                    while (reader.Read())
                    {
                        PrintRow(tableName, reader);
                    }
                }
            }
        }
        private static void PrintRow(string tableName, SqliteDataReader reader)
        {
            if (tableName == "BuyOrders" || tableName == "SellOrders")
            {
                Console.WriteLine(
                    $"ID: {reader.GetInt32(0)} | " +
                    $"User: {reader.GetString(1)} | " +
                    $"Qty: {reader.GetInt32(2)} | " +
                    $"Price: {reader.GetDouble(3)} | " +
                    $"Code: {reader.GetString(4)}"
                );
            }
            else if (tableName == "Trades")
            {
                Console.WriteLine(
                    $"ID: {reader.GetInt32(0)} | " +
                    $"Buyer: {reader.GetString(1)} | " +
                    $"Seller: {reader.GetString(2)} | " +
                    $"Qty: {reader.GetInt32(3)} | " +
                    $"Price: {reader.GetDouble(4)} | " +
                    $"Code: {reader.GetString(5)}"
                );
            }
        }
    }
}
