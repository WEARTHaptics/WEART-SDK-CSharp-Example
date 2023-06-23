using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using WeArt.Components;
using WeArt.Core;
using System.Timers;
using System.Collections.Generic;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WEART_API_Integration
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private WeArtClient _weartClient;
        private TouchEffect _effect;
        private WeArtHapticObject _hapticObject;

        private WeArtThimbleTrackingObject _leftIndexThimble;
        private WeArtThimbleTrackingObject _leftThumbThimble;
        private WeArtThimbleTrackingObject _leftMiddleThimble;

        private WeArtThimbleTrackingObject _rightIndexThimble;
        private WeArtThimbleTrackingObject _rightThumbThimble;
        private WeArtThimbleTrackingObject _rightMiddleThimble;

        private Dictionary<(HandSide, ActuationPoint), WeArtRawSensorsDataTrackingObject> rawDataTrackers = new Dictionary<(HandSide, ActuationPoint), WeArtRawSensorsDataTrackingObject>();

        public MainPage()
        {
            this.Loaded += PageLoaded;
            this.InitializeComponent();
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            // create WEART Controller
            WeArtController weArtController = new WeArtController();
            _weartClient = weArtController.Client;
            // handle state connection 
            _weartClient.OnConnectionStatusChanged += OnConnectionChanged;

            // instantiate Effect 
            _effect = new TouchEffect();

            // instantiate Haptic Object Right hand for Index Thimble
            _hapticObject = new WeArtHapticObject(_weartClient);
            _hapticObject.HandSides = HandSideFlags.Right; // HandSideFlags.Left;
            _hapticObject.ActuationPoints = ActuationPointFlags.Index;  //ActuationPointFlags.Middle| ActuationPointFlags.Thumb

            // Instantiate thimbles for tracking
            _leftIndexThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Left, ActuationPoint.Index);
            _leftThumbThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Left, ActuationPoint.Thumb);
            _leftMiddleThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Left, ActuationPoint.Middle);
            _rightIndexThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Right, ActuationPoint.Index);
            _rightThumbThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Right, ActuationPoint.Thumb);
            _rightMiddleThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Right, ActuationPoint.Middle);

            foreach (HandSide hs in Enum.GetValues(typeof(HandSide)))
            {
                foreach (ActuationPoint ap in Enum.GetValues(typeof(ActuationPoint))) {
                    rawDataTrackers.Add((hs, ap), new WeArtRawSensorsDataTrackingObject(_weartClient, hs, ap));
                }
            }

            // handle calibration
            _weartClient.OnCalibrationStart += OnCalibrationStart;
            _weartClient.OnCalibrationFinish += OnCalibrationFinish;
            _weartClient.OnCalibrationResultSuccess += (HandSide hand) => OnCalibrationResult(hand, true);
            _weartClient.OnCalibrationResultFail += (HandSide hand) => OnCalibrationResult(hand, false);

            // schedule timer to check tracking closure value
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 200; //Milliseconds
            timer.AutoReset = true;
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
        }

        private void OnCalibrationStart(HandSide handSide)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                CalibrationStatusText.Text = $"Calibrating {handSide.ToString().ToLower()} hand...";
                StartCalibration.IsEnabled = false;
            });
        }

        private void OnCalibrationFinish(HandSide handSide)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                StartCalibration.IsEnabled = true;
            });
        }

        private void OnCalibrationResult(HandSide handSide, bool success)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                CalibrationStatusText.Text = $"Calibration for {handSide.ToString().ToLower()} hand {(success ? "completed" : "failed")}";
            });
        }

        private void OnConnectionChanged(bool connected)
        {
            if (connected)
            {

                CreateEffect();
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                RenderTrackingData();
                RenderRawData();
            });
        }

        private void RenderTrackingData()
        {
            ValueIndexRightClosure.Text = _rightIndexThimble.Closure.Value.ToString();
            ValueThumbRightClosure.Text = _rightThumbThimble.Closure.Value.ToString();
            ValueThumbRightAbduction.Text = _rightThumbThimble.Abduction.Value.ToString();
            ValueMiddleRightClosure.Text = _rightMiddleThimble.Closure.Value.ToString();

            ValueIndexLeftClosure.Text = _leftIndexThimble.Closure.Value.ToString();
            ValueThumbLeftClosure.Text = _leftThumbThimble.Closure.Value.ToString();
            ValueThumbLeftAbduction.Text = _leftThumbThimble.Abduction.Value.ToString();
            ValueMiddleLeftClosure.Text = _leftMiddleThimble.Closure.Value.ToString();
        }

        HandSide selectedHandSide = HandSide.Right;
        ActuationPoint selectedActuationPoint = ActuationPoint.Index;
        private void HandSideChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedHandSide = Enum.Parse<HandSide>((e.AddedItems[0] as ComboBoxItem).Content.ToString(), true);
        }

        private void ActuationPointChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedActuationPoint= Enum.Parse<ActuationPoint>((e.AddedItems[0] as ComboBoxItem).Content.ToString(), true);
        }
        private void RenderRawData()
        {
            SensorsData data = rawDataTrackers[(selectedHandSide, selectedActuationPoint)].LastSample;

            Acc_X.Text = data.Accelerometer.X.ToString();
            Acc_Y.Text = data.Accelerometer.Y.ToString();
            Acc_Y.Text = data.Accelerometer.Z.ToString();

            Gyro_X.Text = data.Gyroscope.X.ToString();
            Gyro_Y.Text = data.Gyroscope.Y.ToString();
            Gyro_Y.Text = data.Gyroscope.Z.ToString();

            TimeOfFlight.Text = data.TimeOfFlight.Distance.ToString();

            LastSampleTime.Text = data.Timestamp.ToString();
        }

        private void CreateEffect()
        {
            // cerate defaults haptic components 
            Temperature temperature = Temperature.Default;
            Force force = Force.Default;
            Texture texture = Texture.Default;

            // set proporties effect
            _effect.Set(temperature, force, texture);

            // add effect to thimble index
            _hapticObject.AddEffect(_effect);
        }

        private void StartClient_Click(object sender, RoutedEventArgs e)
        {
            // run mode middleware
            _weartClient.Start();
        }

        private void StopClient_Click(object sender, RoutedEventArgs e)
        {
            // stop and idle mode middleware
            _weartClient.Stop();
        }

        private void AddEffectSample1_Click(object sender, RoutedEventArgs e)
        {
            // create temperature component
            Temperature temperature = Temperature.Default;
            temperature.Active = true; // must be active to work
            temperature.Value = 0.2f;

            // create force component
            Force force = Force.Default;
            force.Active = true;
            force.Value = 0.7f;

            // create texture component
            Texture texture = Texture.Default;
            texture.Active = true;
            texture.TextureType = TextureType.ProfiledAluminiumMeshFast;

            // effect set proporties 
            _effect.Set(temperature, force, texture);

            // add effect if needed, to thimble 
            if (_hapticObject.ActiveEffect == null)
                _hapticObject.AddEffect(_effect);
        }


        private void AddEffectSample2_Click(object sender, RoutedEventArgs e)
        {
            // create temperature component
            Temperature temperature = Temperature.Default;
            temperature.Active = true;
            temperature.Value = 0.6f;

            // create force component
            Force force = Force.Default;
            force.Active = true;
            force.Value = 0.2f;

            // create texture component
            Texture texture = Texture.Default;
            texture.Active = true;
            texture.TextureType = TextureType.ProfiledAluminiumMeshFast;

            // effect set proporties
            _effect.Set(temperature, force, texture);

            // add effect if needed, to thimble
            if (_hapticObject.ActiveEffect == null)
                _hapticObject.AddEffect(_effect);
        }

        private void AddEffectSample3_Click(object sender, RoutedEventArgs e)
        {
            // create temperature component
            Temperature temperature = Temperature.Default;
            temperature.Active = true;
            temperature.Value = 1.0f;

            // create force component
            Force force = Force.Default;
            force.Active = true;
            force.Value = 1.0f;

            // create texture component
            Texture texture = Texture.Default;
            texture.Active = true;
            texture.TextureType = TextureType.ProfiledAluminiumMeshFast;

            // effect set proporties
            _effect.Set(temperature, force, texture);

            // add effect if needed, to thimble
            if (_hapticObject.ActiveEffect == null)
                _hapticObject.AddEffect(_effect);
        }

        private void RemoveEffects_Click(object sender, RoutedEventArgs e)
        {
            // remove effects from thime
            _hapticObject.RemoveEffect(_effect);
        }

        private void StartCalibration_Click(object sender, RoutedEventArgs e)
        {
            _weartClient.StartCalibration();
        }

        private void ButtonStartRawData_Click(object sender, RoutedEventArgs e)
        {
            _weartClient.StartRawData();
        }

        private void ButtonStopRawData_Click(object sender, RoutedEventArgs e)
        {
            _weartClient.StopRawData();
        }
    }
}
