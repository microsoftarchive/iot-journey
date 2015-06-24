echo off
echo Creating Schema
ExecuteSQL %1 %2 %3 "%4" "CreateSqlDatabase_Schema.sql"
echo on