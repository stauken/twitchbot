using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrcDotNet;
namespace TwitchBot
{
    public class IrcBot 
    {
        
        #region Miscellaneous properties
        public bool debugging = false;
        public bool InSRL = false;
        public IrcClient ircConnection = new IrcClient();
        public bool ActiveBot = true;
        public ConfigurationReader config = new ConfigurationReader();
        public DateTime LastPingSent = DateTime.Now;
        public DateTime LastTransform = DateTime.Now;
        //public DateTime LastLiveCommand = DateTime.Now.AddMinutes(-3);
        public Boolean FullyJoined = false;
        public DateTime LastPingReceived = DateTime.Now;
        public DateTime LastFullUpdate = DateTime.Now;
        public System.IO.FileStream LogFile;
        public System.IO.FileStream TwitchLog;
        public bool SweepingChannels = false;
        
        #endregion
        #region "Log stuff"
        public void ConfigureLog()
        {
        }//public static void ConfigureLog()
        public void TwitchLogWrite(string Message)
        {
            System.IO.File.AppendAllText("./TwitchLog.log", Message);
        }//public static void TwitchLogWrite(string Message)
        public void LogWrite(string Message)
        {
            System.IO.File.AppendAllText("./RawLog.log", Message);
        }//public static void LogWrite(string Message)
        #endregion
        public void Start()
        {
            AppDomain.CurrentDomain.UnhandledException += HandleExceptions;
            if (Utilities.IsDebug())
            {
                debugging = true;
            }
            ConfigureLog();
            config.ParseConfig();
            HandleConnection();
            LoginLoop();
        }
        private void HandleExceptions(object sender, UnhandledExceptionEventArgs e)
        {
            LogWrite(((Exception)e.ExceptionObject).Message);
        }
        public void HandleConnection()
        {
            ircConnection.Connected += new EventHandler<EventArgs>(ircConnection_Connected);
            ircConnection.ConnectFailed += new EventHandler<IrcErrorEventArgs>(ircConnection_ConnectFailed);
            ircConnection.Disconnected += new EventHandler<EventArgs>(ircConnection_Disconnected);
            ircConnection.Error += new EventHandler<IrcErrorEventArgs>(ircConnection_Error);
            ircConnection.NetworkInformationReceived += new EventHandler<EventArgs>(ircConnection_NetworkInformationReceived);
            ircConnection.PingReceived += new EventHandler<IrcPingOrPongReceivedEventArgs>(ircConnection_PingReceived);
            ircConnection.PongReceived += new EventHandler<IrcPingOrPongReceivedEventArgs>(ircConnection_PongReceived);
            ircConnection.ProtocolError += new EventHandler<IrcProtocolErrorEventArgs>(ircConnection_ProtocolError);
            if (config.ServerName.Contains("twitch.tv"))
                ircConnection.FloodPreventer = new IrcStandardFloodPreventer(1, 7000);
            ircConnection.Connect(config.ServerName, 6667, false, config.BotInfo);            
        }
        public void LoginLoop()
        {
            // wait until we're all set up and logged in
            while (!ircConnection.IsConnected)
            {

            }
            while (!ircConnection.IsRegistered)
            {


            }
            ircConnection.LocalUser.MessageReceived += new EventHandler<IrcMessageEventArgs>(LocalUser_MessageReceived);
            ircConnection.LocalUser.MessageSent += new EventHandler<IrcMessageEventArgs>(LocalUser_MessageSent);
            ircConnection.LocalUser.NoticeReceived += new EventHandler<IrcMessageEventArgs>(LocalUser_NoticeReceived);
            ircConnection.LocalUser.NoticeSent += new EventHandler<IrcMessageEventArgs>(LocalUser_NoticeSent);

        }
        // join channels
        public void RegisteredChannels()
        {
            DateTime started = DateTime.Now;
            int curcount = ircConnection.Channels.Count;
            foreach (Channels chan in config.TwitchChannels)
            {
                Tuple<string, string> chanpass = new Tuple<string, string>(chan.ChannelName, chan.ChannelPassword);
                ircConnection.Channels.Join(chanpass);
                while (curcount + 1 > ircConnection.Channels.Count)
                {
                    if (started.AddSeconds(15) <= DateTime.Now)                    
                    {
                        ircConnection.Channels.Join(chanpass);
                        started = DateTime.Now;
                    }// wait out joining
                }

                //SendLiveList(ircConnection.Channels[curcount].Name);
                ircConnection.Channels[curcount].MessageReceived += new EventHandler<IrcMessageEventArgs>(Program_MessageReceived);
                ircConnection.Channels[curcount].NoticeReceived += new EventHandler<IrcMessageEventArgs>(Program_NoticeReceived);
                ircConnection.Channels[curcount].UserJoined += new EventHandler<IrcChannelUserEventArgs>(Program_UserJoined);
                curcount++;
            }
            if (!debugging)
            {
                ircConnection.Channels.Join("#speedrunslive");

                while (curcount + 1 > ircConnection.Channels.Count)
                {
                    if (started.AddSeconds(15) <= DateTime.Now)
                    {
                        ircConnection.Channels.Join("#speedrunslive");
                        started = DateTime.Now;
                    }

                    // wait out joining
                }
                ircConnection.Channels[curcount].MessageReceived += new EventHandler<IrcMessageEventArgs>(Program_SRLMessageReceived);
            }
            FullyJoined = true;
        }
        public void SweepChannels()
        {
            while (config.ModifyingConfig)
            { } // wait out config modifications
            SweepingChannels = true;
            if (LastFullUpdate < DateTime.Now)
            {
                LastFullUpdate = DateTime.Now.AddMinutes(2);
                foreach (Channels channel in config.TwitchChannels)
                {
                    Dictionary<string, List<TwitchStuff>> updateDB = new Dictionary<string, List<TwitchStuff>>();
                    List<TwitchStuff> dbList = new List<TwitchStuff>();
                    foreach (TwitchStuff streamInfo in channel.StreamInfo)
                    {
                        TwitchStuff oldInfo = new TwitchStuff();
                        oldInfo.streamerlive = streamInfo.streamerlive;
                        oldInfo.streamername = streamInfo.streamername;
                        oldInfo.streamname = streamInfo.streamname;
                        oldInfo.game = streamInfo.game;
                        try
                        {                            
                            string wasLive = streamInfo.streamerlive;
                            // get the info for the 'stream' field
                            string addToList = "";
                            if(streamInfo.UpdateInfo(streamInfo.streamername,config))
                            {
                                if (oldInfo.streamerlive == "false" && streamInfo.streamerlive == "true") 
                                {
                                    if (channel.LiveMessage != "")
                                    {
                                        addToList = channel.LiveMessage.Trim();
                                    }//if (channel.LiveMessage != "")
                                    else
                                    {
                                        addToList = config.LiveMessage.Trim();
                                    }//else
                                    addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname, config.ServerName.ToLower().Contains("twitch.tv"));
                                    bool meetswhitelist = channel.MeetsWhiteBlackList(streamInfo);
                                    if (streamInfo.lastannounce.AddMinutes(30) <= DateTime.Now && meetswhitelist && streamInfo.LastOffLine.AddMinutes(30) <= DateTime.Now)
                                    {
                                        if (meetswhitelist && !streamInfo.setnotice)
                                            ircConnection.LocalUser.SendMessage(channel.ChannelName, addToList);
                                        streamInfo.lastannounce = DateTime.Now;
                                    }//if (streamInfo.lastannounce.AddMinutes(30) <= DateTime.Now && meetswhitelist)
                                }
                                else if (streamInfo.streamerlive == "true" && oldInfo.streamname != streamInfo.streamname && oldInfo.game != streamInfo.game)
                                {
                                    bool changesmeetwhitelist = channel.MeetsWhiteBlackList(streamInfo);
                                    if (changesmeetwhitelist)
                                    {
                                        if (channel.ChangedMessage != "")
                                        {
                                            addToList = channel.ChangedMessage;
                                        }//if (channel.ChangedMessage != "")
                                        else
                                        {
                                            addToList = config.ChangeMessage.Trim();
                                        }//else
                                        addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname, config.ServerName.ToLower().Contains("twitch.tv"));
                                        if (streamInfo.lastchangeannounce.AddMinutes(15) < DateTime.Now && !streamInfo.setnotice)
                                        {
                                            ircConnection.LocalUser.SendMessage(channel.ChannelName, addToList);
                                            streamInfo.lastchangeannounce = DateTime.Now;
                                        }
                                    }//if (changesmeetwhitelist)
                                    streamInfo.lastannounce = DateTime.Now;
                                }
                                if (streamInfo.streamerlive == "false" && oldInfo.streamerlive == "true")
                                {
                                    streamInfo.LastOffLine = DateTime.Now;
                                }
                                dbList.Add(streamInfo);
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("ERROR: " + ex.Message);
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                    }
                    // Update DB                    
                    updateDB.Add(channel.ChannelName, dbList);
                    if (config.UseDB)
                    {
                        DataAccess daMumbo = new DataAccess();
                        daMumbo.UpdateStreamInfo(updateDB, config.ConnectionString);
                    }
                }
            }
            
            SweepingChannels = false;
            

        }
        /// <summary>
        /// Command to mimic a function that the original mumbo bot had. Typos are intentional, do not fix :)
        /// </summary>
        /// <param name="CurChan"></param>        
        public void DoTransform(Channels CurChan, string user)
        {
            LastTransform = DateTime.Now;
            try
            {
                if (!String.IsNullOrWhiteSpace(user))
                {
                    Random randGen = new Random();
                    int intValue = randGen.Next(1, 7);
                    string message = "";
                    switch (intValue)
                    {
                        case 1:
                            message = String.Format("\x01" + "ACTION transform {0} into funny looking termite!\x01", user);
                            break;
                        case 2:
                            message = String.Format("\x01" + "ACTION magically change {0} into tiny little bouncing pumpkin!\x01", user);
                            break;
                        case 3:
                            message = String.Format("\x01" + "ACTION transform {0} into T-Rex. Wait, who am I? Wumba? I change you back.\x01", user);
                            break;
                        case 4:
                            message = String.Format("\x01" + "ACTION transforms {0} into a funny looking termite!\x01", user);
                            break;
                        case 5:
                            message = String.Format("\x01" + "ACTION magically change {0} into ...washing machine? That not right. I hope you not go for World Record!\x01", user);
                            break;
                        case 6:
                            message = String.Format("\x01" + "ACTION magically change {0} into little crocodile! Yes! Mumbo need new shoes! Only kidding...\x01", user);
                            break;
                        case 7:
                            message = String.Format("\x01" + "ACTION magically change {0} into silly little Bumble Bee!\x01", user);
                            break;
                        default:
                            message = String.Format("\x01" + "ACTION magically change {0} into silly little Bumble Bee!\x01", user);
                            break;
                    }//switch (intValue)
                    ircConnection.LocalUser.SendMessage(CurChan.ChannelName, message);
                }//if (!String.IsNullOrWhiteSpace(user))
            }//try
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }//catch (Exception ex)
        }//public static void DoTransform(Channels CurChan, string user)
        public void SendAllLiveList(Channels channel, IIrcMessageSource e)
        {
            bool foundstream = false;          
            channel.LastLiveAllMessage = DateTime.Now;
            string liveList = "";
            foreach (TwitchStuff streamInfo in channel.StreamInfo)
            {
                if (streamInfo.streamerlive == "true")
                {
                    string addToList = config.LiveMessage;
                    if (channel.LiveMessage != "")
                    {
                        addToList = channel.LiveMessage;
                    }
                    addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname, config.ServerName.ToLower().Contains("twitch.tv"));
                    liveList = addToList.Trim();
                    foundstream = true;
                    ircConnection.LocalUser.SendNotice(e.Name, liveList);

                    liveList = "";
                }
            }
            if (!foundstream)
            {
                ircConnection.LocalUser.SendMessage(channel.ChannelName, "No one is currently streaming.");
            }
        }
        public List<string> GetLiveList(Channels channel, bool UseLiveAll)
        {
            bool foundstream = false;
            bool foundunapprovedstream = false;
            List<string> listmessages = new List<string>();
            //channel.LastLiveAnnouncement = DateTime.Now;
            string liveList = "";
            foreach (TwitchStuff streamInfo in channel.StreamInfo)
            {
                bool meetswhitelist = true;
                if (streamInfo.streamerlive == "true")
                {
                    string addToList = config.LiveMessage;
                    if (channel.LiveMessage != "")
                        addToList = channel.LiveMessage;
                    addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname, config.ServerName.ToLower().Contains("twitch.tv"));
                    liveList = addToList.Trim();
                    meetswhitelist = channel.MeetsWhiteBlackList(streamInfo);
                    if (UseLiveAll)
                    {
                        meetswhitelist = true;
                    }
                    if (meetswhitelist)
                    {
                        foundstream = true;
                        listmessages.Add(liveList);
                    }
                    else
                    {
                        foundunapprovedstream = true;
                    }

                    liveList = "";
                }

            }
            if (!foundstream)
            {
                if (foundunapprovedstream)
                {
                    listmessages.Add("No one is currently streaming a whitelisted game. Use liveall to see streams.");
                }
                else
                {
                    listmessages.Add("No one is currently streaming.");
                }
            }
            return listmessages;
        }
        public void SendLiveList(Channels channel, string nick, string[] args)
        {
            bool foundstream = false;
            bool foundunapprovedstream = false;
            int livestreams = 0;
            bool hasArgs = (args.Length > 0);

            List<TwitchStuff> filteredStreamInfos = channel.StreamInfo;

            foreach (TwitchStuff streamInfo in channel.StreamInfo)
            {
                if (streamInfo.streamerlive == "true")
                {
                    if (channel.MeetsWhiteBlackList(streamInfo))
                    {
                        if (hasArgs)
                        {
                            if(args.Any(s => streamInfo.game.ToLower().Contains(s.ToLower()) || streamInfo.streamname.ToLower().Contains(s.ToLower())))
                                livestreams++;
                        }
                        else
                            livestreams++;
                    }
                }
            }
            bool announcetochannel = true;
            if (livestreams >= 4)
            {
                announcetochannel = false;
            }
            int timecheck = 60;
            if (!announcetochannel)
            {
                timecheck = 5;
            }
            if (channel.LastLiveAnnouncement.AddSeconds(timecheck) <= DateTime.Now)
            {
                if (announcetochannel)
                {
                    channel.LastLiveAnnouncement = DateTime.Now;
                }
                string liveList = "";
                foreach (TwitchStuff streamInfo in channel.StreamInfo)
                {
                    bool meetswhitelist = false;
                    if (streamInfo.streamerlive == "true")
                    {
                        string addToList = config.LiveMessage;
                        if (channel.LiveMessage != "")
                            addToList = channel.LiveMessage;//if (channel.LiveMessage != "")
                        addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname, config.ServerName.ToLower().Contains("twitch.tv"));
                        liveList = addToList.Trim();
                        meetswhitelist = channel.MeetsWhiteBlackList(streamInfo);
                        if (args.Length > 0)
                        {
                            // !LIVE SEARCH FUNCTIONALITY
                            if (!args.Any(s => streamInfo.game.ToLower().Contains(s.ToLower()) || streamInfo.streamname.ToLower().Contains(s.ToLower())))
                                meetswhitelist = false;
                        }
                        if (meetswhitelist)
                        {
                            
                            if (announcetochannel)
                            {   
                                if (!streamInfo.setnotice)
                                {
                                    foundstream = true;
                                    ircConnection.LocalUser.SendMessage(channel.ChannelName, liveList);
                                }//if (!streamInfo.setnotice)
                                else
                                {
                                    foundunapprovedstream = true;
                                }//else
                            }//if(announcetochannel)
                            else
                            {
                                foundstream = true;
                                ircConnection.LocalUser.SendNotice(nick, liveList);
                            }//else
                        }//if (meetswhitelist)
                        else
                        {
                            foundunapprovedstream = true;
                        }//else
                        liveList = "";
                    }//if (streamInfo.streamerlive == "true")
                }//foreach (TwitchStuff streamInfo in channel.StreamInfo)
                if (!foundstream)
                {
                    if (foundunapprovedstream)
                    {
                        ircConnection.LocalUser.SendMessage(channel.ChannelName, "No one is currently cleared to report on !live. Use !liveall to see streams.");
                    }
                    else
                    {

                        ircConnection.LocalUser.SendMessage(channel.ChannelName, "No one is currently streaming.");
                    }
                }
            }
        }
        #region Events


        void ircConnection_RawMessageReceived(object sender, IrcRawMessageEventArgs e)
        {

        }

        void ircConnection_ProtocolError(object sender, IrcProtocolErrorEventArgs e)
        {
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);            
            Console.ForegroundColor = ConsoleColor.Gray;
            LogWrite(String.Format("IRC Protocol Error: {0}", e.Message));
        }
        // watch for races specifically for speedrunslive and echo them to any channel configured to receive them
        public void Program_SRLMessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (e.Source.Name == "RaceBot")
            {
                // track for races
                foreach (Channels channel in config.TwitchChannels)
                {
                    foreach (string racename in channel.ChannelRaces)
                    {
                        if (e.Text.Contains(String.Format("Race initiated for {0}", racename)))
                        {

                            ircConnection.LocalUser.SendMessage(channel.ChannelName, e.Text);
                        }
                    }
                }
            }
        }
        // stuff for when a user joins the channel
        void Program_UserJoined(object sender, IrcChannelUserEventArgs e)
        {

        }
        // This is just printing any privmsgs we send to the screen
        public void LocalUser_MessageSent(object sender, IrcMessageEventArgs e)
        {
            //Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }//public static void LocalUser_MessageSent(object sender, IrcMessageEventArgs e)

        public void SendHelp(string[] cmdargs, IIrcMessageSource source)
        {
            List<string> helpMsg = new List<string>();
            if (cmdargs.Length >= 2)
            {
                if (cmdargs.Length >= 3)
                {
                    if (cmdargs[2] == "!live")
                    {
                        helpMsg.Add("Displays all streamers who are currently watched, live and meet any list requirements. Will send a notice to inform users if there are more than 3 streamers live. Streamers may set themselves to only show when live is sent as a notice by using !setnotice on.");
                    }
                    if (cmdargs[2] == "!liveall")
                    {
                        helpMsg.Add("Displays all streamers who are currently watched and live. Liveall always reports via notice.");
                    }
                    if (cmdargs[2] == "!watching")
                    {
                        helpMsg.Add("Displays all users being watched on the current channel. This sends via a notice.");
                    }
                    if (cmdargs[2] == "!add")
                    {
                        helpMsg.Add("Add a user to the streaming list. Available only to channel ops.");
                    }
                    if (cmdargs[2] == "!remove")
                    {
                        helpMsg.Add("Remove a user from the streaming list. Available only to channel ops.");
                    }
                    if (cmdargs[2] == "!setnotice")
                    {
                        helpMsg.Add("use !setnotice ON to make it so that your stream will only announce on NOTICE reports, !setnotice OFF if you want to show on !live and be announced when you go live.");
                    }
                }
                else
                {
                    if (cmdargs[1] == "channel")
                    {
                        helpMsg.Add("TwitchBot channel commands:");
                        helpMsg.Add("  !live - !liveall - !watching - !add - !remove - !setnotice");
                        helpMsg.Add(String.Format("/msg {0} help channel command - get help on an individual command", config.BotInfo.NickName));
                    }
                    if (cmdargs[1] == "messages")
                    {
                        helpMsg.Add("PM commands:");
                        helpMsg.Add(String.Format("  /msg {0} live #channel - Gets all live users for a watched channel that meet requirements to be listed.", config.BotInfo.NickName));
                        helpMsg.Add(String.Format("  /msg {0} liveall #channel - Gets all live users for a watched channel.", config.BotInfo.NickName));
                        helpMsg.Add(String.Format("  /msg {0} listchannels - Gets all channels being watched.", config.BotInfo.NickName));
                        helpMsg.Add(String.Format("  /msg {0} watching #channel - Gets users being watched on channel.", config.BotInfo.NickName));
                    }
                }
            }

            else
            {
                helpMsg.Add("Please use 'help channel' or 'help messages' for commands for channels and private messages.");
            }

            foreach (string s in helpMsg)
            {
                ircConnection.LocalUser.SendNotice(source.Name, s);
            }//foreach (string s in helpMsg)

        }
        // This is what happens when a privmsg gets received (e.g. someone /msg's the bot)
        public void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            Channels Channel = new Channels();
            bool foundchannel = false;
            string[] cmdargs = e.Text.Split(' ');
            if (cmdargs.Length <= 1 && (cmdargs[0] != "help" && cmdargs[0] != "listchannels"))
            {
                ircConnection.LocalUser.SendMessage(e.Source.Name, String.Format("Try /msg {0} help", config.BotInfo.NickName));
            }
            else if (cmdargs[0] == "help")
            {
                SendHelp(cmdargs, e.Source);
            }//else if (cmdargs[0] == "help" )
            else if (cmdargs[0] == "listchannels")
            {
                foreach (Channels c in config.TwitchChannels)
                {
                    if (c.Streamers.Count > 0)
                        ircConnection.LocalUser.SendMessage(e.Source.Name, c.ChannelName);
                }//foreach (Channels c in config.TwitchChannels)
            }//else if (cmdargs[0] == "listchannels")
            else
            {
                foreach (Channels c in config.TwitchChannels)
                {
                    if (c.ChannelName == cmdargs[1])
                    {
                        foundchannel = true;
                        Channel = c;
                    }//if (c.ChannelName == cmdargs[1])
                }//foreach (Channels c in config.TwitchChannels)
                if (foundchannel)
                {
                    // we want to do some things here.                
                    switch (cmdargs[0])
                    {
                        case "live":
                            List<string> LiveReport = GetLiveList(Channel, false);
                            foreach (string s in LiveReport)
                            {
                                ircConnection.LocalUser.SendMessage(e.Source.Name, s);
                            }
                            break;
                        case "liveall":
                            List<string> LiveAllReport = GetLiveList(Channel, true);
                            foreach (string s in LiveAllReport)
                            {
                                ircConnection.LocalUser.SendMessage(e.Source.Name, s);
                            }
                            break;
                        case "watching":
                            string WatchingReport = string.Empty;
                            foreach (string s in Channel.Streamers)
                            {
                                WatchingReport += s + " ";
                                if (WatchingReport.Length > 250)
                                { 
                                    ircConnection.LocalUser.SendMessage(e.Source.Name, WatchingReport);
                                    WatchingReport = string.Empty;
                                }
                            }
                            if (WatchingReport.Length > 0)
                            {
                                ircConnection.LocalUser.SendMessage(e.Source.Name, WatchingReport);
                                WatchingReport = string.Empty;
                            }                            
                            break;
                        default:
                            break;
                    }
                }//if (foundchannel)
                else
                {
                    ircConnection.LocalUser.SendMessage(e.Source.Name, "No channel by that name is in my watch list.");                    
                }//else
            }//else
            //Console.WriteLine(String.Format("<{0}:{1}> {2}", e.Source.Name, e.Targets[0].Name, e.Text));
        }//public static void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        /// <summary>
        /// Handle a notice being received and print it to the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            //Console.WriteLine(String.Format("<{0}:{1}> {2}", e.Source, e.Targets[0].Name, e.Text));
        }//public static void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)

        void LocalUser_NoticeSent(object sender, IrcMessageEventArgs e)
        {
            //Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }//static void LocalUser_NoticeSent(object sender, IrcMessageEventArgs e)
        void Program_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            //Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }//static void Program_NoticeReceived(object sender, IrcMessageEventArgs e)

        public void Program_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            // I believe in most cases this should be just one element, but it is an array in IrcDotNet's definitions so we'll iterate it anyway.
            foreach (IIrcMessageTarget target in e.Targets)
            {

                Channels CurChan = null;
                foreach (Channels c in config.TwitchChannels)
                {
                    if (target.Name.ToLower() == c.ChannelName.ToLower())
                        CurChan = c;
                }//foreach (Channels c in config.TwitchChannels)
                #region Beta work
                if (e.Text.StartsWith("!viewercount"))
                {
                    int viewertotal = 0;
                    foreach(TwitchStuff tstuff in CurChan.StreamInfo)
                    {
                        if(!String.IsNullOrWhiteSpace(tstuff.streamerviewcount))
                        {
                            int tempcount = Convert.ToInt32(tstuff.streamerviewcount);
                            viewertotal = viewertotal + tempcount;
                        }
                    }
                    if (viewertotal > 0)
                    {
                        ircConnection.LocalUser.SendMessage(CurChan.ChannelName, String.Format("There are {0} total viewers watching channels on the {1} watch list.", viewertotal, CurChan.ChannelName));
                    }
                }
                if (e.Text.StartsWith("!multitwitch"))
                {
                    List<string> matchingnames = new List<string>();
                    string[] args = e.Text.Split(' ');
                    if (args.Length > 1)
                    {
                        for (int i = 1; i < args.Length; i++)
                        {
                            var o = CurChan.StreamInfo.Where(x => (x.game.ToLower().Contains(args[i].ToLower()) || x.streamname.ToLower().Contains(args[i].ToLower())) && x.streamerlive == "true");

                            foreach (TwitchStuff user in o)
                            {
                                // do not repeatedly print the name
                                if (!matchingnames.Contains(user.streamername))
                                    matchingnames.Add(user.streamername);
                            }
                        }
                    }
                    if (matchingnames.Count == 0)
                    {

                            var o = CurChan.StreamInfo.Where(x => x.streamerlive == "true");

                            foreach (TwitchStuff user in o)
                            {
                                // do not repeatedly print the name
                                if (!matchingnames.Contains(user.streamername))
                                    matchingnames.Add(user.streamername);
                            }
                    }
                    if (matchingnames.Count > 1)
                    {

                        StringBuilder multitwitchannounce = new StringBuilder("http://www.multitwitch.tv/");
                        foreach(string s in matchingnames)
                        {
                            multitwitchannounce.Append(string.Format("{0}/",s));
                        }
                        if (args.Length == 0)
                        {
                            ircConnection.LocalUser.SendMessage(CurChan.ChannelName, "No matching searches.");
                        }
                        else
                        {
                            ircConnection.LocalUser.SendMessage(CurChan.ChannelName, multitwitchannounce.ToString());
                        }

                    }
                    else if (matchingnames.Count == 1)
                    {
                        ircConnection.LocalUser.SendMessage(CurChan.ChannelName, String.Format("Only one user matches. http://www.twitch.tv/{0}", matchingnames[0]));
                    }
                }
                #endregion 
                #region Mystery
                if (( e.Text.ToLower().StartsWith("!gimmegame ") || e.Text.ToLower() == "!gimmegame" ) && CurChan.Mystery)
                {
                    string filter = String.Empty;
                    List<MysteryGame> grabgames = null;
                    if (e.Text.ToLower().StartsWith("!gimmegame "))
                    {
                        filter = e.Text.ToLower().Replace("!gimmegame ", "");
                    }
                    DataAccess mysteryAccess = new DataAccess();
                    if (!String.IsNullOrEmpty(filter))
                    {
                        grabgames = mysteryAccess.GameList(config.ConnectionString, "mystery",filter);
                    }
                    else
                    {
                        grabgames = mysteryAccess.GameList(config.ConnectionString, "mystery");
                    }
                    Random getGame = new Random();
                    if (grabgames.Count > 0)
                    { 
                        MysteryGame pulled = grabgames[getGame.Next(0, grabgames.Count - 1)];
                        foreach (var prop in pulled.GetType().GetProperties())
                        {
                            if (String.IsNullOrEmpty(prop.GetValue(pulled,null).ToString()))
                                prop.SetValue(pulled,"",null);                        
                        }
                        mysteryAccess.IncrementDrawCount(config.ConnectionString, "mystery", pulled.gameid);
                        string gamebroadcast = String.Format("$4{0}$x - Platform: $b{1}$x - Submitter: $b{2}$x - Goal: $b{3}$x - Download: $b{4}$x - $b{5}$x - Tournament Race Result: $b{6}$x - Drawn {7}x", 
                            pulled.name, pulled.platform, pulled.submitter, pulled.goal, pulled.download, 
                            pulled.pastebin, pulled.tournamentraceresult,(Convert.ToInt32(pulled.draws)+1));
                        gamebroadcast = Utilities.TemplateMysteryGame(gamebroadcast, pulled, config.ServerName.ToLower().Contains("twitch.tv"));
                        ircConnection.LocalUser.SendMessage(e.Targets[0].Name, gamebroadcast);                    
                    }
                    else
                    {
                        ircConnection.LocalUser.SendMessage(e.Targets[0].Name, "No platform found.");                    
                    }
                }
                #endregion
                #region Lives

                if (e.Text.ToLower().StartsWith("!liveall"))
                {
                    SendAllLiveList(CurChan, e.Source);
                }
                else if (e.Text.ToLower().StartsWith("!live"))
                {
                    string[] args = new string[0];
                    if(e.Text.IndexOf(' ') >= 0)
                    {
                        string argstrings = e.Text.Substring(e.Text.IndexOf(' ')+1);
                        if(argstrings.Length > 0)
                            args = argstrings.Split(' '); 
                    }                                                           
                    SendLiveList(CurChan, e.Source.Name, args);
                }
                #endregion
                if (e.Text.ToLower().StartsWith("!help"))
                {
                    SendHelp(e.Text.Split(' '), e.Source);
                }
                if (e.Text.ToLower().StartsWith("!setnotice on ") || e.Text.ToLower().StartsWith("!setnotice off"))
                {                    
                    string[] args = e.Text.Split(' ');
                    if (args.Length >= 3)       
                    { 
                        if (e.Text.ToLower().StartsWith("!setnotice on "))
                        {
                            if(config.SetNotice(args[2], CurChan, target, e.Source, ircConnection, true))
                            {
                                ircConnection.LocalUser.SendNotice(e.Source.Name, "User " + args[2] + " now has their setnotice value set to true.");
                            }
                        
                        }
                        if (e.Text.ToLower().StartsWith("!setnotice off "))
                        {
                            if(config.SetNotice(args[2], CurChan, target, e.Source, ircConnection, false))
                            {
                                ircConnection.LocalUser.SendNotice(e.Source.Name, "User " + args[2] + " now has their setnotice value set to false.");
                            }
                        }
                    }
                }
                #region Transform
                if (e.Text.StartsWith(".transform"))
                {
                    if (e.Text == ".transform" || e.Text == ".transform ")
                    {
                        DoTransform(CurChan, e.Source.Name);
                    }
                    else
                    {
                        DoTransform(CurChan, e.Text.Replace(".transform ", ""));
                    }
                }
                #endregion Transform
                if (e.Text.StartsWith("!watching"))
                {
                    StringBuilder sbAssembleWatchList = new StringBuilder();
                    foreach (string s in CurChan.Streamers)
                    {
                        sbAssembleWatchList.Append(String.Format("{0} ", s));
                        if (sbAssembleWatchList.Length > 180)
                        {
                            ircConnection.LocalUser.SendNotice(e.Source.Name, sbAssembleWatchList.ToString());                            
                            sbAssembleWatchList.Clear();
                        }//if (sbAssembleWatchList.Length > 180)
                    }//foreach(string s in CurChan.Streamers)
                    if (sbAssembleWatchList.Length > 0)
                    {
                        ircConnection.LocalUser.SendNotice(e.Source.Name, sbAssembleWatchList.ToString());
                        sbAssembleWatchList.Clear();                        
                    }//if (sbAssembleWatchList.Length > 0)
                }//if (e.Text.StartsWith("!watching"))
                #region "Config modifications"
                if (e.Text.StartsWith("!remove "))
                {
                    if (Utilities.CheckOp(e.Source.Name,ircConnection.Channels.First(x => x.Name == CurChan.ChannelName)))
                    {
                        bool success = true;
                        string[] users = e.Text.Split(' ');
                        foreach (string s in users)
                        {
                            bool founduser = false;
                            if (s != "!remove")
                            {
                                while (SweepingChannels)
                                { }//wait out a channel sweep so we don't modify the collection during it
                                foreach (string existingUser in CurChan.Streamers)
                                {
                                    if (s == existingUser)
                                    {
                                        founduser = true;
                                    }
                                }
                                if (!founduser)
                                {
                                    ircConnection.LocalUser.SendMessage(target, "User is not currently being watched.");
                                }
                                else if (!config.RemoveUser(s, CurChan, target, e.Source, ircConnection))
                                {
                                    success = false;
                                }
                            }
                        }
                        if (success)
                        {
                            ircConnection.LocalUser.SendMessage(target, "User(s) successfully removed.");
                        }
                        else
                        {
                            ircConnection.LocalUser.SendMessage(target, "Not all user(s) were successfully removed.");
                        }
                    }
                }
                if (e.Text.StartsWith("!whitelist "))
                {
                    if (Utilities.CheckOp(e.Source.Name,ircConnection.Channels.First(x => x.Name == CurChan.ChannelName)))
                    {
                        if (e.Text == "!whitelist on")
                        {
                            if (config.SetWhiteList(true, CurChan, target, e.Source, ircConnection))
                                ircConnection.LocalUser.SendNotice(CurChan.ChannelName, "Whitelist turned on.");
                        }
                        if (e.Text == "!whitelist off")
                        {
                            if (config.SetWhiteList(false, CurChan, target, e.Source, ircConnection))
                                ircConnection.LocalUser.SendNotice(CurChan.ChannelName, "Whitelist turned off.");
                        }
                    }
                }
                if (e.Text.StartsWith("!whitelistgame "))
                {
                    string game = e.Text.Replace("!whitelistgame ", "");
                    if (Utilities.CheckOp(e.Source.Name, ircConnection.Channels.First(x => x.Name == CurChan.ChannelName)))
                    {
                        if (config.AddWhiteList(game, CurChan, target, e.Source, ircConnection))
                            ircConnection.LocalUser.SendNotice(CurChan.ChannelName, "Whitelisted " + game + ".");
                    }
                }
                if (e.Text.StartsWith("!goliveannouncement"))
                {
                    if (e.Text == "!goliveannouncement")
                    {
                        if (String.IsNullOrEmpty(CurChan.AnnounceMessage))
                        {

                            ircConnection.LocalUser.SendNotice(CurChan.ChannelName, config.BaseMessageStartStreaming);
                        }
                        else
                        {                        
                            ircConnection.LocalUser.SendNotice(CurChan.ChannelName, CurChan.AnnounceMessage);
                        }
                    }
                    if (e.Text.StartsWith("!goliveannouncement "))
                    {
                        string msg = e.Text.Replace("!goliveannouncement ", "");
                        if (Utilities.CheckOp(e.Source.Name, ircConnection.Channels.First(x => x.Name == CurChan.ChannelName)))
                        {
                            if (config.ChangeAnnounceMessage("liveannouncement", msg, CurChan, target, e.Source, ircConnection))
                                ircConnection.LocalUser.SendNotice(CurChan.ChannelName, "Announce message is now " + msg + ".");
                        }
                           
                    }
                }
                if (e.Text.StartsWith("!titlechangeannouncement"))
                {
                    if (e.Text == "!titlechangeannouncement")
                    {
                        if (String.IsNullOrEmpty(CurChan.ChangedMessage))
                        {

                            ircConnection.LocalUser.SendNotice(CurChan.ChannelName, config.ChangeMessage);
                        }
                        else
                        {
                            ircConnection.LocalUser.SendNotice(CurChan.ChannelName, CurChan.ChangedMessage);
                        }

                    }
                    if (e.Text.StartsWith("!titlechangeannouncement "))
                    {
                        string msg = e.Text.Replace("!titlechangeannouncement ", "");
                        if (Utilities.CheckOp(e.Source.Name, ircConnection.Channels.First(x => x.Name == CurChan.ChannelName)))
                        {
                            if (config.ChangeAnnounceMessage("titlechangeannouncement", msg, CurChan, target, e.Source, ircConnection))
                                ircConnection.LocalUser.SendNotice(CurChan.ChannelName, "Announce message is now " + msg + ".");
                        }

                    }
                }
                if (e.Text.StartsWith("!streamannouncement"))
                {
                    if (e.Text == "!streamannouncement")
                    {
                        if (String.IsNullOrEmpty(CurChan.LiveMessage))
                        {
                            ircConnection.LocalUser.SendNotice(CurChan.ChannelName, config.LiveMessage);
                        }
                        else
                        {
                            ircConnection.LocalUser.SendNotice(CurChan.ChannelName, CurChan.LiveMessage);
                        }
                    }
                    if (e.Text.StartsWith("!streamannouncement "))
                    {
                        string msg = e.Text.Replace("!streamannouncement ", "");
                        if (Utilities.CheckOp(e.Source.Name, ircConnection.Channels.First(x => x.Name == CurChan.ChannelName)))
                        {
                            if (config.ChangeAnnounceMessage("streamannouncement", msg, CurChan, target, e.Source, ircConnection))
                                ircConnection.LocalUser.SendNotice(CurChan.ChannelName, "Announce message is now " + msg + ".");
                        }

                    }
                }
                if (e.Text.StartsWith("!add ") || e.Text.StartsWith("!adduser "))
                {
                    if (Utilities.CheckOp(e.Source.Name,ircConnection.Channels.First(x => x.Name == CurChan.ChannelName)))
                    {
                        bool success = true;
                        string[] users = e.Text.Split(' ');
                        foreach (string s in users)
                        {
                            bool founduser = false;
                            if (s != "!add" && s != "!adduser")
                            {

                                if(SweepingChannels)
                                {
                                    ircConnection.LocalUser.SendNotice(e.Source.Name, "Currently checking twitch API for all users, will add when completed.");
                                }
                                while (SweepingChannels)
                                { }//wait out a channel sweep so we don't modify the collection during it
                                foreach (string existingUser in CurChan.Streamers)
                                {
                                    if (s == existingUser)
                                    {
                                        founduser = true;
                                    }
                                }
                                if (founduser)
                                {
                                    ircConnection.LocalUser.SendNotice(e.Source.Name, "User already exists in watchlist");
                                }
                                else if (!config.AddUser(s, CurChan, target, e.Source, ircConnection))
                                {
                                    success = false;
                                }
                            }
                        }
                        if (success)
                        {
                            ircConnection.LocalUser.SendNotice(e.Source.Name, "User(s) successfully added.");
                        }
                        else
                        {
                            ircConnection.LocalUser.SendNotice(e.Source.Name, "Not all user(s) were successfully added.");
                        }
                    }
                }
                #endregion
                //Console.WriteLine(String.Format("<{0}:{1}> {2}", target, e.Source, e.Text));
            }

        }

        public void ircConnection_Connected(object sender, EventArgs e)
        {

        }
        public void ircConnection_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            Console.WriteLine(String.Format("IRC Error thrown: {0}", e.Error));
        }
        public void ircConnection_Disconnected(object sender, EventArgs e)
        {
            ActiveBot = false;
        }
        public void ircConnection_Error(object sender, IrcErrorEventArgs e)
        {
            Console.WriteLine(String.Format("IRC Error thrown: {0}", e.Error));
            LogWrite(String.Format("IRC Error thrown: {0}", e.Error));
            ActiveBot = false;
        }
        public void ircConnection_NetworkInformationReceived(object sender, EventArgs e)
        {

        }
        public void ircConnection_PingReceived(object sender, IrcPingOrPongReceivedEventArgs e)
        {
            Console.WriteLine(String.Format("Ping received at {0}", DateTime.Now.ToString()));
            LastPingReceived = DateTime.Now;
        }
        public void ircConnection_PongReceived(object sender, IrcPingOrPongReceivedEventArgs e)
        {

            //Console.WriteLine("Pong received.");
        }
        #endregion

    }
}
