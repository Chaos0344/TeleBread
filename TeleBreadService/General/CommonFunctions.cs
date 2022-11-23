using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Remoting.Messaging;

namespace TeleBreadService.General
{
    public class CommonFunctions
    {
        private Dictionary<string, string> Config { get; set; }
        
        public string queryWow()
        {
            SqlConnection conn = new SqlConnection("DSN=acore");
            SqlCommand comm = new SqlCommand("select a.username, b.name, b.level from acore_auth.account a join acore_characters.characters b on a.id = b.account WHERE b.online = 1");

            string names = "";

            conn.Open();
            try
            {
                using (SqlDataReader reader = comm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        names += ("\n"+reader.GetValue(1).ToString() + " - " + reader.GetValue(0) + " = " + reader.GetValue(2).ToString());
                        
                        //output[reader.GetValue(0).ToString()] = Int32.Parse(reader.GetValue(1).ToString());
                    }
                }
            }
            catch (Exception z)
            {
                new Service1().WriteToFile(z.ToString());
            }

            string outText = "";
            if (names.Length == 0)
            {
                outText = "There are no users online.";
            }
            else
            {
                outText = "Online Users:\n"+names;
            }

            return outText;
        }

        /// <summary>
        /// Runs a query against the database referenced in the passed config dictionary
        /// </summary>
        /// <param name="query">String containing the query that is passed directly to the database.</param>
        /// <param name="columns">String array containing the columns to be returned (Selected).</param>
        /// <returns>DataTable containing results of the query, to be parsed as needed.</returns>
        public DataTable RunQuery(string query, string[] columns)
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection($"server={Config["dbserver"]};" +
                                                   $"database=TeleBread;" +
                                                   $"uid={Config["dbuser"]};" +
                                                   $"pwd={Config["dbpassword"]}");
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
        public void WriteQuery(string query)
        {
            SqlConnection conn = new SqlConnection($"server={Config["dbserver"]};" +
                                                   $"database=TeleBread;" +
                                                   $"uid={Config["dbuser"]};" +
                                                   $"pwd={Config["dbpassword"]}");
            SqlCommand comm = new SqlCommand(query, conn);
            conn.Open();
            try { comm.ExecuteNonQuery(); }
            catch (Exception z)
            {
                new Service1().WriteToFile(z.ToString());
            }
            conn.Close();
        }
        
        public int WriteQueryWithId(string query)
        {
            int returnedID = 0;
            SqlConnection conn = new SqlConnection($"server={Config["dbserver"]};" +
                                                   $"database=TeleBread;" +
                                                   $"uid={Config["dbuser"]};" +
                                                   $"pwd={Config["dbpassword"]}");
            SqlCommand comm = new SqlCommand(query, conn);
            conn.Open();
            using (SqlDataReader reader = comm.ExecuteReader())
            {
                while (reader.Read())
                {
                    returnedID = int.Parse(reader.GetValue(0).ToString());
                }
            }
            
            conn.Close();
            return returnedID;
        }

        /// <summary>
        /// Queries the private chat ID from the database for the specified user.
        /// If the user does not exist, this will return 0.
        /// </summary>
        /// <param name="userId">userID to query.</param>
        /// <returns>A long representing the users privateChat ID.</returns>
        public long GetPrivateChat(long userId)
        {
            try
            {
                DataTable dt = RunQuery($"SELECT privateChat from Users where userID = {userId}", new[] { "privateChat" });
                return long.Parse(dt.Rows[0]["privateChat"].ToString());
            } catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Queries the group chat ID from the database for the specified user.
        /// If the user does not exist, this will return 0.
        /// </summary>
        /// <param name="userId">userID to query.</param>
        /// <returns>A long representing the users groupChat ID.</returns>
        public long GetGroupChat(long userId)
        {
            try
            {
                DataTable dt = RunQuery($"SELECT groupChat from dbo.Users where userID = {userId}", new[] { "groupChat" });
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
            } catch (Exception)
            {
                return 0;
            }
        }

        public long GetUserId(long groupChat, string username)
        {
            DataTable dt = RunQuery($"SELECT userID " +
                $"FROM dbo.Users " +
                $"WHERE groupChat = {groupChat} " +
                $"AND username = '{username}'", new [] { "userID" });
            if (dt.Rows.Count < 1)
            {
                return 0;
            }
            return long.Parse(dt.Rows[0]["userID"].ToString());
        }

        public string GetFirstName(long userId)
        {
            DataTable dt = RunQuery($"SELECT FirstName from dbo.Users where userID = {userId}", new[] {"FN"});
            return dt.Rows[0]["FN"].ToString();
        }

        /// <summary>
        /// Checks if the chat in chatID is userID's group chat.
        /// </summary>
        /// <param name="userId">User to check.</param>
        /// <param name="chatId">Chat to check.</param>
        /// <returns>A boolean stating whether or not the chatID provided is the userID's group chat.</returns>
        public bool IsGroupChat(long userId, long chatId)
        {
            long groupChat = GetGroupChat(userId);
            return groupChat == chatId;
        }

        /// <summary>
        /// Queries the database for the status/value of a service.
        /// </summary>
        /// <param name="serviceName">The name of the service to check.</param>
        /// <param name="chatId">Chat ID to check</param>
        /// <returns>An integer representing the current status/value of the checked service.</returns>
        public int ServiceStatus(string serviceName, long chatId)
        {
            DataTable dt = RunQuery($"SELECT Status " +
                                    $"FROM dbo.Services " +
                                    $"WHERE Service = '{serviceName}' " +
                                    $"AND groupChat = {chatId}", new[] { "Status" });
            return Int32.Parse(dt.Rows[0]["Status"].ToString());
        }

        public int CheckInventory(string item, long userId)
        {
            try
            {
                DataTable dt =
                    RunQuery($"SELECT Quantity " +
                        $"FROM dbo.Inventory " +
                        $"JOIN dbo.Items on Inventory.ItemID = Items.ItemID " +
                        $"WHERE Items.ItemName = '{item}' " +
                        $"AND Inventory.UserID = {userId}", new[] {"Quantity"});
                if (dt.Rows.Count < 1)
                {
                    return 0;
                }
                return Int32.Parse(dt.Rows[0]["Quantity"].ToString());
            } 
            catch (Exception e)
            {
                new Service1().WriteToFile(e.ToString());
                return 0;
            }
        }

        /// <summary>
        /// Adds qty of item to userID's inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="qty"></param>
        /// <param name="userId"></param>
        /// <returns>A boolean stating whether or not the inventory update was successful.</returns>
        public int AddToInventory(string item, int qty, long userId)
        {
            try
            {
                int q = 0;

                // Get Item ID
                var items = RunQuery($"SELECT itemID FROM dbo.Items where itemName = '{item}'", new[] { "itemID" });
                var itemId = items.Rows[0]["itemID"];

                // Get Current Inventory
                var inv = RunQuery($"SELECT quantity FROM dbo.Inventory WHERE userID = {userId} and itemID = {itemId}", new[] { "quantity" });
                if (inv.Rows.Count != 0)
                {
                    // Inventory exists, get the number
                    q = Int32.Parse(inv.Rows[0]["quantity"].ToString());
                }
                else
                {
                    // Inventory doesn't exist. Add it.
                    WriteQuery($"INSERT INTO dbo.Inventory (userID, itemID, quantity) VALUES ({userId}, {itemId}, {qty})");
                    return qty;
                }

                // Update the inventory to the new quantity
                var add = q + qty;
                WriteQuery($"UPDATE dbo.Inventory set quantity = {add} WHERE userID = {userId} and itemID = {itemId}");
                return add;
            }
            catch (Exception z)
            {
                // Bad things happened.
                new Service1().WriteToFile(z.ToString());
                return 0;
            }
        }

        /// <summary>
        /// Checks if the user is currently in the database
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool UserInDatabase(long userId)
        {
            DataTable dt = RunQuery($"SELECT userID from dbo.Users where userID = {userId}", new[] { "userID" });
            if (dt.Rows.Count > 0)
            {
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Returns true if user running command is currently in 'position'
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="userId">Specific User ID to check</param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool CheckPosition(long chatId, long userId, string position)
        {
            DataTable dt = RunQuery($"SELECT userID " +
                $"FROM dbo.Positions " +
                $"WHERE groupChat = {chatId} " +
                $"AND (expirationDate > '{DateTime.Now}' OR expirationDate is NULL) " +
                $"AND position = '{position}' " +
                $"AND userID = {userId}", new[] { "userId" });

            if (dt.Rows.Count < 1)
            {
                return false;
            } else
            {
                return true;
            }
        }

        public bool GroupChatExists(long chatId)
        {
            DataTable dt = RunQuery($"SELECT groupChat FROM dbo.GroupChats where groupChat = {chatId}", new[] { "groupChat" });
            if (dt.Rows.Count > 0)
            {
                return true;
            } else
            {
                return false;
            }
        }

        public int GetTimesheet(long userId, long chatId)
        {
            DataTable dt =
                RunQuery($"SELECT messages " +
                         $"FROM dbo.Timesheet " +
                         $"WHERE userID = {userId} " +
                         $"AND groupChat = {chatId}", new[] {"messages"});
            if (dt.Rows.Count < 1)
            {
                WriteQuery($"INSERT INTO dbo.Timesheet (userID, messages, groupChat) " +
                           $"VALUES ({userId}, 0, {chatId})");
                return 0;
            }

            return Int32.Parse(dt.Rows[0]["messages"].ToString());
        }

        public CommonFunctions(Dictionary<string, string> c)
        {
            Config = c;
        }
    }
}