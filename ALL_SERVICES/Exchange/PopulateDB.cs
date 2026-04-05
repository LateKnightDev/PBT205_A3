// Run once to create and populate the database with sample data

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Exchange
{
    public static class PopulateDB
    {
        public static void Run()
        {
            Console.WriteLine($"Working Dir: {Directory.GetCurrentDirectory()}");
            CreateDatabase();
            InitialInsert("Luke", "BUY", 100, 10.5, "ABCD");
            InitialInsert("Luke", "BUY", 100, 10.5, "DEFG");
            InitialInsert("Luke", "SELL", 100, 10.5, "HIJK");
            InitialInsert("Luke", "SELL", 100, 10.5, "LMNO");
            InitialInsert("Evan", "BUY", 50, 10.0, "WXYZ");
            InitialInsert("Evan", "BUY", 50, 10.0, "WWXX");
            InitialInsert("Evan", "SELL", 50, 10.0, "YYZZ");
            InitialInsert("Evan", "SELL", 50, 10.0, "ZZZZ");
            InitialInsert("Mathew", "BUY", 200, 20.0, "AABB");
            InitialInsert("Mathew", "BUY", 200, 20.0, "CCDD");
            InitialInsert("Mathew", "SELL", 200, 20.0, "EEFF");
            InitialInsert("Mathew", "SELL", 200, 20.0, "GGGG");
            Console.WriteLine("Database created and populated with sample data.");
            Console.ReadLine();
        }
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


        public static void CreateDatabase()
        {
            string dbFilePath = dbLocation.Replace("Data Source=", "");
            var directory = Path.GetDirectoryName(dbFilePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }


            // Creates three tables using the same variables as A1
            using (SqliteConnection connection = new SqliteConnection(dbLocation))
            {
                connection.Open();
                string createBuyOrdersTable = "CREATE TABLE IF NOT EXISTS BuyOrders (ID INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT, Qty INTEGER, Price REAL, Code TEXT)";
                string createSellOrdersTable = "CREATE TABLE IF NOT EXISTS SellOrders (ID INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT, Qty INTEGER, Price REAL, Code TEXT)";
                string createTradesTable = "CREATE TABLE IF NOT EXISTS Trades (ID INTEGER PRIMARY KEY AUTOINCREMENT, Buyer TEXT, Seller TEXT, Qty INTEGER, Price REAL, Code TEXT, Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP)";
                SqliteCommand sql_createBuyOrdersTable = new SqliteCommand(createBuyOrdersTable, connection);
                SqliteCommand sql_createSellOrdersTable = new SqliteCommand(createSellOrdersTable, connection);
                SqliteCommand sql_createTradesTable = new SqliteCommand(createTradesTable, connection);
                sql_createBuyOrdersTable.ExecuteNonQuery();
                sql_createSellOrdersTable.ExecuteNonQuery();
                sql_createTradesTable.ExecuteNonQuery();
            }
        }
        public static void InitialInsert(string username, string side, int quantity, double price, string code)
        {
            using (SqliteConnection connection = new SqliteConnection(dbLocation))
            {
                connection.Open();
                string tableName = side == "BUY" ? "BuyOrders" : "SellOrders"; // Use side BUY||SELL to determine the correct table
                string insertData = "INSERT INTO " + tableName + " (Username, Qty, Price, Code) VALUES (@username, @quantity, @price, @code)";
                using (SqliteCommand command = new SqliteCommand(insertData, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@quantity", quantity);
                    command.Parameters.AddWithValue("@price", price);
                    command.Parameters.AddWithValue("@code", code);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
