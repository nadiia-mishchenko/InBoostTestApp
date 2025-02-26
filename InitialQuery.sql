DROP TABLE IF EXISTS [dbo].[WeatherHistory]
DROP TABLE IF EXISTS [dbo].[Users]
DROP TABLE IF EXISTS [dbo].[Cities]


CREATE TABLE [dbo].[Users] (
    Id INT PRIMARY KEY IDENTITY (1, 1),
    TelegramId BIGINT UNIQUE NOT NULL,
    Name [nvarchar](250) NOT NULL
)
GO

CREATE TABLE [dbo].[Cities] (
    Id INT PRIMARY KEY IDENTITY (1, 1),
    Name [nvarchar](250) UNIQUE NOT NULL
)
GO

CREATE TABLE [dbo].[WeatherHistory] (
    Id INT PRIMARY KEY IDENTITY (1, 1),
	UserId INT NOT NULL,
	CityId INT NOT NULL,
	[RequestDate] [datetime2] NOT NULL
)
GO

ALTER TABLE [dbo].[WeatherHistory] WITH CHECK ADD CONSTRAINT [User_WeatherHistory] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users]([Id])
GO

ALTER TABLE [dbo].[WeatherHistory] WITH CHECK ADD CONSTRAINT [WeatherHistory_City] FOREIGN KEY([CityId])
REFERENCES [dbo].[Cities]([Id])
GO

/*
INSERT INTO [dbo].[Users] ([TelegramId], [Name])
VALUES (1, 'Name1'),
(2, 'Name2'),
(3, 'Name3'),
(4, 'Name1')
GO

INSERT INTO [dbo].[Cities] ([Name])
VALUES ('City1'),
('City2'),
('City3'),
('City4'),
('City5')
GO

INSERT INTO [dbo].[WeatherHistory] ([UserId], [CityId], [RequestDate])
VALUES
(2, 1, '2014-10-12'),
(3, 1, '2014-10-10'),
(3, 2, '2014-10-11'),
(3, 3, '2014-10-12'),
(3, 3, '2014-10-22'),
(4, 1, '2014-10-10')
GO
*/