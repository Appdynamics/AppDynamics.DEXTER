using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace AppDynamics.Dexter
{
    /// <summary>
    /// Helper functions for dealing with File IO
    /// </summary>
    public class FileIOHelper
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        #region Basic file and folder reading and writing

        public static bool CreateFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    logger.Trace("Creating folder {0}", folderPath);

                    Directory.CreateDirectory(folderPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Unable to create folder {0}", folderPath);
                logger.Error(ex);

                return false;
            }
        }

        public static bool CreateFolderForFile(string filePath)
        {
            return CreateFolder(Path.GetDirectoryName(filePath));
        }

        public static bool DeleteFolder(string folderPath)
        {
            int tryNumber = 1;

            do
            {
                try
                {
                    if (Directory.Exists(folderPath))
                    {
                        logger.Trace("Deleting folder {0}, try #{1}", folderPath, tryNumber);

                        Directory.Delete(folderPath, true);
                    }
                    return true;
                }
                catch (IOException ex)
                {
                    logger.Error(ex);
                    logger.Error("Unable to delete folder {0}", folderPath);

                    if (ex.Message.StartsWith("The directory is not empty"))
                    {
                        tryNumber++;
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        return false;
                    }
                }
            } while (tryNumber <= 3);

            return true;
        }

        public static bool DeleteFile(string filePath)
        {
            int tryNumber = 1;

            do
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        logger.Trace("Deleting file {0}, try #{1}", filePath, tryNumber);

                        File.Delete(filePath);
                    }
                    return true;
                }
                catch (IOException ex)
                {
                    logger.Error(ex);
                    logger.Error("Unable to delete file {0}", filePath);

                    tryNumber++;
                    Thread.Sleep(3000);
                }
            } while (tryNumber <= 3);

            return true;
        }

        public static bool CopyFolder(string folderPathSource, string folderPathTarget)
        {
            CreateFolder(folderPathTarget);

            foreach (string file in Directory.GetFiles(folderPathSource))
            {
                string dest = Path.Combine(folderPathTarget, Path.GetFileName(file));
                try
                {
                    logger.Trace("Copying file {0} to {1}", file, dest);

                    File.Copy(file, dest, true);
                }
                catch (Exception ex)
                {
                    logger.Error("Unable to copy file {0} to {1}", file, dest);
                    logger.Error(ex);

                    return false;
                }
            }

            foreach (string folder in Directory.GetDirectories(folderPathSource))
            {
                string dest = Path.Combine(folderPathTarget, Path.GetFileName(folder));
                CopyFolder(folder, dest);
            }

            return true;
        }

        public static bool CopyFile(string filePathSource, string filePathDestination)
        {
            CreateFolderForFile(filePathDestination);

            try
            {
                logger.Trace("Copying file {0} to {1}", filePathSource, filePathDestination);

                File.Copy(filePathSource, filePathDestination, true);
            }
            catch (Exception ex)
            {
                logger.Error("Unable to copy file {0} to {1}", filePathSource, filePathDestination);
                logger.Error(ex);

                return false;
            }

            return true;
        }

        public static bool SaveFileToPath(string fileContents, string filePath)
        {
            return SaveFileToPath(fileContents, filePath, true);
        }

        public static bool SaveFileToPath(string fileContents, string filePath, bool writeUTF8BOM)
        {
            string folderPath = Path.GetDirectoryName(filePath);

            if (CreateFolder(folderPath) == true)
            {
                try
                {
                    logger.Trace("Writing string length {0} to file {1}", fileContents.Length, filePath);

                    if (writeUTF8BOM == true)
                    {
                        File.WriteAllText(filePath, fileContents, Encoding.UTF8);
                    }
                    else
                    {
                        Encoding utf8WithoutBom = new UTF8Encoding(false);
                        File.WriteAllText(filePath, fileContents, utf8WithoutBom);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error("Unable to write to file {0}", filePath);
                    logger.Error(ex);
                }
            }

            return false;
        }

        public static TextWriter SaveFileToPathWithWriter(string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);

            if (CreateFolder(folderPath) == true)
            {
                try
                {
                    logger.Trace("Opening TextWriter to file {0}", filePath);

                    return File.CreateText(filePath);
                }
                catch (Exception ex)
                {
                    logger.Error("Unable to write to file {0}", filePath);
                    logger.Error(ex);

                    return null;
                }
            }
            return null;
        }

        public static string ReadFileFromPath(string filePath)
        {
            try
            {
                if (File.Exists(filePath) == false)
                {
                    logger.Warn("Unable to find file {0}", filePath);
                }
                else
                {
                    logger.Trace("Reading file {0}", filePath);
                    return File.ReadAllText(filePath, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to read from file {0}", filePath);
                logger.Error(ex);
            }

            return String.Empty;
        }

        #endregion

        #region JSON file reading and writing

        public static JObject LoadJObjectFromFile(string jsonFilePath)
        {
            try
            {
                if (File.Exists(jsonFilePath) == false)
                {
                    logger.Warn("Unable to find file {0}", jsonFilePath);
                }
                else
                {
                    logger.Trace("Reading JObject from file {0}", jsonFilePath);

                    return JObject.Parse(File.ReadAllText(jsonFilePath));
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to load JSON from file {0}", jsonFilePath);
                logger.Error(ex);
            }

            return null;
        }

        public static bool WriteObjectToFile(object objectToWrite, string jsonFilePath)
        {
            string folderPath = Path.GetDirectoryName(jsonFilePath);

            if (CreateFolder(folderPath) == true)
            {
                try
                {
                    logger.Trace("Writing object {0} to file {1}", objectToWrite.GetType().Name, jsonFilePath);

                    using (StreamWriter sw = File.CreateText(jsonFilePath))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.NullValueHandling = NullValueHandling.Include;
                        serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                        serializer.Serialize(sw, objectToWrite);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error("Unable to write object to file {0}", jsonFilePath);
                    logger.Error(ex);
                }
            }

            return false;
        }

        public static JArray LoadJArrayFromFile(string jsonFilePath)
        {
            try
            {
                if (File.Exists(jsonFilePath) == false)
                {
                    logger.Warn("Unable to find file {0}", jsonFilePath);
                }
                else
                {
                    logger.Trace("Reading JArray from file {0}", jsonFilePath);

                    return JArray.Parse(File.ReadAllText(jsonFilePath));
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to load JSON from file {0}", jsonFilePath);
                logger.Error(ex);
            }

            return null;
        }

        public static bool WriteJArrayToFile(JArray array, string jsonFilePath)
        {
            logger.Trace("Writing JSON Array with {0} elements to file {1}", array.Count, jsonFilePath);

            return WriteObjectToFile(array, jsonFilePath);
        }

        public static List<T> LoadListOfObjectsFromFile<T>(string jsonFilePath)
        {
            try
            {
                if (File.Exists(jsonFilePath) == false)
                {
                    logger.Warn("Unable to find file {0}", jsonFilePath);
                }
                else
                {
                    logger.Trace("Reading List<{0}> from file {1}", typeof(T), jsonFilePath);

                    return JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(jsonFilePath));
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to load JSON from file {0}", jsonFilePath);
                logger.Error(ex);
            }

            return null;
        }

        #endregion

        #region JSON job configuration typed functions

        public static JobConfiguration ReadJobConfigurationFromFile(string configurationFilePath)
        {
            try
            {
                if (File.Exists(configurationFilePath) == false)
                {
                    logger.Warn("Unable to find file {0}", configurationFilePath);
                }
                else
                {
                    logger.Trace("Reading JobConfiguration JSON from job file {0}", configurationFilePath);

                    return JsonConvert.DeserializeObject<JobConfiguration>(File.ReadAllText(configurationFilePath));
                }
            }
            catch (Exception ex)
            {
                loggerConsole.Error(ex.Message);
                logger.Error("Unable to load JobConfiguration JSON from job file {0}", configurationFilePath);
                logger.Error(ex);
            }

            return null;
        }

        public static bool WriteJobConfigurationToFile(JobConfiguration jobConfiguration, string configurationFilePath)
        {
            try
            {
                logger.Trace("Writing JobConfiguration JSON to job file {0}", configurationFilePath);

                using (StreamWriter sw = File.CreateText(configurationFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Include;
                    serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    serializer.Serialize(sw, jobConfiguration);
                }

                return true;
            }
            catch (Exception ex)
            {
                loggerConsole.Error(ex);
                logger.Error("Unable to write JobConfiguration JSON to job file {0}", configurationFilePath);
                logger.Error(ex);
            }

            return false;
        }

        #endregion

        #region XML reading and writing

        public static XmlDocument LoadXmlDocumentFromFile(string xmlFilePath)
        {
            try
            {
                if (File.Exists(xmlFilePath) == false)
                {
                    logger.Warn("Unable to find file {0}", xmlFilePath);
                }
                else
                {
                    logger.Trace("Reading XmlDocument from file {0}", xmlFilePath);

                    XmlDocument doc = new XmlDocument();
                    doc.Load(xmlFilePath);
                    return doc;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to load XML from file {0}", xmlFilePath);
                logger.Error(ex);
            }

            return null;
        }

        #endregion

        #region CSV reading and writing

        public static bool WriteListToCSVFile<T>(List<T> listToWrite, ClassMap<T> classMap, string csvFilePath)
        {
            return WriteListToCSVFile(listToWrite, classMap, csvFilePath, false);
        }

        public static bool WriteListToCSVFile<T>(List<T> listToWrite, ClassMap<T> classMap, string csvFilePath, bool appendToExistingFile)
        {
            if (listToWrite == null) return true;

            string folderPath = Path.GetDirectoryName(csvFilePath);

            if (CreateFolder(folderPath) == true)
            {
                try
                {
                    logger.Trace("Writing list with {0} elements containing type {1} to file {2}, append mode {3}", listToWrite.Count, typeof(T), csvFilePath, appendToExistingFile);

                    if (appendToExistingFile == true && File.Exists(csvFilePath) == true)
                    {
                        // Append without header
                        using (StreamWriter sw = File.AppendText(csvFilePath))
                        {
                            // Always use single New Line character as line separator. Otherwise on Mac and Linux report combination gets messed up
                            sw.NewLine = "\n";
                            CsvWriter csvWriter = new CsvWriter(sw);
                            csvWriter.Configuration.RegisterClassMap(classMap);
                            csvWriter.Configuration.HasHeaderRecord = false;
                            csvWriter.WriteRecords(listToWrite);
                        }
                    }
                    else
                    {
                        // Create new
                        using (StreamWriter sw = File.CreateText(csvFilePath))
                        {
                            sw.NewLine = "\n";
                            CsvWriter csvWriter = new CsvWriter(sw);
                            csvWriter.Configuration.RegisterClassMap(classMap);
                            csvWriter.WriteRecords(listToWrite);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error("Unable to write CSV to file {0}", csvFilePath);
                    logger.Error(ex);
                }
            }

            return false;
        }

        public static MemoryStream WriteListToMemoryStream<T>(List<T> listToWrite, ClassMap<T> classMap)
        {
            try
            {
                if (listToWrite == null) return null;

                logger.Trace("Writing list with {0} elements containing type {1} to memory stream", listToWrite.Count, typeof(T));

                MemoryStream ms = new MemoryStream(1024 * listToWrite.Count);
                StreamWriter sw = new StreamWriter(ms);
                CsvWriter csvWriter = new CsvWriter(sw);
                csvWriter.Configuration.RegisterClassMap(classMap);
                csvWriter.WriteRecords(listToWrite);

                sw.Flush();

                // Rewind the stream
                ms.Position = 0;

                return ms;
            }
            catch (Exception ex)
            {
                logger.Error("Unable to write CSV to memory stream");
                logger.Error(ex);
            }

            return null;
        }

        public static List<T> ReadListFromCSVFile<T>(string csvFilePath, ClassMap<T> classMap)
        {
            try
            {
                logger.Trace("Reading List of type {0} from file {1}", typeof(T), csvFilePath);

                if (File.Exists(csvFilePath) == false)
                {
                    logger.Warn("File {0} does not exist", csvFilePath);
                }
                else
                {
                    using (StreamReader sr = File.OpenText(csvFilePath))
                    {
                        CsvReader csvReader = new CsvReader(sr);
                        csvReader.Configuration.RegisterClassMap(classMap);
                        return csvReader.GetRecords<T>().ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to read CSV from file {0}", csvFilePath);
                logger.Error(ex);
            }

            return new List<T>();
        }

        public static List<T> ReadListFromCSVFileForSnapshotsWithRequestIDs<T>(string csvFilePath, ClassMap<T> classMap, List<string> requestIDs)
        {
            List<T> resultsList = new List<T>();

            try
            {
                logger.Trace("Reading List of type {0} from file {1}", typeof(T), csvFilePath);

                if (File.Exists(csvFilePath) == false)
                {
                    logger.Warn("File {0} does not exist", csvFilePath);
                }
                else
                {
                    using (StreamReader sr = File.OpenText(csvFilePath))
                    {
                        CsvReader csvReader = new CsvReader(sr);
                        csvReader.Configuration.RegisterClassMap(classMap);
                        csvReader.Read();
                        csvReader.ReadHeader();

                        int indexOfRequestID = Array.IndexOf(csvReader.Context.HeaderRecord, "RequestID");

                        // Read all rows
                        // All records in the Snapshots, Exits, Etc, are grouped by RequestID by earlier process
                        // Read all records with this RequestID
                        // For Snapshots, there will be just one record
                        // For all other data like Exits and Method Calls there will be more
                        string currentlyReadingRequestID = String.Empty;
                        while (true)
                        {
                            csvReader.Read();
                            if (csvReader.Context.Record == null)
                            {
                                // At end of file, exit
                                break;
                            }
                            else
                            {
                                if (currentlyReadingRequestID.Length == 0 && requestIDs.Count == 0)
                                {
                                    // Found everything, exit
                                    break;
                                }
                                else
                                {
                                    if (csvReader.Context.Row % 10000 == 0)
                                    {
                                        Console.Write("[{0}].", csvReader.Context.Row);
                                    }

                                    // Are we reading a record with matched requestID?
                                    if (currentlyReadingRequestID.Length > 0)
                                    {
                                        // Yes, we are, and we moved to the next record
                                        // Is this record still part of that request ID?
                                        if (String.Compare(csvReader.Context.Record[indexOfRequestID], currentlyReadingRequestID, true) == 0)
                                        {
                                            // Yes it sure is, read it
                                            T record = csvReader.GetRecord<T>();
                                            resultsList.Add(record);
                                        }
                                        else
                                        {
                                            // No, then reset the currently reading request ID and find the next one
                                            currentlyReadingRequestID = String.Empty;
                                        }
                                    }

                                    // Are we ready to find next requestID?
                                    if (currentlyReadingRequestID.Length == 0)
                                    {
                                        // Yes we moved from previously found request ID
                                        // Find a match out of request IDs
                                        foreach (string requestID in requestIDs)
                                        {
                                            if (String.Compare(csvReader.Context.Record[indexOfRequestID], requestID, true) == 0)
                                            {
                                                // Found a match!
                                                Console.WriteLine("[{0}]={1}", csvReader.Context.Row, requestID);

                                                // Read the first record
                                                T record = csvReader.GetRecord<T>();
                                                resultsList.Add(record);

                                                // Save for the next record, what if it has the same request ID?
                                                currentlyReadingRequestID = requestID;

                                                // Remove that one from the list
                                                requestIDs.Remove(requestID);
                                                
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        Console.WriteLine("[{0}].Done", csvReader.Context.Row);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unable to read CSV from file {0}", csvFilePath);
                logger.Error(ex);
            }

            return resultsList;
        }


        public static bool AppendTwoCSVFiles(string csvToAppendToFilePath, string csvFromWhichToAppendFilePath)
        {
            string folderPath = Path.GetDirectoryName(csvToAppendToFilePath);

            if (CreateFolder(folderPath) == true)
            {
                try
                {
                    logger.Trace("Adding to to CSV file {0} this CSV file {1}", csvToAppendToFilePath, csvFromWhichToAppendFilePath);

                    if (File.Exists(csvFromWhichToAppendFilePath) == true)
                    {
                        if (File.Exists(csvToAppendToFilePath) == true)
                        {
                            // Append without header
                            using (FileStream sr = File.Open(csvFromWhichToAppendFilePath, FileMode.Open))
                            {
                                while (true)
                                {
                                    if (sr.Position == sr.Length) break;

                                    char c = (char)sr.ReadByte();
                                    if (c == '\n' || c == '\r')
                                    {
                                        break;
                                    }
                                }

                                using (FileStream csvToAppendToSW = File.Open(csvToAppendToFilePath, FileMode.Append))
                                {
                                    copyStream(sr, csvToAppendToSW);
                                }
                            }
                        }
                        else
                        {
                            // Create new file with header
                            using (StreamReader sr = File.OpenText(csvFromWhichToAppendFilePath))
                            {
                                using (StreamWriter sw = File.CreateText(csvToAppendToFilePath))
                                {
                                    copyStream(sr.BaseStream, sw.BaseStream);
                                }
                            }
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error("Appending file {0} and file {1} failed", csvToAppendToFilePath, csvFromWhichToAppendFilePath);
                    logger.Error(ex);
                }
            }

            return false;
        }

        public static bool AppendTwoCSVFiles(FileStream csvToAppendToSW, string csvToAppendFilePath)
        {
            try
            {
                logger.Trace("Appending CSV file {0} to another CSV file open as stream", csvToAppendFilePath);

                if (File.Exists(csvToAppendFilePath) == true)
                {
                    using (FileStream sr = File.Open(csvToAppendFilePath, FileMode.Open))
                    {
                        // If the stream to append to is already ahead, that means we don't need headers anymore
                        if (csvToAppendToSW.Position > 0)
                        {
                            // Go through the first line to remove the header
                            while (true)
                            {
                                if (sr.Position == sr.Length) break;

                                char c = (char)sr.ReadByte();
                                if (c == '\n' || c == '\r')
                                {
                                    // Found the end of the first lne
                                    break;
                                }
                            }
                        }

                        copyStream(sr, csvToAppendToSW);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Appending CSV file {0} to another CSV file open as stream", csvToAppendFilePath);
                logger.Error(ex);
            }

            return false;
        }

        private static void copyStream(Stream input, Stream output)
        {
            // 1048576 = 1024*1024 = 2^20 = 1MB
            byte[] buffer = new byte[1048576];
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        #endregion
    }
}
