using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
namespace TwitchBot
{
    public class Channels
    {
        public List<string> Streamers = new List<string>();
        public List<TwitchStuff> StreamInfo = new List<TwitchStuff>();
        public DateTime LastLiveAnnouncement = DateTime.Now.AddMinutes(-3);
        public string ChannelName = "";
        public int ChannelIndex = 0;
        public List<string> ChannelRaces = new List<string>();
        public string ChannelPassword = "";
        public string AnnounceMessage = "";
        public string ChangedMessage = "";
        public string LiveMessage = "";
        public uint SendDelay = 0;
        public List<string> WhiteList = new List<string>();
        public List<string> BlackList = new List<string>();
        public bool UseWhiteList = false;
        public bool UseBlackList = false;
        public DateTime LastLiveAllMessage = DateTime.Now.AddMinutes(-3);
        public int ChannelID;
        public bool Mystery = false;
        public static List<Channels> ConvertDataTable(DataTable dTable)
        {
            
            List<Channels> ReturnedChannels = new List<Channels>();
            foreach(DataRow row in dTable.Rows)
            {
                Channels twitchChannel = new Channels();
                twitchChannel.ChannelName = row["ChannelName"].ToString();
                twitchChannel.ChannelID = Convert.ToInt32(row["ChannelID"]);
                ReturnedChannels.Add(twitchChannel);
            }
            return ReturnedChannels;
        }
        // Check against white/black lists or return true if neither are enabled
        public bool MeetsWhiteBlackList(TwitchStuff StreamInfo)
        {
            bool meetswhitelist = false;
            if (this.UseWhiteList)
            {                
                foreach (string s in this.WhiteList)
                {
                    if (StreamInfo.game.Contains(s))
                        meetswhitelist = true;
                }
            }
            if (this.UseBlackList)
            {
                // default to true if it's only the blacklist
                if (!this.UseWhiteList)
                    meetswhitelist = true;
                foreach (string s in this.BlackList)
                {
                    if (StreamInfo.game.Contains(s))
                        meetswhitelist = false;
                    if (StreamInfo.streamname.Contains(s))
                        meetswhitelist = false;
                }
            }
            if (!this.UseBlackList && !this.UseWhiteList)
            {
                meetswhitelist = true;
            }
            return meetswhitelist;
        }        
    }
}
