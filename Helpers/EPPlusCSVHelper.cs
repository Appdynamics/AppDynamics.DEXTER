using OfficeOpenXml;
using System;
using NLog;
using System.IO;
using CsvHelper;
using System.Globalization;

namespace AppDynamics.Dexter
{
    /// <summary>
    /// Helper functions for reading CSV into Excel worksheet
    /// </summary>
    public class EPPlusCSVHelper
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        public static ExcelRangeBase ReadCSVFileIntoExcelRange(MemoryStream csvStream, int skipLinesFromBeginning, ExcelWorksheet sheet, int startRow, int startColumn)
        {
            logger.Trace("Reading CSV file from memory stream to Excel Worksheet {0} at (row {1}, column {2})", sheet.Name, startRow, startColumn);

            try
            {
                using (StreamReader sr = new StreamReader(csvStream))
                {
                    return ReadCSVIntoExcelRage(sr, skipLinesFromBeginning, sheet, startRow, startColumn);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to read CSV from memory stream");
                logger.Error(ex);
            }

            return null;
        }

        public static ExcelRangeBase ReadCSVFileIntoExcelRange(string csvFilePath, int skipLinesFromBeginning, ExcelWorksheet sheet, int startRow, int startColumn)
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
                    return ReadCSVIntoExcelRage(sr, skipLinesFromBeginning, sheet, startRow, startColumn);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to read CSV from file {0}", csvFilePath);
                logger.Error(ex);
            }

            return null;
        }

        public static ExcelRangeBase ReadCSVIntoExcelRage(StreamReader sr, int skipLinesFromBeginning, ExcelWorksheet sheet, int startRow, int startColumn)
        {
            int csvRowIndex = -1;
            int numColumnsInCSV = 0;
            string[] headerRowValues = null;

            CsvParser csvParser = new CsvParser(sr);

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
                        if (fieldValue.StartsWith("=") == true)
                        {
                            cell.Formula = fieldValue;

                            if (fieldValue.StartsWith("=HYPERLINK") == true)
                            {
                                cell.StyleName = "HyperLinkStyle";
                            }
                        }
                        else if (fieldValue.StartsWith("http://") == true || fieldValue.StartsWith("https://") == true)
                        {
                            // If it is in the column ending in Link, I want it to be hyperlinked and use the column name
                            if (headerRowValues[csvFieldIndex] == "Link")
                            {
                                // This is the ART summary table, those links are OK, there are not that many of them
                                cell.Hyperlink = new Uri(fieldValue);
                                cell.Value = "<Go>";
                                cell.StyleName = "HyperLinkStyle";
                            }
                            // Temporarily commenting out until I figure the large number of rows leading to hyperlink corruption thing
                            //else if (headerRowValues[csvFieldIndex].EndsWith("Link"))
                            //{
                            //    cell.Hyperlink = new Uri(fieldValue);
                            //    string linkName = String.Format("<{0}>", headerRowValues[csvFieldIndex].Replace("Link", ""));
                            //    if (linkName == "<>") linkName = "<Go>";
                            //    cell.Value = linkName;
                            //    cell.StyleName = "HyperLinkStyle";
                            //}
                            else
                            {
                                // Otherwise dump it as text
                                cell.Value = fieldValue;
                            }
                        }
                        else
                        {
                            Double numValue;
                            bool boolValue;
                            DateTime dateValue;

                            // Try some casting
                            if (Double.TryParse(fieldValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out numValue) == true)
                            {
                                // Number
                                cell.Value = numValue;
                            }
                            else if (Boolean.TryParse(fieldValue, out boolValue) == true)
                            {
                                // Boolean
                                cell.Value = boolValue;
                            }
                            else if (DateTime.TryParse(fieldValue, out dateValue))
                            {
                                // DateTime
                                cell.Value = dateValue;
                                if (headerRowValues[csvFieldIndex] == "EventTime")
                                {
                                    cell.Style.Numberformat.Format = "hh:mm";
                                }
                                else
                                {
                                    cell.Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss";
                                }
                            }
                            else
                            {
                                // Something else, dump as is

                                // https://support.office.com/en-us/article/Excel-specifications-and-limits-1672b34d-7043-467e-8e27-269d656771c3
                                // Total number of characters that a cell can contain 32,767 characters
                                // Must cut off cell value if it is too big, or risk Excel complaining during sheet load

                                if (fieldValue.Length > 32000)
                                {
                                    cell.Value = fieldValue.Substring(0, 32000);
                                }
                                else
                                {
                                    cell.Value = fieldValue;
                                }
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
                        break;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return sheet.Cells[startRow, startColumn, startRow + csvRowIndex, startColumn + numColumnsInCSV - 1];
        }
    }
}
