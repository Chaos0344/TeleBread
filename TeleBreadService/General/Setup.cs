using System;
using System.Collections.Generic;
using System.Data;
using TeleBreadService.General;
using TeleBreadService.Objects;
namespace TeleBreadService.Common
{
    /// <summary>
    /// Setup contains functions used to prepare the service to run
    /// </summary>
    public class Setup
    {
        private Dictionary<string, string> config = new Dictionary<string, string>();

        /// <summary>
        /// Queries the database for pending Orb Predictions.
        /// </summary>
        /// <returns>List of OrbPredictions from the database.</returns>
        private List<OrbPredictions> getPredictions()
        {
            List<OrbPredictions> predictionsList = new List<OrbPredictions>();
            DataTable dt = new CommonFunctions().runQuery(
                "SELECT userID, groupChat, predictionText FROM dbo.Predictions",
                new string[] {"userID", "groupChat", "predictionText"},
                config);
            foreach (DataRow row in dt.Rows)
            {
                OrbPredictions p = new OrbPredictions(
                    row["predictionText"].ToString(),
                    long.Parse(row["userID"].ToString()),
                    long.Parse(row["groupChat"].ToString()));
                predictionsList.Add(p);
            }

            return predictionsList;
        }
        
        /// <summary>
        /// Import the config Dictionary for use in further setup.
        /// </summary>
        /// <param name="c">The config Dictionary</param>
        public Setup(Dictionary<string, string> c)
        {
            config = c;
        }
    }
}
