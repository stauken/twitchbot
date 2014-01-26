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
        public string MostRecentResponse = null;
        public string StreamName = "";
        public string StreamTitle = "";
        public string StreamGame = "";
        public string StreamStart = "";
        public JObject Data;
        public string GetResponse(string username)
        {
            string apiResponse = "";
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
            }
            catch (WebException webEx)
            {
                Data = JObject.Parse("{\"stream\": null}");
            }
            catch (Exception ex)
            {
                Data = JObject.Parse("{\"stream\": null}");
                return "false";
            }
            finally
            {
                
            }
            return apiResponse;
        }
        
    }
}
