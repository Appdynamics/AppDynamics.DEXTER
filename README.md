
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
* Health Checks for On-Premises Controller – grabbing of data from for later investigation, when Controller is no longer accessible
* Extraction and preservation of fine-grained Metric, Flow map and Snapshot data for interesting time ranges (such as load test, application outage, interesting customer load) with goal of investigation and comparison in the future
* Visualization and correlation of Events, Health Rules Snapshots to the Metric data  
* Discovery and data mining of of Snapshots by the types and contents of the Exits (HTTP call and SQL query parameters), Data Collectors, entities involved (Tier, Backend, Error, Service Endpoint and Applications) and Call Graph data

The 3 part [Walkthrough](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/Getting-Started-Walkthrough) gives an overview and screenshots of the tool in action.

# Example Reports
## Entity Details
"Entity Timeline View" is part of [Entity Details](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/TODO) report that is generated for Application and all of its Tiers, Nodes, Business Transactions, Backends, Service Endpoints and Errors. It provides a single-pane view into many things, including:
*	1-minute granularity Metrics in the 1 hour time frame for each hour in the exported range
*	Filterable list of Events and Health Rule Violations, arranged in the timeline of that hour, with details of the Event
*	Filterable list of Snapshots, broken by Business Transaction and User Experience, arranged in the timeline of that hour, and hotlinked to the specific Snapshot

![Entity Details](../master/docs/introduction/EntityDetailsOverview.png?raw=true)

## Detected Entities
If you ever were presented with a large Controller (or several) full of unknown number of Applications, Tiers and Nodes, you will like the detail provided by [Detected Entities](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/TODO) report.

![Detected Entities](../master/docs/introduction/DetectedEntitiesOverview.png?raw=true)

## Entity Metrics and Graphs
[Entity Metrics](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/TODO) report shows summary and graphs for all Metrics for each and every detected Application, Tier, Node, Business Transaction, Backend, Service Endpoint, Errors and Information Point. This makes it very valuable in times when you want to rapidly assess hundreds of Applications, Tiers and Business Transactions and see which ones need your attention.

A scatterplot of Calls per Minute vs Average Response time is provided for all types of Entities, allowing you to see what items are both slow and frequently called:
![Entity Metrics](../master/docs/walkthrough/part1/EntityMetricsTiersHourly.png?raw=true)

Per minute breakdown with ART vs CPM scatter:
![Entity Metrics](../master/docs/walkthrough/part1/EntityMetricGraphsTiersScatterTransaction.png?raw=true)

All Nodes JVM GC metrics
![Entity Metrics](../master/docs/walkthrough/part1/EntityMetricGraphsTiersGraphsJVMGC.png?raw=true)

## Snapshots Report
Have you ever wanted to find a snapshot that calls a specific Tier, Backend or Application? 

How about the one that uses specific SQL query? 

And how about the one that has a real Call Graph? 

Or maybe also a special Data Collectors? 

Or how about finding out how many times that special Query was slow in a given time range?

Or discover which classes and methods are called in which Snapshots?

How about all of the above, combined?

In [Snapshots](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/TODO) report you can do all of that, and more.

Snapshot Exit Calls broken by time and duration:
![](../master/docs/walkthrough/part2/SnapshotsExitCallsType.png?raw=true)

Snapshots with multiple Segments have an enhanced Waterfall view, with “^” caret character indicating exactly when in the Segment execution the Exit Calls occurred
![](../master/docs/introduction/SnapshotsTimelineWaterfall.png?raw=true)

## Flame Graph and Flame Chart Reports
Using [Flame Graphs and Flame Chart](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/TODO) reports, is an ingenious and useful way to visualize many call graphs in single screen.

Sum of all calls in Application for entire time range
![](../master/docs/walkthrough/part3/FlameGraphApplication.png?raw=true)

Sum of all calls in Application with Time grouping
![](../master/docs/walkthrough/part3/FlameChartApplication.png?raw=true)

## Configuration Report
[Configuration](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/TODO) report provides information about Controller Settings and Application configuration. 

Here is an example showing non-default Agent Properties set on multiple Applications in multiple Controllers
![Configuration](../master/docs/introduction/ConfigurationOverview.png?raw=true)

# Get Started
## Necessary Software
AppDynamics DEXTER is a console application that runs on Windows, MacOS and Linux. You will need:
* Windows, Mac or Linux workstation with decent amount of CPU and disk space. SSD preferred
* Access to the target Controllers running 4.2, 4.3 or 4.4 release of AppDynamics platform
* Access to Excel or other spreadsheet application to render Excel documents

## Install .NET Framework
* Windows - .NET Full 4.7.1 https://www.microsoft.com/net/download/Windows/run
* Linux - .NET Core 2.0 https://www.microsoft.com/net/download/linux/run
* MacOS - .NET Core 2.0 https://www.microsoft.com/net/download/macos/run

## Install Application
Download latest release from [Releases](../../releases) section, (`AppDynamics.DEXTER.#.#.#.#.zip`)

Unzip to some location, for example, `C:\AppDynamics\DEXTER`

## Specify What to Do in Job File
Provide instructions on what to get, from where, and how much in [Job File](../..wiki/Job-File)

## Run Application
When running on Windows, you can choose to run version built using .NET Framework 4.7.1 or .NET Core Framework 2.0.x. When running on Mac or Linux, you need to use .NET Core Framework 2.0.x version.

For example, on Windows:
```
net471\AppDynamics.Dexter.exe --jobfile MyJob.json
```

For example, on MacOS or Linux:
```
dotnet exec netcoreapp2.0/AppDynamics.Dexter.dll -j MyJob.json
```

Review all available command line parameters in [Running Application](../../wiki/Command-Line-Parameters)

## Review Results
You will see the results in the output folder that you specified.

If you did not specify output location, default location on Windows is `C:\AppD.Dexter.Out\<YourJobName>`, and default location on MacOS or Linux is `%HOME%/AppD.Dexter.Out`.

Reports are documented in [Description of Reports](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/TODO) locations

# Useful Links
## Documentation
Review [Documentation](../../wiki) in the project wiki.

## Other location
AppDynamics DEXTER is also hosted on AppDynamics Exchange in [Extensions](https://www.appdynamics.com/community/exchange/extension/appdynamics-dexter-data-extraction-enhanced-reporting/) area.

## Support
Review [Getting Support](https://github.com/Appdynamics/AppDynamics.DEXTER/wiki#getting-support)

## Tools Used
* Microsoft - Thanks for Visual Studio and .NET Core https://github.com/dotnet/core team for letting us all write code in C# on any platform
* Command Line Parser - Simple and fast https://github.com/gsscoder/commandline 
* CSV File Creation and Parsing - An excellent utility https://github.com/JoshClose/CsvHelper 
* JSON Parsing - NewtonSoft JSON is awesome https://www.newtonsoft.com/json
* Logging - NLog is also awesome http://nlog-project.org/ 
* Excel Report Creation - Jan Kallman's excellent helper class is a lifesaver https://github.com/JanKallman/EPPlus 
* Flame Graphs - Brendan Gregg’s Flame Graph generator https://github.com/brendangregg/FlameGraph was used to as reference to build code to generate Flame Graph reports
