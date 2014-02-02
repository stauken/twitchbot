using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace TwitchBot
{
    public class ExceptionHandling
    {
        public Exception regExcept = null;
        public System.Net.WebException webExcept = null;
        public StreamWriter LogHandler;
        public void LogException(Exception ex)
        {
            LogHandler = new StreamWriter("./ExceptionLog.log",true);
            byte[] message = UTF8Encoding.UTF8.GetBytes(ex.Message);
            LogHandler.WriteLine(message);
            LogHandler.Close();
        }
        public ExceptionHandling(Exception ex)
        {
            regExcept = ex;
        }
        public ExceptionHandling(System.Net.WebException webEx)
        {
            webExcept = webEx;
        }
        public void GatherException()
        {
            if (webExcept != null)
            {
                Console.WriteLine("Web Request failed. The following information is being provided:");
                Console.WriteLine("");
                Console.WriteLine("");
            }
            if (regExcept != null)
            {
            }
        }
    }
}
