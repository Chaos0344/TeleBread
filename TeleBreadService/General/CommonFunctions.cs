using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
namespace TeleBreadService.General
{
    public class CommonFunctions
    {
        /// <summary>
        /// Runs a query against the database referenced in the passed config dictionary
        /// </summary>
        /// <param name="query">String containing the query that is passed directly to the database.</param>
        /// <param name="columns">String array containing the columns to be returned (Selected).</param>
        /// <param name="config">The config dictionary with server info.</param>
        /// <returns>DataTable containing results of the query, to be parsed as needed.</returns>
        public DataTable runQuery(string query, string[] columns, Dictionary<string, string> config)
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection($"server={config["dbserver"]};" +
                                                   $"database=TeleBread;" +
                                                   $"uid={config["dbuser"]};" +
                                                   $"pwd={config["dbpass"]}");
            SqlCommand comm = new SqlCommand(query, conn);
            foreach (var c in columns)
            {
                dt.Columns.Add(c);
            }
            conn.Open();
            try
            {
                using (SqlDataReader reader = comm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = dt.NewRow();
                        for (int i = 0; i < columns.Length; i++)
                        {
                            row[columns[i]] = reader.GetValue(i);
                        }
                        dt.Rows.Add(row);
                    }
                }
            } catch (Exception z) {new Service1().WriteToFile(z.ToString());}
            conn.Close();
            return dt;
        }
        
        /// <summary>
        /// Writes records into the database.
        /// </summary>
        /// <param name="query">String containing the update/insert query.</param>
        /// <param name="config">The config dictionary with server info.</param>
        public void writeQuery(string query, Dictionary<string, string> config)
        {
            SqlConnection conn = new SqlConnection($"server={config["dbserver"]};" +
                                                   $"database=TeleBread;" +
                                                   $"uid={config["dbuser"]};" +
                                                   $"pwd={config["dbpass"]}");
            SqlCommand comm = new SqlCommand(query, conn);
            conn.Open();
            try { comm.ExecuteNonQuery(); }
            catch (Exception z)
            {
                new Service1().WriteToFile(z.ToString());
            }
            conn.Close();
        }
        
        /// <summary>
        /// Queries the private chat ID from the database for the specified user.
        /// If the user does not exist, this will return 0.
        /// </summary>
        /// <param name="userID">userID to query.</param>
        /// <param name="config">The config dictionary with server info.</param>
        /// <returns>A long representing the users privateChat ID.</returns>
        public long getPrivateChat(long userID, Dictionary<string, string> config)
        {
            try
            {
                DataTable dt = runQuery($"SELECT privateChat from chatUsers where userID = {userID}", new string[] { "privateChat" }, config);
                return long.Parse(dt.Rows[0]["privateChat"].ToString());
            } catch (Exception z)
            {
                return 0;
            }
        }

        /// <summary>
        /// Queries the group chat ID from the database for the specified user.
        /// If the user does not exist, this will return 0.
        /// </summary>
        /// <param name="userID">userID to query.</param>
        /// <param name="config">The config dictionary with server info.</param>
        /// <returns>A long representing the users groupChat ID.</returns>
        public long getGroupChat(long userID, Dictionary<string, string> config)
        {
            try
            {
                DataTable dt = runQuery($"SELECT groupChat from chatUsers where userID = {userID}", new string[] { "groupChat" }, config);
                if (dt.Rows.Count == 0)
                {
                    return 0;
                } else if (dt.Rows[0]["groupChat"] is null)
                {
                    return 0;
                }
                else
                {
                    return long.Parse(dt.Rows[0]["groupChat"].ToString());
                }
            } catch (Exception z)
            {
                return 0;
            }
        }

        /// <summary>
        /// Checks if the chat in chatID is userID's group chat.
        /// </summary>
        /// <param name="userID">User to check.</param>
        /// <param name="chatID">Chat to check.</param>
        /// <param name="config">The config dictionary with server info.</param>
        /// <returns>A boolean stating whether or not the chatID provided is the userID's group chat.</returns>
        public bool isGroupChat(long userID, long chatID, Dictionary<string, string> config)
        {
            long groupChat = getGroupChat(userID, config);
            return groupChat == chatID;
        }

        /// <summary>
        /// Queries the database for the status/value of a service.
        /// </summary>
        /// <param name="serviceName">The name of the service to check.</param>
        /// <param name="config">The config dictionary with server info.</param>
        /// <returns>An integer representing the current status/value of the checked service.</returns>
        public int serviceStatus(string serviceName, Dictionary<string, string> config)
        {
            DataTable dt = runQuery($"SELECT Status from dbo.Services where ServiceName = '{serviceName}'", new string[] { "Status" }, config);
            return Int32.Parse(dt.Rows[0]["Status"].ToString());
        }
        
        /// <summary>
        /// Adds qty of item to userID's inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="qty"></param>
        /// <param name="userID"></param>
        /// <param name="config"></param>
        /// <returns>A boolean stating whether or not the inventory update was successful.</returns>
        public bool addToInventory(string item, int qty, long userID, Dictionary<string, string> config)
        {
            try
            {
                int q = 0;

                // Get Item ID
                var Items = runQuery($"SELECT itemID FROM dbo.Items where itemName = '{item}'", new string[] { "itemID" }, config);
                var itemID = Items.Rows[0]["itemID"];

                // Get Current Inventory
                var inv = runQuery($"SELECT quantity FROM dbo.Inventory WHERE userID = {userID} and itemID = {itemID}", new string[] { "quantity" }, config);
                if (inv.Rows.Count != 0)
                {
                    // Inventory exists, get the number
                    q = Int32.Parse(inv.Rows[0]["quantity"].ToString());
                }
                else
                {
                    // Inventory doesn't exist. Add it.
                    writeQuery($"INSERT INTO dbo.Inventory (userID, itemID, quantity) VALUES ({userID}, {itemID}, {qty})", config);
                    return true;
                }

                // Update the inventory to the new quantity
                var add = q + qty;
                writeQuery($"UPDATE dbo.Inventory set quantity = {add} WHERE userID = {userID} and itemID = {itemID}", config);
                return true;
            }
            catch (Exception z)
            {
                // Bad things happened.
                new Service1().WriteToFile(z.ToString());
                return false;
            }
        }

        public CommonFunctions()
        {
            
        }
    }
}