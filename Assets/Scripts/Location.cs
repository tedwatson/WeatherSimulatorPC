
namespace Assets.Scripts
{
    class Location // Class used for storing location information retrieved from an API
    {
        // Information on the computer's location
        public string ip { get; set; }
        public string country_code { get; set; }
        public string country_name { get; set; }
        public string region_code { get; set; }
        public string region_name { get; set; }
        public string city { get; set; }
        public string zip_code { get; set; }
        public string time_zone { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string metro_code { get; set; }

    public Location()
        {
            // Initialize variables
            ip = null;
            country_code = null;
            country_name = null;
            region_code = null;
            region_name = null;
            city = null;
            zip_code = null;
            time_zone = null;
            latitude = null;
            longitude = null;
            metro_code = null;
        }
    }
}
