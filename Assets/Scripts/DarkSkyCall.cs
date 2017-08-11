
namespace Assets.Scripts
{
    class DarkSkyCall // class for storing information gained from a Dark Sky API call
    {
        public float latitude { get; set; } // The requested latitude.
        public float longitude { get; set; } // The requested longitude.
        public string timezone { get; set; } // The IANA timezone name for the requested location. This is used for text summaries and for determining when hourly and daily data block objects begin.
        public Currently currently { get; set; } // A data point containing the current weather conditions at the requested location.
        public Minutely minutely { get; set; } // A data block containing the weather conditions minute-by-minute for the next hour.
        public Hourly hourly { get; set; } // A data block containing the weather conditions hour-by-hour for the next two days.
        public Daily daily { get; set; } // A data block containing the weather conditions day-by-day for the next week.

        public DarkSkyCall()
        {
            // Initialize variables
            latitude = float.NaN;
            longitude = float.NaN;
            timezone = null;
            currently = new Currently();
            minutely = new Minutely();
            hourly = new Hourly();
            daily = new Daily();
        }
    }
}
