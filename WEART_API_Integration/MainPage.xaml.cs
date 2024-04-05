using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using WeArt.Core;
using WeArt.Components;
using WeArt.Messages;
using System.Timers;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Media;

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

            InitTrackers();
            InitRawDataTrackers();
            InitSensorAnalogRawData();

            // handle calibration
            _weartClient.OnConnectionStatusChanged += UpdateConnectionStatus;
            _weartClient.OnCalibrationStart += OnCalibrationStart;
            _weartClient.OnCalibrationResultSuccess += (HandSide hand) => OnCalibrationResult(hand, true);
            _weartClient.OnCalibrationResultFail += (HandSide hand) => OnCalibrationResult(hand, false);
            _weartClient.OnCalibrationFinish += (HandSide hand) => Console.WriteLine("Finish");
            _weartClient.OnMiddlewareStatusUpdate+= UpdateUIBasedOnStatus;
            _weartClient.OnMiddlewareStatusUpdate += UpdateDevicesStatus;

            // Init controls
            UpdateUIBasedOnStatus(new MiddlewareStatusUpdate());
            LeftHand.Refresh();
            RightHand.Refresh();

            // schedule timer to check tracking closure value
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 200; //Milliseconds
            timer.AutoReset = true;
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
        }

        private void UpdateConnectionStatus(bool connected)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ConnectionStatus.Text = connected ? "Connected" : "Not Connected";
                ConnectionStatus.Foreground = new SolidColorBrush(connected ? Colors.Green : Colors.Red);
            });
        }

        private void OnCalibrationStart(HandSide handSide)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                CalibrationStatusText.Text = $"Calibrating {handSide.ToString().ToLower()} hand...";
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
            });
        }


        private Color MiddlewareStatusColor(MiddlewareStatus status)
        {
            bool isYellow = status == MiddlewareStatus.STOPPING
                || status == MiddlewareStatus.CALIBRATION
                || status == MiddlewareStatus.UPLOADING_TEXTURES
                || status == MiddlewareStatus.CONNECTING_DEVICE;
            bool isRed = status == MiddlewareStatus.DISCONNECTED;

            return isRed ? Colors.Red : (isYellow ? Colors.Orange : Colors.Green);
        }

        private void UpdateUIBasedOnStatus(MiddlewareStatusUpdate statusUpdate)
        {
            if (statusUpdate is null)
                return;

            MiddlewareStatus status = statusUpdate.Status;
            bool isRunning = status == MiddlewareStatus.RUNNING;

            Color statusColor = MiddlewareStatusColor(status);
            bool isStatusOk = statusUpdate.StatusCode == 0;

            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(() =>
            {
                // Update buttons
                StartClient.IsEnabled = status != MiddlewareStatus.RUNNING && status != MiddlewareStatus.STARTING;
                StopClient.IsEnabled = isRunning;
                StartCalibration.IsEnabled = status == MiddlewareStatus.RUNNING;

                AddEffectSample1.IsEnabled = isRunning;
                AddEffectSample2.IsEnabled = isRunning;
                AddEffectSample3.IsEnabled = isRunning;
                RemoveEffects.IsEnabled = isRunning;
                ButtonStartRawData.IsEnabled = isRunning;
                ButtonStopRawData.IsEnabled = isRunning;


                // Update middleware status panel
                MiddlewareStatus_Text.Text = status.ToString();
                MiddlewareStatus_Text.Foreground = new SolidColorBrush(statusColor);

                if(statusUpdate.Version != null)
                    MiddlewareVersion_Text.Text = statusUpdate.Version;

                Brush statusCodeBrush = new SolidColorBrush(isStatusOk ? Colors.Green : Colors.Red);
                MwStatusCode.Text = statusUpdate.StatusCode.ToString();
                MwStatusCode.Foreground = statusCodeBrush;
                MwStatusCodeDesc.Text = isStatusOk ? "OK" : (statusUpdate.ErrorDesc != null ? statusUpdate.ErrorDesc : "");
                MwStatusCodeDesc.Foreground = statusCodeBrush;

                ConnectedDevicesNum_Text.Text = statusUpdate.Devices.Count.ToString();

                AddEffectSample1.IsEnabled = isRunning;
                AddEffectSample2.IsEnabled = isRunning;
                AddEffectSample3.IsEnabled = isRunning;
                RemoveEffects.IsEnabled = isRunning;
                ButtonStartRawData.IsEnabled = isRunning;
                ButtonStopRawData.IsEnabled = isRunning;
            }));
        }

        private void UpdateDevicesStatus(MiddlewareStatusUpdate statusUpdate)
        {
            LeftHand.Connected = false;
            RightHand.Connected = false;
            foreach (DeviceStatus device in statusUpdate.Devices)
            {
                if(device.HandSide == HandSide.Left)
                {
                    LeftHand.Device = device;
                    LeftHand.Connected = true;
                } else
                {
                    RightHand.Device = device;
                    RightHand.Connected = true;
                }
            }
            LeftHand.Refresh();
            RightHand.Refresh();
        }


        #region Closure/Abduction Tracking

        private WeArtThimbleTrackingObject _leftIndexThimble;
        private WeArtThimbleTrackingObject _leftThumbThimble;
        private WeArtThimbleTrackingObject _leftMiddleThimble;

        private WeArtThimbleTrackingObject _rightIndexThimble;
        private WeArtThimbleTrackingObject _rightThumbThimble;
        private WeArtThimbleTrackingObject _rightMiddleThimble;

        private void InitTrackers()
        {
            // Instantiate thimbles for tracking
            _leftIndexThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Left, ActuationPoint.Index);
            _leftThumbThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Left, ActuationPoint.Thumb);
            _leftMiddleThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Left, ActuationPoint.Middle);
            _rightIndexThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Right, ActuationPoint.Index);
            _rightThumbThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Right, ActuationPoint.Thumb);
            _rightMiddleThimble = new WeArtThimbleTrackingObject(_weartClient, HandSide.Right, ActuationPoint.Middle);
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

        #endregion

        #region Raw Data Tracking

        HandSide selectedHandSide = HandSide.Right;
        ActuationPoint selectedActuationPoint = ActuationPoint.Index;

        private Dictionary<(HandSide, ActuationPoint), WeArtTrackingRawDataObject> rawDataTrackers = new Dictionary<(HandSide, ActuationPoint), WeArtTrackingRawDataObject>();

        private void InitRawDataTrackers()
        {
            rawDataTrackers.Clear();
            foreach (HandSide hs in Enum.GetValues(typeof(HandSide)))
            {
                foreach (ActuationPoint ap in Enum.GetValues(typeof(ActuationPoint)))
                {
                    rawDataTrackers.Add((hs, ap), new WeArtTrackingRawDataObject(_weartClient, hs, ap));
                }
            }
            // Add default tracker callback
            var key = (selectedHandSide, selectedActuationPoint);
            if (rawDataTrackers.ContainsKey(key)) rawDataTrackers[key].DataReceived += RenderRawDataAsync;
        }


        private Dictionary<(HandSide, ActuationPoint), WeArtAnalogSensorRawDataObject> analogSensorRawData = new Dictionary<(HandSide, ActuationPoint), WeArtAnalogSensorRawDataObject>();

        private void InitSensorAnalogRawData()
        {
            analogSensorRawData.Clear();
            foreach (HandSide hs in Enum.GetValues(typeof(HandSide)))
            {
                foreach (ActuationPoint ap in Enum.GetValues(typeof(ActuationPoint)))
                {
                    analogSensorRawData.Add((hs, ap), new WeArtAnalogSensorRawDataObject(_weartClient, hs, ap));
                }
            }
            // Add default tracker callback
            var key = (selectedHandSide, selectedActuationPoint);
            if (analogSensorRawData.ContainsKey(key)) analogSensorRawData[key].DataReceived += RenderAanlogSensorRawDataAsync;
        }

        private void HandSideChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldKey = (selectedHandSide, selectedActuationPoint);
            selectedHandSide = Enum.Parse<HandSide>((e.AddedItems[0] as ComboBoxItem).Content.ToString(), true);
            var newKey = (selectedHandSide, selectedActuationPoint);
            UpdateRawDataTrackerCallback(oldKey, newKey);
        }

        private void ActuationPointChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldKey = (selectedHandSide, selectedActuationPoint);
            selectedActuationPoint = Enum.Parse<ActuationPoint>((e.AddedItems[0] as ComboBoxItem).Content.ToString(), true);
            var newKey = (selectedHandSide, selectedActuationPoint);
            UpdateRawDataTrackerCallback(oldKey, newKey);
        }

        private void UpdateRawDataTrackerCallback((HandSide, ActuationPoint) oldKey, (HandSide, ActuationPoint) newKey)
        {
            if (oldKey == newKey)
                return;

            if (rawDataTrackers.ContainsKey(oldKey)) rawDataTrackers[oldKey].DataReceived -= RenderRawDataAsync;
            if (rawDataTrackers.ContainsKey(newKey)) rawDataTrackers[newKey].DataReceived += RenderRawDataAsync;

            if (analogSensorRawData.ContainsKey(oldKey)) analogSensorRawData[oldKey].DataReceived -= RenderAanlogSensorRawDataAsync;
            if (analogSensorRawData.ContainsKey(newKey)) analogSensorRawData[newKey].DataReceived += RenderAanlogSensorRawDataAsync;
        }

        private void RenderRawDataAsync(TrackingRawData data)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Acc_X.Text = data.Accelerometer.X.ToString();
                Acc_Y.Text = data.Accelerometer.Y.ToString();
                Acc_Y.Text = data.Accelerometer.Z.ToString();

                Gyro_X.Text = data.Gyroscope.X.ToString();
                Gyro_Y.Text = data.Gyroscope.Y.ToString();
                Gyro_Y.Text = data.Gyroscope.Z.ToString();

                TimeOfFlight.Text = data.TimeOfFlight.Distance.ToString();

                LastSampleTime.Text = data.Timestamp.ToString("yyyy/MM/dd HH:mm:ss.fff");
            });
        }

        private void RenderAanlogSensorRawDataAsync(AnalogSensorRawData data)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                ntcTempRawValue.Text = data.NtcTemperatureRaw.ToString();
                ntcTempRawConvertedValue.Text = data.NtcTemperatureConverted.ToString();
                forceSensingRawValue.Text = data.ForceSensingRaw.ToString();
                forceSensingConvertedValue.Text = data.ForceSensingConverted.ToString();

                LastSampleTime.Text = data.Timestamp.ToString("yyyy/MM/dd HH:mm:ss.fff");
            });
        }

        #endregion

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
            // Reset status
            UpdateUIBasedOnStatus(new MiddlewareStatusUpdate());
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
