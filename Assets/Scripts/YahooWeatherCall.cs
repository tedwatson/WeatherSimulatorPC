
namespace Assets.Scripts
{
    class YahooWeatherCall // // Class for storing the deserialized JSON text from an API call containing information from Yahoo Weather
    {
        public Query query { get; set; } // Information from a single API call

        public YahooWeatherCall()
        {
            // Initialize
            query = new Query();
        }
    }

    class Query // Information from a single API call
    {
        public int count; // Number of calls made
        public string created; // Time and date of API call
        public string lang; // Language
        public Y_Results results; // Results of API call

        public Query()
        {
            // Initialize
            count = -1;
            created = null;
            lang = null;
            results = new Y_Results();
        }
    }

    class Y_Results // Results of API call
    {
        public Y_Channel channel; // Class containing weather information classes

        public Y_Results()
        {
            // Initialize
            channel = new Y_Channel();
        }
    }

    class Y_Channel // Class containing weather information classes
    {
        // Classes containing weather information for current time
        public Y_Location location;
        public Y_Wind wind;
        public Y_Atmosphere atmosphere;
        public Y_Astronomy astronomy;

        // Class containing forecast information
        public Y_Item item;

        public Y_Channel()
        {
            // Initialize
            location = new Y_Location();
            wind = new Y_Wind();
            atmosphere = new Y_Atmosphere();
            astronomy = new Y_Astronomy();
            item = new Y_Item();
        }
    }

    class Y_Location // Location information
    {
        public string city;
        public string country;
        public string region;

        public Y_Location()
        {
            // Initialize
            city = null;
            country = null;
            region = null;
        }
    }

    class Y_Wind // Current wind information
    {
        public string chill;
        public string direction;
        public string speed;

        public Y_Wind()
        {
            // Initialize
            chill = null;
            direction = null;
            speed = null;
        }
    }

    class Y_Atmosphere // Current atmosphere information
    {
        public string humidity;
        public string pressure;
        public string rising;
        public string visibility;

        public Y_Atmosphere()
        {
            // Initialize
            humidity = null;
            pressure = null;
            rising = null;
            visibility = null;
        }
    }

    class Y_Astronomy // Sunrise and sunset information
    {
        public string sunrise;
        public string sunset;

        public Y_Astronomy()
        {
            // Initialize
            sunrise = null;
            sunset = null;
        }
    }

    class Y_Item // Class containing forecast information
    {
        public string title;

        // Class containing condition code and temp for current time
        public Y_Condition condition;

        // Array of objects containing forecast information for various days
        public Y_Forecast[] forecast;

        public Y_Item()
        {
            // Initialize
            title = null;
            condition = new Y_Condition();
            forecast = new Y_Forecast[10]; // 10 day forecast 
        }

    }

    class Y_Condition // Class containing condition code and temp for current time
    {
        public string code;
        public string date;
        public string temp;
        public string text;

        public Y_Condition()
        {
            // Initialize
            code = null;
            date = null;
            temp = null;
            text = null;
        }
    }

    class Y_Forecast // Class containing forecast information for a certain day
    {
        public string code;
        public string date;
        public string day;
        public string high;
        public string low;
        public string text;

        public Y_Forecast()
        {
            // Initialize
            code = null;
            date = null;
            day = null;
            high = null;
            low = null;
            text = null;
        }
    }
}
