CREATE TABLE [dbo].[BuildingTemperature](
    [id] [int] IDENTITY(1,1) PRIMARY KEY,
    [buildingId] [int] NOT NULL,
    [endTime] [datetime] NOT NULL,
    [temperature] [float](50) NOT NULL,
)
