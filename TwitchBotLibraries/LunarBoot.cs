using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TwitchBot
{
    public class LunarBoot
    {

        public string fileName = "./TextCommands.xml";
        IrcClient client;
        List<ChickenScoreClass> chickenScore = new List<ChickenScoreClass>();
        List<ChickenBetter> cBetters = new List<ChickenBetter>();

        List<BasicTextCommands> textCommands = new List<BasicTextCommands>();

        List<string> userNames = new List<string>();

        XmlDocument xmlDoc = new XmlDocument();
        public XmlReader xmlReader;
        public XmlWriter xmlWriter;

        bool chickenBettingAllowed = false;
        bool xmlLoaded = false;

        #region XML stuff

        void LoadXML()
        {

            fileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            fileName += "\\TextCommands.xml";
            fileName = fileName.Substring(6);
            xmlDoc.Load(fileName);
            XmlNodeList userNodes = xmlDoc.SelectNodes("//DoC/Users");

            foreach (XmlNode userNode in userNodes)
            {
                for (int i = 0; i < userNode.ChildNodes.Count; ++i)
                {
                    userNames.Add(userNode.ChildNodes[i].InnerText);
                    Console.WriteLine(userNode.ChildNodes[i].InnerText);
                }
            }

            for (int i = 0; i < userNames.Count; ++i)
            {
                userNodes = xmlDoc.SelectNodes(string.Format("//DoC/Commands/{0}", userNames[i]));
                foreach (XmlNode userNode in userNodes)
                {
                    if (userNode.ChildNodes[0] != null)
                    {
                        foreach (XmlNode uNode in userNode.ChildNodes)
                        {
                            BasicTextCommands textCMD = new BasicTextCommands();

                            if (uNode.ChildNodes[0].Name == "trigger")
                            {
                                textCMD.triggerName = uNode.ChildNodes[0].InnerText;
                                textCMD.responseText = uNode.ChildNodes[1].InnerText;
                            }
                            else
                            {
                                textCMD.triggerName = uNode.ChildNodes[0].InnerText;
                                textCMD.responseText = uNode.ChildNodes[1].InnerText;
                            }
                            textCMD.channel = userNames[i];
                            textCommands.Add(textCMD);
                        }
                    }
                }
            }

        }

        public void SaveXML()
        {
            XmlDocument xmlDocSave = new XmlDocument();
            XmlNode rootNode = xmlDocSave.CreateElement("DoC");
            XmlNode userNode = xmlDocSave.CreateElement("Users");
            xmlDocSave.AppendChild(rootNode);
            foreach (string n in userNames)
            {
                XmlNode userNameNode = xmlDocSave.CreateElement("Username");
                userNameNode.InnerText = n;
                userNode.AppendChild(userNameNode);
            }

            rootNode.AppendChild(userNode);

            XmlNode commandNode = xmlDocSave.CreateElement("Commands");
            XmlNode[] cmdChannelNode = new XmlNode[userNames.Count + 1];

            for (int i = 0; i < userNames.Count; ++i)
            {
                cmdChannelNode[i] = xmlDocSave.CreateElement(userNames[i]);
            }
            for (int i = 0; i < textCommands.Count; ++i)
            {
                if (textCommands[i].channel[0] == '#')
                {
                    for (int c = 0; c < userNames.Count; ++c)
                    {
                        if (textCommands[i].channel.Substring(1).ToLower() == userNames[c].ToLower())
                        {
                            XmlNode cmdNode = xmlDocSave.CreateElement("cmd");
                            XmlNode cmdTrigger = xmlDocSave.CreateElement("trigger");
                            XmlNode cmdResponse = xmlDocSave.CreateElement("text");
                            cmdTrigger.InnerText = textCommands[i].triggerName;
                            cmdResponse.InnerText = textCommands[i].responseText;
                            cmdNode.AppendChild(cmdTrigger);
                            cmdNode.AppendChild(cmdResponse);
                            cmdChannelNode[c].AppendChild(cmdNode);
                            break;
                        }
                    }
                }
                else
                {
                    for (int c = 0; c < userNames.Count; ++c)
                    {
                        if (textCommands[i].channel.ToLower() == userNames[c].ToLower())
                        {
                            XmlNode cmdNode = xmlDocSave.CreateElement("cmd");

                            XmlNode cmdTrigger = xmlDocSave.CreateElement("trigger");
                            XmlNode cmdResponse = xmlDocSave.CreateElement("text");
                            cmdTrigger.InnerText = textCommands[i].triggerName;
                            cmdResponse.InnerText = textCommands[i].responseText;
                            cmdNode.AppendChild(cmdTrigger);
                            cmdNode.AppendChild(cmdResponse);
                            cmdChannelNode[c].AppendChild(cmdNode);
                            break;
                        }
                    }
                }

            }
            for (int i = 0; i < userNames.Count; ++i)
            {
                commandNode.AppendChild(cmdChannelNode[i]);
            }
            rootNode.AppendChild(commandNode);
            xmlDocSave.Save(fileName);

        }

        #endregion

        public void CheckMessages(IrcClient c, Channels curChan, IrcMessageEventArgs e)
        {
            if (!xmlLoaded)
            {
                LoadXML();
                xmlLoaded = true;
            }
            if (client == null)
                client = c;


            bool commandFound = false;
            if (e.Text[0] == '!')
            {
                foreach (BasicTextCommands t in textCommands)
                {
                    string chan;
                    if (t.channel[0] == '#')
                        chan = t.channel;
                    else
                        chan = string.Format("#{0}", t.channel);
                    if (chan == curChan.ChannelName || t.channel == "#anychannel")
                    {
                        if (e.Text.ToLower() == t.triggerName.ToLower())
                        {
                            client.LocalUser.SendMessage(curChan.ChannelName, t.responseText);
                            commandFound = true;
                            break;
                        }
                    }
                }
                if (!commandFound)
                {
                    if (curChan.ChannelName == "#sakegeist")
                    {
                        SakegeistCommands(curChan, e);
                    }
                }

                if (e.Source.Name.ToLower() == "moonizz" || e.Source.Name.ToLower() == curChan.ChannelName.Substring(1).ToLower())
                {
                    if (e.Text.ToLower().StartsWith("!add "))
                    {
                        string[] splitText = e.Text.Split('\"');
                        if (splitText.Length == 5)
                        {
                            
                            BasicTextCommands t = new BasicTextCommands();
                            t.channel = curChan.ChannelName;
                            if (splitText[1][0] == '!')
                                t.triggerName = splitText[1];
                            else
                                t.triggerName = string.Format("!{0}",splitText[1]);
                            t.responseText = splitText[3];


                            string chan;
                            bool commandExist = false;
                            for (int i = 0; i < textCommands.Count; ++i)
                            {
                                if (textCommands[i].channel[0] == '#')
                                    chan = textCommands[i].channel;
                                else
                                    chan = string.Format("#{0}", textCommands[i].channel);
                                if (string.Format("#{0}", textCommands[i].channel) == chan || textCommands[i].channel == chan)
                                {
                                    if (textCommands[i].triggerName == t.triggerName)
                                    {
                                        client.LocalUser.SendMessage(curChan.ChannelName, "Command already exists! Overriding!");

                                        textCommands[i] = t;
                                        SaveXML();
                                        commandExist = true;
                                    }
                                }
                            }
                            if (!commandExist)
                            {
                                textCommands.Add(t);
                                client.LocalUser.SendMessage(curChan.ChannelName, "Command added");
                                SaveXML();
                            }
                        }
                        else{
                            client.LocalUser.SendMessage(curChan.ChannelName, "Invalid Command, use !add \"!<TriggerText>\" \"<message>\"");
                        }
                    }

                }
            }

            
            Console.WriteLine(curChan.ChannelName);
        }
        #region Sakegeist
        void SakegeistCommands(Channels curChan, IrcMessageEventArgs e)
        {
            string message = e.Text.ToLower();

            if (message.StartsWith("!bet"))
            {
                if (chickenBettingAllowed)
                {
                    bool foundTarget = false;
                    for (int i = 0; i < cBetters.Count; ++i)
                    {
                        if (cBetters[i].Name == e.Source.Name)
                        {
                            foundTarget = true;
                            Console.WriteLine(e.Source.Name + " is already betting");
                        }

                    }
                    if (!foundTarget)
                        ChickenBet(message, e.Source.Name);
                }
            }
            if (e.Source.Name.ToLower() == "moonizz" || e.Source.Name.ToLower() == "sakegeist")
            {
                if (message.StartsWith("!bet won"))
                {
                    if (message.Length == 10)
                    {
                        ChickenBetWon(curChan, string.Format("{0}", message[9]));
                    }
                }

                if (message.StartsWith("!bet open"))
                {
                    chickenBettingAllowed = true;
                    client.LocalUser.SendMessage(curChan.ChannelName, "Chicken betting is now OPEN!");
                }

                if (message.StartsWith("!bet closed"))
                {
                    if (chickenBettingAllowed)
                    {
                        chickenBettingAllowed = false;
                        client.LocalUser.SendMessage(curChan.ChannelName, "Chicken betting is now CLOSED!");
                    }
                }
                if (message.StartsWith("!chickenboard"))
                {
                    CheckChickenBoard(curChan.ChannelName);
                }
            }
        }

        void ChickenBet(string text, string name)
        {
            ChickenBetter b = new ChickenBetter();
            if (text[4] == '1' || text[5] == '1')
            {
                b.SetBet(name, 1);
            }
            else if (text[4] == '2' || text[5] == '2')
            {
                b.SetBet(name, 2);
            }
            else if (text[4] == '3' || text[5] == '3')
            {
                b.SetBet(name, 3);
            }
            else if (text[4] == '4' || text[5] == '4')
            {
                b.SetBet(name, 4);
            }
            cBetters.Add(b);
        }

        void ChickenBetWon(Channels curChan, string n)
        {
            string cwin = "Winners are: ";
            int success;
            bool first = true;
            if (int.TryParse(n, out success))
            {
                for (int i = 0; i < cBetters.Count; ++i)
                {
                    if (cBetters[i].Number == success)
                    {
                        AddChickenScore(cBetters[i].Name);
                        if (first)
                            cwin = string.Format("{0} {1}", cwin, cBetters[i].Name);
                        else
                            cwin = string.Format("{0},{1}", cwin, cBetters[i].Name);
                    }
                }
                cBetters.Clear();
            }

            client.LocalUser.SendMessage(curChan.ChannelName, cwin);

        }

        void AddChickenScore(string name)
        {
            bool found = false;
            for (int i = 0; i < chickenScore.Count; ++i)
            {
                if (chickenScore[i].name.ToLower() == name.ToLower())
                {
                    chickenScore[i].score += 1;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                ChickenScoreClass cs = new ChickenScoreClass();
                cs.name = name;
                cs.score = 1;
                chickenScore.Add(cs);
            }
        }

        void CheckChickenBoard(string chan)
        {
            ChickenScoreClass first = new ChickenScoreClass(); ;
            ChickenScoreClass second = new ChickenScoreClass();
            ChickenScoreClass third = new ChickenScoreClass();
            for (int i = 0; i < chickenScore.Count; ++i)
            {
                if (i != 0)
                {
                    if (chickenScore[i].score > first.score)
                    {
                        first = chickenScore[i];
                    }
                    else if (chickenScore[i].score > second.score)
                    {
                        second = chickenScore[i];
                    }
                    else if (chickenScore[i].score > first.score)
                    {
                        third = chickenScore[i];
                    }
                }
                else
                {
                    first = chickenScore[i];
                    if (chickenScore.Count >= 2)
                        second = chickenScore[i];
                    if (chickenScore.Count >= 3)
                        third = chickenScore[i];
                }
            }
            
            client.LocalUser.SendMessage(chan, string.Format("First: {0} {1} - Second: {2} {3} - Third: {4} {5}", first.name, first.score, second.name, second.score, third.name, third.score ));

        }

        #endregion

    }

    class ChickenBetter
    {
        public string Name = "";
        public int Number = 0;

        public ChickenBetter() { }
        public ChickenBetter(string n, int num)
        {
            Name = n;
            Number = num;
        }

        public void SetBet(string n, int num)
        {
            Name = n;
            Number = num;
        }
    }

    class ChickenScoreClass
    {
        public string name = "";
        public int score = 0;
    }

    class BasicTextCommands
    {
        public string triggerName;
        public string responseText;
        public string channel;
    }
}
