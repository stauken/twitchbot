using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
namespace TwitchBot
{
    public class MysteryGame
    {
        public string gameid;
        public string drawdate;
        public string submitter;
        public string platform;
        public string goal;
        public string download;
        public string specialrequirements;
        public string pastebin;
        public string tournamentraceresult;
        public string notes;
        public string name;
        public string draws;
    }
    class DataAccess
    {
        public List<MysteryGame> GameList(string ConnectionStringValue,string databasename)
        {
            List<MysteryGame> returnValue = new List<MysteryGame>();
            SqlConnection sqlConn = new SqlConnection(ConnectionStringValue);
            if (sqlConn.State == ConnectionState.Closed)
                    sqlConn.Open();
            sqlConn.ChangeDatabase(databasename);
            SqlCommand spHandler = new SqlCommand("spGames", sqlConn);
            spHandler.Parameters.AddWithValue("@step", 1);
            spHandler.CommandType = CommandType.StoredProcedure;
            SqlDataAdapter daGameHandler = new SqlDataAdapter(spHandler);
            DataTable dtGameHandler = new DataTable();
            daGameHandler.Fill(dtGameHandler);
            
            var convertedList = (from rw in dtGameHandler.AsEnumerable()
                                 select new MysteryGame()
                                 {
                                     gameid = Convert.ToString(rw["gameid"]),
                                     download = Convert.ToString(rw["download"]),
                                     drawdate = Convert.ToString(rw["drawdate"]),
                                     submitter = Convert.ToString(rw["submitter"]),
                                     name = Convert.ToString(rw["name"]),
                                     platform = Convert.ToString(rw["platform"]),
                                     goal = Convert.ToString(rw["goal"]),
                                     specialrequirements = Convert.ToString(rw["specialrequirements"]),
                                     tournamentraceresult = Convert.ToString(rw["tournamentraceresult"]),
                                     notes = Convert.ToString(rw["notes"]),
                                     draws = Convert.ToString(rw["draws"]),
                                     pastebin = (Utilities.PasteBinPass(Convert.ToString(rw["pastebin"]))) ? Convert.ToString(rw["pastebin"]) : "pastebin not provided"
                                 }).ToList();
            
            return convertedList;            
        }        
        public bool UpdateStreamInfo(Dictionary<string,List<TwitchStuff>> updates, string ConnectionStringValue)
        {
            bool success = false;
            SqlConnection sqlConn = new SqlConnection(ConnectionStringValue);
            
            try
            {
                if (sqlConn.State == ConnectionState.Closed)
                    sqlConn.Open();

                SqlCommand spHandler = new SqlCommand("spStreams", sqlConn);
                spHandler.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter daStreamHandler = new SqlDataAdapter(spHandler);
                // yes, nested foreach loops. wanna fight about it?
                foreach (string key in updates.Keys)
                {
                    DataTable getChannel = new DataTable();
                    
                    spHandler.Parameters.Clear();
                    spHandler.Parameters.AddWithValue("@step", "9");
                    spHandler.Parameters.AddWithValue("@ChannelName", key);
                    daStreamHandler.Fill(getChannel);
                    int identity  = 0;
                    if (getChannel.Rows.Count == 0)
                    {
                        // channel does not exist, lets insert                        
                        spHandler.Parameters["@step"].Value = "7";
                        spHandler.Parameters["@ChannelName"].Value = key;
                        identity = Convert.ToInt32(spHandler.ExecuteScalar());
                    }
                    else
                    {
                        identity = Convert.ToInt32(getChannel.Rows[0]["ChannelID"]);
                    }
                    foreach(TwitchStuff stream in updates[key])
                    {
                        DataTable StreamerCheck = new DataTable();
                        spHandler.Parameters.Clear();
                        spHandler.Parameters.AddWithValue("@step","3");
                        spHandler.Parameters.AddWithValue("@StreamerName",stream.streamername);
                        daStreamHandler.Fill(StreamerCheck);
                        spHandler.Parameters.Clear();
                        
                        if(StreamerCheck.Rows.Count == 0)
                        {
                            // Have to add streamer                            
                            spHandler.Parameters.AddWithValue("@step","6");
                            spHandler.Parameters.AddWithValue("@ChannelID", identity);
                        }
                        else
                        {
                            bool AlreadyRelatedToChannel = false;
                            spHandler.Parameters.AddWithValue("@step", 11);
                            spHandler.Parameters.AddWithValue("@ChannelID", identity);
                            spHandler.Parameters.AddWithValue("@StreamID", StreamerCheck.Rows[0]["StreamID"]);
                            DataTable CheckStreamRelation = new DataTable();
                            daStreamHandler.Fill(CheckStreamRelation);
                            if (CheckStreamRelation.Rows.Count > 0)
                                AlreadyRelatedToChannel = true;

                            if (!AlreadyRelatedToChannel)
                            {
                                spHandler.Parameters["@step"].Value = 12;
                                spHandler.ExecuteNonQuery();
                            }
                            spHandler.Parameters.Clear();
                            // Have to update streamer
                            spHandler.Parameters.AddWithValue("@step", "5");
                            spHandler.Parameters.AddWithValue("@StreamID", StreamerCheck.Rows[0]["StreamID"]);
                        }
                        spHandler.Parameters.AddWithValue("@StreamerName", stream.streamername);
                        spHandler.Parameters.AddWithValue("@StreamGame", stream.game);
                        spHandler.Parameters.AddWithValue("@StreamTitle", stream.streamname);
                        spHandler.Parameters.AddWithValue("@StreamViewerCount", stream.streamerviewcount);
                        if (stream.streamerlive == "true")
                            spHandler.Parameters.AddWithValue("@StreamOnline", 1);
                        else
                            spHandler.Parameters.AddWithValue("@StreamOnline", 0);
                        spHandler.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(String.Format("Error: {0}", ex.Message));
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            finally
            {
                if (sqlConn.State == ConnectionState.Open)
                    sqlConn.Close();               
            }

            return success;
        }
    }
}
