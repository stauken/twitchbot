using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using Newtonsoft.Json;
using TwitchBot;
public partial class UpdateStreams : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["channels"] == null)
        {
            Response.Write(GetStreams().ToString());            
        }
        else
            Response.Write(GetChannels().ToString());
        Response.End();
    }
    public StringBuilder GetStreams()
    {
        StringBuilder JSONResponse = new StringBuilder();
        DA_Streams daStreams = new DA_Streams();
        List<TwitchStuff> streams = daStreams.GetStreamObjects(String.Format("#{0}", Request["channel"].ToString()));
        var streamVariable = streams.OrderBy(x => Convert.ToInt32(x.streamerviewcount));
        JSONResponse.Append(JsonConvert.SerializeObject(streamVariable));
        return JSONResponse;
    }
    public StringBuilder GetChannels()
    {
        StringBuilder JSONResponse = new StringBuilder();
        DA_Streams daStreams = new DA_Streams();
        List<Channels> channels= daStreams.GetChannelObjects();
        JSONResponse.Append(JsonConvert.SerializeObject(channels));
        return JSONResponse;
    }

}