# Ad Hoc Exploration

We use the term "ad hoc exploration" for scenarios where the user wants to query event data on the fly. 
 
For example, you might use ad hoc exploration for incident investigation. Some failure occurs in the system, and you want to know if the device readings can pinpoint the cause. A typical query might look for an average value over a time period, or look for spikes in the data.    


## Common requirements

- Queries are ad hoc. You may not know in advance what queries are needed.
- Queries must execute reasonably quickly, so the user can explore the data. This differs from running a batch query at some fixed interval.
- Queries typically involve a time range: “What was the average temperature during this 20-minute window?” 
- Queries might need relational data: “What was the average temperature across all sensors in building 25?” 
- Relational data might be stored separately from event data; for example, a device registry that maps device IDs to buildings.
- Ad hoc exploration *tends* to target more recent data, rather than the entire set of historical data. The exact definition of “recent” depends on your particular scenario. It might mean the past 24 hours, the past week, or the past month.
- The system must aggregate results into a meaningful view. IoT solutions can generate a *lot* of data, even across a relatively small time slice, so you need a way for a human to understand the results.
- Ad hoc exploration is generally "user initiated," meaning the user submits a query (or perhaps logs into a dashboard), versus having the system detect a condition and notify the user.
- Some latency may be acceptable, in making event data available for ad hoc queries. For example, it might take 5 minutes to index the data. 

	> We don't include critical alerts and other real-time processing under ad hoc exploration. 


## Fabrikam requirements

:memo: For more about our Fabrikam scenario, see [About the Reference Implementation][reference-impl]. 

Here are the specific requirements for our scenario:

- Fabrikam needs to query temperature data by building or room number, over a specified time window. For example, "What was the average temperature in building 25 over the past hour?"
- Ad hoc queries should be able to target approximately the past 3 months' worth of event data.

**[Other requirements?]**

## Solutions

We explored the following solutions:

- Azure Stream Analytics + Azure SQL
- Elastic Search

> **Time series databases** are another interesting possibility. As the name implies, time series  databases are optimized for time series data, which seems like a natural fit for IoT. We have not investigated this approach yet. 

> We also looked at writing the raw events directly to SQL, PowerBI, or DocumentDB. However, none of these has enough throughput and/or capacity for our requirements. (See issues [88], [89], [90].)

### Stream Analytics + SQL

[Azure Stream Analytics] is a real-time stream processing service. It uses an SQL-like language to process incoming data. Unlike traditional SQL, Stream Analytics runs queries over an event stream, making it easy to analyze events within a temporal window.     

In this approach, we use Stream Analytics to aggregate the event data in real time and store the results in SQL. The aggregated data has smaller throughput than the raw data, making SQL feasible for this purpose.

![Stream analytics diagram](./media/stream-analytics.png)


**Advantages:**

- Easy to configure. Stream Analytics uses a declarative model to specify the input and output streams, and the transformations to be performed by the processing.
- Easy to manage. Stream Analytics is an Azure service, so you don't need to provision or maintain VMs. 
- Stream Analytics can pull in *reference data* from blob storage. Reference data is static or infrequently changing data. For example, in our Fabrikam scenario, we use this look up the building number from the device ID.   
- Stream Analytics is integrated with Event Hubs, which we picked for [event ingestion]. 

**Concerns: **

- You have to configure the processing ahead of time. This limits the idea of "ad hoc" exploration somewhat, because the user is running queries against the aggregated results, not the raw event data.


### Elasticsearch

[Elasticsearch] is an open-source analytics engine. It uses a distributed store and indexes as it writes. You can use a REST API to query, or use the [Kibana] web front end.

In this approach, a custom event processor fetches the data from Event Hubs and writes it to Elasticsearch. To get enough throughput, you might need to use batch writes. 

![Elasticsearch diagram](media/elasticsearch.png)


**Advantages:**

- **Ease of use**. The [Kibana] dashboard provides an easy way to query and visualize the data.
- **Performance**. Elasticsearch can perform fast queries against large data sets. 
- **Scalability**. Elasticsearch uses a distributed document store, so it scales horizontally. 
- **Data fidelity**. This approach lets you query over the raw event data, rather than aggregating it first. 

**Concerns:**

- **Maintenance**. Elasticsearch is not an Azure service, so you must set up VMs, install and configure Elasticsearch, monitor the health of the cluster, and so forth. ([This blog post][elasticsearch-on-azure] has a nice write up of the steps.)
- **Security**. Elasticsearch does not provide any security out of the box. You must run it inside a virtual network. 
- **Not relational**. Elasticsearch is not relational and cannot use reference data (that is, data from another store). For example, in the Fabrikam scenario, we need to look up the building and room number for each sensor. One solution is to have the event processor add this information to the event data before writing to Elasticsearch. Or, a separate process can run that updates the Elasticsearch data after it's written.
- **Resiliency?** [TBD]
- **Latency?** Compared with SQL, there may be some latency due to indexing. Event data cannot immediately be queried.


## Lessons Learned

During our investigation, we realized that long-term storage and ad-hoc exploration really form a continuum. They both follow this basic pattern:

- Events come in. 
- A processor consumes them.  
- The processor writes them somewhere. 
 
The processor might collect a buffer of events and then batch-write. Or, it might aggregate the data before writing, as in the case of Stream Analytics. 

When you choose a storage technology, the trade-offs include capacity, throughput, cost, and ease of querying, among others. In general,

- Long-term storage emphasizes *high capacity* and *low storage cost*.
- Ad hoc exploration emphasizes *ease of querying*. 

Also, we realized that throughput tends to be the sticking point for ad hoc exploration. You can reduce *capacity* by storing a shorter time window, but if your storage can’t handle the volume of incoming data (throughput), you're in trouble.  

The Stream Analytics approach solves this problem by aggregating the events before writing them to SQL (although the raw data is still available in long-term storage). 

With Elasticsearch and other NoSQL options, you give up some of the advantages of a fully relational database (e.g., ACID transactions). 

[reference-impl]: 03-reference-implementation.md
[event-ingestion]: 04-event-ingestion.md
[Azure Stream Analytics]: http://azure.microsoft.com/en-us/services/stream-analytics/
[Elasticsearch]: https://www.elastic.co/products/elasticsearch
[Kibana]: https://www.elastic.co/products/kibana
[real-time-milestone]: https://github.com/mspnp/iot-journey/milestones/Real-time%20processing
[88]: https://github.com/mspnp/iot-journey/issues/88
[89]: https://github.com/mspnp/iot-journey/issues/88
[90]: https://github.com/mspnp/iot-journey/issues/88
[elasticsearch-on-azure]: https://blogs.endjin.com/2014/08/gotchas-when-installing-an-elasticsearch-cluster-on-azure/

