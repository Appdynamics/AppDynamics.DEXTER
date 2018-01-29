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
    public class FileIOHelper
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        #region Basic file and folder reading and writing

        public static bool createFolder(string folderPath)
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

        public static bool deleteFolder(string folderPath)
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

        public static bool deleteFile(string filePath)
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

        public static bool saveFileToPath(string fileContents, string filePath)
        {
            return saveFileToPath(fileContents, filePath, true);
        }

        public static bool saveFileToPath(string fileContents, string filePath, bool writeUTF8BOM)
        {
            string folderPath = Path.GetDirectoryName(filePath);

            if (createFolder(folderPath) == true)
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

        public static string readFileFromPath(string filePath)
        {
            try
            {
                logger.Trace("Reading file {0}", filePath);
                return File.ReadAllText(filePath, Encoding.UTF8);
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

        public static JObject loadJObjectFromFile(string jsonFilePath)
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

        public static bool writeObjectToFile(object objectToWrite, string jsonFilePath)
        {
            string folderPath = Path.GetDirectoryName(jsonFilePath);

            if (createFolder(folderPath) == true)
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

        public static JArray loadJArrayFromFile(string jsonFilePath)
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

        public static bool writeJArrayToFile(JArray array, string jsonFilePath)
        {
            logger.Trace("Writing JSON Array with {0} elements to file {1}", array.Count, jsonFilePath);

            return writeObjectToFile(array, jsonFilePath);
        }

        public static List<T> loadListOfObjectsFromFile<T>(string jsonFilePath)
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

        public static JobConfiguration readJobConfigurationFromFile(string configurationFilePath)
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
                loggerConsole.Error(ex);
                logger.Error("Unable to load JobConfiguration JSON from job file {0}", configurationFilePath);
                logger.Error(ex);
            }

            return null;
        }

        public static bool writeJobConfigurationToFile(JobConfiguration jobConfiguration, string configurationFilePath)
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

        public static XmlDocument loadXmlDocumentFromFile(string xmlFilePath)
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

        public static bool writeListToCSVFile<T>(List<T> listToWrite, ClassMap<T> classMap, string csvFilePath)
        {
            return writeListToCSVFile(listToWrite, classMap, csvFilePath, false);
        }

        public static bool writeListToCSVFile<T>(List<T> listToWrite, ClassMap<T> classMap, string csvFilePath, bool appendToExistingFile)
        {
            string folderPath = Path.GetDirectoryName(csvFilePath);

            if (createFolder(folderPath) == true)
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

        public static MemoryStream writeListToMemoryStream<T>(List<T> listToWrite, ClassMap<T> classMap)
        {
            try
            {
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

        public static List<T> readListFromCSVFile<T>(string csvFilePath, ClassMap<T> classMap)
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

            return null;
        }

        public static bool appendTwoCSVFiles(string csvToAppendToFilePath, string csvToAppendFilePath)
        {
            string folderPath = Path.GetDirectoryName(csvToAppendToFilePath);

            if (createFolder(folderPath) == true)
            {
                try
                {
                    logger.Trace("Appending CSV file {0} and file {1}", csvToAppendToFilePath, csvToAppendFilePath);

                    if (File.Exists(csvToAppendFilePath) == true)
                    {
                        if (File.Exists(csvToAppendToFilePath) == true)
                        {
                            // Append without header
                            using (FileStream sr = File.Open(csvToAppendFilePath, FileMode.Open))
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
                            using (StreamReader sr = File.OpenText(csvToAppendFilePath))
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
                    logger.Error("Appending file {0} and file {1} failed", csvToAppendToFilePath, csvToAppendFilePath);
                    logger.Error(ex);
                }
            }

            return false;
        }

        public static bool appendTwoCSVFiles(FileStream csvToAppendToSW, string csvToAppendFilePath)
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
            byte[] buffer = new byte[1024 * 128];
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        #endregion

    }
}
