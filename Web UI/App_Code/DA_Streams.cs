using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
/// <summary>
/// Summary description for DA_Streams
/// </summary>
public class DA_Streams
{
    public List<TwitchBot.TwitchStuff> GetStreamObjects(string ChannelName)
    {
        return TwitchBot.TwitchStuff.ConvertDataTable(GetStreams(ChannelName), ChannelName);
    }
    public List<TwitchBot.Channels> GetChannelObjects()
    {
        return TwitchBot.Channels.ConvertDataTable(GetChannels());
    }
    public int GetViewers()
    {
        DataTable returnValue = new DataTable();
        SqlConnection sqlConn = new SqlConnection(ConfigurationManager.AppSettings["ConnString"]);
        if (sqlConn.State == ConnectionState.Closed)
            sqlConn.Open();

        SqlCommand spCommand = new SqlCommand("spStreams", sqlConn);
        SqlDataAdapter daFiller = new SqlDataAdapter(spCommand);

        spCommand.CommandType = CommandType.StoredProcedure;
        spCommand.Parameters.AddWithValue("@step", 13);

        daFiller.Fill(returnValue);

        if (sqlConn.State == ConnectionState.Open)
            sqlConn.Close();
        return Convert.ToInt32(returnValue.Rows[0][0]);
    }
    public DataTable GetStreams()
    {
        DataTable returnValue = new DataTable();
        SqlConnection sqlConn = new SqlConnection(ConfigurationManager.AppSettings["ConnString"]);
        if (sqlConn.State == ConnectionState.Closed)
            sqlConn.Open();

        SqlCommand spCommand = new SqlCommand("spStreams", sqlConn);
        SqlDataAdapter daFiller = new SqlDataAdapter(spCommand);

        spCommand.CommandType = CommandType.StoredProcedure;
        spCommand.Parameters.AddWithValue("@step", 1);        

        daFiller.Fill(returnValue);

        if (sqlConn.State == ConnectionState.Open)
            sqlConn.Close();
        return returnValue;
    }
    public DataTable GetStreams(string ChannelName)
    {                
        DataTable returnValue = new DataTable();
        SqlConnection sqlConn = new SqlConnection(ConfigurationManager.AppSettings["ConnString"]);        
        if (sqlConn.State == ConnectionState.Closed)
            sqlConn.Open();

        SqlCommand spCommand = new SqlCommand("spStreams", sqlConn);
        SqlDataAdapter daFiller = new SqlDataAdapter(spCommand);

        spCommand.CommandType = CommandType.StoredProcedure;
        spCommand.Parameters.AddWithValue("@step", 4);
        spCommand.Parameters.AddWithValue("@ChannelName", ChannelName);
        
        daFiller.Fill(returnValue);

        if (sqlConn.State == ConnectionState.Open)
            sqlConn.Close();
        return returnValue;
    }
    public DataTable GetChannels()
    {
        DataTable returnValue = new DataTable();
        SqlConnection sqlConn = new SqlConnection(ConfigurationManager.AppSettings["ConnString"]);
        if (sqlConn.State == ConnectionState.Closed)
            sqlConn.Open();

        SqlCommand spCommand = new SqlCommand("spStreams", sqlConn);
        SqlDataAdapter daFiller = new SqlDataAdapter(spCommand);

        spCommand.CommandType = CommandType.StoredProcedure;
        spCommand.Parameters.AddWithValue("@step", 10);        

        daFiller.Fill(returnValue);

        if (sqlConn.State == ConnectionState.Open)
            sqlConn.Close();
        return returnValue;
    }
}