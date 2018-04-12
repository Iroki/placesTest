using Newtonsoft.Json;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PlacesTest
{
    public class ViewModelOne : INotifyPropertyChanged
    {
        public ViewModelOne()
        {
            StartListening().Wait();
            GetCurrentPosition().Wait();

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // PART 1. "So these are the Places models and my own Address model:"

        public class AddressInfo
        {

            public string Address { get; set; }

            public string City { get; set; }

            //public string State { get; set; }

            public string ZipCode { get; set; }

            public double Longitude { get; set; }

            public double Latitude { get; set; }

            public string Reference { get; set; }

            public Xamarin.Forms.Maps.Position Position { get; set; }
        }

        public class PlacesMatchedSubstring
        {

            [Newtonsoft.Json.JsonProperty("length")]
            public int Length { get; set; }

            [Newtonsoft.Json.JsonProperty("offset")]
            public int Offset { get; set; }
        }

        public class PlacesTerm
        {

            [Newtonsoft.Json.JsonProperty("offset")]
            public int Offset { get; set; }

            [Newtonsoft.Json.JsonProperty("value")]
            public string Value { get; set; }
        }

        public class Prediction
        {
            [Newtonsoft.Json.JsonProperty("id")]
            public string Id { get; set; }

            [Newtonsoft.Json.JsonProperty("description")]
            public string Description { get; set; }

            [Newtonsoft.Json.JsonProperty("matched_substrings")]
            public List<PlacesMatchedSubstring> MatchedSubstrings { get; set; }

            [Newtonsoft.Json.JsonProperty("place_id")]
            public string PlaceId { get; set; }

            [Newtonsoft.Json.JsonProperty("reference")]
            public string Reference { get; set; }

            [Newtonsoft.Json.JsonProperty("terms")]
            public List<PlacesTerm> Terms { get; set; }

            [Newtonsoft.Json.JsonProperty("types")]
            public List<string> Types { get; set; }
        }

        public class PlacesLocationPredictions
        {

            [Newtonsoft.Json.JsonProperty("predictions")]
            public List<Prediction> Predictions { get; set; }

            [Newtonsoft.Json.JsonProperty("status")]
            public string Status { get; set; }
        }




        // PART 2. Here we call the Places API:

        // Default: public const string GooglePlacesApiAutoCompletePath = "https://maps.googleapis.com/maps/api/place/autocomplete/json?key={0}&input={1}&components=country:ua"; //Adding country:us limits results to us
        public const string GooglePlacesApiAutoCompletePath = "https://maps.googleapis.com/maps/api/place/autocomplete/json?key={0}&input={1}&components=country:ua&language=uk&types=establishment&location={2},{3}&strictbounds&radius=2000";
        //public const string GooglePlacesApiAutoCompletePathFarther = "https://maps.googleapis.com/maps/api/place/autocomplete/json?key={0}&input={1}&components=country:ua&language=ru&types=establishment&location={2},{3}&strictbounds&radius=2000";
        //public const string GooglePlacesApiAutoCompletePathFarthest = "https://maps.googleapis.com/maps/api/place/autocomplete/json?key={0}&input={1}&components=country:ua&language=ru&types=establishment&location={2},{3}&strictbounds&radius=5000";
        //"location — The point around which you wish to retrieve place information. Must be specified as latitude,longitude."

        public const string GooglePlacesApiKey = "AIzaSyAfP9wu3t4g0y3-UQE6sy5W6wFF03w9Cd0";

        private static HttpClient _httpClientInstance;
        public static HttpClient HttpClientInstance => _httpClientInstance ?? (_httpClientInstance = new HttpClient());

        private ObservableCollection<AddressInfo> _addresses;
        public ObservableCollection<AddressInfo> Addresses
        {
            get
            {
                return _addresses ?? (_addresses = new ObservableCollection<AddressInfo>());
            }
            set
            {
                if (_addresses != value)
                {
                    _addresses = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _addressInput;
        public string AddressInput
        {
            get => _addressInput;
            set
            {
                if (_addressInput != value)
                {
                    _addressInput = value;
                    RaisePropertyChanged();
                }
            }
        }

        public Command GetPlaces
        {
            get
            {
                return new Command(async () =>
               {
                   {

                       // TODO: Add throttle logic, Google begins denying requests if too many are made in a short amount of time

                       CancellationToken cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;

                       // DEFAULT:   using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, string.Format(GooglePlacesApiAutoCompletePath, GooglePlacesApiKey, WebUtility.UrlEncode(_addressInput))) //Be sure to UrlEncode the search term they enters
                       using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, string.Format(GooglePlacesApiAutoCompletePath, GooglePlacesApiKey, WebUtility.UrlEncode(_addressInput), ViewModelOne.GetCurrentPosition().Result.Latitude, ViewModelOne.GetCurrentPosition().Result.Longitude))) //Be sure to UrlEncode the search term they enter
                       {

                           using (HttpResponseMessage message = await HttpClientInstance.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                           {
                               if (message.IsSuccessStatusCode)
                               {
                                   string json = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

                                   PlacesLocationPredictions predictionList = await Task.Run(() => JsonConvert.DeserializeObject<PlacesLocationPredictions>(json)).ConfigureAwait(false);


                                   //if (predictionList.Status=="ZERO_RESULTS") //ДОБАВИТЬ ЕЩЁ ПРОВЕРКУ НА РЕЗУЛЬТАТ "ZERO_RESULTS", "INVALID_REQUEST"
                                   //{

                                   //}


                                   if (predictionList.Status == "OK")
                                   {

                                       Addresses.Clear();
                                       ReferencePositions.Clear();

                                       if (predictionList.Predictions.Count > 0)
                                       {
                                           foreach (Prediction prediction in predictionList.Predictions)
                                           {
                                               Addresses.Add(new AddressInfo
                                               {
                                                   Address = prediction.Description,
                                                   Reference = prediction.Reference,
                                                   Position = await GetLocationByReference(prediction.Reference)
                                               });
                                           }
                                       }
                                   }
                                   else
                                   {
                                       throw new Exception(predictionList.Status);
                                   }
                               }
                           }
                       }
                   }
               });
            }
        }


        //TEST search by reference


        // От себя: добавлено для хранения данных о локации по возвращении запроса по reference


        //TEST JSON 2 SHARP

     

        public class Location
        {
            public double lat { get; set; }
            public double lng { get; set; }
        }

        public class Geometry
        {
            public Location location { get; set; }

        }

        public class Result
        {
            
            public Geometry geometry { get; set; }
            
        }

        public class ReferenceSearchResult
        {
            public Result result { get; set; }
            public string Status { get; set; }
        }



        //List of Positions for PINS

        private ObservableCollection<Xamarin.Forms.Maps.Position> _referencePositions = new ObservableCollection<Xamarin.Forms.Maps.Position>();
        public ObservableCollection<Xamarin.Forms.Maps.Position> ReferencePositions
        {
            get
            {
                return _referencePositions;
            }
            set
            {
                _referencePositions = value;
                RaisePropertyChanged();
            }

        }

        public event EventHandler<Xamarin.Forms.Maps.Position> ReferencePositionAdded; // Добавил Коля возвращение позиции

        private void InvokeReferencePositionAdded(Xamarin.Forms.Maps.Position position) // Добавил Коля, возвращается позиция
        {
            ReferencePositionAdded?.Invoke(this, position);
        }

        public const string GooglePlacesReferenceSearch = "https://maps.googleapis.com/maps/api/place/details/json?reference={0}&sensor=true&key={1}";


        public async Task<Xamarin.Forms.Maps.Position> GetLocationByReference(string reference)
        {
            CancellationToken cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, string.Format(GooglePlacesReferenceSearch, reference, GooglePlacesApiKey)))
            {
                using (HttpResponseMessage message = await HttpClientInstance.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                {
                    if (message.IsSuccessStatusCode)
                    {
                        string json = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

                        ReferenceSearchResult refSearch = await Task.Run(() => JsonConvert.DeserializeObject<ReferenceSearchResult>(json)).ConfigureAwait(false);

                        if (refSearch.Status == "OK")
                        {

                            Xamarin.Forms.Maps.Position newPosition = new Xamarin.Forms.Maps.Position(refSearch.result.geometry.location.lat, refSearch.result.geometry.location.lng);
                            ReferencePositions.Add(newPosition);
                            
                            InvokeReferencePositionAdded(newPosition); //добавил Коля
                            return newPosition;
                        }
                        else
                        {
                            throw new Exception(refSearch.Status);

                        }

                    };
                    return new Xamarin.Forms.Maps.Position(); //redo later
                }
            }
        }


        //Geolocation testing


        public static async Task<Position> GetCurrentPosition()
        {
            Position position = null;
            try
            {
                var locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = 100;

                position = await locator.GetLastKnownLocationAsync();

                if (position != null)
                {
                    //got a cached position, so let's use it.
                    return position;
                }

                if (!locator.IsGeolocationAvailable || !locator.IsGeolocationEnabled)
                {
                    //not available or enabled
                    return null;
                }

                position = await locator.GetPositionAsync(TimeSpan.FromSeconds(10), null, true);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to get location: " + ex);
            }

            if (position == null)
                return null;

            var output = string.Format("Time: {0} \nLat: {1} \nLong: {2} \nAltitude: {3} \nAltitude Accuracy: {4} \nAccuracy: {5} \nHeading: {6} \nSpeed: {7}",
                    position.Timestamp, position.Latitude, position.Longitude,
                    position.Altitude, position.AltitudeAccuracy, position.Accuracy, position.Heading, position.Speed);

            Debug.WriteLine(output);

            return position;
        }




        //LISTENER GEOLOCATION COPY-PASTE

        private Plugin.Geolocator.Abstractions.Position _currentPosition;

        public Plugin.Geolocator.Abstractions.Position CurrentPosition
        {
            get
            {
                return _currentPosition;
            }
            set
            {
                _currentPosition = value;
                RaisePropertyChanged();
            }
        }


        async Task StartListening()
        {
            if (CrossGeolocator.Current.IsListening)
                return;

            await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(10), 10, true);

            CrossGeolocator.Current.PositionChanged += PositionChanged;
            CrossGeolocator.Current.PositionError += PositionError;
        }

        public void PositionChanged(object sender, PositionEventArgs e)
        {

            //If updating the UI, ensure you invoke on main thread
            CurrentPosition = e.Position;
            var output = "Full: Lat: " + CurrentPosition.Latitude + " Long: " + CurrentPosition.Longitude;
            output += "\n" + $"Time: {CurrentPosition.Timestamp}";
            output += "\n" + $"Heading: {CurrentPosition.Heading}";
            output += "\n" + $"Speed: {CurrentPosition.Speed}";
            output += "\n" + $"Accuracy: {CurrentPosition.Accuracy}";
            output += "\n" + $"Altitude: {CurrentPosition.Altitude}";
            output += "\n" + $"Altitude Accuracy: {CurrentPosition.AltitudeAccuracy}";
            Debug.WriteLine(output);
        }

        private void PositionError(object sender, PositionErrorEventArgs e)
        {
            Debug.WriteLine(e.Error);
            //Handle event here for errors
        }

        async Task StopListening()
        {
            if (!CrossGeolocator.Current.IsListening)
                return;

            await CrossGeolocator.Current.StopListeningAsync();

            CrossGeolocator.Current.PositionChanged -= PositionChanged;
            CrossGeolocator.Current.PositionError -= PositionError;
        }
    }

}
