using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data;
using System.Web.UI.HtmlControls;
using TwitchBot;
public partial class Streams : System.Web.UI.Page
{
    public string ChannelValue;
    protected void Page_Load(object sender, EventArgs e)
    {
        DateTime dtStart = DateTime.Now;
        DA_Streams daStreams = new DA_Streams();
        List<Channels> ChannelList = daStreams.GetChannelObjects();
        rptChannels.DataSource = ChannelList;
        rptChannels.DataBind();
        litChannelCount.Text = ChannelList.Count.ToString();
        rptChannelStreams.DataSource = ChannelList;
        rptChannelStreams.DataBind();
        DateTime dtEnd = DateTime.Now;        
        DataTable streamList = daStreams.GetStreams();
        litStreamWatches.Text = streamList.Rows.Count.ToString();
        List<TwitchStuff> fullList = TwitchStuff.ConvertDataTableNoChannel(streamList);
        var o = from twitchstuff in fullList
                where twitchstuff.streamerlive == "true"
                select twitchstuff;
        litStreamersLive.Text = o.Count().ToString();
        litViewerCount.Text = daStreams.GetViewers().ToString();
        if (Request["stream"] != null)
        {
            String scriptText = "";
            scriptText += "$(document).ready(function() {";
            scriptText += "UpdateStreamInfo('" + Request["stream"] + "');";
            scriptText += "});";
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(),"StartStream", scriptText, true);
        }
        TimeSpan renderTime = dtEnd - dtStart;
        litPageRender.Text = renderTime.TotalSeconds.ToString() + " seconds";        
    }
    public Dictionary<string,List<string>> OfflineStreamers = new Dictionary<string,List<string>>();
    public void rptChannels_Bind(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
        {
            Channels twitchChannel = ((Channels)e.Item.DataItem);
            HtmlAnchor linkNav = (HtmlAnchor)e.Item.FindControl("linkNavigateChannel");  
            linkNav.InnerText = twitchChannel.ChannelName;
            linkNav.Attributes.Add("onclick","$('.ChannelStreams').hide();$('.streamsfor" + twitchChannel.ChannelID + "').fadeToggle();");
            linkNav.Attributes["class"] = "Channel" + twitchChannel.ChannelName.Replace("#", "");
            DA_Streams daStreams = new DA_Streams();
            //DataTable dtStreams = daStreams.GetStreams(navLink.CommandArgument);
            List<TwitchStuff> twitchInfo = daStreams.GetStreamObjects(twitchChannel.ChannelName);
            if (twitchInfo.Count == 0)
            {
                linkNav.Visible = false;
            }
            if (!OfflineStreamers.ContainsKey(twitchChannel.ChannelName))
            {
                OfflineStreamers.Add(twitchChannel.ChannelName, new List<string>());
            }
        }
    }
    public void rptChannelStreams_Bind(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
        {
            Channels twitchChannel = ((Channels)e.Item.DataItem);
            Repeater rptStreams = (Repeater)e.Item.FindControl("rptStreams");
            HtmlControl genericDiv = (HtmlControl)e.Item.FindControl("individualstreamdiv");
            
            //LinkButton navLink = (LinkButton)sender;
            DA_Streams daStreams = new DA_Streams();
            //DataTable dtStreams = daStreams.GetStreams(navLink.CommandArgument);
            List<TwitchStuff> twitchInfo = daStreams.GetStreamObjects(twitchChannel.ChannelName);
            genericDiv.Attributes.Add("class", "ChannelStreams streamsfor" + twitchChannel.ChannelID);
            if (Request["channel"] != null)
            {
                if (("#" + Request["channel"].ToString()) == twitchChannel.ChannelName)
                {
                    
                }
                else
                {
                    genericDiv.Attributes.Add("style", "display:none;");            
                }
            }
            else
            {
                genericDiv.Attributes.Add("style", "display:none;");
            }
            
            rptStreams.DataSource = twitchInfo;
            rptStreams.DataBind();
            HtmlControl offlineDiv = (HtmlControl)e.Item.FindControl("OfflineStreamers");
            Label channelname = (Label)e.Item.FindControl("lblchannelName");
            channelname.Text = twitchChannel.ChannelName;
            if (OfflineStreamers.ContainsKey(twitchChannel.ChannelName)) {
                if (OfflineStreamers[twitchChannel.ChannelName].Count != 0 && OfflineStreamers[twitchChannel.ChannelName] != null)
                {
                    string msg = "<h3>The following users are offline:</h3><div class='writeOffline" + twitchChannel.ChannelID + "'>" + String.Join(", ", OfflineStreamers[twitchChannel.ChannelName]) + "</div>";
                    LiteralControl displaymsg = new LiteralControl(msg);
                    offlineDiv.Controls.Add(displaymsg);
                }
            }
        }
    }

    public void rptStreams_Bind(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
        {
            HtmlControl genericList = (HtmlControl)e.Item.FindControl("streamShown");
            HtmlAnchor linkNav = (HtmlAnchor)e.Item.FindControl("hypStreamNavigation");
            TwitchStuff StreamerInfo = ((TwitchStuff)e.Item.DataItem);            
            linkNav.Attributes["style"] = "background-image:url(http://static-cdn.jtvnw.net/previews-ttv/live_user_" + StreamerInfo.streamername.ToLower() + "-320x200.jpg);";
            HtmlControl genericDiv = (HtmlControl)e.Item.FindControl("StreamerName");
            HtmlControl titleDiv = (HtmlControl)e.Item.FindControl("StreamerTitle");
            HtmlControl viewerDiv = (HtmlControl)e.Item.FindControl("ViewerCount");
            
            genericDiv.Controls.Add(new LiteralControl(StreamerInfo.streamername));
            genericDiv.Controls.Add(new LiteralControl(" playing " + StreamerInfo.game));
            LiteralControl streamtitle = null;
            if (StreamerInfo.streamname.Length < 40)
            {
                streamtitle = new LiteralControl(StreamerInfo.streamname);                
            }
            else
            {
                streamtitle = new LiteralControl(StreamerInfo.streamname.Substring(0, 40) + "...");                
            }
            linkNav.Title = StreamerInfo.streamname;
            linkNav.HRef = "http://mumbo.beatthega.me/?channel=" + StreamerInfo.ChannelName.Replace("#", "") +"&stream=" + StreamerInfo.streamername;
            titleDiv.Controls.Add(streamtitle);
            viewerDiv.Controls.Add(new LiteralControl("&nbsp;" + StreamerInfo.streamerviewcount.ToString()));
            if (Convert.ToBoolean(StreamerInfo.streamerlive) == true)
            {
                genericList.Visible = true;
            }
            else
            {
                genericList.Visible = false;
                OfflineStreamers[StreamerInfo.ChannelName].Add("<a target='_blank' href='http://www.twitch.tv/" + StreamerInfo.streamername + "'>" + StreamerInfo.streamername + "</a>");
            }
            linkNav.Attributes.Add("onclick","UpdateStreamInfo('"  + StreamerInfo.streamername + "');return false;");
        }
    }
    protected void hypStreamNavigation_Click(object sender, EventArgs e)
    {
        LinkButton navLink = (LinkButton)sender;
        StreamContainer.Visible = true;
    }
    protected void rptChannels_DataBinding(object sender, EventArgs e)
    {

    }
    
}