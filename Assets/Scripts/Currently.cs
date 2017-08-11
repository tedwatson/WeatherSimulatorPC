
namespace Assets.Scripts
{
    class Currently // A data point containing the current weather conditions at the requested location.
    {
        public int time { get; set; } // The UNIX time at which this data point begins. minutely data point are always aligned to the top of the minute, hourly data point objects to the top of the hour, and daily data point objects to midnight of the day, all according to the local time zone.
        public string summary { get; set; } // A human-readable text summary of this data point. (This property has millions of possible values, so don’t use it for automated purposes: use the icon property, instead!)
        public string icon { get; set; } // A machine-readable text summary of this data point, suitable for selecting an icon for display. If defined, this property will have one of the following values: clear-day, clear-night, rain, snow, sleet, wind, fog, cloudy, partly-cloudy-day, or partly-cloudy-night. (Developers should ensure that a sensible default is defined, as additional values, such as hail, thunderstorm, or tornado, may be defined in the future.)
        public float nearestStormDistance { get; set; } // The approximate distance to the nearest storm in miles. (A storm distance of 0 doesn’t necessarily refer to a storm at the requested location, but rather a storm in the vicinity of that location.)
        public float precipIntensity { get; set; } // The intensity (in inches of liquid water per hour) of precipitation occurring at the given time. This value is conditional on probability (that is, assuming any precipitation occurs at all) for minutely data points, and unconditional otherwise.
        public float precipProbability { get; set; } // The probability of precipitation occurring, between 0 and 1, inclusive.
        public string precipType { get; set; } // The type of precipitation occurring at the given time. If defined, this property will have one of the following values: "rain", "snow", or "sleet" (which refers to each of freezing rain, ice pellets, and “wintery mix”). (If precipIntensity is zero, then this property will not be defined.)
        public float temperature { get; set; } // The air temperature in degrees Fahrenheit.
        public float apparentTemperature { get; set; } // The apparent (or “feels like”) temperature in degrees Fahrenheit.
        public float dewPoint { get; set; } // The dew point in degrees Fahrenheit.
        public float humidity { get; set; } // The relative humidity, between 0 and 1, inclusive.
        public float windSpeed { get; set; } // The wind speed in miles per hour.
        public float windGust { get; set; } // The wind gust speed in miles per hour.
        public float windBearing { get; set; } // The direction that the wind is coming from in degrees, with true north at 0° and progressing clockwise. (If windSpeed is zero, then this value will not be defined.)
        public float visibility { get; set; } // The average visibility in miles, capped at 10 miles.
        public float cloudCover { get; set; } // The percentage of sky occluded by clouds, between 0 and 1, inclusive.
        public float pressure { get; set; } // The sea-level air pressure in millibars.
        public float ozone { get; set; } // The columnar density of total atmospheric ozone at the given time in Dobson units.

        public Currently()
        {
            // Initialize variables
            time = -1;
            summary = null;
            icon = null;
            nearestStormDistance = float.NaN;
            precipIntensity = float.NaN;
            precipProbability = float.NaN;
            precipType = null;
            temperature = float.NaN;
            apparentTemperature = float.NaN;
            dewPoint = float.NaN;
            humidity = float.NaN;
            windSpeed = float.NaN;
            windGust = float.NaN;
            windBearing = float.NaN;
            visibility = float.NaN;
            cloudCover = float.NaN;
            pressure = float.NaN;
            ozone = float.NaN;
        }
    }
}
