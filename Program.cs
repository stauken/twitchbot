using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrcDotNet;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace TwitchSnitch
{
    public class Program
    {
        #region Miscellaneous properties
        public static string BotUsername = "";
        public static string BotRealname = "";
        public static string BotPassword = "";
        public static bool InSRL = false;
        public static Boolean UsePassword = false;
        public static Int32 ServerPort = 6667;
        public static IrcClient ircConnection = new IrcClient();
        public static IrcUserRegistrationInfo BotInfo = new IrcUserRegistrationInfo();
        public static string ServerName = "";
        public static bool ActiveBot = true;
        public static string BaseMessageStartStreaming = "";
        public static string LiveMessage = "";
        public static string ChangeMessage = "";
        public static List<Channels> TwitchChannels = new List<Channels>();
        public static Dictionary<string, TwitchStuff> AllStreamers = new Dictionary<string, TwitchStuff>();
        public static DateTime LastPingSent = DateTime.Now;
        public static DateTime LastTransform = DateTime.Now;
        //public static DateTime LastLiveCommand = DateTime.Now.AddMinutes(-3);
        public static Boolean FullyJoined = false;
        public static DateTime LastPingReceived = DateTime.Now;
        public static DateTime LastFullUpdate = DateTime.Now;
        public static System.IO.FileStream LogFile;
        public static System.IO.FileStream TwitchLog;
        #endregion
        public static string TemplateString(string addToList, string streamername, string game, string viewercount, string streamname)
        {
            addToList = addToList.Replace("\n", "");
            addToList = addToList.Replace("$n", streamername);
            addToList = addToList.Replace("$g", game);
            addToList = addToList.Replace("$v", viewercount);
            addToList = addToList.Replace("$t", streamname);
            //addToList = addToList.Replace("\\x03", 0x03.ToString());
            addToList = addToList.Replace("$10", "\x03" + "10");
            addToList = addToList.Replace("$11", "\x03" + "11");
            addToList = addToList.Replace("$12", "\x03" + "12");
            addToList = addToList.Replace("$13", "\x03" + "13");
            addToList = addToList.Replace("$14", "\x03" + "14");
            addToList = addToList.Replace("$15", "\x03" + "15");
            addToList = addToList.Replace("$1", "\x03" + "01");
            addToList = addToList.Replace("$2", "\x03" + "02");
            addToList = addToList.Replace("$3", "\x03" + "03");
            addToList = addToList.Replace("$4", "\x03" + "04");
            addToList = addToList.Replace("$5", "\x03" + "05");
            addToList = addToList.Replace("$6", "\x03" + "06");
            addToList = addToList.Replace("$7", "\x03" + "07");
            addToList = addToList.Replace("$8", "\x03" + "08");
            addToList = addToList.Replace("$9", "\x03" + "09");
            addToList = addToList.Replace("$reset", "\x03");
            addToList = addToList.Replace("$x", "\x03");

            return addToList;
        }
        #region "Log stuff"
        public static void ConfigureLog()
        {
            LogFile = System.IO.File.Open("./RawLog.log", System.IO.FileMode.OpenOrCreate);
            TwitchLog = System.IO.File.Open("./TwitchLog.Log", System.IO.FileMode.OpenOrCreate);
        }//public static void ConfigureLog()
        public static void TwitchLogWrite(string Message)
        {
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(Message);
            TwitchLog.Write(bytes, 0, bytes.Length);
            TwitchLog.Write(UTF8Encoding.UTF8.GetBytes("\r\n"), 0, UTF8Encoding.UTF8.GetBytes("\r\n").Length);
            TwitchLog.Flush();

#if Debug
            Console.WriteLine(Message);
#endif
        }//public static void TwitchLogWrite(string Message)

        public static void LogWrite(string Message)
        {
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(Message);
            LogFile.Write(bytes, 0, bytes.Length);
            LogFile.Flush();
#if Debug
            Console.WriteLine(Message);
#endif
        }//public static void LogWrite(string Message)
        #endregion
        #region Entry point and main loop
        static void Main(string[] args)
        {
            ConfigureLog();
            ParseConfig();
            HandleConnection();
            LoginLoop();
            MainLoop();
        }//static void Main(string[] args)
        public static void MainLoop()
        {
            bool LiveOnServer = false;
            while (!LiveOnServer)
            {
                if (ircConnection.LocalUser.IsOnline && ircConnection.IsConnected && ircConnection.IsRegistered)
                {
                    RegisteredChannels();
                    LiveOnServer = true;
                }

            }
            while (ActiveBot)
            {
                Object syncRoot = new object();
                if (FullyJoined)
                {
                    //SweepChannels();
                    lock (TwitchChannels)
                    {
                        System.Threading.Thread doWork = new System.Threading.Thread(new System.Threading.ThreadStart(SweepChannels));
                        doWork.Start();
                        while (doWork.ThreadState != System.Threading.ThreadState.Stopped)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }
                
            }
            if (!ActiveBot)
            {
                TwitchLog.Close();
                LogFile.Close();
            }
        }//public static void MainLoop()
        public static void HandleConnection()
        {
            ircConnection.Connected += new EventHandler<EventArgs>(ircConnection_Connected);
            ircConnection.ConnectFailed += new EventHandler<IrcErrorEventArgs>(ircConnection_ConnectFailed);
            ircConnection.Disconnected += new EventHandler<EventArgs>(ircConnection_Disconnected);
            ircConnection.Error += new EventHandler<IrcErrorEventArgs>(ircConnection_Error);
            ircConnection.NetworkInformationReceived += new EventHandler<EventArgs>(ircConnection_NetworkInformationReceived);
            ircConnection.PingReceived += new EventHandler<IrcPingOrPongReceivedEventArgs>(ircConnection_PingReceived);
            ircConnection.PongReceived += new EventHandler<IrcPingOrPongReceivedEventArgs>(ircConnection_PongReceived);
            ircConnection.ProtocolError += new EventHandler<IrcProtocolErrorEventArgs>(ircConnection_ProtocolError);            
            ircConnection.Connect(ServerName, 6667, false, BotInfo);
        }
        public static void SweepChannels()
        {

            if (LastFullUpdate < DateTime.Now)
            {
                LastFullUpdate = DateTime.Now.AddMinutes(2);
                foreach (Channels channel in TwitchChannels)
                {
                    foreach (TwitchStuff streamInfo in channel.StreamInfo)
                    {
                        TwitchAPIInterface getInfo = new TwitchAPIInterface();
                        TwitchLogWrite(String.Format("Updating stream info for: {0}", streamInfo.streamername));
                        try
                        {
                            getInfo.GetResponse(streamInfo.streamername);
                            streamInfo.lastrefresh = DateTime.Now;
                            string test = getInfo.Data["stream"].ToString();
                            if (test != "")
                            {
                                string streamname = getInfo.Data["stream"]["channel"]["status"].ToString();
                                string streamviewers = getInfo.Data["stream"]["viewers"].ToString();
                                string streamgame = getInfo.Data["stream"]["game"].ToString();
                                streamInfo.streamerviewcount = streamviewers;
                                streamInfo.lastrefresh = DateTime.Now;
                                if (streamname != streamInfo.streamname && streamgame != streamInfo.game)
                                {
                                    streamInfo.streamname = streamname;
                                    streamInfo.game = streamgame;
                                    string addToList = "";
                                    if (streamInfo.streamerlive == "false")
                                    {
                                        streamInfo.streamerlive = "true";
                                        if (channel.LiveMessage != "")
                                        {
                                            addToList = channel.LiveMessage.Trim();
                                        }
                                        else{
                                            addToList = LiveMessage.Trim();
                                        }
                                        addToList = TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
                                        bool meetswhitelist = true;
                                        if (channel.UseWhiteList)
                                        {
                                            meetswhitelist = false;
                                            foreach (string s in channel.WhiteList)
                                            {
                                                if (streamInfo.game.Contains(s))
                                                    meetswhitelist = true;
                                            }
                                        }
                                        else if (channel.UseBlackList)
                                        {
                                            foreach (string s in channel.BlackList)
                                            {
                                                if (streamInfo.game.Contains(s))
                                                    meetswhitelist = false;
                                                if (streamInfo.streamname.Contains(s))
                                                    meetswhitelist = false;
                                            }
                                        }
                                        if (streamInfo.lastannounce.AddMinutes(30) <= DateTime.Now && meetswhitelist)
                                        {
                                            ircConnection.LocalUser.SendMessage(channel.ChannelName, addToList);

                                        }
                                        streamInfo.lastannounce = DateTime.Now;
                                    }
                                    else
                                    {
                                        bool changesmeetwhitelist = true;
                                        if (channel.UseWhiteList)
                                        {
                                            changesmeetwhitelist = false;
                                            foreach (string s in channel.WhiteList)
                                            {
                                                if (streamInfo.game.Contains(s))
                                                    changesmeetwhitelist = true;
                                            }
                                        }
                                        else if (channel.UseBlackList)
                                        {
                                            foreach (string s in channel.WhiteList)
                                            {
                                                if (streamInfo.game.Contains(s))
                                                {
                                                    changesmeetwhitelist = false;
                                                }
                                                if (streamInfo.streamname.Contains(s))
                                                {
                                                    changesmeetwhitelist = false;
                                                }
                                            }
                                        }

                                        if (changesmeetwhitelist)
                                        {
                                            if (channel.ChangedMessage != "")
                                            {
                                                addToList = channel.ChangedMessage;
                                            }
                                            else
                                            {

                                                addToList = ChangeMessage.Trim();
                                            }
                                            addToList = TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
                                            ircConnection.LocalUser.SendMessage(channel.ChannelName, addToList);
                                        }
                                        streamInfo.lastannounce = DateTime.Now;
                                    }
                                }
                            }
                            else
                            {
                                streamInfo.streamerviewcount = "";
                                streamInfo.streamname = "";
                                streamInfo.lastrefresh = DateTime.Now;
                                streamInfo.game = "";
                                streamInfo.streamerlive = "false";
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }
        public static void LoginLoop()
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
        public static void RegisteredChannels()
        {
            DateTime started = DateTime.Now;
            int curcount = ircConnection.Channels.Count;
            foreach (Channels chan in TwitchChannels)
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
#if !Debug
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
#endif
            FullyJoined = true;
        }
        #endregion

        /// <summary>
        /// NickServ Communications
        /// </summary>
        /// <param name="e"></param>
        public static void HandleNickserv(IrcMessageEventArgs e)
        {
            if (e.Text.Contains("Password accepted") || e.Text.Contains("You are already identified"))
            {

            }//if (e.Text.Contains("Password accepted") || e.Text.Contains("You are already identified"))
        }//public static void HandleNickserv(IrcMessageEventArgs e)

        public static void SendAllLiveList(Channels channel, IIrcMessageSource e)
        {
            bool foundstream = false;
            if (channel.LastLiveAllMessage.AddSeconds(3) <= DateTime.Now)
            {
                channel.LastLiveAllMessage = DateTime.Now;
                string liveList = "";
                foreach (TwitchStuff streamInfo in channel.StreamInfo)
                {
                    bool meetswhitelist = true;
                    if (streamInfo.streamerlive == "true")
                    {
                        string addToList = LiveMessage;
                        if (channel.LiveMessage != "")
                        {
                            addToList = channel.LiveMessage;
                        }
                        addToList = TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
                        liveList = addToList.Trim();
                        if (channel.UseWhiteList)
                        {
                            meetswhitelist = true;
                            foreach (string s in channel.WhiteList)
                            {
                                if (streamInfo.game.Contains(s))
                                    meetswhitelist = true;
                            }
                            if (meetswhitelist)
                            {
                                foundstream = true;
                                ircConnection.LocalUser.SendMessage(e.Name, liveList);
                            }
                        }
                        if (channel.UseBlackList)
                        {
                            foreach (string s in channel.BlackList)
                            {
                                if (streamInfo.game.Contains(s))
                                    meetswhitelist = false;
                            }
                        }
                        if (!channel.UseBlackList && !channel.UseWhiteList)
                        {
                            foundstream = true;
                            ircConnection.LocalUser.SendMessage(e.Name, liveList);
                        }
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
                ircConnection.LocalUser.SendMessage(e.Name, "There must be 3 seconds inbetween !liveall.  Please try again momentarily.");
            }
        }

        public static void SendLiveList(Channels channel)
        {
            bool foundstream = false;
            bool foundunapprovedstream = false;
            if (channel.LastLiveAnnouncement.AddSeconds(15) <= DateTime.Now)
            {
                channel.LastLiveAnnouncement = DateTime.Now;
                string liveList = "";
                foreach (TwitchStuff streamInfo in channel.StreamInfo)
                {
                    bool meetswhitelist = true;
                    if (streamInfo.streamerlive == "true")
                    {
                        string addToList = LiveMessage;
                        if (channel.LiveMessage != "")
                            addToList = channel.LiveMessage;
                        addToList = TemplateString(addToList, streamInfo.streamername, streamInfo.game, streamInfo.streamerviewcount, streamInfo.streamname);
                        liveList = addToList.Trim();
                        if (channel.UseWhiteList)
                        {
                            meetswhitelist = false;
                            foreach (string s in channel.WhiteList)
                            {
                                if (streamInfo.game.Contains(s))
                                    meetswhitelist = true;
                            }
                            if (meetswhitelist)
                            {
                                foundstream = true;
                                ircConnection.LocalUser.SendMessage(channel.ChannelName, liveList);
                            }
                            else
                            {
                                foundunapprovedstream = true;
                            }
                        }
                        else if (channel.UseBlackList)
                        {

                        }
                        else
                        {
                            foundstream = true;
                            ircConnection.LocalUser.SendMessage(channel.ChannelName, liveList);
                        }
                        liveList = "";
                    }
                }
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

        static void ircConnection_RawMessageReceived(object sender, IrcRawMessageEventArgs e)
        {
            if (e.RawContent.Contains("#speedrunslive"))
            {

            }
            else
            {
                LogWrite(e.RawContent);
            }
        }

        static void ircConnection_ProtocolError(object sender, IrcProtocolErrorEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
        public static void Program_SRLMessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (e.Source.Name == "RaceBot")
            {
                // track for races
                foreach (Channels channel in TwitchChannels)
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
        static void Program_UserJoined(object sender, IrcChannelUserEventArgs e)
        {

        }
        public static void LocalUser_MessageSent(object sender, IrcMessageEventArgs e)
        {
            Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }//public static void LocalUser_MessageSent(object sender, IrcMessageEventArgs e)


        public static void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (e.Source.Name == "NickServ")
            {
                // join SRL 
                HandleNickserv(e);
            }//if (e.Source.Name == "NickServ")
            else
            {
                if (e.Text == "!list")
                {
                    StringBuilder ResponseString = new StringBuilder();
                    ResponseString.Append("I can announce all streamers for the following channels:");
                    foreach (Channels tc in TwitchChannels)
                    {
                        ResponseString.Append(String.Format(" {0}", tc.ChannelName));
                    }//foreach (Channels tc in TwitchChannels)
                    ircConnection.LocalUser.SendMessage(e.Source.Name, ResponseString.ToString());
                }//if (e.Text == "!list")
            }//else
            Console.WriteLine(String.Format("<{0}:{1}> {2}", e.Source.Name, e.Targets[0].Name, e.Text));
        }//public static void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)

        /// <summary>
        /// Handle a notice being received and print it to the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            if (e.Source.Name == "NickServ")
            {
                HandleNickserv(e);
            }//if (e.Source.Name == "NickServ")
            Console.WriteLine(String.Format("<{0}:{1}> {2}", e.Source, e.Targets[0].Name, e.Text));
        }//public static void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)

        static void LocalUser_NoticeSent(object sender, IrcMessageEventArgs e)
        {
            Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }
        static void Program_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            Console.WriteLine(String.Format("*{0}* {1}", e.Targets[0].Name, e.Text));
        }

        public static void Program_MessageReceived(object sender, IrcMessageEventArgs e)
        {

            foreach (IIrcMessageTarget target in e.Targets)
            {

                if (e.Text.ToLower().StartsWith("!liveall"))
                {
                    Channels CurChan = null;
                    foreach (Channels c in TwitchChannels)
                    {
                        if (target.Name == c.ChannelName)
                            CurChan = c;
                    }
                    SendAllLiveList(CurChan, e.Source);
                }
                else if (e.Text.ToLower().StartsWith("!live"))
                {
                    Channels CurChan = null;
                    foreach (Channels c in TwitchChannels)
                    {
                        if (target.Name == c.ChannelName)
                            CurChan = c;
                    }
                    SendLiveList(CurChan);
                }
                if (e.Text.StartsWith(".transform ") && LastTransform.AddSeconds(15) < DateTime.Now)
                {
                    Channels CurChan = null;
                    foreach (Channels c in TwitchChannels)
                    {
                        if (target.Name == c.ChannelName)
                            CurChan = c;
                    }

                    LastTransform = DateTime.Now;
                    try
                    {
                        string[] messageSplit = e.Text.Split(' ');
                        if (!String.IsNullOrWhiteSpace(messageSplit[1]))
                        {
                            Random randGen = new Random();
                            int intValue = randGen.Next(1, 7);
                            string message = "";
//                            * S2 transform {0} into funny looking termite!
//* S2 magically change {0} into tiny little bouncing pumpkin!
//* S2 transform {0} into T-Rex. Wait, who am I? Wumba? I change you back.
//* S2 magically change {0} into ...washing machine? That not right. I hope you not go for World Record!
//* S2 magically change {0} into little crocodile! Yes! Mumbo need new shoes! Only kidding...
//* S2 magically change {0} into silly little Bumble Bee!
                            switch (intValue)
                            {
                                case 1:
                                    message = String.Format("\x01" + "ACTION transform {0} into funny looking termite!\x01", messageSplit[1]);
                                    break;
                                case 2:
                                    message = String.Format("\x01" + "ACTION magically change {0} into tiny little bouncing pumpkin!\x01", messageSplit[1]);
                                    break;
                                case 3:
                                    message = String.Format("\x01" + "ACTION transform {0} into T-Rex. Wait, who am I? Wumba? I change you back.\x01", messageSplit[1]);
                                    break;
                                case 4:
                                    message = String.Format("\x01" + "ACTION transforms {0} into a funny looking termite!\x01", messageSplit[1]);
                                    break;
                                case 5:
                                    message = String.Format("\x01" + "ACTION magically change {0} into ...washing machine? That not right. I hope you not go for World Record!\x01", messageSplit[1]);
                                    break;
                                case 6:
                                    message = String.Format("\x01" + "ACTION magically change {0} into little crocodile! Yes! Mumbo need new shoes! Only kidding...\x01", messageSplit[1]);
                                    break;
                                case 7:
                                    message = String.Format("\x01" + "ACTION magically change {0} into silly little Bumble Bee!\x01", messageSplit[1]);
                                    break;
                                default:
                                    message = String.Format("\x01" + "ACTION magically change {0} into silly little Bumble Bee!\x01", messageSplit[1]);
                                    break;
                            }
                            ircConnection.LocalUser.SendMessage(CurChan.ChannelName, message);
                        }
                        //ircConnection.LocalUser.SendMessage(target, "Added user(s)");

                    }
                    catch (Exception ex)
                    {
                        break;
                    }

                }
                if (e.Text.StartsWith("!add"))
                {
                    foreach (IrcChannel ircChan in ircConnection.Channels)
                    {
                        if (ircChan.Name == target.Name)
                        {
                            foreach (IrcChannelUser user in ircChan.Users)
                            {
                                if (user.User.NickName == e.Source.Name && (user.Modes.Contains('o') || user.Modes.Contains('h')))
                                {
                                    // we got the business
                                    string[] addlist = e.Text.Split(' ');
                                    XDocument xDoc = XDocument.Load("./XMLFile1.xml");
                                    try
                                    {
                                        bool changesmade = false;
                                        foreach (string s in addlist)
                                        {

                                            if (s != "!add")
                                            {
                                                foreach (Channels c in TwitchChannels)
                                                {
                                                    if (c.ChannelName == ircChan.Name)
                                                    {
                                                        if (c.Streamers.Contains(s))
                                                        {
                                                            ircConnection.LocalUser.SendMessage(target, "User " + s + " already on streamers list for " + ircChan.Name + ".");
                                                        }
                                                        else
                                                        {
                                                            XElement xElem = xDoc.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == ircChan.Name);
                                                            if (xElem.Attribute("id").Value == ircChan.Name)
                                                            {
                                                                XElement newElement = new XElement("streamer");
                                                                newElement.Add(new XAttribute("value", s));
                                                                xDoc.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == ircChan.Name).Descendants("streamers").FirstOrDefault().Add(newElement);
                                                                changesmade = true;
                                                                try
                                                                {
                                                                    TwitchAPIInterface getTwitch = new TwitchAPIInterface();
                                                                    getTwitch.GetResponse(s);
                                                                    string test = getTwitch.Data["stream"].ToString();
                                                                    if (getTwitch.GetResponse(s) != "false")
                                                                    {
                                                                        TwitchStuff StreamInfo = new TwitchStuff();
                                                                        StreamInfo.streamername = s;
                                                                        StreamInfo.lastannounce = new DateTime();
                                                                        StreamInfo.lastrefresh = DateTime.Now;
                                                                        if (test == "")
                                                                        {
                                                                            StreamInfo.game = "";
                                                                            StreamInfo.streamname = "";
                                                                            StreamInfo.streamerviewcount = "";
                                                                            StreamInfo.streamerlive = "false";
                                                                        }
                                                                        else
                                                                        {

                                                                            string streamname = getTwitch.Data["stream"]["channel"]["status"].ToString();
                                                                            string streamviewers = getTwitch.Data["stream"]["viewers"].ToString();
                                                                            string streamgame = getTwitch.Data["stream"]["game"].ToString();
                                                                            Console.WriteLine(String.Format("Adding streamer  {0} to the monitor list for {1}", s, ircChan.Name));
                                                                            StreamInfo.streamerviewcount = streamviewers;
                                                                            StreamInfo.streamname = streamname;
                                                                            StreamInfo.game = streamgame;
                                                                            StreamInfo.streamerlive = "true";
                                                                        }
                                                                        c.Streamers.Add(s);
                                                                        c.StreamInfo.Add(StreamInfo);
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {

                                                                }

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        if (changesmade)
                                        {
                                            string xDocWrite = xDoc.ToString();
                                            System.IO.File.WriteAllText("./XMLFile1.xml", xDocWrite);
                                            ircConnection.LocalUser.SendMessage(target, "Added user(s)");
                                            //foreach (Channels c in TwitchChannels)
                                            //{
                                            //    if (target.Name == c.ChannelName)
                                            //        SendLiveList(c);
                                            //}
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string test = ex.Message;
                                    }
                                }
                            }
                        }
                    }
                }
                Console.WriteLine(String.Format("<{0}:{1}> {2}", e.Targets[0], e.Source, e.Text));
            }

        }

        public static void ircConnection_Connected(object sender, EventArgs e)
        {

        }
        public static void ircConnection_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            Console.WriteLine(String.Format("IRC Error thrown: {0}", e.Error));
        }
        public static void ircConnection_Disconnected(object sender, EventArgs e)
        {
            ActiveBot = false;
        }
        public static void ircConnection_Error(object sender, IrcErrorEventArgs e)
        {
            Console.WriteLine(String.Format("IRC Error thrown: {0}", e.Error));
            ActiveBot = false;
        }
        public static void ircConnection_NetworkInformationReceived(object sender, EventArgs e)
        {

        }
        public static void ircConnection_PingReceived(object sender, IrcPingOrPongReceivedEventArgs e)
        {
            Console.WriteLine(String.Format("Ping received at {0}", DateTime.Now.ToString()));
            LastPingReceived = DateTime.Now;
        }
        public static void ircConnection_PongReceived(object sender, IrcPingOrPongReceivedEventArgs e)
        {

            Console.WriteLine("Pong received.");
        }
        #endregion
        

        public static void ParseConfig()
        {
            XDocument xDoc = XDocument.Load("XMLFile1.xml");
            XElement serversNode = xDoc.Root;
            foreach (XElement serverNode in serversNode.Descendants("server"))
            {
                XElement userinfo = serverNode.Descendants("userinfo").FirstOrDefault();
                BotUsername = userinfo.Descendants("nick").FirstOrDefault().Value;
                BotRealname = userinfo.Descendants("name").FirstOrDefault().Value;
                try
                {
                    UsePassword = Convert.ToBoolean(userinfo.Descendants("needsserverpassword").FirstOrDefault().Value.ToString());
                }
                catch (Exception ex)
                {
                    UsePassword = false;
                }//catch (Exception ex)
                BotInfo.NickName = BotUsername;
                BotInfo.UserName = BotUsername;
                BotInfo.RealName = BotRealname;
                BotInfo.Password = userinfo.Descendants("userpassword").FirstOrDefault().Value;
                ServerName = serverNode.Attribute("id").Value;
                Console.WriteLine("Server pointed to: " + ServerName);
                List<TwitchStuff> StreamersList = new List<TwitchStuff>();
                TwitchStuff StreamInfo = new TwitchStuff();
                LiveMessage = serversNode.Elements("liveannouncement").FirstOrDefault().Attribute("value").Value.ToString();
                string valuecheck = serversNode.Elements("liveannouncement").FirstOrDefault().Value;
                XElement serversubnode = serversNode.Elements("streamannouncement").FirstOrDefault();
                BaseMessageStartStreaming = serversubnode.Attribute("value").Value.ToString();
                ChangeMessage = serversNode.Elements("titlechangeannouncement").FirstOrDefault().Attribute("value").Value.ToString();
                foreach (XElement channelNode in serverNode.Descendants("channel"))
                {
                    valuecheck = "";
                    Channels channelMonitor = new Channels();
                    try
                    {
                        valuecheck = channelNode.Descendants("liveannouncement").FirstOrDefault().Attribute("value").Value.ToString();
                    }
                    catch
                    {
                        valuecheck = "";
                    }
                    if (valuecheck != "") // custom announcements inside the channel, let's use them instead.
                    {
                        

                        channelMonitor.LiveMessage = valuecheck;
                    }//if (valuecheck != "")
                    
                    valuecheck = "";
                    try
                    {
                    valuecheck = channelNode.Descendants("streamannouncement").FirstOrDefault().Attribute("value").Value.ToString();
                    }
                    catch
                    {
                        valuecheck = "";
                    }
                    if (valuecheck != "") // custom announcements inside the channel, let's use them instead.
                    {
                        channelMonitor.AnnounceMessage = valuecheck;
                    }//if (valuecheck != "")
                    valuecheck = "";
                    try
                    {
                    valuecheck = channelNode.Descendants("titlechangeannouncement").FirstOrDefault().Attribute("value").Value.ToString();
                    }
                    catch
                    {
                        valuecheck = "";
                    }
                    if (valuecheck != "") // custom announcements inside the channel, let's use them instead.
                    {
                        channelMonitor.ChangedMessage = valuecheck;
                    }//if (valuecheck != "")
                    channelMonitor.ChannelName = channelNode.Attribute("id").Value;
                    try
                    {
                        string test = channelNode.Attribute("usewhitelist").Value.ToString();
                        channelMonitor.UseWhiteList = Convert.ToBoolean(channelNode.Attribute("usewhitelist").Value.ToString());
                    }//try
                    catch
                    {
                        channelMonitor.UseWhiteList = false;
                    }//catch
                    try
                    {
                        channelMonitor.UseBlackList = Convert.ToBoolean(channelNode.Attribute("useblacklist").Value.ToString());
                    }//try
                    catch
                    {
                        channelMonitor.UseBlackList = false;
                    }//catch
                    try
                    {
                        channelMonitor.ChannelPassword = channelNode.Attribute("password").Value.ToString();
                    }//try
                    catch
                    {
                        channelMonitor.ChannelPassword = "";
                    }//catch
                    channelMonitor.Streamers = new List<string>();
                    Console.WriteLine("Adding Channel " + channelMonitor.ChannelName + " to the monitor list");
                    List<string> Streamers = new List<string>();
                    TwitchAPIInterface checkTwitch = new TwitchAPIInterface();
                    StreamersList = new List<TwitchStuff>();
                    XElement whiteListInfo = channelNode.Elements("whitelist").FirstOrDefault();
                    if (whiteListInfo != null)
                    {
                        foreach (XElement whitelistitem in whiteListInfo.Elements("game").DefaultIfEmpty())
                        {
                            string gamename = whitelistitem.Attribute("name").Value.ToString();
                            channelMonitor.WhiteList.Add(gamename);
                        }//foreach (XElement whitelistitem in whiteListInfo.Elements("game").DefaultIfEmpty())
                    }//if (whiteListInfo != null)
                    XElement blackListInfo = channelNode.Elements("blacklist").FirstOrDefault();
                    if (blackListInfo != null)
                    {
                        foreach (XElement BlackListitem in blackListInfo.Elements("game").DefaultIfEmpty())
                        {
                            string gamename = BlackListitem.Attribute("name").Value.ToString();
                            channelMonitor.BlackList.Add(gamename);
                        }//foreach (XElement BlackListitem in blackListInfo.Elements("game").DefaultIfEmpty())
                    }//if (blackListInfo != null)

                    foreach (XElement streamer in channelNode.Elements("streamers").Elements("streamer").ToList())
                    {
                        try
                        {
                            string twitchid = streamer.Attribute("value").Value.ToString();
                            StreamInfo = new TwitchStuff();
                            Streamers.Add(twitchid);
                            if (checkTwitch.GetResponse(twitchid) != "false")
                            {
                                string test = checkTwitch.Data["stream"].ToString();
                                if (test != "")
                                {

                                    string streamname = checkTwitch.Data["stream"]["channel"]["status"].ToString();
                                    string streamviewers = checkTwitch.Data["stream"]["viewers"].ToString();
                                    string streamgame = checkTwitch.Data["stream"]["game"].ToString();
                                    Console.WriteLine(String.Format("Adding streamer {0} to the monitor list for {1}", streamer.Attribute("value").Value.ToString(), channelMonitor.ChannelName));

                                    StreamInfo.streamerviewcount = streamviewers;
                                    StreamInfo.streamname = streamname;
                                    StreamInfo.streamername = twitchid;
                                    StreamInfo.lastannounce = new DateTime();
                                    StreamInfo.lastrefresh = DateTime.Now;
                                    StreamInfo.game = streamgame;
                                    StreamInfo.streamerlive = "true";
                                }////if (test != "");
                                else
                                {
                                    StreamInfo = new TwitchStuff();
                                    StreamInfo.streamerviewcount = "";
                                    StreamInfo.streamname = "";
                                    StreamInfo.streamername = twitchid;
                                    StreamInfo.lastannounce = new DateTime();
                                    StreamInfo.lastrefresh = DateTime.Now;
                                    StreamInfo.game = "";
                                    StreamInfo.streamerlive = "false";
                                    Console.WriteLine("Adding offline stream info for: " + StreamInfo.streamername);
                                }//if (test != ""); else;
                            }
                            else
                            {
                                StreamInfo = new TwitchStuff();
                                StreamInfo.streamerviewcount = "";
                                StreamInfo.streamname = "";
                                StreamInfo.streamername = twitchid;
                                StreamInfo.lastannounce = new DateTime();
                                StreamInfo.lastrefresh = DateTime.Now;
                                StreamInfo.game = "";
                                StreamInfo.streamerlive = "false";
                                Console.WriteLine("Adding offline stream info for: " + StreamInfo.streamername);
                                
                            }//else
                            channelMonitor.StreamInfo.Add(StreamInfo);
                            channelMonitor.Streamers.Add(StreamInfo.streamername);
                        }//try
                        catch (Exception ex)
                        {
                            continue;
                        }//catch (Exception ex)
                    }//foreach (XElement streamer in channelNode.Elements("streamers").Elements("streamer").ToList())
                    channelMonitor.LastLiveAnnouncement = DateTime.Now.AddMinutes(-3);
                    TwitchChannels.Add(channelMonitor);
                    List<string> raceList = new List<string>();
                    foreach (XElement race in channelNode.Elements("races").Elements("race").ToList())
                    {
                        channelMonitor.ChannelRaces.Add(race.Attribute("name").Value.ToString());
                        Console.WriteLine("Adding watch for race: " + race.Attribute("name").Value.ToString() + " for channel " + channelMonitor.ChannelName);
                    }//foreach (XElement race in channelNode.Elements("races").Elements("race").ToList())
                }//foreach (XElement channelNode in serverNode.Descendants("channel"))
            }
        }
    }
}
