-- OT Assessment Database Schema
-- This script creates the database and tables required for the casino wager system

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'OT_Assessment_DB')
BEGIN
    CREATE DATABASE OT_Assessment_DB;
END
GO

USE OT_Assessment_DB;
GO

-- Create Players table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Players' AND xtype='U')
BEGIN
    CREATE TABLE Players (
        AccountId UNIQUEIDENTIFIER PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_Players_Username UNIQUE (Username)
    );
    
    -- Index for fast username lookups
    CREATE INDEX IX_Players_Username ON Players (Username);
END
GO

-- Create Providers table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Providers' AND xtype='U')
BEGIN
    CREATE TABLE Providers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_Providers_Name UNIQUE (Name)
    );
END
GO

-- Create Games table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Games' AND xtype='U')
BEGIN
    CREATE TABLE Games (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Theme NVARCHAR(100),
        ProviderId INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Games_Provider FOREIGN KEY (ProviderId) REFERENCES Providers(Id),
        CONSTRAINT UQ_Games_Name_Provider UNIQUE (Name, ProviderId)
    );
    
    -- Index for fast game lookups by provider
    CREATE INDEX IX_Games_ProviderId ON Games (ProviderId);
    CREATE INDEX IX_Games_Name ON Games (Name);
END
GO

-- Create CasinoWagers table (main table for storing wager data)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CasinoWagers' AND xtype='U')
BEGIN
    CREATE TABLE CasinoWagers (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        WagerId UNIQUEIDENTIFIER NOT NULL,
        AccountId UNIQUEIDENTIFIER NOT NULL,
        GameId INT NOT NULL,
        TransactionId UNIQUEIDENTIFIER NOT NULL,
        BrandId UNIQUEIDENTIFIER NOT NULL,
        ExternalReferenceId UNIQUEIDENTIFIER NOT NULL,
        TransactionTypeId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        CreatedDateTime DATETIME2 NOT NULL,
        NumberOfBets INT NOT NULL,
        CountryCode NCHAR(2) NOT NULL,
        SessionData NVARCHAR(MAX),
        Duration BIGINT NOT NULL,
        ProcessedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT FK_CasinoWagers_Player FOREIGN KEY (AccountId) REFERENCES Players(AccountId),
        CONSTRAINT FK_CasinoWagers_Game FOREIGN KEY (GameId) REFERENCES Games(Id),
        CONSTRAINT UQ_CasinoWagers_WagerId UNIQUE (WagerId)
    );
    
    -- Indexes for performance
    CREATE INDEX IX_CasinoWagers_AccountId_CreatedDateTime ON CasinoWagers (AccountId, CreatedDateTime DESC);
    CREATE INDEX IX_CasinoWagers_CreatedDateTime ON CasinoWagers (CreatedDateTime DESC);
    CREATE INDEX IX_CasinoWagers_Amount ON CasinoWagers (Amount DESC);
    CREATE INDEX IX_CasinoWagers_WagerId ON CasinoWagers (WagerId);
END
GO

-- Stored procedure to get player casino wagers with pagination
CREATE OR ALTER PROCEDURE sp_GetPlayerCasinoWagers
    @AccountId UNIQUEIDENTIFIER,
    @Page INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    
    -- Get total count
    DECLARE @TotalCount INT;
    SELECT @TotalCount = COUNT(*)
    FROM CasinoWagers cw
    WHERE cw.AccountId = @AccountId;
    
    -- Get paginated results
    SELECT 
        cw.WagerId,
        g.Name AS Game,
        p.Name AS Provider,
        cw.Amount,
        cw.CreatedDateTime AS CreatedDate,
        @TotalCount AS TotalCount,
        CEILING(CAST(@TotalCount AS FLOAT) / @PageSize) AS TotalPages
    FROM CasinoWagers cw
    INNER JOIN Games g ON cw.GameId = g.Id
    INNER JOIN Providers p ON g.ProviderId = p.Id
    WHERE cw.AccountId = @AccountId
    ORDER BY cw.CreatedDateTime DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- Stored procedure to get top spending players
CREATE OR ALTER PROCEDURE sp_GetTopSpendingPlayers
    @Count INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Count)
        pl.AccountId,
        pl.Username,
        SUM(cw.Amount) AS TotalAmountSpend
    FROM Players pl
    INNER JOIN CasinoWagers cw ON pl.AccountId = cw.AccountId
    GROUP BY pl.AccountId, pl.Username
    ORDER BY SUM(cw.Amount) DESC;
END
GO

-- Function to get or create provider
CREATE OR ALTER FUNCTION fn_GetOrCreateProvider(@ProviderName NVARCHAR(200))
RETURNS INT
AS
BEGIN
    DECLARE @ProviderId INT;
    
    SELECT @ProviderId = Id FROM Providers WHERE Name = @ProviderName;
    
    IF @ProviderId IS NULL
    BEGIN
        INSERT INTO Providers (Name) VALUES (@ProviderName);
        SET @ProviderId = SCOPE_IDENTITY();
    END
    
    RETURN @ProviderId;
END
GO

-- Function to get or create game
CREATE OR ALTER FUNCTION fn_GetOrCreateGame(@GameName NVARCHAR(200), @Theme NVARCHAR(100), @ProviderId INT)
RETURNS INT
AS
BEGIN
    DECLARE @GameId INT;
    
    SELECT @GameId = Id FROM Games WHERE Name = @GameName AND ProviderId = @ProviderId;
    
    IF @GameId IS NULL
    BEGIN
        INSERT INTO Games (Name, Theme, ProviderId) VALUES (@GameName, @Theme, @ProviderId);
        SET @GameId = SCOPE_IDENTITY();
    END
    
    RETURN @GameId;
END
GO

-- Function to get or create player
CREATE OR ALTER FUNCTION fn_GetOrCreatePlayer(@AccountId UNIQUEIDENTIFIER, @Username NVARCHAR(100))
RETURNS UNIQUEIDENTIFIER
AS
BEGIN
    DECLARE @ExistingAccountId UNIQUEIDENTIFIER;
    
    SELECT @ExistingAccountId = AccountId FROM Players WHERE AccountId = @AccountId;
    
    IF @ExistingAccountId IS NULL
    BEGIN
        INSERT INTO Players (AccountId, Username) VALUES (@AccountId, @Username);
    END
    
    RETURN @AccountId;
END
GO

PRINT 'Database schema created successfully!';