# Introduction
AppDynamics provides a rich source of information about your monitored applications, including the performance of individual business activities, dependency flow between application components, and details on every business transaction in an instrumented environment. 

AppDynamics APM provides a rich toolkit for turning the vast corpus of data captured by AppDynamics into valuable insights.

AppDynamics DEXTER (Data Extraction and Enhanced Reporting) can make this process even faster and simpler. DEXTER provides new ways to unlock the data stored in the AppDynamics platform. You can analyze this information in a number of data warehousing and visualization applications, and combine it with your own data to generate customized reports.

# Turn Data Store into Data Warehouse
If you’re familiar with data warehousing terminology, think of DEXTER as an extract/transform/load (ETL) utility for AppDynamics data. It extracts information from the AppDynamics platform, transforms it into an enriched, query-able form for faster access, and loads it into variety of reports for:
* Application logical model (applications, tiers, nodes, backends, business transactions)
* Performance metrics (average response time, calls, errors per minute, CPU, memory, JVM, JMX, GC metrics)
* Dependency data (flow maps, relationships between components)
* Events (errors, resource pool exhaustion, application crashes and restarts, health rule violations)
* Configuration rules (business transaction, backend detection, data collectors, error detection, agent properties)
* Snapshots (SQL queries, HTTP destinations, data collectors, call graph data, errors)

By extracting the data from AppDynamics, converting it into queryable format and storing it locally, the data can be preserved with full fidelity indefinitely and interrogated in new and novel ways.

# Scenarios Enabled by This Tool
Here are some scenarios that are possible with data provided by AppDynamics DEXTER:
* Investigation of what is detected and reporting across multiple Controllers and multiple Applications
* Evaluating what components (Tiers, Nodes, Backends, Business Transactions) are reporting and what load they have
* Inventory of configuration in multiple environments
* Comparison of configuration between multiple environments
* Health Checks for On-Premises Controller – grabbing of data from for later investigation, when Controller is no longer accessible
* Extraction and preservation of fine-grained Metric, Flow map and Snapshot data for interesting time ranges (such as load test, application outage, interesting customer load) with goal of investigation and comparison in the future
* Visualization and correlation of Events, Health Rules Snapshots to the Metric data  
* Discovery and data mining of of Snapshots by the types and contents of the Exits (HTTP call and SQL query parameters), Data Collectors, entities involved (Tier, Backend, Error, Service Endpoint and Applications) and Call Graph data

The 3 part [Walkthrough](../../wiki/Home#walkthrough) gives an overview and screenshots of the tool in action.

# Example Reports
## Entity Details
"Entity Timeline View" is part of [Entity Details](../../wiki/Entity-Details-Report) report that is generated for Application and all of its Tiers, Nodes, Business Transactions, Backends, Service Endpoints and Errors. It provides a single-pane view into many things, including:
* 1-minute granularity Metrics in the 1 hour time frame for each hour in the exported range
* Filterable list of Events and Health Rule Violations, arranged in the timeline of that hour, with details of the Event
* Filterable list of Snapshots, broken by Business Transaction and User Experience, arranged in the timeline of that hour, and hotlinked to the specific Snapshot

![Entity Details](../master/docs/introduction/EntityDetailsOverview.png?raw=true)

## Detected Entities
If you ever were presented with a large Controller (or several) full of unknown number of Applications, Tiers and Nodes, you will like the detail provided by [Detected Entities](../../wiki/Detected-Entities-Report) report.

![Detected Entities](../master/docs/introduction/DetectedEntitiesOverview.png?raw=true)

As alternative to Excel, PowerBI visual analytics tool offers advanced exploration possibilities of the same data in [Detected Entities in PowerBI](../../wiki/Detected-Entities-Report-in-PowerBI) report.

![Detected Entities in PowerBI](../master/docs/introduction/DetectedEntitiesPowerBIOverview.png?raw=true)

## Entity Metrics, Graphs and Flow Maps
[Entity Metrics](../../wiki/Entity-Metrics-Report) report shows summary and graphs for all Metrics for each and every detected Application, Tier, Node, Business Transaction, Backend, Service Endpoint, Errors and Information Point. This makes it very valuable in times when you want to rapidly assess hundreds of Applications, Tiers and Business Transactions and see which ones need your attention.

A scatterplot of Calls per Minute vs Average Response time is provided for all types of Entities, allowing you to see what items are both slow and frequently called:
![Entity Metrics](../master/docs/introduction/EntityMetricsTiersHourly.png?raw=true)

Per minute breakdown with ART vs CPM scatter:
![Entity Metrics](../master/docs/introduction/EntityMetricGraphsTiersScatterTransaction.png?raw=true)

All Nodes JVM GC metrics are stored in high granularity forever:
![Entity Metrics](../master/docs/introduction/EntityMetricGraphsTiersGraphsJVMGC.png?raw=true)

To take this a step further, PowerBI visual analytics tool offers advanced, interactive displays of the same data in [Entity Metrics in PowerBI](../../wiki/Entity-Metrics-Report-in-PowerBI) and [Entity Flowmaps in PowerBI](../../wiki/Entity-Flowmaps-Report-in-PowerBI) reports.

![Entity Metrics in PowerBI](../master/docs/introduction/EntityMetricsPowerBITiersARTvsCPM.png?raw=true)

Render Flowmap of activity with Sankey style flow diagram for both Average Response Time and Calls per Minute metrics:
![Entity Flowmaps in PowerBI](../master/docs/introduction/EntityFlowmapsPowerBIApplicationFlow.png?raw=true)

## Snapshots Report
Have you ever wanted to find a snapshot that calls a specific Tier, Backend or Application? 

How about the one that uses specific SQL query? 

And how about the one that has a real Call Graph? 

Or maybe also a special Data Collectors? 

Or how about finding out how many times that special Query was slow in a given time range?

Or discover which classes and methods are called in which Snapshots?

How about all of the above, combined?

In [Snapshots](../../wiki/Snapshots-Report) report you can do all of that, and more.

Snapshot Exit Calls broken by time and duration:
![](../master/docs/introduction/SnapshotsExitCallsType.png?raw=true)

Snapshots with multiple Segments have an enhanced Waterfall view, with “^” caret character indicating exactly when in the Segment execution the Exit Calls occurred
![](../master/docs/introduction/SnapshotsTimelineWaterfall.png?raw=true)

An even better way to explore this data is by using PowerBI visual analytics tool with very interactive and rapid exploration views provided by [Snapshots in PowerBI](../../wiki/Snapshots-Report-in-PowerBI) and [Snapshots Method Calls in PowerBI](../../wiki/Snapshot-Method-Calls-Report-in-PowerBI) reports.

![Snapshots in PowerBI](../master/docs/introduction/SnaphotsPowerBI.png?raw=true)

You can even explore your Call Graph data:
![Snapshot Method Calls in PowerBI](../master/docs/introduction/SnapshotMethodCallsPowerBI.png?raw=true)

## Flame Graph and Flame Chart Reports
[Flame Graphs and Flame Chart](../../wiki/Flame-Graph-Report) reports are an ingenious and useful way to visualize many call graphs in single screen.

Sum of all calls in Application for entire time range
![](../master/docs/introduction/FlameGraphApplication.png?raw=true)

Sum of all calls in Application with Time grouping
/![](../master/docs/introduction/FlameChartApplication.png?raw=true)

## Configuration Report
[Configuration](../../wiki/Configuration-Report) report provides information about Controller Settings and Application configuration as well as comparison of configuration between multiple environments.

Here is an example showing non-default Agent Properties set on multiple Applications in multiple Controllers
![Configuration](../master/docs/introduction/ConfigurationOverview.png?raw=true)

Here is an example showing differences
![Configuration Differences](../master/docs/introduction/ConfigurationComparison.png?raw=true)

## Detected Entities for Server Infrastructure Monitoring and Database Monitoring
Detected entities are not limited to APM Applications.

Server Infrastructure Monitoring inventory of Machines, CPUs, Disks and Processes is available in [Detected SIM Entities](../../wiki/Detected-SIM-Entities-Report) report

Database Monitoring Queries, Sessions, Blocked Sessions and other interesting database-related artifacts can be seen in [Detected DB Entities](../../wiki/Detected-DB-Entities-Report) report.

# Get Started
## Install Application
Download latest release from [Releases](https://github.com/Appdynamics/AppDynamics.DEXTER/releases) section, (AppDynamics.DEXTER.#.#.#.#.zip)

Unzip to some location, for example, `C:\AppDynamics\DEXTER` or `/home/yourusername/DEXTER`.

## Job File
Specify data to operate on with [Job File](../../wiki/Job-File). Review some of the [Job File Examples](../../wiki/Job-File-Examples).

You can find a job file template in `full-win\DefaultJob.json`. Make a copy of it and modify according to your needs using your favorite text editor.

Alternatively, create the Job File using [DEXTER-UI](https://appdynamics.github.io/dexter-ui) visual tool.
 
## Run Application
DEXTER ships with .NET Full Framework 4.7.2 and .NET Core Framework 2.1 versions for full flexibility.

When running on Windows, you can choose to run version built using .NET Full or .NET Core. If you choose .NET Core, you can run portable (where you have to install .NET Core runtime on your OS) or self-contained (where .NET Core runtime is included with application) version of the application.

When running on Mac or Linux, run portable (where you have to install .NET Core runtime on your OS) or self-contained (where .NET Core runtime is included with application) version of the application.

### Windows

Framework | Folder | Command
-- | -- | --
.NET Full <br/>[Install](https://www.microsoft.com/net/download/Windows/run) | full-win | `full-win\AppDynamics.Dexter.exe --jobfile MyJob.json`
.NET Core <br/>[Install](https://www.microsoft.com/net/download/Windows/run), [Prerequisites](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore21) | core | `dotnet core\AppDynamics.Dexter.dll -j MyJob.json`
.NET Core self-contained | core-win | `core-win\AppDynamics.Dexter --jobfile MyJob.json`

### MAC OS

Framework | Folder | Command
-- | -- | --
.NET Core <br/>[Install](https://www.microsoft.com/net/download/macos/run), [Prerequisites](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x) | core | `dotnet core/AppDynamics.Dexter.dll -j MyJob.json`
.NET Core self-contained | core-osx | `core-osx/AppDynamics.Dexter --jobfile MyJob.json`

### Linux

Framework | Folder | Command
-- | -- | --
.NET Core <br/>[Install](https://www.microsoft.com/net/download/linux/run), [Prerequisites](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x) | core | `dotnet core/AppDynamics.Dexter.dll -j MyJob.json`
.NET Core self-contained | core-linux | `core-linux/AppDynamics.Dexter --jobfile MyJob.json`

# Review Results
You will see the results in the [Output](../../wiki/Home#output-folder-structure) folder.

To understand what you are looking at, read [Description of Excel Reports](../../wiki/Home#excel-report-descriptions).

# Useful Links
## Documentation 
If you are new to AppDynamics DEXTER and want an introduction, read through 3 part [Walkthrough](../../wiki/Home#walkthrough).

Learn how the tool works by reading [Documentation](../../wiki) in the project wiki.

## Support
If you need help with issues and/or intepretation of results, read [Getting Support](../../wiki#getting-support).

## Other Location
AppDynamics DEXTER is also hosted on AppDynamics Exchange in [Extensions](https://www.appdynamics.com/community/exchange/extension/appdynamics-dexter-data-extraction-enhanced-reporting/) area.

# Acknowledgements
* Microsoft - Thanks for Visual Studio and .NET Core https://github.com/dotnet/core team for letting us all write code in C# on any platform
* Command Line Parser - Simple and fast https://github.com/gsscoder/commandline 
* CSV File Creation and Parsing - An excellent utility https://github.com/JoshClose/CsvHelper 
* JSON Parsing - NewtonSoft JSON is awesome https://www.newtonsoft.com/json
* Logging - NLog is also awesome http://nlog-project.org/ 
* Excel Report Creation - Jan Kallman's excellent helper class is a lifesaver https://github.com/JanKallman/EPPlus 
* Flame Graphs - Brendan Gregg’s Flame Graph generator https://github.com/brendangregg/FlameGraph was used to as reference to build code to generate Flame Graph reports
