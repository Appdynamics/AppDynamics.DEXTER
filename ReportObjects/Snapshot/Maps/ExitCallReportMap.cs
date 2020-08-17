using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ExitCallReportMap : ClassMap<ExitCall>
    {
        public ExitCallReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierType).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.BTType).Index(i); i++;

            Map(m => m.SegmentUserExperience).Index(i); i++;
            Map(m => m.SnapshotUserExperience).Index(i); i++;

            Map(m => m.RequestID).Index(i); i++;
            Map(m => m.SegmentID).Index(i); i++;

            Map(m => m.SequenceNumber).Index(i); i++;
            Map(m => m.ToEntityName).Index(i); i++;
            Map(m => m.ToEntityType).Index(i); i++;
            Map(m => m.ToSegmentID).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.Occurred), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.OccurredUtc), i); i++;

            Map(m => m.Duration).Index(i); i++;
            Map(m => m.DurationRange).Index(i); i++;
            Map(m => m.AvgDuration).Index(i); i++;

            Map(m => m.ExitType).Index(i); i++;
            Map(m => m.Detail).Index(i); i++;
            Map(m => m.Framework).Index(i); i++;
            Map(m => m.Method).Index(i); i++;
            Map(m => m.IsAsync).Index(i); i++;

            Map(m => m.CallChain).Index(i); i++;

            Map(m => m.NumCalls).Index(i); i++;

            Map(m => m.HasErrors).Index(i); i++;
            Map(m => m.NumErrors).Index(i); i++;
            Map(m => m.ErrorDetail).Index(i); i++;

            Map(m => m.SQLClauseType).Index(i); i++;
            Map(m => m.SQLJoinType).Index(i); i++;
            Map(m => m.SQLGroupBy).Index(i); i++;
            Map(m => m.SQLHaving).Index(i); i++;
            Map(m => m.SQLOrderBy).Index(i); i++;
            Map(m => m.SQLUnion).Index(i); i++;
            Map(m => m.SQLWhere).Index(i); i++;

            Map(m => m.SQLTable).Index(i); i++;
            Map(m => m.SQLTables).Index(i); i++;
            Map(m => m.NumSQLTables).Index(i); i++;

            Map(m => m.URLCleaned).Index(i); i++;
            Map(m => m.URLScheme).Index(i); i++;
            Map(m => m.URLHost).Index(i); i++;
            Map(m => m.URLPort).Index(i); i++;
            Map(m => m.URLPath).Index(i); i++;
            Map(m => m.URLQuery).Index(i); i++;
            Map(m => m.URLFragment).Index(i); i++;
            Map(m => m.URLNumQueryParams).Index(i); i++;

            Map(m => m.NumProps).Index(i); i++;
            Map(m => m.PropQueryType).Index(i); i++;
            Map(m => m.PropStatementType).Index(i); i++;
            Map(m => m.PropURL).Index(i); i++;
            Map(m => m.PropServiceName).Index(i); i++;
            Map(m => m.PropOperationName).Index(i); i++;
            Map(m => m.PropName).Index(i); i++;
            Map(m => m.PropAsync).Index(i); i++;
            Map(m => m.Prop1Name).Index(i); i++;
            Map(m => m.Prop1Value).Index(i); i++;
            Map(m => m.Prop2Name).Index(i); i++;
            Map(m => m.Prop2Value).Index(i); i++;
            Map(m => m.Prop3Name).Index(i); i++;
            Map(m => m.Prop3Value).Index(i); i++;
            Map(m => m.Prop4Name).Index(i); i++;
            Map(m => m.Prop4Value).Index(i); i++;
            Map(m => m.Prop5Name).Index(i); i++;
            Map(m => m.Prop5Value).Index(i); i++;
            Map(m => m.PropsAll).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
            Map(m => m.ToEntityID).Index(i); i++;
        }
    }
}