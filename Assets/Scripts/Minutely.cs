
namespace Assets.Scripts
{
    class Minutely // A data block containing the weather conditions minute-by-minute for the next hour.
    {
        public string summary { get; set; } // A human-readable text summary of this data point. (This property has millions of possible values, so don’t use it for automated purposes: use the icon property, instead!)
        public string icon { get; set; } // A machine-readable text summary of this data point, suitable for selecting an icon for display. If defined, this property will have one of the following values: clear-day, clear-night, rain, snow, sleet, wind, fog, cloudy, partly-cloudy-day, or partly-cloudy-night. (Developers should ensure that a sensible default is defined, as additional values, such as hail, thunderstorm, or tornado, may be defined in the future.)
        public Data[] data { get; set; } // An array of data points, ordered by time, which together describe the weather conditions at the requested location over time.

        public Minutely()
        {
            // Initialize Variables
            summary = null;
            icon = null;
            data = new Data[60]; // 60 Minutes
            for (int m = 0; m < 60; m++)
            {
                data[m] = new Data();
            }
        }

    }
}
