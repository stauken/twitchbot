using System;
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
        public Boolean UsePassword = false;
        public Int32 ServerPort = 6667;
        public IrcUserRegistrationInfo BotInfo = new IrcUserRegistrationInfo();
        public string ServerName = "";
        public List<Channels> TwitchChannels = new List<Channels>();
        public string BaseMessageStartStreaming = "";
        public string LiveMessage = "";
        public string ChangeMessage = "";
        public List<string> GlobalBlacklist = new List<string>();
        public string FileName = "./XMLFile1.xml";
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
                        }
                    }
                    // config writing
                    XElement newStreamer = new XElement("streamer");
                    newStreamer.SetAttributeValue("value", username);
                    XElement xElem = ConfigDocument.Descendants("servers").FirstOrDefault().Descendants("server").FirstOrDefault().Elements("channel").First(x => x.Attribute("id").Value == Watch.ChannelName);
                    xElem.Descendants("streamers").FirstOrDefault().Add(newStreamer);
                    TwitchStuff twitchInfo = new TwitchStuff();
                    twitchInfo.UpdateInfo(username);
                    Watch.Streamers.Add(username);
                    Watch.StreamInfo.Add(twitchInfo);
                    returnvalue = true;
                    ConfigDocument.Save(FileName);
                }
                catch (Exception ex)
                {
                    returnvalue = false;
                }
            }
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
                string test = "";
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
                        test = "";
                        try
                        {
                            string twitchid = streamer.Attribute("value").Value.ToString();
                            StreamInfo = new TwitchStuff();
                            Streamers.Add(twitchid);
                            if (StreamInfo.UpdateInfo(twitchid) != false)
                            {
                                
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
                            AllStreamers.Add(StreamInfo.streamername, StreamInfo);
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
