using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchBot
{
    public class Program
    {
        public static IrcBot client = new IrcBot();        
        static void Main(string[] args)
        {
            client.Start();
            MainLoop();
        }//static void Main(string[] args)        
        public static void MainLoop()
        {
            bool LiveOnServer = false;
            while (!LiveOnServer)
            {
                if (client.ircConnection.LocalUser.IsOnline && client.ircConnection.IsConnected && client.ircConnection.IsRegistered)
                {
                    client.RegisteredChannels();
                    LiveOnServer = true;
                }

            }
            while (client.ActiveBot)
            {
                Object syncRoot = new object();
                if (client.FullyJoined)
                {
                    //SweepChannels();
                    lock (client.config.TwitchChannels)
                    {
                        System.Threading.Thread doWork = new System.Threading.Thread(new System.Threading.ThreadStart(client.SweepChannels));
                        doWork.Start();
                        while (doWork.ThreadState != System.Threading.ThreadState.Stopped)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }

            }
            if (!client.ActiveBot)
            {
                client.TwitchLog.Close();
                client.LogFile.Close();
            }
        }//public static void MainLoop()

    }
}