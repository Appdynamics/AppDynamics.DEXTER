namespace AppDynamics.OfflineData
{
    public class EventId
    {
        // Startup and Shutdown
        public const int APPLICATION_STARTED = 100;
        public const int APPLICATION_ENDED = 101;
        public const int LOG_STARTED = 102;

        // Timing of any activity
        public const int FUNCTION_DURATION_EVENT = 200;

        // Errors and Warnings
        public const int EXCEPTION_GENERIC = 5000;
        public const int EXCEPTION_IO = 5001;
        public const int EXCEPTION_ARGUMENT = 5002;
        public const int EXCEPTION_JSONREADEREXCEPTION = 5003;
        public const int EXCEPTION_JSONWRITEREXCEPTION = 5004;
        public const int EXCEPTION_XMLEXCEPTION = 5005;
        public const int EXCEPTION_INVALID_OPERATION = 5006;

        public const int INVALID_PROGRAM_PARAMETERS = 5020;
        public const int FOLDER_DELETE_FAILED = 5021;
        public const int FOLDER_CREATE_FAILED = 5022;
        public const int INVALID_JSON_FORMAT = 5023;
        public const int UNABLE_TO_READ_FILE = 5024;
        public const int UNABLE_TO_WRITE_FILE = 5025;
        public const int UNABLE_TO_RENDER_JSON = 5026;
        public const int NO_TARGETS_IN_JSON_JOB_FILE = 5027;
        public const int INVALID_TARGET_IN_JSON_JOB_FILE = 5028;
        public const int INVALID_PROPERTY_IN_JSON_JOB_FILE = 5029;
        public const int UNABLE_TO_RENDER_CSV = 5032;
        public const int UNABLE_TO_READ_CSV = 5034;
        public const int INVALID_XML_FORMAT = 5033;

        public const int CONTROLLER_NOT_ACCESSIBLE = 5030;
        public const int CONTROLLER_APPLICATION_DOES_NOT_EXIST = 5031;

        public const int CONTROLLER_REST_API_ERROR = 5010;
        //public const int CONTROLLER_PUBLIC_REST_API_ERROR = 5011;

        // Information and Verbose messages
        public const int FOLDER_CREATE = 2000;
        public const int FOLDER_DELETE = 2001;

        public const int JOBFILE_PARAMETERS = 2002;

        public const int JOBFILE_READ = 2004;
        public const int JOBFILE_WRITE = 2005;

        public const int TARGET_APPLICATION_EXPANDED = 2006;

        public const int JOB_STATUS_INFORMATION = 2007;

        public const int TARGET_STATUS_INFORMATION = 2008;
        public const int JOB_INPUT_AND_OUTPUT_PARAMETERS = 2011;

        public const int FILE_READ = 2009;
        public const int FILE_WRITE = 2010;

        public const int METRIC_RETRIEVAL = 2012;
        public const int FLOWMAP_RETRIEVAL = 2014;

        public const int SNAPSHOT_LIST_RETRIEVAL = 2013;

        public const int SNAPSHOT_RETRIEVAL = 2015;

        public const int CSV_FILE_TO_EXCEL_RANGE = 2016;

        public const int ENTITY_METRICS_RETRIEVAL_FROM_FILE = 2017;

    }
}
