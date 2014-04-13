using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Net;
using System.Web;

namespace TwitchBot
{
    public class TwitchAPIInterface
    {
        public Dictionary<string, string> LastUpdateForStreamer = new Dictionary<string, string>();
        public string MostRecentResponse = null;
        public string StreamName = String.Empty;
        public string StreamTitle = String.Empty;
        public string StreamGame = String.Empty;
        public string StreamStart = String.Empty;
        public String Error = String.Empty;
        public int catalogupdateinterval = 30;
        public JObject Data;
        public string GetResponse(string username)
        {
     
            string apiResponse = String.Empty;
            WebRequest httpWR = WebRequest.Create(String.Format("https://api.twitch.tv/kraken/streams/{0}?client_id=orxfsw5hp13j70u47drvhhgux7vhhhj", username));
            httpWR.Timeout = 5000;
            //httpWR.ContentType = "application/x-www-form-urlencoded";
            //httpWR.Method = "POST";
            //httpWR.ContentLength = "client_id=orxfsw5hp13j70u47drvhhgux7vhhhj".Length;
            //System.IO.Stream RequestStream = httpWR.GetRequestStream();
            //RequestStream.Write(Encoding.UTF8.GetBytes("client_id=orxfsw5hp13j70u47drvhhgux7vhhhj"), 0, Encoding.UTF8.GetBytes("client_id=orxfsw5hp13j70u47drvhhgux7vhhhj").Length);
            //RequestStream.Close();
            try
            {
                WebResponse httpResponse = httpWR.GetResponse();
                httpResponse = httpWR.GetResponse();                
                System.IO.StreamReader sRead = new System.IO.StreamReader(httpResponse.GetResponseStream());
                apiResponse = sRead.ReadToEnd();
                MostRecentResponse = apiResponse;
                Data = JObject.Parse(apiResponse);
                LastUpdateForStreamer[username] = apiResponse;
            }
            catch (WebException webEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine(e.Message);
                //Console.WriteLine("ERROR: " + ex.Message);
                Console.WriteLine(String.Format("Problem retrieving user {0}: {1}", username, webEx.Message));
                Console.ForegroundColor = ConsoleColor.Gray;
                
                if (LastUpdateForStreamer.ContainsKey(username))
                {
                    Data = JObject.Parse(LastUpdateForStreamer[username]);
                    apiResponse = LastUpdateForStreamer[username];
                    Error = webEx.Message;
                }
                Error = webEx.Message;                
                return "false";
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine(e.Message);
                //Console.WriteLine("ERROR: " + ex.Message);
                Console.WriteLine(String.Format("Problem retrieving user {0}: {1}", username, ex.Message));
                Console.ForegroundColor = ConsoleColor.Gray;
                if (LastUpdateForStreamer.ContainsKey(username))
                {
                    Data = JObject.Parse(LastUpdateForStreamer[username]);
                    apiResponse = LastUpdateForStreamer[username];                    
                }
                Error = ex.Message;                
                return "false";
            }
            finally
            {
                
            }
            return apiResponse;
        }
        
    }
}
