IF OBJECT_ID('dbo.BuildingTemperature', 'U') IS NOT NULL
  DROP TABLE dbo.BuildingTemperature; 
GO

CREATE TABLE [dbo].[BuildingTemperature](
    [Id] [int] IDENTITY(1,1) PRIMARY KEY,
    [BuildingId] varchar(100) NOT NULL,
    [LastObservedTime] datetime NOT NULL,
    [Temperature] decimal(3,1) NOT NULL,
)
GO

IF OBJECT_ID(N'BuildingTemperatureInserted', N'TR') IS NOT NULL
    DROP TRIGGER BuildingTemperatureInserted;
GO

SET NOCOUNT ON;
GO

CREATE TRIGGER BuildingTemperatureInserted 
ON [dbo].[BuildingTemperature] 
INSTEAD OF INSERT 
AS
BEGIN 
	MERGE dbo.BuildingTemperature AS T  
	USING INSERTED AS S 
	ON T.BuildingId = S.BuildingId 
	WHEN MATCHED THEN  
		UPDATE SET T.LastObservedTime = S.LastObservedTime, T.Temperature = S.Temperature 
	WHEN NOT MATCHED THEN  
		INSERT (BuildingId,LastObservedTime,Temperature) VALUES (S.BuildingId,S.LastObservedTime,S.Temperature);
END
GO

