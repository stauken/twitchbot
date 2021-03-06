﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrcDotNet;
using System.Xml;
using System.Xml.Linq;
namespace TwitchBot
{
    
    public class ConfigurationReader
    {
        public bool ModifyingConfig = false;
        public string BotUsername = "";
        public string BotRealname = "";
        public string BotPassword = "";
        public string ConnectionString = "";
        public bool UseDB = false;
        public Boolean UsePassword = false;
        public Int32 ServerPort = 6667;
        public IrcUserRegistrationInfo BotInfo = new IrcUserRegistrationInfo();
        public string ServerName = "";
        public List<Channels> TwitchChannels = new List<Channels>();
        public string BaseMessageStartStreaming = "";
        public string LiveMessage = "";
        public string ChangeMessage = "";
        public List<string> GlobalBlacklist = new List<string>();
        public string OwnerIdentity = String.Empty;
        public bool HasOwner = false;
        public string FileName = "./Configuration.xml";
        public Dictionary<string, TwitchStuff> AllStreamers = new Dictionary<string, TwitchStuff>();
        /// <summary>
        /// List of all server nodes in the configuration file.
        /// </summary>
        public List<XElement> ServerNodes = new List<XElement>();
        /// <summary>e
        /// List of all channel nodes by given server id value.
        /// </summary>
        public Dictionary<string,List<XElement>> ChannelNodes = new Dictionary<string, List<XElement>>();
        /// <summary>
        /// List of all streamer nodes for a given channel
        /// </summary>
        public Dictionary<string, List<XElement>> StreamerNodes = new Dictionary<string, List<XElement>>();
        /// <summary>
        /// List of each xml node that has a streamer attached
        /// </summary>
        public Dictionary<string, XElement> StreamersNodes = new Dictionary<string, XElement>();
        public XDocument ConfigDocument = new XDocument();
        public XmlReader ReadConfig;
        public XmlWriter WriteConfig;
        public bool RemoveUser(string username, Channels ChannelWatch, IIrcMessageTarget target, IIrcMessageSource source, IrcClient ircConnection)
        {
            ModifyingConfig = true;
            bool returnvalue = false;
            IrcChannel findChannel = null;
            foreach (IrcChannel ircChannel in ircConnection.Channels)
            {
                if (ircChannel.Name == target.Name)
                {
                    findChannel = ircChannel;
                }
            }
            if (Utilities.CheckOp(source.Name, findChannel))
            {
                try
                {
                    Channels Watch = new Channels();
                    foreach (Channels c in TwitchChannels)
                    {
                        if (findChannel.Name == c.ChannelName)
                        {
                            Watch = c;
                        }//if (findChannel.Name == c.ChannelName)
                    }//foreach (Channels c in TwitchChannels)
                    Watch.Streamers.Remove(username);
                    TwitchStuff twitchInfo = new TwitchStuff();
                    foreach (TwitchStuff huntInfo in Watch.StreamInfo)
                    {
                        if (huntInfo.streamername == username)
                        {
                            twitchInfo = huntInfo;
                        }//if (huntInfo.streamername == username)
                    }//foreach (TwitchStuff huntInfo in Watch.StreamInfo)
                    Watch.StreamInfo.Remove(twitchInfo);
                    // config writing
                    if(AllStreamers.Keys.Contains(username))
                    {
                        AllStreamers.Remove(username);
                    }

                    XElement chanNode = ConfigDocument.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == Watch.ChannelName);
                    XElement streamercheck = chanNode.Element("streamers").Descendants("streamer").First(x => x.Attribute("value").Value == username);
                    if (streamercheck != null)
                        streamercheck.Remove();
                    
                    returnvalue = true;
                    ConfigDocument.Save(FileName);
                }//try
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: " + ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    returnvalue = false;
                }//catch
            }
            ModifyingConfig = false;
            return returnvalue;
        }
        public bool SetNotice(string username, Channels ChannelWatch, IIrcMessageTarget target, IIrcMessageSource source, IrcClient ircConnection, bool SetNoticeValue)
        {
            ModifyingConfig = true;
            bool returnvalue = false;
            IrcChannel findChannel = null;
            foreach (IrcChannel ircChannel in ircConnection.Channels)
            {
                if (ircChannel.Name == target.Name)
                {
                    findChannel = ircChannel;
                }
            }
            if (ChannelWatch.Streamers.Contains(username))
            {
                try
                {                                        
                    TwitchStuff twitchInfo = new TwitchStuff();
                    foreach (TwitchStuff huntInfo in ChannelWatch.StreamInfo)
                    {
                        if (huntInfo.streamername == username)
                        {
                            twitchInfo = huntInfo;
                        }//if (huntInfo.streamername == username)
                    }//foreach (TwitchStuff huntInfo in Watch.StreamInfo)
                    XElement chanNode = ConfigDocument.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == ChannelWatch.ChannelName);
                    XElement streamercheck = chanNode.Element("streamers").Descendants("streamer").First(x => x.Attribute("value").Value == username);
                    if (streamercheck != null)
                    {
                        if (SetNoticeValue)
                        {
                            try
                            {
                                streamercheck.Attribute("setnotice").SetValue("true");
                            }
                            catch { 
                                streamercheck.Add(new XAttribute("setnotice", "true"));
                            }
                            twitchInfo.setnotice = true;
                        }
                        else { 
                            streamercheck.Attribute("setnotice").SetValue("false");
                            twitchInfo.setnotice = false;
                        }
                    }
                    returnvalue = true;
                    ConfigDocument.Save(FileName);
                }//try
                catch (Exception ex)
                {
                    returnvalue = false;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: " + ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }//catch
            }
            else
            {
                ircConnection.LocalUser.SendNotice(source.Name, "You are not on the streamer list.");
            }
            ModifyingConfig = false;
            return returnvalue;
        }

        public bool AddUser(string username, Channels ChannelWatch, IIrcMessageTarget target, IIrcMessageSource source, IrcClient ircConnection)
        {
            ModifyingConfig = true;
            bool returnvalue = false;
            IrcChannel findChannel = null;
            foreach (IrcChannel ircChannel in ircConnection.Channels)
            {
                if(ircChannel.Name == target.Name)
                {
                    findChannel = ircChannel;
                }
            }
            if (Utilities.CheckOp(source.Name, findChannel))
            {
                try
                {
                    Channels Watch = new Channels();
                    foreach (Channels c in TwitchChannels)
                    {
                        if (findChannel.Name == c.ChannelName)
                        {
                            Watch = c;
                        }//if (findChannel.Name == c.ChannelName)
                    }//foreach (Channels c in TwitchChannels)
                    // config writing
                    XElement newStreamer = new XElement("streamer");
                    newStreamer.SetAttributeValue("value", username);
                    XElement xElem = ConfigDocument.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == Watch.ChannelName);
                    xElem.Descendants("streamers").FirstOrDefault().Add(newStreamer);
                    TwitchStuff twitchInfo = new TwitchStuff();
                    twitchInfo.UpdateInfo(username,this);
                    Watch.Streamers.Add(username);
                    Watch.StreamInfo.Add(twitchInfo);
                    returnvalue = true;
                    ConfigDocument.Save(FileName);
                }//try
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: " + ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    returnvalue = false;
                }
            }
            ModifyingConfig = false;
            return returnvalue;            
        }
        public bool SetWhiteList(bool value, Channels ChannelWatch, IIrcMessageTarget target, IIrcMessageSource source, IrcClient ircConnection)
        {
            ModifyingConfig = true;
            bool returnvalue = false;
            XElement xElem = ConfigDocument.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == ChannelWatch.ChannelName);
            if (xElem.Attributes("usewhitelist") == null)
            {
                XAttribute whiteList = new XAttribute("usewhitelist", value.ToString());
                xElem.Add(whiteList);
            }
            else
            {
                if (value)
                {
                    xElem.Attribute("usewhitelist").SetValue("true");
                    ChannelWatch.UseWhiteList = true;
                }
                else
                {
                    xElem.Attribute("usewhitelist").SetValue("false");
                    ChannelWatch.UseWhiteList = false;
                }
            }
            returnvalue = true;
            ConfigDocument.Save(FileName);
            ModifyingConfig = false;
            return returnvalue;            
        }
        public bool AddWhiteList(string game, Channels ChannelWatch, IIrcMessageTarget target, IIrcMessageSource source, IrcClient ircConnection)
        {
            ModifyingConfig = true;
            bool returnvalue = false;
            XElement xElem = ConfigDocument.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == ChannelWatch.ChannelName);
            XElement whiteList = new XElement("whitelist");
            if(xElem.Descendants("whitelist").Count() > 0)
            {
                whiteList = xElem.Descendants("whitelist").FirstOrDefault();
            }
            else
            {
                xElem.Add(whiteList);
            }
            XElement newGame = new XElement("game");
            XAttribute newGameAttr = new XAttribute("name", game);
            newGame.Add(newGameAttr);
            whiteList.Add(newGame);
            ChannelWatch.WhiteList.Add(game);
            returnvalue = true;
            ConfigDocument.Save(FileName);
            ModifyingConfig = false;
            return returnvalue;
        }
        public bool ChangeAnnounceMessage(string MsgTitle, string newMsg, Channels ChannelWatch, IIrcMessageTarget target, IIrcMessageSource source, IrcClient ircConnection)
        {
            ModifyingConfig = true;
            bool returnvalue = false;
            XElement xElem = ConfigDocument.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == ChannelWatch.ChannelName);
            XElement announce = new XElement(MsgTitle);
            if (xElem.Descendants(MsgTitle).Count() > 0)
            {
                announce = xElem.Descendants(MsgTitle).FirstOrDefault();
            }            
            announce.SetValue(newMsg);
            if (xElem.Descendants(MsgTitle).Count() == 0)
            {
                xElem.Add(announce);
            }
            ChannelWatch.AnnounceMessage = newMsg;
            //ChannelWatch.WhiteList.Add(game);
            returnvalue = true;
            ConfigDocument.Save(FileName);
            ModifyingConfig = false;
            return returnvalue;
        }

        // this is a hot mess, trying to improve it
        public void ParseConfig()
        {
            ConfigDocument = XDocument.Load(FileName);
            XElement serversNode = ConfigDocument.Root;
            foreach (XElement serverNode in serversNode.Descendants("server"))
            {
                ServerNodes.Add(serverNode);
                try
                {
                    string updateToDB = serverNode.Attribute("updatedb").Value;
                    if (updateToDB == "true")
                    {
                        this.ConnectionString = serverNode.Attribute("ConnString").Value;
                        this.UseDB = true;
                    }
                }
                catch (Exception ex)
                {

                }
                XElement userinfo = serverNode.Descendants("userinfo").FirstOrDefault();
                BotUsername = userinfo.Descendants("nick").FirstOrDefault().Value;
                BotRealname = userinfo.Descendants("name").FirstOrDefault().Value;
                try 
                { 
                    OwnerIdentity = userinfo.Descendants("ownerident").FirstOrDefault().Value;
                }
                catch
                {
                    OwnerIdentity = "";
                }
                try { 
                    HasOwner = Convert.ToBoolean(userinfo.Descendants("hasowner").FirstOrDefault().Value);
                }
                catch
                {
                    HasOwner = false;
                }
                try
                {
                    UsePassword = Convert.ToBoolean(userinfo.Descendants("needsserverpassword").FirstOrDefault().Value.ToString());
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: " + ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    UsePassword = false;
                }//catch (Exception ex)
                BotInfo.NickName = BotUsername;
                BotInfo.UserName = BotUsername;
                BotInfo.RealName = BotRealname;
                //BotInfo.Password = userinfo.Descendants("userpassword").FirstOrDefault().Value;     
                BotInfo.Password = ServerNodes.Descendants("serverpassword").FirstOrDefault().Value;
                ServerName = serverNode.Attribute("id").Value;
                Console.WriteLine("Server pointed to: " + ServerName);
                List<TwitchStuff> StreamersList = new List<TwitchStuff>();
                TwitchStuff StreamInfo = new TwitchStuff();
                LiveMessage = serversNode.Elements("liveannouncement").FirstOrDefault().Attribute("value").Value.ToString();
                string valuecheck = serversNode.Elements("liveannouncement").FirstOrDefault().Value;
                XElement serversubnode = serversNode.Elements("streamannouncement").FirstOrDefault();
                BaseMessageStartStreaming = serversubnode.Attribute("value").Value.ToString();
                ChangeMessage = serversNode.Elements("titlechangeannouncement").FirstOrDefault().Attribute("value").Value.ToString();                
                List<XElement> channelNodeElements = new List<XElement>();
                foreach (XElement channelNode in serverNode.Descendants("channel"))
                {

                    List<XElement> StreamerNodes = new List<XElement>();
                    channelNodeElements.Add(channelNode);
                    #region TODO look into better error handling for this shit lol -- Pull channel config values                                        
                    valuecheck = "";
                    Channels channelMonitor = new Channels();
                    try
                    {
                        valuecheck = channelNode.Descendants("liveannouncement").FirstOrDefault().Attribute("value").Value.ToString();
                    }//try
                    catch
                    {
                        valuecheck = "";
                    }//catch
                    if (valuecheck != "") // custom announcements inside the channel, let's use them instead.
                    {
                        channelMonitor.LiveMessage = valuecheck;
                    }//if (valuecheck != "")

                    valuecheck = "";
                    try
                    {
                        valuecheck = channelNode.Attribute("useinfo").Value.ToString();
                    }
                    catch
                    {
                        valuecheck = "";
                    }
                    if (valuecheck != "")
                    {
                        channelMonitor.InfoCommands = Convert.ToBoolean(valuecheck);
                    }
                    try
                    {
                        valuecheck = channelNode.Attribute("").Value.ToString();
                    }
                    catch
                    {
                        valuecheck = "";
                    }
                    if (valuecheck != "")
                    {
                        channelMonitor.Mystery = Convert.ToBoolean(valuecheck);
                    }

                    try
                    {
                        valuecheck = channelNode.Descendants("streamannouncement").FirstOrDefault().Attribute("value").Value.ToString();
                    }//try
                    catch
                    {
                        valuecheck = "";
                    }//catch
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
                        valuecheck = channelNode.Attribute("usewhitelist").Value.ToString();
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
                    #endregion
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
                    foreach (XElement streamers in channelNode.Elements("streamers"))
                    {
                        StreamersNodes.Add(channelMonitor.ChannelName, streamers);
                    }                    
                    foreach (XElement streamer in channelNode.Elements("streamers").Elements("streamer").ToList())
                    {
                        StreamerNodes.Add(streamer);                                                
                        try
                        {                            
                            string twitchid = streamer.Attribute("value").Value.ToString();
                            Streamers.Add(twitchid);
                            bool setnoticevalue = false;
                            try
                            {
                                setnoticevalue = Convert.ToBoolean(streamer.Attribute("setnotice").Value.FirstOrDefault());
                            }
                            catch
                            {
                                setnoticevalue = false;
                            }
                            StreamInfo = new TwitchStuff();
                            StreamInfo.setnotice = setnoticevalue;
                            if (StreamInfo.UpdateInfo(twitchid,this) != false)
                            {
                                if (StreamInfo.streamerlive == "true")
                                {
                                    
                                }
                            }
                            else
                            {
                                StreamInfo = new TwitchStuff(twitchid);
                                Console.WriteLine("Adding offline stream info for: " + StreamInfo.streamername);
                            }//else
                            
                            channelMonitor.StreamInfo.Add(StreamInfo);
                            channelMonitor.Streamers.Add(StreamInfo.streamername);
                            if(!AllStreamers.Keys.Contains(StreamInfo.streamername))
                                AllStreamers.Add(StreamInfo.streamername, StreamInfo);
                        }//try
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("ERROR: " + ex.Message);
                            Console.ForegroundColor = ConsoleColor.Gray;
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
