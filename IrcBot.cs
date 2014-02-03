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
            LogFile = System.IO.File.Open("./RawLog.log", System.IO.FileMode.OpenOrCreate);
            TwitchLog = System.IO.File.Open("./TwitchLog.Log", System.IO.FileMode.OpenOrCreate);
        }//public static void ConfigureLog()
        public void TwitchLogWrite(string Message)
        {
            Console.WriteLine(Message);
        }//public static void TwitchLogWrite(string Message)
        public void LogWrite(string Message)
        {
            Console.WriteLine(Message);
        }//public static void LogWrite(string Message)
        #endregion
        public void Start()
        {
            if (Utilities.IsDebug())
            {
                debugging = true;
            }
            ConfigureLog();
            config.ParseConfig();
            HandleConnection();
            LoginLoop();
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
                ircConnection.Channels.Join(chan.ChannelName);
                while (curcount + 1 > ircConnection.Channels.Count)
                {
                    if (started.AddSeconds(15) <= DateTime.Now)
                    {
                        ircConnection.Channels.Join(chan.ChannelName);
                        started = DateTime.Now;
                    }

                    // wait out joining
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
                                    addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
                                    bool meetswhitelist = channel.MeetsWhiteBlackList(streamInfo);
                                    if (streamInfo.lastannounce.AddMinutes(30) <= DateTime.Now && meetswhitelist)
                                    {
                                        if (meetswhitelist)
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
                                        addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
                                        if (streamInfo.lastchangeannounce.AddMinutes(15) < DateTime.Now)
                                        {
                                            ircConnection.LocalUser.SendMessage(channel.ChannelName, addToList);
                                            streamInfo.lastchangeannounce = DateTime.Now;
                                        }
                                    }//if (changesmeetwhitelist)
                                    streamInfo.lastannounce = DateTime.Now;
                                }
                            }                            

                        }
                        catch (Exception ex)
                        {

                        }
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

            }//catch (Exception ex)
        }//public static void DoTransform(Channels CurChan, string user)
        public void SendAllLiveList(Channels channel, IIrcMessageSource e)
        {
            bool foundstream = false;
            if (channel.LastLiveAllMessage.AddSeconds(5) <= DateTime.Now)
            {
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
                        addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
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
            else
            {
                ircConnection.LocalUser.SendNotice(e.Name, "There must be 5 seconds inbetween !liveall.  Please try again momentarily.");
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
                    addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
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
        public void SendLiveList(Channels channel, string nick)
        {
            bool foundstream = false;
            bool foundunapprovedstream = false;
            int livestreams = 0;
            foreach (TwitchStuff streamInfo in channel.StreamInfo)
            {
                if (streamInfo.streamerlive == "true")
                {
                    if (channel.MeetsWhiteBlackList(streamInfo))
                        livestreams++;
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
                        addToList = Utilities.TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
                        liveList = addToList.Trim();
                        meetswhitelist = channel.MeetsWhiteBlackList(streamInfo);

                        if (meetswhitelist)
                        {
                            foundstream = true;
                            if (announcetochannel)
                                ircConnection.LocalUser.SendMessage(channel.ChannelName, liveList);//if(announcetochannel)
                            else
                                ircConnection.LocalUser.SendNotice(nick, liveList);//else
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
                        ircConnection.LocalUser.SendMessage(channel.ChannelName, "No one is currently streaming a whitelisted game. Use !liveall to see streams.");
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
            Console.WriteLine(e.Message);
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
            Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }//public static void LocalUser_MessageSent(object sender, IrcMessageEventArgs e)

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
                List<string> helpMsg = new List<string>();
                helpMsg.Add("TwitchBot channel commands:");
                helpMsg.Add("  !live - Retrieves all streaming users who meet any requirements to be shown to the channel.");
                helpMsg.Add("  !liveall - Retrieves all streaming users on the list, period.");
                helpMsg.Add("  !add - Ops only, will allow you to add a user to the channel's watchlist.");
                helpMsg.Add("PM commands:");
                helpMsg.Add(String.Format("  /msg {0} live #channel - Gets all live users for a watched channel that meet requirements to be listed.", config.BotInfo.NickName));
                helpMsg.Add(String.Format("  /msg {0} liveall #channel - Gets all live users for a watched channel.", config.BotInfo.NickName));
                helpMsg.Add(String.Format("  /msg {0} listchannels - Gets all channels being watched.", config.BotInfo.NickName));
                foreach (string s in helpMsg)
                {
                    ircConnection.LocalUser.SendMessage(e.Source.Name, s);
                }//foreach (string s in helpMsg)
            }//else if (cmdargs[0] == "help")
            else if (cmdargs[0] == "listchannels")
            {
                foreach (Channels c in config.TwitchChannels)
                {
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
                        default:
                            break;
                    }
                }//if (foundchannel)
                else
                {
                    ircConnection.LocalUser.SendMessage(e.Source.Name, "No channel by that name is in my watch list.");                    
                }//else
            }//else
            Console.WriteLine(String.Format("<{0}:{1}> {2}", e.Source.Name, e.Targets[0].Name, e.Text));
        }//public static void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        /// <summary>
        /// Handle a notice being received and print it to the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            Console.WriteLine(String.Format("<{0}:{1}> {2}", e.Source, e.Targets[0].Name, e.Text));
        }//public static void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)

        void LocalUser_NoticeSent(object sender, IrcMessageEventArgs e)
        {
            Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }//static void LocalUser_NoticeSent(object sender, IrcMessageEventArgs e)
        void Program_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }//static void Program_NoticeReceived(object sender, IrcMessageEventArgs e)

        public void Program_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            // I believe in most cases this should be just one element, but it is an array in IrcDotNet's definitions so we'll iterate it anyway.
            foreach (IIrcMessageTarget target in e.Targets)
            {

                Channels CurChan = null;
                foreach (Channels c in config.TwitchChannels)
                {
                    if (target.Name == c.ChannelName)
                        CurChan = c;
                }//foreach (Channels c in config.TwitchChannels)

                #region Lives

                if (e.Text.ToLower().StartsWith("!liveall"))
                {
                    SendAllLiveList(CurChan, e.Source);
                }
                else if (e.Text.ToLower().StartsWith("!live"))
                {
                    SendLiveList(CurChan, e.Source.Name);
                }
                #endregion

                #region Transform
                if (e.Text.StartsWith(".transform") && LastTransform.AddSeconds(15) < DateTime.Now)
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
                            ircConnection.LocalUser.SendMessage(e.Source.Name, sbAssembleWatchList.ToString());                            
                            sbAssembleWatchList.Clear();
                        }//if (sbAssembleWatchList.Length > 180)
                    }//foreach(string s in CurChan.Streamers)
                    if (sbAssembleWatchList.Length > 0)
                    {
                        ircConnection.LocalUser.SendMessage(e.Source.Name, sbAssembleWatchList.ToString());
                        sbAssembleWatchList.Clear();                        
                    }//if (sbAssembleWatchList.Length > 0)
                }//if (e.Text.StartsWith("!watching"))
                #region "Config modifications"
                if (e.Text.StartsWith("!remove"))
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
                if (e.Text.StartsWith("!add"))
                {
                    bool success = true;
                    string[] users = e.Text.Split(' ');
                    foreach (string s in users)
                    {
                        bool founduser = false;
                        if (s != "!add")
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
                            if (founduser)
                            {
                                ircConnection.LocalUser.SendMessage(target, "User already exists in watchlist");
                            }
                            else if (!config.AddUser(s, CurChan, target, e.Source, ircConnection))
                            {
                                success = false;
                            }
                        }
                    }
                    if (success)
                    {
                        ircConnection.LocalUser.SendMessage(target, "User(s) successfully added.");
                    }
                    else
                    {
                        ircConnection.LocalUser.SendMessage(target, "Not all user(s) were successfully added.");
                    }
                }
                #endregion
                Console.WriteLine(String.Format("<{0}:{1}> {2}", target, e.Source, e.Text));
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

            Console.WriteLine("Pong received.");
        }
        #endregion

    }
}
