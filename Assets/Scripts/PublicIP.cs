
namespace Assets.Scripts
{
    class PublicIP // Class used for storing JSON text information recieved from an API call
    {
        public string ip { get; set; } // our computer's public ip address

        public PublicIP()
            {
            // Initialize variable
            ip = null;
            }
    }
}
