/*
 * DB actions for Luke:
 * - Insert new orders - Done
 * - Insert new trades - Done
 * - Delete orders when they are traded - Done
 * - Query DB for matching orders and execute trade or add to correct order table - Done
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

            // Finding the full directory path to the Database folder
            while (!Directory.Exists(Path.Combine(basePath, "Database")))
            {
                basePath = Directory.GetParent(basePath).FullName;
            }

            string fullPath = Path.Combine(basePath, "Database", "trading.db");

            // Console.WriteLine($"DB Path: {fullPath}");

            return $"Data Source={fullPath}";
        }

        public static void InsertOrder(Order newOrder) // Inserts new order to database where no trading match is found
        {
            Console.WriteLine("Inserting order to DB"); // Debugging

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
        public static void DeleteOrder(string side, int orderId) // Deletes order from the unmatched orders table when a successful trade is executed
        {
            Console.WriteLine("Deleting order from DB"); // Debugging
            
            using (SqliteConnection connection = new SqliteConnection(dbLocation))
            {
                connection.Open();
                string tableName = side == "BUY" ? "BuyOrders" : "SellOrders"; // Use side BUY||SELL to determine the correct table
                string deleteData = "DELETE FROM " + tableName + " WHERE ID = @orderId";
                using (SqliteCommand command = new SqliteCommand(deleteData, connection))
                {
                    command.Parameters.AddWithValue("@orderId", orderId);
                    command.ExecuteNonQuery();
                }
            }
        }
        public static Trade InsertTrade(string buyer, string seller, int quantity, double price, string code) // Inserts trade into the database
        {
            Console.WriteLine("Inserting trade into DB"); // Debugging
            
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
                    // command.ExecuteNonQuery();

                    try
                    {
                        command.ExecuteNonQuery(); // Ddebugging
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR inserting trade:");
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            Console.WriteLine("Trade halfway inserted into DB"); // Debugging

            Trade newTrade = new Trade
            {
                Buyer = buyer,
                Seller = seller,
                Quantity = quantity,
                Price = price,
                Code = code,
                Timestamp = DateTime.Now
            };

            Console.WriteLine("Insert into DB for trade complete"); // Debugging

            return newTrade;
        }
        public static void QueryOrders(Order newOrder, out bool successfulTrade, out Trade? tradeDetails) // Does all the heavy lifting
        {
            Console.WriteLine("QueryOrders called"); // Debugging
            Console.WriteLine($"Incoming: {newOrder.Username} {newOrder.Side} {newOrder.Quantity} {newOrder.Price} {newOrder.Code}"); // Debugging

            int orderID = 0; // Requires declaration to avoid errors
            string matchedUsername = ""; // Same with this one
            int matchedQuantity;
            double matchedPrice;
            string matchedCode;
            bool matchFound = false;

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
                string queryData = "SELECT * FROM " + tableName + " WHERE Code = @code AND " + priceCondition + " AND Qty = @quantity ORDER BY Price " + orderDirection;

                using (SqliteCommand command = new SqliteCommand(queryData, connection))
                {
                    command.Parameters.AddWithValue("@price", newOrder.Price);
                    command.Parameters.AddWithValue("@code", newOrder.Code);
                    command.Parameters.AddWithValue("@quantity", newOrder.Quantity);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // Does not run at all if the queryData string returns no matches
                        {
                            orderID = reader.GetInt32(0);
                            matchedUsername = reader.GetString(1);
                            matchedQuantity = reader.GetInt32(2);
                            matchedPrice = reader.GetDouble(3);
                            matchedCode = reader.GetString(4);
                            matchFound = true;
                        }
                    }
                }
            }
            if (matchFound) // Process Trade
            {
                Console.WriteLine("We found a trade!!!"); // Debugging

                successfulTrade = true;
                if (newOrder.Side == "BUY")
                {
                    tradeDetails = InsertTrade(newOrder.Username, matchedUsername, newOrder.Quantity, newOrder.Price, newOrder.Code);
                    DeleteOrder("SELL", orderID);
                    Console.WriteLine("Trade executed and deleted from SELL orders"); // Debugging
                }
                else // side == "SELL"
                {
                    tradeDetails = InsertTrade(matchedUsername, newOrder.Username, newOrder.Quantity, newOrder.Price, newOrder.Code);
                    DeleteOrder("BUY", orderID);
                    Console.WriteLine("Trade executed and deleted from BUY orders"); // Debugging
                }
            }
            else // No match found
            {
                Console.WriteLine("We didn't find a trade!!!"); // Debugging

                successfulTrade = false;
                InsertOrder(newOrder);
            }
            Console.WriteLine("successfulTrade: " + successfulTrade); // Debugging
        }
    }
}
