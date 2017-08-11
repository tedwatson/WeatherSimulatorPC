using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour {

    // Input fields for entering latitude and longitude manually
    public InputField latitudeInputField;
    public InputField longitudeInputField;

    // Text to display warning
    public Text warningText;

    // Text to display debug information 
    public Text debugText;

	void Start () {
        
        // Reset warning text
        warningText.text = "";

        // Set the Override Setting to "none" by default
        PlayerPrefs.SetString("Override Setting", "none");
	}

    public void UseCurrentLocationButtonClicked() // Method triggered when user clicks the "Use Current Location" button
    {
        // Set manual location to false
        PlayerPrefs.SetInt("isManualLocation", 0);

        // Begin the simulation
        StartSimulation();
    }

    public void UseManualLocationButtonClicked() // Method triggered when user clicks the "Use Manual Location" button
    {
        // Declare variables for lat and lon
        float latitude;
        float longitude;

        // If the given lat and lon are usable...
        if (float.TryParse(latitudeInputField.text, out latitude) &&
            float.TryParse(longitudeInputField.text, out longitude) &&
            isGoodLatitude(latitude) &&
            isGoodLongitude(longitude))
        {
            // Set manual location to true
            PlayerPrefs.SetInt("isManualLocation", 1);

            // Put the given lat and lon into PlayerPrefs so our Main class can access them
            PlayerPrefs.SetFloat("Latitude", latitude);
            PlayerPrefs.SetFloat("Longitude", longitude);

            // Begin the simulation
            StartSimulation();
        }

        // If the given lat and lon are not usable, tell the user.
        else ShowInputError();
    }

    public void GentleSnowButtonClicked() // Method triggered when user clicks the "Gentle Snow" button
    {
        // Set the override setting to "gentle snow"
        PlayerPrefs.SetString("Override Setting", "gentle snow");

        // Begin the simulation
        StartSimulation();
    }

    public void SnowStormButtonClicked() // Method triggered when user clicks the "Snow Storm" button
    {
        // Set the override setting to "snow storm"
        PlayerPrefs.SetString("Override Setting", "snow storm");

        // Begin the simulation
        StartSimulation();
    }

    public void ClearDayButtonClicked() // Method triggered when user clicks the "Clear Day" button
    {
        // Set the override setting to "clear day"
        PlayerPrefs.SetString("Override Setting", "clear day");

        // Begin the simulation
        StartSimulation();
    }

    public void HailButtonClicked() // Method triggered when user clicks the "Hail" button
    {
        // Set the override setting to "hail"
        PlayerPrefs.SetString("Override Setting", "hail");

        // Begin the simulation
        StartSimulation();
    }

    public void SleetButtonClicked() // Method triggered when user clicks the "Sleet" button
    {
        // Set the override setting to "sleet"
        PlayerPrefs.SetString("Override Setting", "sleet");

        // Begin the simulation
        StartSimulation();
    }

    public void ThunderStormButtonClicked() // Method triggered when user clicks the "Thunderstorm" button
    {
        // Set the override setting to "thunder storm"
        PlayerPrefs.SetString("Override Setting", "thunder storm");

        // Begin the simulation
        StartSimulation();
    }

    void StartSimulation() // Begin the simulation
    {
        SceneManager.LoadScene("Weather Scene");
    }

    bool isGoodLatitude(float lat) // Check to see if the given latitude is an appropriate value
    {
        // On earth, latitudes fall between -90 and 90, inclusive
        if (lat >= -90 && lat <= 90) return true;
        else return false;
    }

    bool isGoodLongitude(float lon) // Check to see if the given longitude is an appropriate value
    {
        // On earth, longitudes fall between -180 and 180, inclusive
        if (lon >= -180 && lon <= 180) return true;
        else return false;
    }

    void ShowInputError() // Tell the user that their lat and lon are unusable
    {
        warningText.text = "Please Provide a suitable Latitude and Longitude";
    }

}
