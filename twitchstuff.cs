using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchBot
{
    public class TwitchStuff
    {
        public string streamname;
        public string streamername;
        public string streamerlive;
        public string game;
        public DateTime lastrefresh;
        public DateTime lastannounce;
        public DateTime lastchangeannounce;
        public bool AnnounceAgain = false;
        public string streamerviewcount;
        public bool UpdateInfo(string TwitchName)
        {
            Console.WriteLine("Updating User Information for " + TwitchName);
            bool returnvalue = false;
            try
            {
                TwitchAPIInterface getTwitch = new TwitchAPIInterface();
                getTwitch.GetResponse(TwitchName);
                string test = getTwitch.Data["stream"].ToString();
                if (getTwitch.GetResponse(TwitchName) != "false")
                {
                    this.streamername = TwitchName;
                    this.lastannounce = new DateTime();
                    this.lastrefresh = DateTime.Now;
                    if (test == "")
                    {
                        this.game = "";
                        this.streamname = "";
                        this.streamerviewcount = "";
                        this.streamerlive = "false";
                    }
                    else
                    {

                        string streamname = getTwitch.Data["stream"]["channel"]["status"].ToString();
                        string streamviewers = getTwitch.Data["stream"]["viewers"].ToString();
                        string streamgame = getTwitch.Data["stream"]["game"].ToString();
                        this.streamerviewcount = streamviewers;
                        this.streamname = streamname;
                        this.game = streamgame;
                        this.streamerlive = "true";
                    }
                }//if (getTwitch.GetResponse(s) != "false")
                else
                {
                    return false;
                }
                returnvalue = true;
            }
            catch (Exception ex)
            {
                returnvalue = false;
            }
            return returnvalue;
        }
    }
}