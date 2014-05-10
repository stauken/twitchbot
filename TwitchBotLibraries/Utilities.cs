using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrcDotNet;
namespace TwitchBot
{
    public class Utilities
    {
        public static bool IsDebug()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                return true;
            else
                return false;

        }
        public static bool CheckOwner(string OwnerIdentity, IrcChannel Channel)
        { 
            foreach(IrcChannelUser user in Channel.Users)
            {                
                if (user.User.HostName == OwnerIdentity)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        public static bool CheckOp(string UserNick, IrcChannel Channel)
        {
            foreach (IrcChannelUser user in Channel.Users)
            {
                if (user.User.NickName == UserNick && (user.Modes.Contains('o') || user.Modes.Contains('h')))
                {
                    return true;
                }//if (user.User.NickName == e.Source.Name && (user.Modes.Contains('o') || user.Modes.Contains('h')))                    
                else if (user.User.NickName == UserNick && (!user.Modes.Contains('o') && !user.Modes.Contains('h')))
                {
                    return false;
                }                
            }//foreach (IrcChannelUser user in Channel.Users)
            return false;
        }//public static bool CheckOp(string UserNick, IrcChannel Channel)

        public static string TemplateString(string addToList, string streamername, string game, string viewercount, string streamname)
        {
            addToList = addToList.Replace("\n", "");
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
            addToList = addToList.Replace("$reset", "\x03\x0f");
            addToList = addToList.Replace("$b", "\x02");
            addToList = addToList.Replace("$x", "\x03\x0f");
            addToList = addToList.Replace("$n", streamername);
            addToList = addToList.Replace("$g", game);
            addToList = addToList.Replace("$v", viewercount);
            addToList = addToList.Replace("$t", streamname);

            return addToList;
        }//public static string TemplateString(string addToList, string streamername, string game, string viewercount, string streamname)
        public static string TemplateMysteryGame(string addToList, MysteryGame gameobj)
        {
            addToList = addToList.Replace("\n", "");
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
            addToList = addToList.Replace("$reset", "\x03\x0f");
            addToList = addToList.Replace("$b", "\x02");
            addToList = addToList.Replace("$x", "\x03\x0f");
            addToList = addToList.Replace("$submitter$", gameobj.submitter);
            addToList = addToList.Replace("$game$", gameobj.name);
            addToList = addToList.Replace("$drawdate$", gameobj.drawdate.ToString());
            addToList = addToList.Replace("$download$", gameobj.download);
            addToList = addToList.Replace("$draws$", gameobj.draws.ToString());
            addToList = addToList.Replace("$goal$", gameobj.goal);
            addToList = addToList.Replace("$notes$", gameobj.notes);
            addToList = addToList.Replace("$pastebin$", gameobj.pastebin);
            addToList = addToList.Replace("$platform$",gameobj.platform);
            addToList = addToList.Replace("$specialrequirements$", gameobj.specialrequirements);
            addToList = addToList.Replace("$tournamentraceresult$", gameobj.tournamentraceresult);
            return addToList;
        }//public static string TemplateString(string addToList, string streamername, string game, string viewercount, string streamname)
        public static bool PasteBinPass(string pastebin)
        {
            return pastebin.StartsWith("http://pastebin");
        }
    }
    
}
