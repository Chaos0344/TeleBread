using System.Collections.Generic;
using System.Data;
using System.Linq;
using TeleBreadService.Objects;
namespace TeleBreadService.General
{
    /// <summary>
    /// Setup contains functions used to prepare the service to run
    /// </summary>
    public class Setup
    {
        private Dictionary<string, string> Config { get; set; }

        /// <summary>
        /// Queries the database for pending Orb Predictions.
        /// </summary>
        /// <returns>List of OrbPredictions from the database.</returns>
        private List<OrbPredictions> GetPredictions()
        {
            var dt = new CommonFunctions(Config).RunQuery(
                "SELECT userID, groupChat, predictionText FROM dbo.Predictions",
                new [] {"userID", "groupChat", "predictionText"});

            return (from DataRow row 
                in dt.Rows 
                select new OrbPredictions(row["predictionText"].ToString(),
                long.Parse(row["userID"].ToString()),
                long.Parse(row["groupChat"].ToString()))).ToList();
        }
        
        /// <summary>
        /// Import the config Dictionary for use in further setup.
        /// </summary>
        /// <param name="c">The config Dictionary</param>
        public Setup(Dictionary<string, string> c)
        {
            Config = c;
        }
    }
}
