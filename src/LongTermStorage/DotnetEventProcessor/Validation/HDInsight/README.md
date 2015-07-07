**Summary**

The script creates a Hive external table on cold storage blobs, executes a Hive query that will calculate the number of events for each device and display the results. You will need to provision your HDInsight cluster in the same storage account as your cold storage.

**Supporting multiple blob directories**
 
 The current version of this query supports the use of only one storage account. Depending on the naming strategy you choose for your cold storage solution, and in order for the query to return a result set, the [directoryPath] parameter should contain blobs directly in it.
 
 If you wish to query data in multiple directories from a parent directory, consider partitioning the hive table so that each partition includes the blobs in each directory tree.
 