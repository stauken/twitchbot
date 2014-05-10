USE [master]
GO
/****** Object:  Database [mumbo]    Script Date: 05/09/2014 23:24:04 ******/
CREATE DATABASE [mumbo] ON  PRIMARY 
( NAME = N'mumbo', FILENAME = N'c:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\mumbo.mdf' , SIZE = 2048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'mumbo_log', FILENAME = N'c:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\mumbo_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [mumbo] SET COMPATIBILITY_LEVEL = 100
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [mumbo].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [mumbo] SET ANSI_NULL_DEFAULT OFF
GO
ALTER DATABASE [mumbo] SET ANSI_NULLS OFF
GO
ALTER DATABASE [mumbo] SET ANSI_PADDING OFF
GO
ALTER DATABASE [mumbo] SET ANSI_WARNINGS OFF
GO
ALTER DATABASE [mumbo] SET ARITHABORT OFF
GO
ALTER DATABASE [mumbo] SET AUTO_CLOSE OFF
GO
ALTER DATABASE [mumbo] SET AUTO_CREATE_STATISTICS ON
GO
ALTER DATABASE [mumbo] SET AUTO_SHRINK OFF
GO
ALTER DATABASE [mumbo] SET AUTO_UPDATE_STATISTICS ON
GO
ALTER DATABASE [mumbo] SET CURSOR_CLOSE_ON_COMMIT OFF
GO
ALTER DATABASE [mumbo] SET CURSOR_DEFAULT  GLOBAL
GO
ALTER DATABASE [mumbo] SET CONCAT_NULL_YIELDS_NULL OFF
GO
ALTER DATABASE [mumbo] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [mumbo] SET QUOTED_IDENTIFIER OFF
GO
ALTER DATABASE [mumbo] SET RECURSIVE_TRIGGERS OFF
GO
ALTER DATABASE [mumbo] SET  DISABLE_BROKER
GO
ALTER DATABASE [mumbo] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO
ALTER DATABASE [mumbo] SET DATE_CORRELATION_OPTIMIZATION OFF
GO
ALTER DATABASE [mumbo] SET TRUSTWORTHY OFF
GO
ALTER DATABASE [mumbo] SET ALLOW_SNAPSHOT_ISOLATION OFF
GO
ALTER DATABASE [mumbo] SET PARAMETERIZATION SIMPLE
GO
ALTER DATABASE [mumbo] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [mumbo] SET HONOR_BROKER_PRIORITY OFF
GO
ALTER DATABASE [mumbo] SET  READ_WRITE
GO
ALTER DATABASE [mumbo] SET RECOVERY SIMPLE
GO
ALTER DATABASE [mumbo] SET  MULTI_USER
GO
ALTER DATABASE [mumbo] SET PAGE_VERIFY CHECKSUM
GO
ALTER DATABASE [mumbo] SET DB_CHAINING OFF
GO
USE [mumbo]
GO
/****** Object:  Table [dbo].[Channels]    Script Date: 05/09/2014 23:24:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Channels](
	[ChannelID] [int] IDENTITY(1,1) NOT NULL,
	[ChannelName] [nvarchar](50) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Streams_Channels]    Script Date: 05/09/2014 23:24:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Streams_Channels](
	[StreamID] [int] NOT NULL,
	[ChannelID] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Streams]    Script Date: 05/09/2014 23:24:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Streams](
	[StreamID] [int] IDENTITY(1,1) NOT NULL,
	[StreamerName] [nvarchar](250) NULL,
	[StreamTitle] [nvarchar](max) NULL,
	[StreamGame] [nvarchar](max) NULL,
	[StreamOnline] [bit] NULL,
	[StreamViewerCount] [int] NULL
) ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[spStreams]    Script Date: 05/09/2014 23:24:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[spStreams]
	-- Add the parameters for the stored procedure here
	@StreamID int = 0,
	@StreamerName nvarchar(250) = '',
	@StreamTitle nvarchar(max) = '',
	@StreamGame nvarchar(max) = '',
	@StreamOnline bit = null,
	@ChannelName nvarchar(50) = '',
	@ChannelID int = 0,
	@StreamViewerCount int = 0,
	@step int = 0

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	if @step = 1
	begin
		select S.StreamerName, S.StreamGame, S.StreamID, S.StreamOnline, S.StreamTitle,S.StreamViewerCount from Streams As S				
	end
    
	if @step = 2
	begin
		select S.StreamerName, S.StreamGame, S.StreamID, S.StreamOnline, S.StreamTitle,S.StreamViewerCount from Streams As S				
		WHERE S.StreamID = @StreamID
	end
	if @step = 3
	begin
		select S.StreamID, S.StreamerName, S.StreamGame,  S.StreamOnline, S.StreamTitle,S.StreamViewerCount from Streams As S				
		WHERE S.StreamerName = @StreamerName
	end
	if @step = 4
	begin
		select S.StreamerName, S.StreamGame, S.StreamID, S.StreamOnline, S.StreamTitle,S.StreamViewerCount, C.ChannelName,C.ChannelID from Streams As S				
		INNER JOIN Streams_Channels As SC ON SC.StreamID = S.StreamID
		INNER JOIN Channels As C ON C.ChannelID = SC.ChannelID
		WHERE C.ChannelName = @ChannelName
		ORDER BY S.StreamViewerCount DESC
	end
	if @step = 5
	begin
		update Streams SET StreamerName = @StreamerName, StreamGame = @StreamGame, StreamOnline = @StreamOnline, StreamTitle = @StreamTitle, StreamViewerCount = @StreamViewerCount WHERE StreamID = @StreamID
	end
	if @step = 6
	begin
		insert into Streams(StreamerName, StreamGame, StreamOnline, StreamTitle, StreamViewerCount) VALUES (@StreamerName,@StreamGame,@StreamOnline, @StreamTitle, @StreamViewerCount);
		SET @StreamID = @@IDENTITY;
		INSERT INTO Streams_Channels(ChannelID,StreamID) VALUES(@ChannelID,@StreamID);
	end
	if @step = 7
	begin
		insert into Channels(ChannelName) VALUES(@ChannelName);
		SELECT @@IDENTITY;
	end
	if @step = 8
	begin
		SELECT ChannelName from Channels WHERE ChannelID = @ChannelID;
	end
	if @step = 9
	begin
		SELECT ChannelName,ChannelID from Channels WHERE ChannelName = @ChannelName;
	end
	if @step = 10
	begin
		SELECT C.ChannelName,C.ChannelID from Channels As C;
	end
	if @step = 11
	begin
		SELECT SC.StreamID, SC.ChannelID from Streams_Channels As SC
		WHERE SC.StreamID = @StreamID And SC.ChannelID = @ChannelID
	end
	if @step = 12
	begin
		INSERT INTO Streams_Channels (StreamID, ChannelID) VALUES(@StreamID, @ChannelID);
	end
	if @step = 13
	begin
		SELECT SUM(StreamViewerCount) From Streams
	end
END
GO
