
namespace Assets.Scripts
{
    class TimeZoneDbCall // Class for storing the deserialized JSON text from an API call containing Time Zone information
    {
        public string status { get; set; }// Status of the API query. Either OK or FAILED.
        public string message { get; set; } // Error message. Empty if no error.
        public string countryCode { get; set; } // Country code of the time zone.
        public string countryName { get; set; } // Country name of the time zone.
        public string zoneName { get; set; } // The time zone name.
        public string abbreviation { get; set; } // Abbreviation of the time zone.
        public int gmtOffset { get; set; } // The time offset in seconds based on UTC time.
        public string dst { get; set; } // Whether Daylight Saving Time(DST) is used.Either 0 (No) or 1 (Yes).
        public int dstStart { get; set; } // 	The Unix time in UTC when current time zone start.
        public int dstEnd { get; set; } // The Unix time in UTC when current time zone end.
        public int timestamp { get; set; } // Current local time in Unix time. Minus the value with gmtOffset to get UTC time.
        public string formatted { get; set; } // Formatted timestamp in Y-m-d h:i:s format. E.g.: 2017-07-16 16:57:47

        public TimeZoneDbCall()
        {
            // Initialize Variables
            status = null;
            message = null;
            countryCode = null;
            countryName = null;
            zoneName = null;
            abbreviation = null;
            gmtOffset = -1;
            dst = null;
            dstStart = -1;
            dstEnd = -1;
            timestamp = -1;
            formatted = null;
        }
    }
}
