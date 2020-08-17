using CsvHelper;
using CsvHelper.Configuration;
using NLog;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace AppDynamics.Dexter
{
    /// <summary>
    /// Helper functions for reading CSV into Excel worksheet
    /// </summary>
    public class EPPlusCSVHelper
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        public static ExcelRangeBase ReadCSVFileIntoExcelRange(MemoryStream csvStream, int skipLinesFromBeginning, Type dtoType, ExcelWorksheet sheet, int startRow, int startColumn)
        {
            logger.Trace("Reading CSV file from memory stream to Excel Worksheet {0} at (row {1}, column {2})", sheet.Name, startRow, startColumn);

            try
            {
                using (StreamReader sr = new StreamReader(csvStream))
                {
                    return ReadCSVIntoExcelRange(sr, skipLinesFromBeginning, dtoType, sheet, startRow, startColumn);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to read CSV from memory stream");
                logger.Error(ex);
            }

            return null;
        }

        public static ExcelRangeBase ReadCSVFileIntoExcelRange(string csvFilePath, int skipLinesFromBeginning, Type dtoType, ExcelWorksheet sheet, int startRow, int startColumn)
        {
            logger.Trace("Reading CSV file {0} to Excel Worksheet {1} at (row {2}, column {3})", csvFilePath, sheet.Name, startRow, startColumn);

            if (File.Exists(csvFilePath) == false)
            {
                logger.Warn("Unable to find file {0}", csvFilePath);

                return null;
            }

            try
            {
                using (StreamReader sr = File.OpenText(csvFilePath))
                {
                    return ReadCSVIntoExcelRange(sr, skipLinesFromBeginning, dtoType, sheet, startRow, startColumn);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to read CSV from file {0}", csvFilePath);
                logger.Error(ex);
            }

            return null;
        }

        public static ExcelRangeBase ReadCSVIntoExcelRange(StreamReader sr, int skipLinesFromBeginning, Type dtoType, ExcelWorksheet sheet, int startRow, int startColumn)
        {
            int csvRowIndex = -1;
            int numColumnsInCSV = 0;
            string[] headerRowValues = null;

            CsvParser csvParser = new CsvParser(sr);

            // mm/dd/yyyy hh:mm:ss is the USA
            // d/MM/yyyy is the AUS
            string cellDateTimeFormat = String.Format("{0} hh:mm:ss", CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);

            Dictionary<string, string> dictionaryColumnTypes = new Dictionary<string, string>(50);

            // Analyze data transfer object property types and prepare Excel mappings
            foreach (PropertyInfo prop in dtoType.GetProperties())
            {
                string underlyingPropertyName = prop.PropertyType.Name;
                if (underlyingPropertyName == "Nullable`1") underlyingPropertyName = prop.PropertyType.GenericTypeArguments[0].Name;

                switch (underlyingPropertyName)
                {
                    case "String":
                        switch (prop.Name)
                        {
                            case "DetailLink":
                            case "MetricGraphLink":
                            case "FlameGraphLink":
                            case "FlameChartLink":
                            case "MetricsListLink":
                                dictionaryColumnTypes.Add(prop.Name, "Hyperlink");
                                break;

                            default:
                                dictionaryColumnTypes.Add(prop.Name, "String");
                                break;
                        }
                        break;

                    case "TimeSpan":
                        dictionaryColumnTypes.Add(prop.Name, "TimeSpan");
                        break;

                    case "DateTime":
                        switch (prop.Name)
                        {
                            case "EventTime":
                                dictionaryColumnTypes.Add(prop.Name, "TimeHourMinute");
                                break;

                            default:
                                dictionaryColumnTypes.Add(prop.Name, "DateTime");
                                break;
                        }
                        break;

                    case "Boolean":
                        dictionaryColumnTypes.Add(prop.Name, "Boolean");
                        break;

                    case "Int32":
                    case "Int64":
                        dictionaryColumnTypes.Add(prop.Name, "Int64");
                        break;

                    case "Double":
                        dictionaryColumnTypes.Add(prop.Name, "Double");
                        break;

                    case "Decimal":
                        dictionaryColumnTypes.Add(prop.Name, "Decimal");
                        break;

                    default:
                        dictionaryColumnTypes.Add(prop.Name, "String");
                        break;
                }
            }

            // Read all rows
            while (true)
            {
                string[] rowValues = csvParser.Read();
                if (rowValues == null)
                {
                    break;
                }
                csvRowIndex++;

                // Grab the headers
                if (csvRowIndex == 0)
                {
                    headerRowValues = rowValues;
                    numColumnsInCSV = headerRowValues.Length;
                }

                // Should we skip?
                if (csvRowIndex < skipLinesFromBeginning)
                {
                    // Skip this line
                    continue;
                }

                // Read row one field at a time
                int csvFieldIndex = 0;
                try
                {
                    foreach (string fieldValue in rowValues)
                    {
                        ExcelRange cell = sheet.Cells[csvRowIndex + startRow - skipLinesFromBeginning, csvFieldIndex + startColumn];

                        if (csvRowIndex == 0)
                        {
                            // output header row right away without any conversions
                            cell.Value = fieldValue;
                        }
                        else
                        {
                            string fieldName = headerRowValues[csvFieldIndex];
                            string fieldDesiredType;
                            if (dictionaryColumnTypes.TryGetValue(fieldName, out fieldDesiredType) == false)
                            {
                                fieldDesiredType = "String";
                            }

                            // Based on previous mapping of column datatypes to what we want in Excel, output it
                            switch (fieldDesiredType)
                            {
                                case "Hyperlink":
                                    cell.Formula = fieldValue;
                                    cell.StyleName = "HyperLinkStyle";

                                    break;

                                case "Boolean":
                                    bool boolValue;
                                    if (Boolean.TryParse(fieldValue, out boolValue) == true)
                                    {
                                        cell.Value = boolValue;
                                    }
                                    else
                                    {
                                        cell.Value = fieldValue;
                                    }
                                    break;

                                case "Int64":
                                    long longValue;
                                    if (Int64.TryParse(fieldValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out longValue) == true)
                                    {
                                        cell.Value = longValue;
                                    }
                                    else
                                    {
                                        cell.Value = fieldValue;
                                    }

                                    break;

                                case "Double":
                                    Double doubleValue;
                                    if (Double.TryParse(fieldValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out doubleValue) == true)
                                    {
                                        cell.Value = doubleValue;
                                    }
                                    else
                                    {
                                        cell.Value = fieldValue;
                                    }
                                    break;

                                case "Decimal":
                                    Decimal decimalValue;
                                    if (Decimal.TryParse(fieldValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out decimalValue) == true)
                                    {
                                        cell.Value = decimalValue;
                                    }
                                    else
                                    {
                                        cell.Value = fieldValue;
                                    }
                                    break;

                                case "DateTime":
                                    DateTime dateTimeValue;

                                    if (DateTime.TryParse(fieldValue, out dateTimeValue))
                                    {
                                        if (fieldName.EndsWith("Utc", StringComparison.InvariantCultureIgnoreCase) == true)
                                        {
                                            cell.Value = dateTimeValue.ToUniversalTime();
                                        }
                                        else
                                        {
                                            cell.Value = dateTimeValue;
                                        }
                                        cell.Style.Numberformat.Format = cellDateTimeFormat;
                                    }
                                    else
                                    {
                                        cell.Value = fieldValue;
                                    }


                                    break;

                                case "TimeHourMinute":
                                    DateTime dateTimeValue2;
                                    if (DateTime.TryParse(fieldValue, out dateTimeValue2))
                                    {
                                        cell.Value = dateTimeValue2;
                                        cell.Style.Numberformat.Format = "hh:mm";
                                    }
                                    else
                                    {
                                        cell.Value = fieldValue;
                                    }
                                    break;


                                case "TimeSpan":
                                    TimeSpan timeSpanValue;
                                    if (TimeSpan.TryParse(fieldValue, out timeSpanValue))
                                    {
                                        cell.Value = timeSpanValue;
                                        cell.Style.Numberformat.Format = "[h]:mm:ss";
                                    }
                                    else
                                    {
                                        cell.Value = fieldValue;
                                    }
                                    break;

                                case "String":
                                default:
                                    // Dump as is
                                    if (fieldValue.Length > 32000)
                                    {
                                        // https://support.office.com/en-us/article/Excel-specifications-and-limits-1672b34d-7043-467e-8e27-269d656771c3
                                        // Total number of characters that a cell can contain 32,767 characters
                                        // Must cut off cell value if it is too big, or risk Excel complaining during sheet load
                                        cell.Value = fieldValue.Substring(0, 32000);
                                    }
                                    else
                                    {
                                        cell.Value = fieldValue;
                                    }
                                    break;
                            }
                        }

                        csvFieldIndex++;
                    }
                }
                catch (ArgumentException ex)
                {
                    if (ex.Message == "Row out of range")
                    {
                        logger.Warn("Max number of rows in sheet {0} reached", sheet.Name);
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    logger.Error(ex);
                    logger.Warn("Max number of rows or cells in sheet {0} reached", sheet.Name);
                    return null;
                }
            }

            return sheet.Cells[startRow, startColumn, startRow + csvRowIndex, startColumn + numColumnsInCSV - 1];
        }

        /// <summary>
        /// The "O" or "o" standard format specifier represents a custom date and time format string using a pattern that preserves time zone information and emits a result string that complies with ISO 8601. 
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#the-round-trip-o-o-format-specifier
        /// </summary>
        /// <param name="map"></param>
        /// <param name="index"></param>
        internal static void setISO8601DateFormat(MemberMap map, int index)
        {
            map.TypeConverterOption.Format("O");
            map.Index(index);
            
            return;
        }
    }
}
