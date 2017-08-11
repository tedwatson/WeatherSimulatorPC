using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using Assets.Scripts;
using System;
using System.IO;
using DigitalRuby.WeatherMaker;

public class Main : MonoBehaviour {

    // Public Variables for Street Lamp
    public GameObject japaneseStreetLamp;
    public Light lampPointLight;

    // Public Variable for World (to Toggle on/off)
    public GameObject world;

    // Manual Weather Overrides for debugging purposes
    public bool printDebug = false;
    public bool manualWeatherOverride = false;
    public bool gentleSnow = false;
    public bool snowStorm = false;
    public bool thunderstorm = false;
    public bool clearDay = false;
    public bool hail = false;
    public bool sleet = false;

    // Multiplier to dial in cloud speed
    public float cloudSpeedMultiplier = 1f;

    // WeatherMaker Scripts for changing weather parameters
    public WeatherMakerScript weatherMaker;
    public WeatherMakerDayNightCycleScript dayNightCycle;
    public WeatherMakerThunderAndLightningScript thunderAndLightning;
    public WeatherMakerFullScreenFogScript fullScreenFog;
    public WeatherMakerWindScript wind;
    public WeatherMakerSkySphereScript skySphere;

    // Number of minutes to store information locally before looking it up again
    public static double ipStoreMinutes = 60;
    public static double locationStoreMinutes = 60;
    public static double weatherStoreMinutes = 10;

    // Variables to set the speed at which cloud cover and velocity changes
    public float cloudCoverChangeSpeed = 1f;
    public float cloudVelocityChangeSpeed = 1f;

    // Bool to indicate that we're using a manually enterred location
    private bool manualLocation;

    // Lat and Lon for said manual location
    private string manualLatitude;
    private string manualLongitude;

    // Bool set to true until we have updated weather once
    private bool isFirstPass = true;

    // Coroutines for changing cloud cover gradually
    private Coroutine cloudChangeCoroutine;
    private Coroutine cloudVelocityCoroutine_x;
    private Coroutine cloudVelocityCoroutine_y;

    // DateTime variables to store the local and computer times at the start of the process
    private DateTime computerStartTime;
    private DateTime localStartTime;

    // Mathematical constant e
    private const float e = 2.7182818284590452353602874713527f;

    // Bool to set debugMode. If true, will only use locally stored information.
    // This is useful to avoid overusing APIs during extensive testing.
    private bool debugMode = false;
    // Manual Lat and Lon overrides for use during debugMode.
    private string debugLat = "35.8569";
    private string debugLon = "139.6489";

    // Variables for storing the UTC Offset (in seconds) of both the host computer and destination location.
    private int DestinationUtcOffsetInSeconds;
    private int ComputerUtcOffsetInSeconds;

    // URLs for use in calling APIs
    private string ipUrl = "https://api.ipify.org/?format=json";
    private string locationUrl = "http://freegeoip.net/json/";
    private string timeZoneDbUrl = "http://api.timezonedb.com/v2/get-time-zone?key=VZHJKBZ3QFZB&format=json&by=zone&zone=";
    private string weatherUrl = "https://api.darksky.net/forecast/1c6c6640edb8328a5a51501fbeaeed55/";
    private string googleGeocodeUrlFront = "http://maps.googleapis.com/maps/api/geocode/json?latlng=";
    private string googleGeocodeUrlBack = "&sensor=true";
    private string yahooWeatherApiUrlFront = "https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20weather.forecast%20where%20woeid%20in%20(SELECT%20woeid%20FROM%20geo.places%20WHERE%20text%3D%22(";
    private string yahooWeatherApiUrlBack = ")%22)&format=json&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=";

    // Variables to store instances of various objects resulting from API calls.
    private Location myLocation;
    private G_Location myG_Location;
    private TimeZoneDbCall myTimeZoneDbCall;
    private DarkSkyCall myDarkSkyCall;
    private YahooWeatherCall myYahooWeatherCall;

    // Public IP address of computer
    private string myIP;

    // Variables for storing lat and lon, formatted corectly for API urls
    private string myLat;
    private string myLon;
    private string myLatLon;

    // Folder path for storing API information locally
    private static string folderPath;

    // Variables for controlling street lamp settings
    private Color lampOffColor;
    private Color lampOnColor;
    private Renderer lampRenderer;
    private bool lampOn;

    // Variables for storing the sunrise and sunset times at the destination location
    private float sunriseTime;
    private float sunsetTime;

    // Bools for telling the update function to increment cloud cover and velocity
    private bool incrementCloudCover = false;
    private bool decrementCloudCover = false;
    private bool incrementCloudVelocity_x = false;
    private bool incrementCloudVelocity_y = false;
    private bool decrementCloudVelocity_x = false;
    private bool decrementCloudVelocity_y = false;

    public static DateTime UnixToDateTime(double unixTimeStamp) // Converts a date and time from Unix time to DateTime
    {
        /* Unix timestamp is in seconds past unix epoch, so we create a new DateTime at that epoch and add a number of
         * seconds equal to our given Unix time to the new DateTime. */
        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dtDateTime;
    }

    private void Start() // Initialization
    {
        // Start a coroutine to execute the program's main functions
        StartCoroutine(RunWeatherSimulator());
    } 

    private void FixedUpdate() // Runs once every physics update
    {
        // Increment cloud Cover and Velocity, if necessary
        IncrementCloudCover();
        IncrementCloudVelocity();

        // Check to see if we've passed sunset or surise, and turn street lamp on or off accordingly
        UpdateStreetLampLight();
    }

    private void Initialization() // Perform some initialization
    {
        // Turn off world while APIs load
        world.SetActive(false);

        // Look at the PlayerPrefs set in the main menu scene, and set the appropriate bools
        ExaminePlayerPrefs();

        // Perform initialization for street lamp
        InitializeStreetLamp();

        // Initialize and/or create directory for storing API data
        InitializeStorageDirectory();
    }

    private void InitializeDayNightCycle() // Initialize WeatherMaker's Day Night Cycle Parameters
    {
        // Set the Time Zone Offset
        dayNightCycle.TimeZoneOffsetSeconds = DestinationUtcOffsetInSeconds;

        // At the start, both day and night speed should be realtime (1)
        dayNightCycle.Speed = 1;
        dayNightCycle.NightSpeed = 1;

        // Get the local Unix time at our target location
        double localUnixTime = myDarkSkyCall.currently.time;

        // Get the current computer DateTime
        DateTime now = DateTime.Now;

        // If we have (and therefore are using) a DarkSky Weather Call file for this lat and lon...
        if (hasFile(myLatLon))
        {
            // Since we are getting localUnixTime from a file, it will be a little old and we need to correct it.
            // Here, we simply add the file's age (difference between current time and creation time) to localUnixTime in order to correct it.
            DateTime weatherFileCreationTime = File.GetCreationTime(filePath(myLatLon)); // Get the file's creation time
            double weatherFileAgeInSeconds = (now - weatherFileCreationTime).TotalSeconds; // Get the file's age
            localUnixTime += weatherFileAgeInSeconds; // Add the file's age to localUnixTime
        }
        
        // Convert our local Unix time into DateTime format
        DateTime localDateTime = UnixToDateTime(localUnixTime);

        // If we're using a manual location, we need to convert our computer's local DateTime to the local time at our target location
        if (manualLocation) localDateTime = ConvertLocalDateTimeToDestination(localDateTime);

        // Set the current time in WeatherMaker to our local DateTime
        SetWeatherMakerTime(localDateTime);

        // Set the Latitude and Longitude in WeatherMaker
        dayNightCycle.Latitude = double.Parse(myLat);
        dayNightCycle.Longitude = double.Parse(myLon);

        // Set the (computer time) start time to now for use later
        computerStartTime = now;

        // Set the (target location local time) start time to now for use later
        localStartTime = localDateTime;

        // Print local start time if necessary
        if (printDebug) print("localStartTime = " + localStartTime.ToLongDateString() + " " + localStartTime.ToLongTimeString());
    }

    private void ExaminePlayerPrefs() // Look at PlayerPrefs set in the main menu and set our values accordingly
    {
        // Check in the PlayerPrefs for an override set in the menu scene
        if (!PlayerPrefs.GetString("Override Setting").Equals("none"))
        {
            // If so, set the override bool and the matching setting bool to true.
            manualWeatherOverride = true;
            switch (PlayerPrefs.GetString("Override Setting"))
            {
                case "gentle snow":
                    gentleSnow = true;
                    break;
                case "snow storm":
                    snowStorm = true;
                    break;
                case "clear day":
                    clearDay = true;
                    break;
                case "hail":
                    hail = true;
                    break;
                case "sleet":
                    sleet = true;
                    break;
                case "thunder storm":
                    thunderstorm = true;
                    break;
            }
        }
        // Check to see if player selected Manual Location in the main menu
        if (PlayerPrefs.GetInt("isManualLocation") == 1)
        {
            if (printDebug) print("isManualLocation = 1");
            manualLocation = true;
            // Get the player-entered lat and lon from PlayerPrefs
            manualLatitude = PlayerPrefs.GetFloat("Latitude").ToString();
            manualLongitude = PlayerPrefs.GetFloat("Longitude").ToString();
        }
        else manualLocation = false;
    }

    private void InitializeStreetLamp() // Perform some Initialization related to the street lamp
    {
        // Store lamp color values
        lampOffColor = new Color(0.483f, 0.475162f, 0.3693529f);
        lampOnColor = new Color(1.21f, 1.190365f, 0.9252941f);

        // Store the renderer attached to the street lamp
        lampRenderer = japaneseStreetLamp.GetComponent<Renderer>();
    }
    
    private void InitializeStorageDirectory() // Perform initialization for storage directory for API data
    {
        // Set the folder path (for storing API info) based on our platform.
        folderPath = (Application.platform == RuntimePlatform.Android ||
                        Application.platform == RuntimePlatform.IPhonePlayer ?
                        Application.persistentDataPath :
                        Application.dataPath)
                        + "/Resources/";

        // If the directory for our folder path does not already exit, create it
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    private void SetSunriseSunsetTimes() // Set global variables for sunrise and sunset times
    {
        // Get DateTime values for sunrise and sunset from Unix data in our Dark Sky Call
        DateTime sunriseDT = UnixToDateTime(myDarkSkyCall.daily.data[0].sunriseTime);
        DateTime sunsetDT = UnixToDateTime(myDarkSkyCall.daily.data[0].sunsetTime);

        // If we're using a manual location...
        if (manualLocation)
        {
            // We need to convert these DateTimes to their local time zone
            sunriseDT = ConvertLocalDateTimeToDestination(sunriseDT);
            sunsetDT = ConvertLocalDateTimeToDestination(sunsetDT);
        }
        // WeatherMaker represents time of day as total seconds elapsed so far that day, so we
        // format sunriseTime and sunsetTime accordingly.
        sunriseTime = (float)(sunriseDT - new DateTime(sunriseDT.Year,
                                                       sunriseDT.Month,
                                                       sunriseDT.Day)).TotalSeconds;
        sunsetTime = (float)(sunsetDT - new DateTime(sunsetDT.Year,
                                                     sunsetDT.Month,
                                                     sunsetDT.Day)).TotalSeconds;

        // Print sunriseTime and sunsetTime if necessary
        if (printDebug) print("sunriseTime = " + sunriseTime);
        if (printDebug) print("sunsetTime = " + sunsetTime);

    }

    private void SetManualOverrideParameters() // Sets WeatherMaker parameters for any true override bool
    {
        /* This method checks the manual override booleans and, when it finds one that's true, it
         * sets the appropriate WeatherMaker parameters for that weather effect. */
        if (gentleSnow)
        {
            UpdatePercip(1f, "snow", .05f);
            UpdateWind(.2f, .5f);
            UpdateVisibility(.9f);
            UpdateCloudCover(.9f);
        }
        else if (snowStorm)
        {
            UpdatePercip(1f, "snow", 1f);
            UpdateWind(.7f, .5f);
            UpdateVisibility(.3f);
            UpdateCloudCover(1f);
        }
        else if (thunderstorm)
        {
            CheckForThunderstorm("severe thunderstorms");
            UpdatePercip(1f, "rain", 1f);
            UpdateWind(.7f, .5f);
            UpdateVisibility(.7f);
            UpdateCloudCover(1f);
        }
        else if (clearDay)
        {
            UpdatePercip(0f, "", 0f);
            UpdateWind(.1f, .5f);
            UpdateVisibility(1f);
            UpdateCloudCover(.2f);
        }
        else if (hail)
        {
            UpdatePercip(1f, "hail", 1f);
            UpdateWind(.7f, .5f);
            UpdateVisibility(.3f);
            UpdateCloudCover(1f);
        }
        else if (sleet)
        {
            UpdatePercip(1f, "sleet", 1f);
            UpdateWind(.7f, .5f);
            UpdateVisibility(.3f);
            UpdateCloudCover(1f);
        }
    }

    private void IncrementCloudCover() // checks bools and increments cloud cover if necessary
    {
        /* In this method, we check for bools that indicade whether we need to increment or decrement cloud cover
         * If necessary, we change cloud cover using a public variable for change speed */

        if (incrementCloudCover) skySphere.CloudCover += cloudCoverChangeSpeed * Time.deltaTime;
        else if (decrementCloudCover) skySphere.CloudCover -= cloudCoverChangeSpeed * Time.deltaTime;
    } 

    private void IncrementCloudVelocity() // checks bools and increments cloud velocity if necessary
    {
        /* In this method, we check for bools that indicade whether we need to increment or decrement cloud velocity
         * If necessary, we change cloud velocity using a public variable for change speed */

        if (incrementCloudVelocity_x) skySphere.CloudNoiseVelocity.x += cloudVelocityChangeSpeed * Time.deltaTime;
        else if (decrementCloudVelocity_x) skySphere.CloudNoiseVelocity.x -= cloudVelocityChangeSpeed * Time.deltaTime;

        if (incrementCloudVelocity_y) skySphere.CloudNoiseVelocity.y += cloudVelocityChangeSpeed * Time.deltaTime;
        else if (decrementCloudVelocity_y) skySphere.CloudNoiseVelocity.y -= cloudVelocityChangeSpeed * Time.deltaTime;
    } 

    private void UpdateStreetLampLight() // Check to see if we've passed sunset or surise, and turn street lamp on or off accordingly
    {
        // Set a float for WeatherMaker time of day
        float currentTime = dayNightCycle.TimeOfDay;

        // If we have values for sunrise and sunset time
        if (sunriseTime != -1 && sunriseTime != 0 && sunsetTime != -1 && sunsetTime != 0)
        {
            // If the sun is set
            if ((currentTime < sunriseTime || currentTime > sunsetTime) && !lampOn)
            {
                // Turn lamp on
                print("turning lamp on");
                lampRenderer.material.SetColor("_EmissionColor", lampOnColor);
                lampPointLight.enabled = true;
                lampOn = true;
            }

            // If the sun is down
            else if (currentTime > sunriseTime && currentTime < sunsetTime && lampOn)
            {
                // Turn lamp off
                print("turning lamp off");
                lampRenderer.material.SetColor("_EmissionColor", lampOffColor);
                lampPointLight.enabled = false;
                lampOn = false;
            }
        }
    }

    private void GetLatLon() // Get Lat Lon using Location object, or just get it if using manual location
    {
        if (manualLocation)
        {
            // If doing manual location, just use the given values
            myLat = manualLatitude;
            myLon = manualLongitude;
        }
        else
        {
            if (!debugMode)
            {
                // If using automatic location and not in debugMode, get lat and lon from our myLocation object
                myLat = myLocation.latitude;
                myLon = myLocation.longitude;
            }
            else
            {
                // If using debugMode, use our preset debug lat and lon
                myLat = debugLat;
                myLon = debugLon;
            }
        }
        // Format the latlon string correctly for use in API urls
        myLatLon = myLat + "," + myLon;

        // Print if necessary
        if (printDebug) print("myLatLon = " + myLatLon);
    }

    private void UpdateWeather(Data data) // Update the WeatherMaker paramters to those stored in "data"
    {
        // Use the weather icon to determine if there is currently a thunderstorm
        CheckForThunderstorm(data.icon);

        // Update the current percipitation given the probability, type, and intensity
        UpdatePercip(data.precipProbability,
                     data.precipType,
                     data.precipIntensity);

        // Update the current wind given it's speed and bearing
        UpdateWind(data.windSpeed, data.windBearing);

        // Update the current visibility
        UpdateVisibility(data.visibility);

        // Update the current cloud cover
        UpdateCloudCover(data.cloudCover);
    }

    private void UpdateCloudCover(float cloudCover) // Update the current cloud cover
    {
        // If we have data for cloud cover...
        if (!float.IsNaN(cloudCover))
        {
            // Print cloud cover if necessary
            if (printDebug) print("cloud clover = " + cloudCover);

            // If cloud cover is different than current
            if (skySphere.CloudCover != cloudCover)
            {
                // On the first pass, just set the cloud cover in WeatherMaker immediately
                if (isFirstPass && !manualWeatherOverride) skySphere.CloudCover = cloudCover;

                // On subseqeunt passes, use a coroutine to gradually shift cloud cover to new value
                else
                {
                    // If we already have a cloud change coroutine running, stop it
                    if (cloudChangeCoroutine != null)
                    {
                        StopCoroutine(cloudChangeCoroutine);
                        incrementCloudCover = false;
                        decrementCloudCover = false;
                    }
                    // Start coroutine to gradually change cloud cover
                    cloudChangeCoroutine = StartCoroutine(SetCloudCover(cloudCover));
                }
            }
        }
    }

    private void UpdateVisibility(float visibility) // Update the current visibility
    {
        // Set WeatherMaker's fog density parameter to the current visibility
        if (visibility != float.NaN) fullScreenFog.FogDensity = VisibilityToFogDensity(visibility);
    }

    private void UpdateWind(float windSpeed, float windBearing) // Update the current wind given it's speed and bearing
    {
        // If we have data for wind speed...
        if (windSpeed != float.NaN)
        {
            // Print wind speed if necessary
            if (printDebug) print("Wind speed is " + windSpeed);

            // Convert wind speed to intensity and set it in WeatherMaker
            wind.WindIntensity = ConvertWindSpeed(windSpeed);

            /* Next, we want to control cloud movement based on wind speed and bearing.
             * We want this to scale as time speeds up so we'll multiply it by our current speed.
             * We'll also multiply wind speed by a public multiplier so that we can dial it in and make it look nice. */
            windSpeed *= dayNightCycle.Speed * cloudSpeedMultiplier;

            // d is used for cloud speed, we'll scale wind speed down to get this value, since the clouds are far away
            double d = (0.005 * windSpeed);

            // Next, we'll do some math to turn our wind bearing into x and y velocity values
            double x = d * Math.Cos(windBearing % 90);
            double y = d * Math.Sin(windBearing % 90);
            if (windBearing < 180) x *= -1;
            if (windBearing < 90 || windBearing > 270) y *= -1;

            // Next, we'll apply the change in cloud speed in direction
            // If this is our first pass, we simply apply the change
            if (isFirstPass)
            {
                skySphere.CloudNoiseVelocity.x = (float)x;
                skySphere.CloudNoiseVelocity.y = (float)y;
            }

            // If this is not our first pass, we want the change to appear smooth, so we use a coroutine to gradually apply the change
            else
            {
                // Only do this if x has changed
                if (skySphere.CloudNoiseVelocity.x != x)
                {
                    // If we already have a cloud velocity coroutine for x running, stop it
                    if (cloudVelocityCoroutine_x != null)
                    {
                        StopCoroutine(cloudVelocityCoroutine_x);
                        incrementCloudVelocity_x = false;
                        decrementCloudVelocity_x = false;
                    }
                    // Start coroutine to gradually change x
                    cloudVelocityCoroutine_x = StartCoroutine(SetCloudVelocity_x((float)x));
                }
                // Only do this if y has changed
                if (skySphere.CloudNoiseVelocity.y != y)
                {
                    // If we already have a cloud velocity coroutine for y running, stop it
                    if (cloudVelocityCoroutine_y != null)
                    {
                        StopCoroutine(cloudVelocityCoroutine_y);
                        incrementCloudVelocity_y = false;
                        decrementCloudVelocity_y = false;
                    }
                    // Start coroutine to gradually change y
                    cloudVelocityCoroutine_y = StartCoroutine(SetCloudVelocity_y((float)y));
                }
            }

        }
    }

    private void UpdatePercip(float probability, string type, float intensity) // Update the current percipitation given the probability, type, and intensity
    {
        /* In this method, we will use a random roll in conjunction with the percipitation probability to determine
         * if parcipitation occurs. 
         * We will then set the appropriate percipitation type and intensity. */

        // Declare a variable for the result of the percip roll, set it to false by default
        bool percipRollAchieved = false;

        // Print the percip type and probability, if necessary
        if (!string.IsNullOrEmpty(type)) if (printDebug) print("Percip type is " + type);
        if (probability != float.NaN) if (printDebug) print("Percip probability is " + probability);

        // If the data does not include a probability...
        if (probability == float.NaN)
        {
            // If the data does not have a percip type, fail the percip roll
            if (string.IsNullOrEmpty(type)) percipRollAchieved = false;

            // If it does have a percip type, pass the percip roll
            else percipRollAchieved = true;
        }

        // If there is some chance of percipitation...
        else if (probability > 0)
        {
            // Roll for Percipitation
            float percipRoll = UnityEngine.Random.Range(0, 1f);

            // Print the roll result if necessary
            if (printDebug) print("percipRoll = " + percipRoll);

            // Check to see if the roll passes
            if (probability >= percipRoll)
            {
                percipRollAchieved = true;
                if (printDebug) print("percip roll achieved!");
            }
            else
            {
                percipRollAchieved = false;
                if (printDebug) print("percip roll failed");
            }
        }

        // If the percip roll passed...
        if ((!string.IsNullOrEmpty(type)) && percipRollAchieved)
        {
            // Set the percipitation type in WeatherMaker
            switch (type)
            {
                case "rain":
                    if (printDebug) print("raining");
                    weatherMaker.Precipitation = WeatherMakerPrecipitationType.Rain;
                    break;
                case "snow":
                    if (printDebug) print("snowing");
                    weatherMaker.Precipitation = WeatherMakerPrecipitationType.Snow;
                    break;
                case "sleet":
                    if (printDebug) print("sleeting");
                    weatherMaker.Precipitation = WeatherMakerPrecipitationType.Sleet;
                    break;
                case "hail":
                    if (printDebug) print("hailing");
                    weatherMaker.Precipitation = WeatherMakerPrecipitationType.Hail;
                    break;
                default:
                    weatherMaker.Precipitation = WeatherMakerPrecipitationType.None;
                    break;
            }
            // If the data does not have a value for intensity...
            if (intensity == float.NaN)
            {
                // Set the value to 0.5, aka medium intensity
                if (printDebug) print("percip intensity is NaN, setting intensity to 0.5");
                weatherMaker.PrecipitationIntensity = 0.5f;
            }
            // If the data does have a value for intensity...
            else
            {
                // Set the WeatherMaker intensity to this value
                if (printDebug) print("percip intensity is " + ConvertPercipIntensity(intensity));
                weatherMaker.PrecipitationIntensity = ConvertPercipIntensity(intensity);
            }
        }
        // If the percip roll failed...
        else
        {
            // Turn off all percipitation in WeatherMaker
            weatherMaker.Precipitation = WeatherMakerPrecipitationType.None;
            weatherMaker.PrecipitationIntensity = 0;
        }
    }

    private void CheckForThunderstorm(string icon) // Use the weather icon to determine if there is currently a thunderstorm
    {
        // If we have a thunderstorm...
        if (!string.IsNullOrEmpty(icon) && (icon.Equals("thunderstorm") ||
                                            icon.Equals("thunderstorms") ||
                                            icon.Equals("severe thunderstorm") ||
                                            icon.Equals("severe thunderstorms") ||
                                            icon.Equals("thunder") ||
                                            icon.Equals("severe thunder")))
        {
            // Enable Thunder and Lightning
            thunderAndLightning.EnableLightning = true;

            // Print if necessary
            if (printDebug) print("thunderstorm");

            // If the thundertorm is severe...
            if (icon.Contains("severe"))
            {
                // More lightning strikes per minute
                thunderAndLightning.LightningIntervalTimeRange.Maximum = 12.5f;
                thunderAndLightning.LightningIntervalTimeRange.Minimum = 5f;
            }
            else // If the thunderstorm is not severe...
            {
                // Fewer lightning strikes per minute
                thunderAndLightning.LightningIntervalTimeRange.Maximum = 25f;
                thunderAndLightning.LightningIntervalTimeRange.Minimum = 10f;
            }
        }
        // If there is no thunderstorm, make sure thunder and lightning is turned off
        else thunderAndLightning.EnableLightning = false;
    }

    private void ImportLocation(string text) // Import JSON text into a Location object
    {
        // Deserialize the JSON text
        myLocation = JsonConvert.DeserializeObject<Location>(text);

        // Print if necessary
        if (!manualLocation) if (printDebug) print("My location is " + myLocation.city);
    }

    private void ImportG_Location(string text) // Import JSON text into a G_Location object
    {
        // Deserialize the JSON text
        myG_Location = JsonConvert.DeserializeObject<G_Location>(text);

        // Print if necessary
        if (printDebug) print("My location is " + myG_Location.results[0].formatted_address);
    }

    private void DS_ImportWeather(string text) // Import JSON text into a DarkSkyCall object
    {
        // Initialize the DarkSkyCall object
        myDarkSkyCall = new DarkSkyCall();

        // Deserialize the JSON text
        myDarkSkyCall = JsonConvert.DeserializeObject<DarkSkyCall>(text);

        // Print if necessary
        if (printDebug) print("The weather is " + myDarkSkyCall.currently.icon);
        if (printDebug) print("The temperature is " + myDarkSkyCall.currently.temperature);
    }

    private void Y_ImportWeather(string text) // Import JSON text into a YahooWeatherCall object
    {
        // Initialize the YahooWeatherCall object
        myYahooWeatherCall = new YahooWeatherCall();

        // Deserialize the JSON text
        myYahooWeatherCall = JsonConvert.DeserializeObject<YahooWeatherCall>(text);

        // Print if necessary
        if (printDebug) print("The Yahoo weather is " + myYahooWeatherCall.query.results.channel.item.condition.text);
        if (printDebug) print("The Yahoo temperature is " + myYahooWeatherCall.query.results.channel.item.condition.temp);
    }

    private void CorrectDarkSkyData() // Use Yahoo Weather data to correct Dark Sky data, if needed
    {
        /* Dark Sky is great because it provides a lot of very specific weather data, as well as minute-by-minute
         * forecast for the next hour and hour-by-hour forecasts for the next 48 hours.
         * However, while Dark Sky is reliable in North America, it is offen innacurate in other areas of the world.
         * Yahoo Weather is useful here, because while it does not offer the same level of specificity, is is ofen
         * more accurate outside of North America.
         * In this method, we compare our Dark Sky data to our Yahoo Weather data, and if the difference is outside
         * of an acceptable range, we replace the Dark Sky parameter with information from Yahoo Weather.
         * This allows us to keep the specificity of information from Dark Sky, where available, without compromizing
         * accuracy outside of North America. */

        // Check next 60 minutes
        for (int m = 0; m < 60; m++)
        {
            // Get the Yahoo Condition Code
            int code;
            if (m < 15) code = int.Parse(myYahooWeatherCall.query.results.channel.item.condition.code);
            else if ((dayNightCycle.TimeOfDay + (m * 60)) <= 86400)
                code = int.Parse(myYahooWeatherCall.query.results.channel.item.forecast[0].code);
            else code = int.Parse(myYahooWeatherCall.query.results.channel.item.forecast[1].code);
            // Check to see if Dark Sky parameters are in acceptable range, given Yahoo code
            CorrectDarkSkyDataUsingYahooCode(myDarkSkyCall.minutely.data[m], code);
        }

        // Check next 48 hours
        for (int h = 0; h < 48; h++)
        {
            // Get the Yahoo Condition Code
            int code;
            if (h == 0) code = int.Parse(myYahooWeatherCall.query.results.channel.item.condition.code);
            else if ((dayNightCycle.TimeOfDay + (h * 60 * 60)) <= 86400)
                code = int.Parse(myYahooWeatherCall.query.results.channel.item.forecast[0].code);
            else code = int.Parse(myYahooWeatherCall.query.results.channel.item.forecast[1].code);
            // Check to see if Dark Sky parameters are in acceptable range, given Yahoo code
            CorrectDarkSkyDataUsingYahooCode(myDarkSkyCall.hourly.data[h], code);
        }

        // Check next 7 days
        for (int d = 0; d < 7; d++)
        {
            // Get the Yahoo Condition Code
            int code = int.Parse(myYahooWeatherCall.query.results.channel.item.forecast[d].code);
            // Check to see if Dark Sky parameters are in acceptable range, given Yahoo code
            CorrectDarkSkyDataUsingYahooCode(myDarkSkyCall.daily.data[d], code);
        }
    }

    private void CorrectDarkSkyDataUsingYahooCode(Data data, int code) // Check to see if Dark Sky parameters are in acceptable range, given Yahoo code
    {
        /* Here, we use a switch statement to check the appropriate parameters of our Dark Sky call
         * If those parameters are outside of a range that makes sense given the Yahoo Weather code,
         * we manually set that parameter to an acceptable value */

        switch (GetStringFromConditionCode(code)) // Turns the int code into a string describing that weather condition
        {
            case "severe thunderstorms":
                data.icon = "severe thunderstorms";
                break;
            case "thunderstorms":
                data.icon = "thunderstorms";
                break;
            case "mixed rain and snow":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "rain";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "mixed rain and sleet":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "rain";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "mixed snow and sleet":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "snow";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "freezing drizzle":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "rain";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .05) data.precipIntensity = .05f;
                break;
            case "drizzle":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "rain";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .05) data.precipIntensity = .05f;
                break;
            case "freezing rain":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "rain";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "showers":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "rain";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "snow flurries":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "snow";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .05) data.precipIntensity = .05f;
                break;
            case "light snow showers":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "snow";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .05) data.precipIntensity = .05f;
                break;
            case "blowing snow":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "snow";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .05) data.precipIntensity = .05f;
                break;
            case "snow":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "snow";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "hail":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "hail";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "sleet":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "sleet";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "foggy":
                data.foggy = true;
                break;
            case "windy":
                if (data.windSpeed < 3) data.windSpeed = 3f;
                break;
            case "cloudy":
                if (data.cloudCover < .5) data.cloudCover = .5f;
                break;
            case "mostly cloudy (night)":
                if (data.cloudCover < .35) data.cloudCover = .35f;
                break;
            case "mostly cloudy(day)":
                if (data.cloudCover < .35) data.cloudCover = .35f;
                break;
            case "partly cloudy(night)":
                if (data.cloudCover < .15) data.cloudCover = .15f;
                break;
            case "partly cloudy(day)":
                if (data.cloudCover < .15) data.cloudCover = .15f;
                break;
            case "mixed rain and hail":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "rain";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "heavy snow":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "snow";
                if (data.precipProbability < .35) data.precipProbability = .35f;
                if (data.precipIntensity < .5) data.precipIntensity = .5f;
                break;
            case "partly cloudy":
                if (data.cloudCover < .15) data.cloudCover = .15f;
                break;
            case "thundershowers":
                data.icon = "thunderstorms";
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "rain";
                if (data.precipProbability < .1) data.precipProbability = .1f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
            case "snow showers":
                if (string.IsNullOrEmpty(data.precipType)) data.precipType = "snow";
                if (data.precipProbability < .1) data.precipProbability = .1f;
                if (data.precipIntensity < .1) data.precipIntensity = .1f;
                break;
        }
    }

    private void ReportDarkSkyDataCompleteness() // Analyze completeness of DarkSky Data, and print analysis if necessary
    {
        // This method goes through our myDarkSkyCall object and determines which areas contain data, and what is missing
        bool hasCurrently = false;
        int minutelyCount = 0;
        int hourlyCount = 0;
        int dailyCount = 0;

        // Currently
        if (!string.IsNullOrEmpty(myDarkSkyCall.currently.summary)) hasCurrently = true;

        // Minutely
        for (int m = 0; m < 60; m++)
        {
            if (!string.IsNullOrEmpty(myDarkSkyCall.minutely.data[m].summary)) minutelyCount++;
            else break;
        }

        // Hourly
        for (int h = 0; h < 48; h++)
        {
            if (!string.IsNullOrEmpty(myDarkSkyCall.hourly.data[h].summary)) hourlyCount++;
            else break;
        }

        // Daily
        for (int d = 0; d < 7; d++)
        {
            if (!string.IsNullOrEmpty(myDarkSkyCall.daily.data[d].summary)) dailyCount++;
            else break;
        }

        if (printDebug) print("Dark Sky call does " + (hasCurrently ? "" : "not ") + "have current weather. Has first " + minutelyCount + " minutes, " + hourlyCount + " hours, and " + dailyCount + " days.");
    }

    private void CopyPasteFromCurrentlyToFirstMinute() // Copy the "Currently" data from our myDarkSkyCall object and paste it into Minute 0 of the same call. We do this because the Minute data is often empty.
    {
        myDarkSkyCall.minutely.data[0].time = myDarkSkyCall.currently.time;
        myDarkSkyCall.minutely.data[0].summary = myDarkSkyCall.currently.summary;
        myDarkSkyCall.minutely.data[0].icon = myDarkSkyCall.currently.icon;
        myDarkSkyCall.minutely.data[0].precipIntensity = myDarkSkyCall.currently.precipIntensity;
        myDarkSkyCall.minutely.data[0].precipProbability = myDarkSkyCall.currently.precipProbability;
        myDarkSkyCall.minutely.data[0].precipType = myDarkSkyCall.currently.precipType;
        myDarkSkyCall.minutely.data[0].temperature = myDarkSkyCall.currently.temperature;
        myDarkSkyCall.minutely.data[0].apparentTemperature = myDarkSkyCall.currently.apparentTemperature;
        myDarkSkyCall.minutely.data[0].dewPoint = myDarkSkyCall.currently.dewPoint;
        myDarkSkyCall.minutely.data[0].humidity = myDarkSkyCall.currently.humidity;
        myDarkSkyCall.minutely.data[0].windSpeed = myDarkSkyCall.currently.windSpeed;
        myDarkSkyCall.minutely.data[0].windGust = myDarkSkyCall.currently.windGust;
        myDarkSkyCall.minutely.data[0].windBearing = myDarkSkyCall.currently.windBearing;
        myDarkSkyCall.minutely.data[0].visibility = myDarkSkyCall.currently.visibility;
        myDarkSkyCall.minutely.data[0].cloudCover = myDarkSkyCall.currently.cloudCover;
        myDarkSkyCall.minutely.data[0].pressure = myDarkSkyCall.currently.pressure;
        myDarkSkyCall.minutely.data[0].ozone = myDarkSkyCall.currently.ozone;
        myDarkSkyCall.minutely.data[0].time = myDarkSkyCall.currently.time;
        myDarkSkyCall.minutely.data[0].time = myDarkSkyCall.currently.time;
        myDarkSkyCall.minutely.data[0].time = myDarkSkyCall.currently.time;
        myDarkSkyCall.minutely.data[0].time = myDarkSkyCall.currently.time;
        myDarkSkyCall.minutely.data[0].time = myDarkSkyCall.currently.time;
        myDarkSkyCall.minutely.data[0].time = myDarkSkyCall.currently.time;
    }

    private void SetWeatherMakerTime(DateTime dt) // Set the current time in WeatherMaker
    {
        // In WeatherMaker, the time of day is represented as the total number of secconds elapsed so far that day.
        dayNightCycle.TimeOfDay = (float)(dt - new DateTime(dt.Year, dt.Month, dt.Day)).TotalSeconds;

        dayNightCycle.Year = dt.Year;
        dayNightCycle.Month = dt.Month;
        dayNightCycle.Day = dt.Day;
    }

    private static void DeleteOldFiles() // Check to see if any files in our storage directory are too old, and if so delete them
    {
        // Create local variabes for current time and deadline (the DateTime by which a file should be destroyed)
        DateTime now = DateTime.Now;
        DateTime deadline;

        foreach (string file in Directory.GetFiles(folderPath)) // for every file in our resource directory
        {
            // Get the name of the file as a string
            string fileName = file.ToString();

            // if the file does not have ".meta" in it's name (we want to ignore .meta files)
            if (!fileName.Substring(fileName.Length - ".meta".Length).Equals(".meta"))
            {
                // remove resource path
                fileName = fileName.Substring(folderPath.Length);

                // remove .txt
                fileName = fileName.Substring(0, fileName.Length - ".txt".Length);

                // for now, set deadline equal to the time this file was created
                deadline = fileCreatedTime(fileName);

                // Add the appropriate number of minutes to deadline, depending on the file's name
                switch (fileName)
                {
                    case "ip":
                        deadline = deadline.AddMinutes(ipStoreMinutes);
                        break;
                    case "Location":
                        deadline = deadline.AddMinutes(locationStoreMinutes);
                        break;
                    default:
                        deadline = deadline.AddMinutes(weatherStoreMinutes);
                        break;
                }

                // If the deadline has passed, delete the file
                if (DateTime.Compare(now, deadline) > 0) DeleteFile(fileName);
            }
        }
    }

    private static void DeleteFile(string fileName) // Delete the file with this name in our storage directory
    {
        File.Delete(filePath(fileName));
    }

    private static void WriteFile(string name, string text) // Write a file with this name and text into our storage directory
    {
        File.WriteAllText(filePath(name), text);
        File.SetCreationTime(filePath(name), DateTime.Now);
    }

    private bool hasFile(string name) // Returns true if the file exists in our storage directory, otherwise returns false
    {
        // for each file in our storage directory..
        foreach (string file in Directory.GetFiles(folderPath))
        {
            // Get it's file name in a string
            string fileName = file.ToString();
            if (!fileName.Substring(fileName.Length - ".meta".Length).Equals(".meta")) // ignore meta files
            {
                fileName = fileName.Substring(folderPath.Length); // remove resource path
                fileName = fileName.Substring(0, fileName.Length - ".txt".Length); // remove .txt

                // if the names match, return true
                if (fileName.Equals(name)) return true;
            }
        }
        // Since we found no matches, return false
        return false;
    }

    private string GetStringFromConditionCode(int code) // Turns the int code into a string describing that weather condition
    {
        switch (code)
        {
            case 0: return "tornado";
            case 1: return "tropical storm";
            case 2: return "hurricane";
            case 3: return "severe thunderstorms";
            case 4: return "thunderstorms";
            case 5: return "mixed rain and snow";
            case 6: return "mixed rain and sleet";
            case 7: return "mixed snow and sleet";
            case 8: return "freezing drizzle";
            case 9: return "drizzle";
            case 10: return "freezing rain";
            case 11: return "showers";
            case 12: return "showers";
            case 13: return "snow flurries";
            case 14: return "light snow showers";
            case 15: return "blowing snow";
            case 16: return "snow";
            case 17: return "hail";
            case 18: return "sleet";
            case 19: return "dust";
            case 20: return "foggy";
            case 21: return "haze";
            case 22: return "smoky";
            case 23: return "blustery";
            case 24: return "windy";
            case 25: return "cold";
            case 26: return "cloudy";
            case 27: return "mostly cloudy (night)";
            case 28: return "mostly cloudy(day)";
            case 29: return "partly cloudy(night)";
            case 30: return "partly cloudy(day)";
            case 31: return "clear(night)";
            case 32: return "sunny";
            case 33: return "fair(night)";
            case 34: return "fair(day)";
            case 35: return "mixed rain and hail";
            case 36: return "hot";
            case 37: return "isolated thunderstorms";
            case 38: return "scattered thunderstorms";
            case 39: return "scattered thunderstorms";
            case 40: return "scattered showers";
            case 41: return "heavy snow";
            case 42: return "scattered snow showers";
            case 43: return "heavy snow";
            case 44: return "partly cloudy";
            case 45: return "thundershowers";
            case 46: return "snow showers";
            case 47: return "isolated thundershowers";
            default: return "none";
        }
    }

    private static string filePath(string fileName) // Returns the formatted filepath for this filename in our directory
    {
        return folderPath + fileName + ".txt";
    }

    private static string ReadFile(string name) // Returns the text in the file of this name in our storage directory
    {
        return File.ReadAllText(filePath(name));
    }

    private float VisibilityToFogDensity(float v) // Converts Visibility from Dark Sky to usable value for setting Fog Density
    {
        // Get fog density value using pre-made equation
        float d = -0.00001020408f + (0.1000102f * (float)Math.Pow(e, (-0.919024f * v)));
        if (d < 0) return 0;
        else if (d > 1) return 1;
        else return d;
    }

    private float ConvertPercipIntensity(float before) // // Converts Percipitation Intensity from Dark Sky to usable value for setting Percip Intensity in WeatherMaker
    {
        // Convert percip intensity using pre-made equation
        float after = 3.333333f * before;
        if (after < 0) return 0;
        else if (after > 1) return 1;
        else return after;
    }

    private float ConvertWindSpeed(float before) // Convert wind speed to wind intensity
    {
        /* In Dark Sky, wind speed is represented in miles per hour
         * However, in WeatherMaker, wind speed is denoted by a variable called wind intensity, which has a value
         * between 0 and 1.
         * This method converts wind speed to wind intensity using a pre-made quadratic equation that scales the
         * wind intensity in a way that realistically represents any given wind speed. */

        float after = -1.665335e-16f - (0.004661178f * before) + 0.002733059f * (float)Math.Pow(before, 2);
        if (after < 0) return 0;
        else if (after > 1) return 1;
        else return after;
    }

    private double DateTimetoUnix(DateTime dateTime) // Converts a date and time from DateTime to Unix time
    {
        return (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
               new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
    }

    private DateTime ConvertLocalDateTimeToDestination(DateTime localDateTime) // Convert our local DateTime to be correct at the target location
    {
        // First, subtract the Computer's UTC offset (to get the UTC + 0 time)
        localDateTime = localDateTime.AddSeconds(-ComputerUtcOffsetInSeconds);

        // Seccond, add the Destination's UTC offset (to get the local time at destination)
        localDateTime = localDateTime.AddSeconds(DestinationUtcOffsetInSeconds);

        // Return the converted DateTime
        return localDateTime;
    }

    private DateTime GetDateTimeFromWeathermakerTime(float timeOfDay, int day, int month, int year) // Get the current WeatherMaker time and date as a DateTime
    {
        // In WeatherMaker, the time of day is represented as the total number of secconds elapsed so far that day.
        DateTime dt = new DateTime(year, month, day);
        dt = dt.AddSeconds(timeOfDay);
        return dt;
    }

    private static DateTime fileCreatedTime(string name) // Finds the file of this name in our storage directory and returns the DateTime it was created
    {
        return File.GetCreationTime(filePath(name));
    }

    private IEnumerator RunWeatherSimulator() // Coroutine to execute the program's main functions
    {
        // Perform some initialization
        Initialization();

        // Get and store the information we need from APIs
        yield return GetAPIData();

        // Initialize WeatherMaker's Day Night Cycle Parameters
        InitializeDayNightCycle();

        // Use Yahoo Weather data to correct Dark Sky data, if needed (explained in method)
        CorrectDarkSkyData();

        // Update the Weather for minute zero, and, having completed our first update, set isFirstPass to false
        UpdateWeather(myDarkSkyCall.minutely.data[0]);
        isFirstPass = false;

        // Set global variables for sunrise and sunset times
        SetSunriseSunsetTimes();

        // Start a Coroutine to gradually speed up time
        StartCoroutine(SpeedUpTime());

        // If we do not have a manual weather override, start a coroutine to update the weather as time passes.
        if (!manualWeatherOverride) StartCoroutine(ProgressWeather());

        // If we do have a mnual weather override, set the appropriate parameters for the given weather condition
        else SetManualOverrideParameters();
    }

    private IEnumerator GetAPIData() // Get and store the information we need from APIs
    { 
        // Check to see if any files in our storage directory are too old, and if so delete them
        DeleteOldFiles();

        // Get Public IP using an API
        yield return GetPublicIP();

        // Get computer's location using Public IP and an API
        yield return GetLocation();

        // Get Lat Lon using Location object, or getting it if using manual location, and use them to set myLatLon
        GetLatLon();
        
        // If enterred manual location, let's get the name of that location from Lat and Lon using Google Geocode API
        if (manualLocation) GetGoogleGeocodeLocation(myLatLon);

        // Get weather information from Dark Sky API
        yield return CallDarkSky(myLatLon);

        // Analyze completeness of DarkSky Data, and print analysis if necessary
        ReportDarkSkyDataCompleteness();

        // Copy the "Currently" data from our myDarkSkyCall object and paste it into Minute 0 of the same call. We do this because the Minute data is often empty.
        CopyPasteFromCurrentlyToFirstMinute();

        // Get more weather information, this time from the Yahoo Weather API
        yield return CallYahooWeather(myLatLon);

        // Get the UTC Offset of the location by calling the Time Zone Database API (utc offset is needed for correctly setting our sun position in WeatherMaker)
        yield return CallTimeZoneDb();
        
        // Now that the APIs have returned, we'll turn the world back on
        world.SetActive(true);
    }

    private IEnumerator GetPublicIP() // Get Public IP using an API
    {
        // Check to see if we already have the IP stored locally
        if (!hasFile("ip"))
        {
            // If not, retrieve IP from API
            if (!debugMode) // make sure we're not in debugMode (in debugMode, we manually set API info to avoid excessive calls)
            {
                // Get Public IP using an API, save it to myIP
                WWW IPwww = new WWW(ipUrl);
                yield return IPwww;
                if (IPwww.error == null) myIP = JsonConvert.DeserializeObject<PublicIP>(IPwww.text).ip;
                else Debug.Log("Get Public IP ERROR: " + IPwww.error);

                // Write IP to file
                WriteFile("ip", myIP);
            }
        }
        else // We have the IP stored locally
        {
            // Read IP from file, store it in myIP
            myIP = ReadFile("ip");
        }
    }

    private IEnumerator GetLocation()  // Get computer's location using Public IP and an API
    {
        // Check to see if we already have the computer's location stored locally
        if (!hasFile("Location"))
        {
            // If not, retrieve location from API
            if (!debugMode) // make sure we're not in debugMode (in debugMode, we manually set API info to avoid excessive calls)
            {
                // Get location in JSON using an API
                string Lurl = locationUrl + myIP;
                WWW Lwww = new WWW(Lurl);
                yield return Lwww;
                if (Lwww.error == null)
                {
                    // Import JSON text into our global myLocation object
                    ImportLocation(Lwww.text);
                }
                else Debug.Log("Get Location ERROR: " + Lwww.error);

                // Write Location to file
                WriteFile("Location", Lwww.text);
            }
        }
        else // We have the computer's location stored locally
        {
            // Read Location from file into our myLocation object
            ImportLocation(ReadFile("Location"));
        }
    }

    private IEnumerator ProgressWeather() // Coroutine that updates the weather as time passes
    {
        /* This method uses while loops to check to see if enough time has passed to update the weather.
         * Dark Sky provides weather information for the next 60 minutes, 48 hours, and 7 days, so we use
         * three while loops to loop through the 59 remaining minutes, 47 remaining hours, and 5 remaining
         * days, updating the weather with the corresponding data every time the WeatherMaker time passes
         * a mark. */

        yield return LoopThroughMinutes();

        yield return LoopThroughHours();

        yield return LoopThroughDays();

    }

    private IEnumerator LoopThroughMinutes() // Loop through the 59 remaining minutes
    {
        // Set variable for the number of times per second we want to check to see if enough time has passed
        const float checksPerSecond = 60;

        // Loop through the 59 remaining minutes
        for (int m = 1; m < 60; m++)
        {
            // If we have data for this minute
            if (myDarkSkyCall.minutely.data[m].time != -1)
            {
                // Print that we are waiting, if necessary
                if (printDebug) print("waiting for minute " + m);

                // Get the DateTime at which this minute occurs
                DateTime minuteTime = UnixToDateTime(myDarkSkyCall.minutely.data[m].time);

                // If we're using a manual location, convert minuteTime to the destination time zone
                if (manualLocation) minuteTime = ConvertLocalDateTimeToDestination(minuteTime);

                // While the time at which this minute is set to occur has not yet passed...
                while (dayNightCycle.Year < minuteTime.Year ||
                        dayNightCycle.Month < minuteTime.Month ||
                        dayNightCycle.Day < minuteTime.Day ||
                        dayNightCycle.TimeOfDay < minuteTime.TimeOfDay.TotalSeconds)
                {
                    // Wait
                    yield return new WaitForSeconds(1 / checksPerSecond);
                }

                // When the time has come, update weather with this minute's data
                UpdateWeather(myDarkSkyCall.minutely.data[m]);
            }
        }
    }

    private IEnumerator LoopThroughHours() // Loop through the 47 remaining hours
    {
        // Set variable for the number of times per second we want to check to see if enough time has passed
        const float checksPerSecond = 60;

        // Loop through the 47 remaining hours
        for (int h = 1; h < 48; h++)
        {
            // If we have data for this hour
            if (myDarkSkyCall.hourly.data[h].time != -1)
            {
                // Print that we are waiting, if necessary
                if (printDebug) print("waiting for hour " + h);

                // Get the DateTime at which this hour occurs
                DateTime hourTime = UnixToDateTime(myDarkSkyCall.hourly.data[h].time);

                // If we're using a manual location, convert hourTime to the destination time zone
                if (manualLocation) hourTime = ConvertLocalDateTimeToDestination(hourTime);

                // While the time at which this hour is set to occur has not yet passed...
                while (dayNightCycle.Year < hourTime.Year ||
                        dayNightCycle.Month < hourTime.Month ||
                        dayNightCycle.Day < hourTime.Day ||
                        dayNightCycle.TimeOfDay < hourTime.TimeOfDay.TotalSeconds)
                {
                    // Wait
                    yield return new WaitForSeconds(1 / checksPerSecond);
                }

                // When the time has come, update weather with this minute's data
                UpdateWeather(myDarkSkyCall.hourly.data[h]);
            }
        }
    }

    private IEnumerator LoopThroughDays() // Loop through the 5 remaining days
    {
        // Set variable for the number of times per second we want to check to see if enough time has passed
        const float checksPerSecond = 60;

        // Loop through the 5 remaining days
        for (int d = 2; d < 7; d++)
        {
            // If we have data for this day
            if (myDarkSkyCall.daily.data[2].time != -1)
            {
                // Print that we are waiting, if necessary
                if (printDebug) print("waiting for day " + d);

                // Get the DateTime at which this day occurs
                DateTime dayTime = UnixToDateTime(myDarkSkyCall.daily.data[d].time);

                // If we're using a manual location, convert dayTime to the destination time zone
                if (manualLocation) dayTime = ConvertLocalDateTimeToDestination(dayTime);

                // While the time at which this day is set to occur has not yet passed...
                while (dayNightCycle.Year < dayTime.Year ||
                        dayNightCycle.Month < dayTime.Month ||
                        dayNightCycle.Day < dayTime.Day ||
                        dayNightCycle.TimeOfDay < dayTime.TimeOfDay.TotalSeconds)
                {
                    // Wait
                    yield return new WaitForSeconds(1 / checksPerSecond);
                }
                // When the time has come, update weather with this minute's data
                UpdateWeather(myDarkSkyCall.daily.data[d]);
            }
        }
    }

    private IEnumerator SpeedUpTime() // Coroutine to gradually speed up time
    {
        // Set a variable for the number of seconds in a day
        double secondsInADay = 86400;

        // Create a variable for the speed at which time should pass
        double speed;

        // Wait 5 seconds before beginning speed-up
        yield return new WaitForSeconds(5);

        // While less than 1 week has passed
        while (GetDateTimeFromWeathermakerTime( dayNightCycle.TimeOfDay,
                                                dayNightCycle.Day,
                                                dayNightCycle.Month,
                                                dayNightCycle.Year ) 
                                                < localStartTime.AddSeconds(secondsInADay * 7))
        {
            // Get the real world time passed since this program began
            double realTimePassedInSeconds = (DateTime.Now - computerStartTime).TotalSeconds;

            // Change speed as a function of real time passed according to our pre-made equation 
            speed = ((1929999 * realTimePassedInSeconds) + 6102055) / 5000;

            // Set the day and night speed to this value
            dayNightCycle.Speed = (float)speed;
            dayNightCycle.NightSpeed = (float)speed;

            // Run this while loop 60 times per second
            yield return new WaitForSeconds(1f / 60f);
        }

        // When 7 days have passed, set the speed back to normal (realtime)
        dayNightCycle.Speed = 1;
        dayNightCycle.NightSpeed = 1;
        
    }

    private IEnumerator GetGoogleGeocodeLocation(string latlon) // Get the name of target location from Lat and Lon using Google Geocode API
{
        // Check to see if we already have a Google Geocode file stored locally for this Lat and Lon
        if (!hasFile("Google_Geocode_" + latlon))
        {
            // If we dont have a local file stored, retrieve location name from API
            if (!debugMode) // make sure we're not in debugMode (in debugMode, we manually set API info to avoid excessive calls)
            {
                // Get location in JSON using an API
                string Gurl = googleGeocodeUrlFront + latlon + googleGeocodeUrlBack;
                WWW Gwww = new WWW(Gurl);
                yield return Gwww;
                if (Gwww.error == null)
                {
                    // Import JSON text into our global myG_Location object
                    ImportG_Location(Gwww.text);
                }
                else Debug.Log("Google Geocode ERROR: " + Gwww.error);
                // Write Location to file
                WriteFile("Google_Geocode_" + latlon, Gwww.text);
            }
        }
        else // // We have the Google Geocode location stored locally
        {
            // Read Location from file
            ImportG_Location(ReadFile("Google_Geocode_" + latlon));
        }
    }

    private IEnumerator CallDarkSky(string latlon) // Get weather information from Dark Sky API
    {
        // Check to see if we already have a Dark Sky Call for this lat and lon stored locally
        if (!hasFile(latlon))
        {
            // If not, retrieve Dark Sky Weather from API
            if (!debugMode) // make sure we're not in debugMode (in debugMode, we manually set API info to avoid excessive calls)
            {
                // Retrieve the JSON text from API
                string Wurl = weatherUrl + latlon;
                WWW Wwww = new WWW(Wurl);
                yield return Wwww;
                if (Wwww.error == null)
                {
                    // Import JSON text into our global DarkSkyCall object
                    print(Wwww.text);
                    DS_ImportWeather(Wwww.text);

                    // Write JSON text to file
                    WriteFile(latlon, Wwww.text);
                }
                else Debug.Log("Get Dark Sky Weather ERROR: " + Wwww.error);
            }
        }
        else // We have the weather information stored locally
        {
            // Read Weather from file and store in our DarkSkyCall object
            DS_ImportWeather(ReadFile(latlon));
        }
    }

    private IEnumerator CallYahooWeather(string latlon) // Get weather information from Dark Sky API
    { 
        // Check to see if we already have a Yahoo Weather Call for this lat and lon stored locally
        if (!hasFile("Y_" + latlon))
        {
            // If not, retrieve Yahoo Weather from API
            if (!debugMode) // make sure we're not in debugMode (in debugMode, we manually set API info to avoid excessive calls)
            {
                // Retrieve the JSON text from API
                string YWurl = yahooWeatherApiUrlFront + latlon + yahooWeatherApiUrlBack;
                WWW YWwww = new WWW(YWurl);
                yield return YWwww;
                if (YWwww.error == null)
                {
                    // Import JSON text into our global YahooWeatherCall object
                    Y_ImportWeather(YWwww.text);

                    // Write JSON text to file
                    WriteFile("Y_" + latlon, YWwww.text);
                }
                else Debug.Log("Get Yahoo Weather ERROR: " + YWwww.error);
            }
        }
        else // We have the weather information stored locally
        {
            // Read Weather from file and dore it in our YahooWeatherCall object
            Y_ImportWeather(ReadFile("Y_" + latlon));
        }
    }

    private IEnumerator CallTimeZoneDb() // Get the UTC Offset of the location by calling the Time Zone Database API
    {
        // For automatic location, we only need to get the utc offset for that one location
        // For manual location, we need to get the utc offset for the manually enterred location as well as the offset at the computer's location
        // So, we'll run this code twice, using  a for loop
        for (int i = 0; i < 2; i++)
        {
            // If automatic location, run once, if manual run twice
            if (i == 0 || manualLocation)
            {
                // Set timezone to the weather call's time zone the first time, and to your computer's time zone the second time.
                string timezone = i == 0 ? myDarkSkyCall.timezone : myLocation.time_zone;

                // Using timezone, get the JSON text from the Time Zone Database API call
                string Uurl = timeZoneDbUrl + timezone;
                WWW Uwww = new WWW(Uurl);
                yield return Uwww;

                if (Uwww.error == null)
                {
                    // Deserialize the JSON into our TimeZoneDbCall object
                    myTimeZoneDbCall = new TimeZoneDbCall();
                    myTimeZoneDbCall = JsonConvert.DeserializeObject<TimeZoneDbCall>(Uwww.text);

                    // On first pass, set our global variable for the destination offset
                    if (i == 0) DestinationUtcOffsetInSeconds = myTimeZoneDbCall.gmtOffset;

                    // On second pass, set our global variable for computer offset
                    else ComputerUtcOffsetInSeconds = myTimeZoneDbCall.gmtOffset;
                }
                else Debug.Log("Get UTC Offset ERROR: " + Uwww.error);
            }
        }
    }

    private IEnumerator SetCloudCover(float cloudCover) // Gradually change the cloud cover
    {
        if (skySphere.CloudCover > cloudCover)
        {
            decrementCloudCover = true;
            while (skySphere.CloudCover > cloudCover)
            {
                yield return new WaitForSeconds(1f / 30f);
            }
            decrementCloudCover = false;
        }
        else if (skySphere.CloudCover < cloudCover)
        {
            incrementCloudCover = true;
            while (skySphere.CloudCover < cloudCover)
            {
                yield return new WaitForSeconds(1f / 30f);
            }
            incrementCloudCover = false;
        }
    }

    private IEnumerator SetCloudVelocity_x(float cloudVelocity_x) // Gradually change cloud's x velocity
    {
        /* In this method, we set either an increment or decrement bool to true, which tells our
         * fixedUpdate function to either increment or decrement that value.
         * Then, every 30th of a second we check to see if we've reached our goal.
         * Once we reach our goal, we set the increment or decrement bool back to false. */
        if (skySphere.CloudNoiseVelocity.x > cloudVelocity_x)
        {
            decrementCloudVelocity_x = true;
            while (skySphere.CloudNoiseVelocity.x > cloudVelocity_x)
            {
                yield return new WaitForSeconds(1f / 30f);
            }
            incrementCloudVelocity_x = false;
        }
        else if (skySphere.CloudNoiseVelocity.x < cloudVelocity_x)
        {
            incrementCloudVelocity_x = true;
            while (skySphere.CloudNoiseVelocity.x < cloudVelocity_x)
            {
                yield return new WaitForSeconds(1f / 30f);
            }
            decrementCloudVelocity_x = false;
        }
    }

    private IEnumerator SetCloudVelocity_y(float cloudVelocity_y) // Gradually change cloud's y velocity
    {
        /* In this method, we set either an increment or decrement bool to true, which tells our
         * fixedUpdate function to either increment or decrement that value.
         * Then, every 30th of a second we check to see if we've reached our goal.
         * Once we reach our goal, we set the increment or decrement bool back to false. */
        if (skySphere.CloudNoiseVelocity.y > cloudVelocity_y)
        {
            decrementCloudVelocity_y = true;
            while (skySphere.CloudNoiseVelocity.y > cloudVelocity_y)
            {
                yield return new WaitForSeconds(1f / 30f);
            }
            incrementCloudVelocity_y = false;
        }
        else if (skySphere.CloudNoiseVelocity.y < cloudVelocity_y)
        {
            incrementCloudVelocity_y = true;
            while (skySphere.CloudNoiseVelocity.y < cloudVelocity_y)
            {
                yield return new WaitForSeconds(1f / 30f);
            }
            decrementCloudVelocity_y = false;
        }
    }

    //[MenuItem("Tools/Clear Cache")]
    private static void ClearCache() // MenuItem for use with debugging. Deletes all files in our storage directory
    {
        foreach (string file in Directory.GetFiles(folderPath))
        {
            File.Delete(file);
        }
    }

}

