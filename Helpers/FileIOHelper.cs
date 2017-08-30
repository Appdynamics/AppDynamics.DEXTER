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

        public static bool saveFileToFolder(string fileContents, string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);

            if (createFolder(folderPath) == true)
            {
                try
                {
                    logger.Trace("Writing string length {0} to file {1}", fileContents.Length, filePath);
                    File.WriteAllText(filePath, fileContents, Encoding.UTF8);

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

        public static CredentialStore readCredentialStoreFromFile(string credentialStorePath)
        {
            try
            {
                if (File.Exists(credentialStorePath) == false)
                {
                    logger.Warn("Unable to find file {0}", credentialStorePath);
                }
                else
                {
                    logger.Trace("Reading CredentialStore JSON from job file {0}", credentialStorePath);

                    return JsonConvert.DeserializeObject<CredentialStore>(File.ReadAllText(credentialStorePath));
                }
            }
            catch (Exception ex)
            {
                loggerConsole.Error(ex);
                logger.Error("Unable to load CredentialStore JSON from job file {0}", credentialStorePath);
                logger.Error(ex);
            }

            return null;
        }

        public static bool writeJobConfigurationToFile(CredentialStore credentialStore, string credentialStorePath)
        {
            try
            {
                logger.Trace("Writing CredentialStore JSON to job file {0}", credentialStorePath);

                using (StreamWriter sw = File.CreateText(credentialStorePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Include;
                    serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    serializer.Serialize(sw, credentialStore);
                }

                return true;
            }
            catch (Exception ex)
            {
                loggerConsole.Error(ex);
                logger.Error("Unable to write CredentialStore JSON to job file {0}", credentialStorePath);
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

        public static bool writeListToCSVFile<T>(List<T> listToWrite, CsvClassMap<T> classMap, string csvFilePath)
        {
            return writeListToCSVFile(listToWrite, classMap, csvFilePath, false);
        }

        public static bool writeListToCSVFile<T>(List<T> listToWrite, CsvClassMap<T> classMap, string csvFilePath, bool appendToExistingFile)
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

        public static List<T> readListFromCSVFile<T>(string csvFilePath, CsvClassMap<T> classMap)
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

        #endregion

    }
}
