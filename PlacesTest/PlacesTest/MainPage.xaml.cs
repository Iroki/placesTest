using Plugin.Geolocator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace PlacesTest
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            viewModelOne = new ViewModelOne();
            BindingContext = viewModelOne;
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(50.470691, 30.465316), Distance.FromKilometers(1.00))); //change later somehow
            CrossGeolocator.Current.PositionChanged += (sender, e) =>
            {
                Current_PositionChanged(sender, e);
            };

            viewModelOne.ReferencePositionAdded += (sender, e) => //doesn't work, should add pins to the map at some point
            {

                foreach (var position in viewModelOne.ReferencePositions)
                {
                    AddMorePins(position);
                }
            };


        }

        ViewModelOne viewModelOne;

        Plugin.Geolocator.Abstractions.Position position; // = viewModelOne.CurrentPosition;




        //public void OnPositionChanged()
        //{
        //    CrossGeolocator.Current.PositionChanged += Current_PositionChanged;
        //}

        private void Current_PositionChanged(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
        {
            MyMap.Pins.Clear();
            position = e.Position;
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(position.Latitude, position.Longitude), Distance.FromKilometers(1.00)));
            AddPin();
        }

        public void AddPin()
        {
            Position p = new Position(position.Latitude, position.Longitude);

            Pin pin = new Pin
            {
                Type = PinType.Place,
                Position = p,
                Label = "Current position",
                Address = "Happy happy"
            };

            MyMap.Pins.Add(pin);

            position = null; // test
        }

        public void AddMorePins(Position position)
        {

            Pin pin = new Pin
            {
                Type = PinType.Place,
                Position = position,
                Label = "to be added later",
                Address = "to be added later"
            };

            MyMap.Pins.Add(pin);


            //TO TEST LATER MAYBE

            // create buttons
            //  var morePins = new Button { Text = "Add more pins" };

            //morePins.Clicked += (sender, e) =>
            //{
            //MyMap.Pins.Add(new Pin
            //{
            //    Position = new Position(36.9641949, -122.0177232),
            //    Label = "Boardwalk"
            //});
            //MyMap.Pins.Add(new Pin
            //{
            //    Position = new Position(36.9571571, -122.0173544),
            //    Label = "Wharf"
            //});
            //MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(

            //new Position(36.9628066, -122.0194722), Distance.FromMiles(1.5)));

            //};
            //    var reLocate = new Button { Text = "Re-center" };
            //    reLocate.Clicked += (sender, e) =>
            //    {
            //        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(

            //            new Position(36.9628066, -122.0194722), Distance.FromMiles(3)));
            //    };
            //}

        }
    }
}
