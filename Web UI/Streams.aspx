<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Streams.aspx.cs" Inherits="Streams" EnableViewState="false" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link rel="stylesheet" href="~/css/main.css" type="text/css"/>
    <link href='http://fonts.googleapis.com/css?family=Armata' rel='stylesheet' type='text/css'>   
    <script src="//ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js"></script>
    <script type="text/javascript">
function UpdateStreamInfo(streamname) {    
    $('.StreamContainer').html('\
        <object type="application/x-shockwave-flash" class="stream" id="live_embed_player_flash" data="http://www.twitch.tv/widgets/live_embed_player.swf?channel=' + streamname + '" bgcolor="#000000">\
            <param name="allowFullScreen" value="true" />\
            <param name="allowScriptAccess" value="always" />\
            <param name="allowNetworking" value="all" />\
            <param name="movie" value="http://www.twitch.tv/widgets/live_embed_player.swf" />\
            <param name="flashvars" value="hostname=www.twitch.tv&channel=' + streamname + '&auto_play=true&start_volume=25" />\
        </object>\
        <iframe class="chat" src="http://www.twitch.tv/' + streamname + '/chat" />');
    $('.StreamContainer').show();    
}
function updateStream() {
}
function doSomething(e) {
    var rightclick;
    if (!e) var e = window.event;
    if (e.which) {
        rightclick = (e.which == 3);
    } else if (e.button) {
        rightclick = (e.button == 2);
    }
    if (rightclick)
    {

    }
}
function trimTrailingChars(s, charToTrim) {
    var regExp = new RegExp(charToTrim + "+$");
    var result = s.replace(regExp, "");

    return result;
}
function UpdatePage()
{
    $.get("/UpdateStreams.aspx?channels=true", function (data) {
        var channels = $.parseJSON(data);
        for (var i = 0; i < channels.length; i++)
        {
            var channelname = channels[i].ChannelName.replace("#", "");
            var channelid = channels[i].ChannelID;            
            if ($('.streamsfor' + channelid) != null)
            {
                $('.streamsfor' + channelid + '>.IndividualStream').remove();                
                $.get("/UpdateStreams.aspx?channel=" + channelname, function (data2) {
                    var streams = $.parseJSON(data2);
                    var offline = '';
                    for (var y = 0; y < streams.length; y++) {
                        if (streams[y].streamerlive == "true") {
                            var link = "http://mumbo.beatthega.me/?channel=" + streams[y].ChannelName.replace("#","") + "&stream=" + streams[y].streamername;
                            $('.streamsfor' + streams[y].ChannelID + '>h3').after('\
                                <div id="streamShown" class="IndividualStream" >\
                                <a class="playButton" href="' + link + '" style="background-image:url(http://static-cdn.jtvnw.net/previews-ttv/live_user_' + streams[y].streamername.toLowerCase() + '-320x200.jpg);" title="' + streams[y].streamname + '" onclick="UpdateStreamInfo(\'' + streams[y].streamername + '\');return false;">\
                                    <div class="TransparentBG" style="width:320px;">\
                                        <div class="StreamerName" id="StreamerName">\
                                            ' + streams[y].streamername + ' playing ' + streams[y].game + '\
                                        </div>\
                                        <br />\
                                        <div class="StreamerName" id="StreamerTitle" style="clear:both;">\
                                            ' + streams[y].streamname + '\
                                        </div>\
                                    </div>\
                                </a>\
                                <br />\
                                <div class="StreamTitle">\
                                </div>\
                                <div class="viewers">\
                                    <div id="ViewerCount" class="ViewerCount" >\
                                        <div class="reddot"></div>&nbsp;\
                                            ' + streams[y].streamerviewcount + '\
                                        </div>\
                                    </div>\
                                </div>');
                            console.log("Updating " + streams[y].streamername);
                        }
                        else
                        {
                            offline += "\
                                <a target='_blank' href='http://www.twitch.tv/" + streams[y].streamername + "'>\
                                    " + streams[y].streamername + "\
                                </a>, ";
                        }
                    }
                    if (streams.length > 0)
                    {
                        $('.writeOffline' + streams[0].ChannelID).html(trimTrailingChars(offline,", "));
                    }
                                        
                });
            }
        }
    });
    setTimeout(function () {
        UpdatePage();
    }, 120000);
}
$(document).ready(function () {
    setTimeout(function() {
        UpdatePage();
    },120000);
});
    </script>
</head>
<body>


    <form id="form1" runat="server">

        <div class="ChannelNavigation Gradient">
            <div class="ContainNav">
                <asp:Repeater ID="rptChannels" runat="server" OnItemDataBound="rptChannels_Bind">
                    <ItemTemplate><a id="linkNavigateChannel" runat="server"></a>
                        <div class="StreamNavigation Gradient"></div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
        <div class="NewContainer">
            <div class="StreamContainer" id="StreamContainer" runat="server">
            </div>
            <asp:Repeater ID="rptChannelStreams" runat="server" OnItemDataBound="rptChannelStreams_Bind">
                <ItemTemplate>
                    <div runat="server" id="individualstreamdiv">
                        <h3>Streams for
                            <asp:Label ID="lblchannelName" runat="server">   </asp:Label></h3>
                        <asp:Repeater ID="rptStreams" runat="server" OnItemDataBound="rptStreams_Bind">
                            <ItemTemplate>
                                <div id="streamShown" class="IndividualStream" runat="server"><a runat="server" id="hypStreamNavigation" class="playButton">
                                    <div class="TransparentBG" style="width: 320px;">
                                        <div class="StreamerName" id="StreamerName" runat="server"></div>
                                        <br />
                                        <div class="StreamerName" id="StreamerTitle" style="clear: both;" runat="server"></div>
                                    </div>
                                </a>
                                    <br />
                                    <div class="StreamTitle"></div>
                                    <div class="viewers">
                                        <div id="ViewerCount" class="ViewerCount" runat="server">
                                            <div class="reddot"></div>
                                        </div>
                                    </div>
                                </div>

                            </ItemTemplate>
                        </asp:Repeater>
                        <br />
                        <div id="OfflineStreamers" class="OfflineStreamers" runat="server" style="clear: both;"></div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
        <div id="footer" class="footer">
            Number of channels being watched:
            <asp:Literal ID="litChannelCount" runat="server"></asp:Literal>.             
            Number of streams live vs being watched:
            <asp:Literal ID="litStreamersLive" runat="server"></asp:Literal>
            live /
            <asp:Literal ID="litStreamWatches" runat="server"></asp:Literal>
            total watched. Number of viewers:
            <asp:Literal ID="litViewerCount" runat="server"></asp:Literal><br />
            Powered by mumbo_reincarnate on <a href="http://www.speedrunslive.com">SpeedRunsLive</a>'s IRC server.
            Page rendered in
            <asp:Literal ID="litPageRender" runat="server"></asp:Literal>. 
        </div>
    </form>
    <p>
&nbsp;</p>
</body>
</html>
