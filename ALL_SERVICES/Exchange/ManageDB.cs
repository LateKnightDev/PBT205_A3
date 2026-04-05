/*
 * DB actions for Luke:
 * - Insert new orders - Done but not tested
 * - Insert new trades - Done but not tested
 * - Delete orders when they are traded - Done but not tested
 * - Query DB for matching orders and execute trade or add to correct order table - Done but not tested
 * 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Exchange
{
    public static class ManageDB
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

        public static void InsertOrder(Order newOrder)
        {
            using (SqliteConnection connection = new SqliteConnection(dbLocation))
            {
                connection.Open();
                string tableName = newOrder.Side == "BUY" ? "BuyOrders" : "SellOrders"; // Use side BUY||SELL to determine the correct table
                string insertData = "INSERT INTO " + tableName + " (Username, Qty, Price, Code) VALUES (@username, @quantity, @price, @code)";
                using (SqliteCommand command = new SqliteCommand(insertData, connection))
                {
                    command.Parameters.AddWithValue("@username", newOrder.Username);
                    command.Parameters.AddWithValue("@quantity", newOrder.Quantity);
                    command.Parameters.AddWithValue("@price", newOrder.Price);
                    command.Parameters.AddWithValue("@code", newOrder.Code);
                    command.ExecuteNonQuery();
                }
            }
        }
        public static void DeleteOrder(string side, int orderId)
        {
            using (SqliteConnection connection = new SqliteConnection(dbLocation))
            {
                connection.Open();
                string tableName = side == "BUY" ? "BuyOrders" : "SellOrders"; // Use side BUY||SELL to determine the correct table
                string deleteData = "DELETE FROM " + tableName + " WHERE Id = @orderId";
                using (SqliteCommand command = new SqliteCommand(deleteData, connection))
                {
                    command.Parameters.AddWithValue("@orderId", orderId);
                    command.ExecuteNonQuery();
                }
            }
        }
        public static Trade InsertTrade(string buyer, string seller, int quantity, double price, string code)
        {
            using (SqliteConnection connection = new SqliteConnection(dbLocation))
            {
                connection.Open();
                string insertData = "INSERT INTO Trades (Buyer, Seller, Qty, Price, Code) VALUES (@buyer, @seller, @quantity, @price, @code)";
                using (SqliteCommand command = new SqliteCommand(insertData, connection))
                {
                    command.Parameters.AddWithValue("@buyer", buyer);
                    command.Parameters.AddWithValue("@seller", seller);
                    command.Parameters.AddWithValue("@quantity", quantity);
                    command.Parameters.AddWithValue("@price", price);
                    command.Parameters.AddWithValue("@code", code);
                    command.ExecuteNonQuery();
                }
            }
            Trade newTrade = new Trade
            {
                Buyer = buyer,
                Seller = seller,
                Quantity = quantity,
                Price = price,
                Code = code,
                Timestamp = DateTime.Now
            };
            return newTrade;
        }
        public static void QueryOrders(Order newOrder, out bool successfulTrade, out Trade? tradeDetails)
        {
            using (SqliteConnection connection = new SqliteConnection(dbLocation))
            {
                connection.Open();
                string tableName;
                string priceCondition;
                tradeDetails = null;

                if (newOrder.Side == "BUY")
                {
                    tableName = "SellOrders";
                    priceCondition = "Price <= @price";
                }
                else
                {
                    tableName = "BuyOrders";
                    priceCondition = "Price >= @price";
                }

                string orderDirection = newOrder.Side == "BUY" ? "ASC" : "DESC";
                string queryData = "SELECT * FROM " + tableName + " WHERE Code = @code AND " + priceCondition + " AND Quantity = @quantity ORDER BY Price " + orderDirection;

                using (SqliteCommand command = new SqliteCommand(queryData, connection))
                {
                    command.Parameters.AddWithValue("@price", newOrder.Price);
                    command.Parameters.AddWithValue("@code", newOrder.Code);
                    command.Parameters.AddWithValue("@quantity", newOrder.Quantity);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        int orderID = 0; // Requires declaration to avoid errors
                        string matchedUsername = ""; // Same with this one
                        int matchedQuantity;
                        double matchedPrice;
                        string matchedCode;
                        bool matchFound = false;
                        if (reader.Read()) // Does not run at all if the queryData string returns no matches
                        {
                            orderID = reader.GetInt32(0);
                            matchedUsername = reader.GetString(1);
                            matchedQuantity = reader.GetInt32(2);
                            matchedPrice = reader.GetDouble(3);
                            matchedCode = reader.GetString(4);
                            matchFound = true;
                        }
                        if (matchFound) // Process Trade
                        {
                            successfulTrade = true;
                            if (newOrder.Side == "BUY")
                            {
                                tradeDetails = InsertTrade(newOrder.Username, matchedUsername, newOrder.Quantity, newOrder.Price, newOrder.Code);
                                DeleteOrder("SELL", orderID);
                            }
                            else // side == "SELL"
                            {
                                tradeDetails = InsertTrade(newOrder.Username, newOrder.Username, newOrder.Quantity, newOrder.Price, newOrder.Code);
                                DeleteOrder("BUY", orderID);
                            }
                        }
                        else // No match found
                        {
                            successfulTrade = false;
                            InsertOrder(newOrder);
                        }
                    }
                }
            }
        }
    }
}
