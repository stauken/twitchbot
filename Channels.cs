using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                foreach (string s in this.BlackList)
                {
                    if (StreamInfo.game.Contains(s))
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
