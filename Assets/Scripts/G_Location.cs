
namespace Assets.Scripts
{
    class G_Location // Object for storing the deserialized JSON text from a Google Location API call
    {
        public G_Results[] results; // Array for storing the results of the API call
    }

    class G_Results // Class for storing the results of a Google Location API call
    {
        public string formatted_address; // Street address, if any, at given latitude and longitude

        public G_Results()
        {
            // Initialize variable
            formatted_address = null;
        } 
    }
}
