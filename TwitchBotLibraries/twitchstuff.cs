using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
namespace TwitchBot
{
    public class TwitchStuff
    {
        public int streamid = 0;
        public string streamname;
        public string streamername;
        public string streamerlive;
        public string game;
        public bool setnotice;
        public DateTime lastrefresh;
        public DateTime lastannounce;
        public DateTime lastchangeannounce;
        public bool AnnounceAgain = false;
        public string streamerviewcount;
        public DateTime LastOffLine;
        // Not actually used in IRC. This is a placeholder to dictate the link of the streamer to a channel for databinding purposes.
        public string ChannelName;
        public int ChannelID;

        public static List<TwitchStuff> ConvertDataTable(DataTable dTable)
        {
            List<TwitchStuff> ReturnValue = new List<TwitchStuff>();
            foreach(DataRow row in dTable.Rows)
            {
                TwitchStuff newTwitchStuff = new TwitchStuff();
                newTwitchStuff.game = row["StreamGame"].ToString();
                if (Convert.ToInt32(row["StreamOnline"]) == 1)
                    newTwitchStuff.streamerlive = "true";
                else
                    newTwitchStuff.streamerlive = "false";
                newTwitchStuff.streamid = Convert.ToInt32(row["StreamID"]);
                newTwitchStuff.streamerviewcount = row["StreamViewerCount"].ToString();
                newTwitchStuff.streamername = row["StreamerName"].ToString();
                newTwitchStuff.streamname = row["StreamTitle"].ToString();
                newTwitchStuff.ChannelName = row["ChannelName"].ToString();
                newTwitchStuff.ChannelID = Convert.ToInt32(row["ChannelID"]);
                ReturnValue.Add(newTwitchStuff);
            }
            return ReturnValue;
        }
        public static List<TwitchStuff> ConvertDataTable(DataTable dTable, string ChannelName)
        {
            List<TwitchStuff> ReturnValue = new List<TwitchStuff>();
            foreach (DataRow row in dTable.Rows)
            {
                TwitchStuff newTwitchStuff = new TwitchStuff();
                newTwitchStuff.game = row["StreamGame"].ToString();
                if (Convert.ToInt32(row["StreamOnline"]) == 1)
                    newTwitchStuff.streamerlive = "true";
                else
                    newTwitchStuff.streamerlive = "false";
                newTwitchStuff.streamid = Convert.ToInt32(row["StreamID"]);
                newTwitchStuff.streamerviewcount = row["StreamViewerCount"].ToString();
                newTwitchStuff.streamername = row["StreamerName"].ToString();
                newTwitchStuff.streamname = row["StreamTitle"].ToString();
                newTwitchStuff.ChannelName = row["ChannelName"].ToString();
                newTwitchStuff.ChannelID = Convert.ToInt32(row["ChannelID"]);
                ReturnValue.Add(newTwitchStuff);
            }
            return ReturnValue;
        }
        public static List<TwitchStuff> ConvertDataTableNoChannel(DataTable dTable)
        {
            List<TwitchStuff> ReturnValue = new List<TwitchStuff>();
            foreach (DataRow row in dTable.Rows)
            {
                TwitchStuff newTwitchStuff = new TwitchStuff();
                newTwitchStuff.game = row["StreamGame"].ToString();
                if (Convert.ToInt32(row["StreamOnline"]) == 1)
                    newTwitchStuff.streamerlive = "true";
                else
                    newTwitchStuff.streamerlive = "false";
                newTwitchStuff.streamid = Convert.ToInt32(row["StreamID"]);
                newTwitchStuff.streamerviewcount = row["StreamViewerCount"].ToString();
                newTwitchStuff.streamername = row["StreamerName"].ToString();
                newTwitchStuff.streamname = row["StreamTitle"].ToString();
                ReturnValue.Add(newTwitchStuff);
            }
            return ReturnValue;
        }
        public bool UpdateInfo(string TwitchName, ConfigurationReader config)
        {            
            bool properlyupdated = true;
            bool allstreamerspresent = false;
            try
            {
                if (config.AllStreamers.ContainsKey(TwitchName))
                {
                    if (config.AllStreamers[TwitchName].lastrefresh.AddSeconds(30) >= DateTime.Now)                
                    {
                        allstreamerspresent = true;
                    }
                }
                if (allstreamerspresent) 
                { 
                    // This is so we don't repeatedly nail the twitch api for our own speed and whatnot
                    this.streamerviewcount = config.AllStreamers[TwitchName].streamerviewcount;
                    this.streamname = config.AllStreamers[TwitchName].streamname;
                    this.game = config.AllStreamers[TwitchName].game;
                    this.streamerlive = config.AllStreamers[TwitchName].streamerlive;                    
                }
                else
                { 
                    TwitchAPIInterface getTwitch = new TwitchAPIInterface();
                    getTwitch.GetResponse(TwitchName);
                    if (!String.IsNullOrEmpty(getTwitch.Error))
                    {
                        properlyupdated = false;
                    }
                    else
                    {
                        // no error
                        if (String.IsNullOrWhiteSpace(getTwitch.Data["stream"].ToString()))
                        {
                            // it appears that twitch has told us this person is not streaming.
                            this.game = "";
                            this.streamname = "";
                            this.streamerviewcount = "";                            
                            this.streamerlive = "false"; 
                        }
                        else
                        {
                            string streamname = getTwitch.Data["stream"]["channel"]["status"].ToString().Trim('\r','\n');
                            string streamviewers = getTwitch.Data["stream"]["viewers"].ToString();
                            string streamgame = getTwitch.Data["stream"]["game"].ToString();
                            this.streamerviewcount = streamviewers;
                            this.streamname = streamname;
                            this.game = streamgame;
                            this.streamerlive = "true";
                        }
                    }//else                    
                }
                if (properlyupdated == true)
                {
                    this.streamername = TwitchName;
                    this.lastrefresh = DateTime.Now;
                    if (config.AllStreamers.ContainsKey(TwitchName))
                    {
                        config.AllStreamers[TwitchName].streamerviewcount = this.streamerviewcount;
                        config.AllStreamers[TwitchName].streamname = this.streamname;
                        config.AllStreamers[TwitchName].game = this.game;
                        config.AllStreamers[TwitchName].streamerlive = this.streamerlive;
                        config.AllStreamers[TwitchName].lastrefresh = DateTime.Now;                        
                    }
                    else
                    {
                        config.AllStreamers[TwitchName] = this;
                    }
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
        }
        public TwitchStuff()
        {
            this.streamerviewcount = "";
            this.streamname = "";
            this.streamername = "";
            this.lastannounce = DateTime.Now.AddMinutes(-30);
            this.lastrefresh = DateTime.Now.AddMinutes(-30);
            this.lastchangeannounce = DateTime.Now.AddMinutes(-30);
            this.LastOffLine = DateTime.Now.AddMinutes(-60);
            this.game = "";
            this.streamerlive = "false";
        }
        public TwitchStuff(string twitchid)
        {
            this.streamerviewcount = "";
            this.streamname = "";
            this.streamername = twitchid;
            this.lastannounce = DateTime.Now.AddMinutes(-30);
            this.lastrefresh = DateTime.Now.AddMinutes(-30);
            this.lastchangeannounce = DateTime.Now.AddMinutes(-30);
            this.LastOffLine = DateTime.Now.AddMinutes(-60);
            this.game = "";
            this.streamerlive = "false";
        }
    }
}