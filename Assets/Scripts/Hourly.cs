
namespace Assets.Scripts
{
    class Hourly // A data block containing the weather conditions hour-by-hour for the next two days.
    {
        public string summary { get; set; } // A human-readable text summary of this data point. (This property has millions of possible values, so don’t use it for automated purposes: use the icon property, instead!)
        public string icon { get; set; } // A machine-readable text summary of this data point, suitable for selecting an icon for display. If defined, this property will have one of the following values: clear-day, clear-night, rain, snow, sleet, wind, fog, cloudy, partly-cloudy-day, or partly-cloudy-night. (Developers should ensure that a sensible default is defined, as additional values, such as hail, thunderstorm, or tornado, may be defined in the future.)
        public Data[] data { get; set; } // An array of data points, ordered by time, which together describe the weather conditions at the requested location over time.

        public Hourly()
        {
            // Initialize Variables
            summary = null;
            icon = null;
            data = new Data[48]; // 48 Hours
            for (int h = 0; h < 48; h++)
            {
                data[h] = new Data();
            }
        }
    }
}
