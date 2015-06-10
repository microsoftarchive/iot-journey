echo off
echo Creating Schema
ExecuteSQL %1 %2 %3 "%4" "CreateBuildingTemperatureTable.sql"
echo on