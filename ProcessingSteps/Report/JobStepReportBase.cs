using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class JobStepReportBase : JobStepBase
    {
        #region Constants for Common Reports sheets

        internal const string SHEET_PARAMETERS = "1.Parameters";
        internal const string SHEET_TOC = "2.Contents";

        internal const string TABLE_PARAMETERS_TARGETS = "t_InputTargets";
        internal const string TABLE_TOC = "t_TOC";

        #endregion

        #region Constants for various report colors

        internal static string LEGEND_THICK_LINE = "\u25ac\u25ac\u25ac";

        // This color is from the default theme that seems to be associated with the blueish tables I've picked out
        internal static Color colorLightBlueForDatabars = Color.FromArgb(0x63, 0x8E, 0xC6);
        internal static Color colorRedForDatabars = Color.FromArgb(0xFF, 0x69, 0x69);

        internal static Color colorGreenFor3ColorScales = Color.LightGreen;
        internal static Color colorYellowFor3ColorScales = Color.LightYellow;
        internal static Color colorRedFor3ColorScales = Color.FromArgb(0xFF, 0x69, 0x69);

        // Snapshot colors
        // This color is assigned to the Normal snapshots in the timeline view and list of Snapshots
        internal static Color colorGreenForNormalSnapshots = Color.FromArgb(0x00, 0x99, 0x0);
        // This color is assigned to the Slow snapshots in the timeline view and list of Snapshots
        internal static Color colorYellowForSlowSnapshots = Color.Yellow;
        // This color is assigned to the Slow snapshots in the timeline view and list of Snapshots. Similar to Color.Orange
        internal static Color colorOrangeForVerySlowSnapshots = Color.FromArgb(0xFF, 0xC0, 0x0);
        // This color is assigned to the Stall snapshots in the timeline view and list of Snapshots. Similar to Color.Purple
        internal static Color colorOrangeForStallSnapshots = Color.FromArgb(0x99, 0x33, 0xFF);
        // This color is assigned to the Error snapshots in the timeline view and list of Snapshots. Similar Color.IndianRed
        internal static Color colorRedForErrorSnapshots = Color.FromArgb(0xFF, 0x69, 0x69);

        // Event colors
        // This color is close to Color.LightBlue
        internal static Color colorLightBlueForInfoEvents = Color.FromArgb(0x0, 0x70, 0xC0);
        // This color is orange
        internal static Color colorOrangeForWarnEvents = Color.FromArgb(0xFF, 0xC0, 0x0);
        // This color is close to Color.IndianRed
        internal static Color colorRedForErrorEvents = Color.FromArgb(0xFF, 0x69, 0x69);

        // Configuration comparison colors
        // This color is for differences, kind of pinkish
        internal static Color colorDifferent = Color.FromArgb(0xE8, 0xAF, 0xC3);
        // This color is for missing configuration entries
        internal static Color colorMissing = Color.LightBlue;
        // This color is for extra configuration entries. Similar to Color.Orange
        internal static Color colorExtra = Color.FromArgb(0xFF, 0xC0, 0x0);

        // Hyperlink colors
        internal static Color colorBlueForHyperlinks = Color.Blue;

        // Metric colors
        internal static Color colorMetricART = Color.Green;
        internal static Color colorMetricCPM = Color.Blue;
        internal static Color colorMetricEPM = Color.Red;
        internal static Color colorMetricEXCPM = Color.Orange;
        internal static Color colorMetricHTTPEPM = Color.Pink;

        // Color for the invisible text on graphs report
        internal static Color colorGrayForRepeatedText = Color.LightGray;

        // Colors for Flame Graphs
        // This color is kind of reddish orange
        internal static Color colorFlameGraphStackStart = Color.FromArgb(0xFE, 0x58, 0x10);
        // This color is kind of egg yolk yellow
        internal static Color colorFlameGraphStackEnd = Color.FromArgb(0xFA, 0xF4, 0x38);

        // This color is kind of grasshopper green
        internal static Color colorFlameGraphStackNodeJSStart = Color.FromArgb(0x80, 0xE5, 0x00);
        // This color is kind of greenish olive yellow
        internal static Color colorFlameGraphStackNodeJSEnd = Color.FromArgb(0xA0, 0xBF, 0x00);

        #endregion

        #region Helper functions to build Pivot tables in Excel

        internal static void setDefaultPivotTableSettings(ExcelPivotTable pivot)
        {
            pivot.ApplyWidthHeightFormats = false;
            pivot.DataOnRows = false;
        }

        internal static void addFilterFieldToPivot(ExcelPivotTable pivot, string fieldName)
        {
            addFilterFieldToPivot(pivot, fieldName, eSortType.None);
        }
        internal static void addFilterFieldToPivot(ExcelPivotTable pivot, string fieldName, eSortType sort)
        {
            ExcelPivotTableField fieldF = pivot.PageFields.Add(pivot.Fields[fieldName]);
            fieldF.Sort = sort;
        }

        internal static void addRowFieldToPivot(ExcelPivotTable pivot, string fieldName)
        {
            addRowFieldToPivot(pivot, fieldName, eSortType.None);
        }

        internal static void addRowFieldToPivot(ExcelPivotTable pivot, string fieldName, eSortType sort)
        {
            ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields[fieldName]);
            fieldR.Compact = false;
            fieldR.Outline = false;
            fieldR.SubTotalFunctions = eSubTotalFunctions.None;
            fieldR.Sort = sort;
        }

        internal static void addColumnFieldToPivot(ExcelPivotTable pivot, string fieldName)
        {
            addColumnFieldToPivot(pivot, fieldName, eSortType.None);
        }

        internal static void addColumnFieldToPivot(ExcelPivotTable pivot, string fieldName, eSortType sort)
        {
            ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields[fieldName]);
            fieldC.Compact = false;
            fieldC.Outline = false;
            fieldC.SubTotalFunctions = eSubTotalFunctions.None;
            fieldC.Sort = sort;
        }

        internal static void addDataFieldToPivot(ExcelPivotTable pivot, string fieldName, DataFieldFunctions function)
        {
            addDataFieldToPivot(pivot, fieldName, function, String.Empty);
        }

        internal static void addDataFieldToPivot(ExcelPivotTable pivot, string fieldName, DataFieldFunctions function, string displayName)
        {
            ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields[fieldName]);
            fieldD.Function = function;
            if (displayName.Length != 0)
            {
                fieldD.Name = displayName;
            }
        }

        #endregion

        #region Helper function to render sheets

        internal static void fillReportParametersSheet(ExcelWorksheet sheet, JobConfiguration jobConfiguration, string reportName)
        {

            int l = 1;
            sheet.Cells[l, 1].Value = "Table of Contents";
            sheet.Cells[l, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[l, 2].StyleName = "HyperLinkStyle";
            l++; l++;
            sheet.Cells[l, 1].Value = reportName;
            l++;
            sheet.Cells[l, 1].Value = "Version";
            sheet.Cells[l, 2].Value = Assembly.GetEntryAssembly().GetName().Version;
            l++; l++;
            sheet.Cells[l, 2].Value = "From";
            sheet.Cells[l, 3].Value = "To";
            l++;
            sheet.Cells[l, 1].Value = "Local";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.From.ToLocalTime().ToString("G");
            sheet.Cells[l, 3].Value = jobConfiguration.Input.TimeRange.To.ToLocalTime().ToString("G");
            sheet.Cells[l, 4].Value = TimeZoneInfo.Local.DisplayName;
            l++;
            sheet.Cells[l, 1].Value = "UTC";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.From.ToString("G");
            sheet.Cells[l, 3].Value = jobConfiguration.Input.TimeRange.To.ToString("G");
            l++;
            sheet.Cells[l, 1].Value = "Number of Hour Intervals";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.HourlyTimeRanges.Count;
            l++;
            sheet.Cells[l, 1].Value = "Export Entities";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.DetectedEntities;
            l++;
            sheet.Cells[l, 1].Value = "Export Metrics";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.Metrics;
            l++;
            sheet.Cells[l, 1].Value = "Export Snapshots";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.Snapshots;
            l++;
            sheet.Cells[l, 1].Value = "Export Flowmaps";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.Flowmaps;
            l++;
            sheet.Cells[l, 1].Value = "Export Configuration";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.Configuration;
            l++;
            sheet.Cells[l, 1].Value = "Export Events";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.Events;
            l++;
            sheet.Cells[l, 1].Value = "Export RBAC";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.UsersGroupsRolesPermissions;
            l++;
            sheet.Cells[l, 1].Value = "Export Dashboards";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.Dashboards;
            l++; l++;
            sheet.Cells[l, 1].Value = "Targets:";
            l++; l++;
            ExcelRangeBase range = sheet.Cells[l, 1].LoadFromCollection(from jobTarget in jobConfiguration.Target
                                                                        select new
                                                                        {
                                                                            Controller = jobTarget.Controller,
                                                                            UserName = jobTarget.UserName,
                                                                            Application = jobTarget.Application,
                                                                            ApplicationID = jobTarget.ApplicationID,
                                                                            ApplicationType = jobTarget.Type
                                                                        }, true);
            ExcelTable table = sheet.Tables.Add(range, TABLE_PARAMETERS_TARGETS);
            table.ShowHeader = true;
            table.TableStyle = TableStyles.Medium2;
            table.ShowFilter = true;
            table.ShowTotal = false;

            sheet.Column(1).Width = 25;
            sheet.Column(2).Width = 25;
            sheet.Column(3).Width = 25;

            return;
        }

        internal static void fillTableOfContentsSheet(ExcelWorksheet sheet, ExcelPackage excelReport)
        {
            sheet.Cells[1, 1].Value = "Sheet Name";
            sheet.Cells[1, 2].Value = "# Entities";
            sheet.Cells[1, 3].Value = "Link";
            int rowNum = 1;
            foreach (ExcelWorksheet s in excelReport.Workbook.Worksheets)
            {
                rowNum++;
                sheet.Cells[rowNum, 1].Value = s.Name;
                sheet.Cells[rowNum, 3].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", s.Name);
                sheet.Cells[rowNum, 3].StyleName = "HyperLinkStyle";
                if (s.Tables.Count > 0)
                {
                    sheet.Cells[rowNum, 2].Value = s.Tables[0].Address.Rows - 1;
                }
            }
            ExcelRangeBase range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
            ExcelTable table = sheet.Tables.Add(range, TABLE_TOC);
            table.ShowHeader = true;
            table.TableStyle = TableStyles.Medium2;
            table.ShowFilter = true;
            table.ShowTotal = false;

            sheet.Column(table.Columns["Sheet Name"].Position + 1).Width = 25;
            sheet.Column(table.Columns["# Entities"].Position + 1).Width = 25;

            return;
        }

        #endregion

        #region Helper functions for color creation 

        internal static Color getColorFromHexString(string hexColorString)
        {
            int r = Convert.ToInt32(hexColorString.Substring(0, 2), 16);
            int g = Convert.ToInt32(hexColorString.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColorString.Substring(4, 2), 16);

            return Color.FromArgb(r, g, b);
        }

        #endregion

        #region Helper function for metric range mapping in metric tables

        internal static ExcelRangeBase getSingleColumnRangeFromTable(ExcelTable table, string columnName, int rowIndexStart, int rowIndexEnd)
        {
            // Find index of the important columns
            int columnIndexEventTime = table.Columns[columnName].Position;

            if (rowIndexStart != -1 && rowIndexEnd != -1)
            {
                return table.WorkSheet.Cells[
                    table.Address.Start.Row + rowIndexStart + 1,
                    table.Address.Start.Column + columnIndexEventTime,
                    table.Address.Start.Row + rowIndexEnd + 1,
                    table.Address.Start.Column + columnIndexEventTime];
            }

            return null;
        }

        #endregion

        #region Helper function for various entity naming in Excel

        internal static string getShortenedEntityNameForExcelTable(string entityName, long entityID)
        {
            // First, strip out unsafe characters
            entityName = getExcelTableOrSheetSafeString(entityName);

            // Second, shorten the string 
            if (entityName.Length > 50) entityName = entityName.Substring(0, 50);

            return String.Format("{0}.{1}", entityName, entityID);
        }

        internal static string getShortenedEntityNameForExcelSheet(string entityName, int entityID, int maxLength)
        {
            // First, strip out unsafe characters
            entityName = getExcelTableOrSheetSafeString(entityName);

            // Second, measure the unique ID length and shorten the name of string down
            maxLength = maxLength - 1 - entityID.ToString().Length;

            // Third, shorten the string 
            if (entityName.Length > maxLength) entityName = entityName.Substring(0, maxLength);

            return String.Format("{0}.{1}", entityName, entityID);
        }

        internal static string getShortenedNameForExcelSheet(string sheetName)
        {
            // First, strip out unsafe characters
            sheetName = getExcelTableOrSheetSafeString(sheetName);

            // Second, shorten the string 
            if (sheetName.Length > 32) sheetName = sheetName.Substring(0, 32);

            return sheetName;
        }

        internal static string getExcelTableOrSheetSafeString(string stringToClear)
        {
            char[] excelTableInvalidChars = { ' ', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '=', ',', '/', '\\', '[', ']', ':', '?', '|', '"', '<', '>' };
            foreach (var c in excelTableInvalidChars)
            {
                stringToClear = stringToClear.Replace(c, '-');
            }
            // Apparently it is possible to have a NUL character as a BT name courtesy of penetration testing somehow
            stringToClear = stringToClear.Replace("\u0000", "NULL");

            return stringToClear;
        }

        #endregion

        #region Helper function for various entity naming in Word

        internal static string getShortenedEntityNameForWordBookmark(string entityType, string entityName, long entityID)
        {
            // First, strip out unsafe characters
            entityName = getWordBookmarkSafeString(entityName);

            // Second, measure the unique ID length and shorten the name of string down
            int maxLength = 40;
            maxLength = maxLength - 1 - entityType.Length - 1 - entityID.ToString().Length;

            // Third, shorten the string 
            if (entityName.Length > maxLength) entityName = entityName.Substring(0, maxLength);

            // Can't have first character be number
            if (entityName.Length > 0)
            {
                string firstCharacter = entityName.Substring(0, 1);
                int firstCharacterNumber = -1;
                if (Int32.TryParse(firstCharacter, out firstCharacterNumber) == true)
                { 
                    entityName = String.Format("{0}{1}", "A", entityName.Substring(1));
                }
            }


            return String.Format("{0}.{1}.{2}", entityType, entityName, entityID);
        }

        internal static string getWordBookmarkSafeString(string stringToClear)
        {
            char[] wordBookmarkInvalidChars = { ' ', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '=', ',', '/', '\\', '[', ']', ':', '?', '|', '"', '<', '>' };
            foreach (var c in wordBookmarkInvalidChars)
            {
                stringToClear = stringToClear.Replace(c, '-');
            }
            // Apparently it is possible to have a NUL character as a BT name courtesy of penetration testing somehow
            stringToClear = stringToClear.Replace("\u0000", "NULL");

            return stringToClear;
        }

        #endregion

        internal static void adjustColumnsOfEntityRowTableInMetricReport(string entityType, ExcelWorksheet sheet, ExcelTable table)
        {
            if (entityType == APMApplication.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
            }
            else if (entityType == APMTier.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierType"].Position + 1).Width = 25;
                sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
            }
            else if (entityType == APMNode.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
            }
            else if (entityType == APMBackend.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BackendName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BackendType"].Position + 1).Width = 20;
                sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
            }
            else if (entityType == APMBusinessTransaction.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTType"].Position + 1).Width = 25;
                sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
            }
            else if (entityType == APMServiceEndpoint.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["SEPName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["SEPType"].Position + 1).Width = 25;
                sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
            }
            else if (entityType == APMError.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
            }
            else if (entityType == APMInformationPoint.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["IPName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["IPType"].Position + 1).Width = 20;
                sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
            }
        }

        internal static void adjustColumnsOfActivityFlowRowTableInMetricReport(string entityType, ExcelWorksheet sheet, ExcelTable table)
        {
            sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
            sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
            sheet.Column(table.Columns["CallType"].Position + 1).Width = 10;
            sheet.Column(table.Columns["FromName"].Position + 1).Width = 35;
            sheet.Column(table.Columns["ToName"].Position + 1).Width = 35;
            sheet.Column(table.Columns["From"].Position + 1).Width = 20;
            sheet.Column(table.Columns["To"].Position + 1).Width = 20;
            sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
            sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

            if (entityType == APMApplication.ENTITY_TYPE)
            {
            }
            else if (entityType == APMTier.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
            }
            else if (entityType == APMNode.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
            }
            else if (entityType == APMBackend.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["BackendName"].Position + 1).Width = 20;
            }
            else if (entityType == APMBusinessTransaction.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
            }
        }

        internal static void addUserExperienceConditionalFormatting(ExcelWorksheet sheet, ExcelAddress cfAddressUserExperience)
        {
            var cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressUserExperience);
            cfUserExperience.Style.Font.Color.Color = Color.White;
            cfUserExperience.Style.Fill.BackgroundColor.Color = colorGreenForNormalSnapshots;
            cfUserExperience.Formula = @"=""NORMAL""";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressUserExperience);
            cfUserExperience.Style.Font.Color.Color = Color.Black;
            cfUserExperience.Style.Fill.BackgroundColor.Color = colorYellowForSlowSnapshots;
            cfUserExperience.Formula = @"=""SLOW""";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressUserExperience);
            cfUserExperience.Style.Font.Color.Color = Color.Black;
            cfUserExperience.Style.Fill.BackgroundColor.Color = colorOrangeForVerySlowSnapshots;
            cfUserExperience.Formula = @"=""VERY_SLOW""";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressUserExperience);
            cfUserExperience.Style.Font.Color.Color = Color.White;
            cfUserExperience.Style.Fill.BackgroundColor.Color = colorOrangeForStallSnapshots;
            cfUserExperience.Formula = @"=""STALL""";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressUserExperience);
            cfUserExperience.Style.Font.Color.Color = Color.Black;
            cfUserExperience.Style.Fill.BackgroundColor.Color = colorRedForErrorSnapshots;
            cfUserExperience.Formula = @"=""ERROR""";
        }

        internal static void addDifferenceConditionalFormatting(ExcelWorksheet sheet, ExcelAddress cfAddressDifference)
        {
            var cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressDifference);
            cfUserExperience.Style.Font.Color.Color = Color.Black;
            cfUserExperience.Style.Fill.BackgroundColor.Color = colorDifferent;
            cfUserExperience.Formula = @"=""DIFFERENT""";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressDifference);
            cfUserExperience.Style.Font.Color.Color = Color.Black;
            cfUserExperience.Style.Fill.BackgroundColor.Color = colorExtra;
            cfUserExperience.Formula = @"=""EXTRA""";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressDifference);
            cfUserExperience.Style.Font.Color.Color = Color.Black;
            cfUserExperience.Style.Fill.BackgroundColor.Color = colorMissing;
            cfUserExperience.Formula = @"=""MISSING""";
        }
    }
}
