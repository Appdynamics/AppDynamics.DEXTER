using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DBApplicationReportMap : ClassMap<DBApplication>
    {
        public DBApplicationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.NumCollectors).Index(i); i++;

            Map(m => m.NumOracle).Index(i); i++;
            Map(m => m.NumMySQL).Index(i); i++;
            Map(m => m.NumSQLServer).Index(i); i++;
            Map(m => m.NumMongo).Index(i); i++;
            Map(m => m.NumPostgres).Index(i); i++;
            Map(m => m.NumDB2).Index(i); i++;
            Map(m => m.NumSybase).Index(i); i++;
            Map(m => m.NumOther).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}
