# AppDynamics DEXTER

## "Data EXTraction and Enhanced Reporting"

## Challenges and Purpose

AppDynamics DEXTER (which stands for "Data EXTraction and Enhanced Reporting") is tool that extracts configuration, metadata and performance data from multiple AppDynamics Controllers and Application sources, indexes the data in novel ways, and provides enhanced reporting for the scenarios that are either difficult or impossible to accomplish using current regular AppDynamics interface.
Here are some challenges that the drove the creation of the tool:
- Investigation of what is operating and what is detected across multiple Controllers and multiple Applications simultaneously
- Evaluating what components (Tiers, Nodes, Backends) are reporting and what load they have
- Inventory of configuration in multiple environments
- Health Checks for On-Premises Controller – grabbing of data from for later investigation, when Controller is no longer accessible
- Extraction and preservation of fine-grained Metric, Flow map and Snapshot data for interesting time ranges (load test, outage, regular snapshot) with goal of investigation in the future when Controller has already destroyed that data
- Discovery and timeline reporting of Snapshots by the types and contents of the Exits (HTTP call and SQL query parameters), Data Collectors as well as entities involved (Tier, Backend, Error, Service Endpoint and Applications)
- Visualization and correlation of Events, Health Rules Snapshots in the Timeline view 

By extracting the data from AppDynamics and storing it locally, the data can be very rapidly interrogated and preserved with full fidelity indefinitely.

## Example Reports

### Entity Details Report and Entity Timeline View
"Entity Timeline View" is part of "Entity Details" report that is generated for Application and all of its Tiers, Nodes, Business Transactions, Backends, Service Endpoints and Errors
"Entity Timeline View" provides a single-pane view into many things, including:
- 1-minute granularity Metrics in the 1 hour time frame for each hour in the exported range
- Filterable list of Events and Health Rule Violations, arranged in the timeline of that hour, with details of the Event
- Filterable list of Snapshots, broken by Business Transaction and User Experience, arranged in the timeline of that hour, and hotlinked to the specific Snapshot
Here is an example of "Entity Timeline View" for a fairly busy Application through which flow many Business Transactions:

![Alt text](https://github.com/Appdynamics/AppDynamics.DEXTER/blob/master/Images/EntityDetailsApplication.png?raw=true)

And here is an hourly report of the Backend (Oracle database), filtered to only Slow and Very Slow transactions. Note the higher than usual ART from minutes 00 to 19, corresponding to the higher incidence of Slow, Very Slow and Error "Checkout" and "Checkout – Mobile" transactions, with some Snapshots happening more than once per minute:

![Alt text](https://github.com/Appdynamics/AppDynamics.DEXTER/blob/master/Images/EntityDetailsBackend.png?raw=true)

This is only a small part of Entity report. Additional data includes Grid view of Flow Map, Calls/Response/Errors summaries, list of Events, list of Snapshots, list of Errors and Backends, and Business Data (data collectors) applicable to this entity.

For more details, see AppDynamics DEXTER Documentation.docx in one of the Releases