using System.Collections.Generic;
using WeArt.Core;
using WeArt.Messages;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WEART_API_Integration
{
    public sealed partial class HandStatus : UserControl
    {
        public bool Connected { get; set; }

        public HandSide HandSide { get; set; }

        public DeviceGeneration DeviceGeneration = DeviceGeneration.TD_Pro; 

        public DeviceStatusData? Device { get; set; }

        public TouchDiverProStatusData? TouchDiverPro { get; set; }

        public HandStatus()
        {
            this.InitializeComponent();
        }

        public void SetDeviceGeneration(DeviceGeneration deviceGeneration)
        {
            this.DeviceGeneration = deviceGeneration;

            Refresh();
        }

        public void Refresh()
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                RefreshInternal();
            });
        }

        private void RefreshInternal()
        {
            // Rendere info about actuation points
            if (DeviceGeneration == DeviceGeneration.TD)
            {
                AnnularThimbel.Visibility = Visibility.Collapsed;
                PinkyThimble.Visibility = Visibility.Collapsed;
                PalmNode.Visibility = Visibility.Collapsed;

                // Render battery
                bool charging = Device?.Charging ?? false;
                BatteryPanel.Visibility = Connected ? Visibility.Visible : Visibility.Collapsed;
                BatteryIconCharging.Visibility = charging ? Visibility.Visible : Visibility.Collapsed;
                BatteryIconNotCharging.Visibility = !charging ? Visibility.Visible : Visibility.Collapsed;
                BatteryLevelText.Text = Device != null ? Device?.BatteryLevel.ToString() : "";
            }
            else if (DeviceGeneration == DeviceGeneration.TD_Pro)
            {
                AnnularThimbel.Visibility = Visibility.Visible;
                PinkyThimble.Visibility = Visibility.Visible;
                PalmNode.Visibility = Visibility.Visible;

                // Render battery
                bool charging = TouchDiverPro?.Master.Charging ?? false;
                BatteryPanel.Visibility = Connected ? Visibility.Visible : Visibility.Collapsed;
                BatteryIconCharging.Visibility = charging ? Visibility.Visible : Visibility.Collapsed;
                BatteryIconNotCharging.Visibility = !charging ? Visibility.Visible : Visibility.Collapsed;
                BatteryLevelText.Text = TouchDiverPro != null ? TouchDiverPro?.Master.BatteryLevel.ToString() : "";
            }

            ScaleTransform handScale = new ScaleTransform();
            handScale.ScaleX = HandSide == HandSide.Left ? 1 : -1;
            HandCanvas.RenderTransform = handScale;

            // Set connection status
            HandImage.Opacity = Connected ? 1 : 0.5;

            // Set mac address
            MacAddressText.Visibility = Connected ? Visibility.Visible : Visibility.Collapsed;
            MacAddressText.Text = Device?.MacAddress ?? "";         //TD
            MacAddressText.Text = TouchDiverPro?.MacAddress ?? "";  //TDPro



            
            // Set Thimbles
            if (!Connected)
            {
                RenderThimble(ActuationPoint.Thumb, false, true);
                RenderThimble(ActuationPoint.Index, false, true);
                RenderThimble(ActuationPoint.Middle, false, true);
                RenderThimble(ActuationPoint.Annular, false, true);
                RenderThimble(ActuationPoint.Pinky, false, true);
                RenderThimble(ActuationPoint.Palm, false, true);
            }
            else
            {
                if (DeviceGeneration == DeviceGeneration.TD)
                {
                    foreach (ThimbleStatus thimble in Device?.Thimbles)
                    {
                        RenderThimble(thimble.Id, thimble.Connected, thimble.StatusCode == 0);
                    }
                }
                else if (DeviceGeneration == DeviceGeneration.TD_Pro)
                {
                    foreach (TouchDiverProThimbleStatus node in TouchDiverPro?.Nodes)
                    {
                        RenderThimble(node.Id, node.Connected, true);
                    }
                }
            }
        }

        private void RenderThimble(ActuationPoint ap, bool connected, bool ok)
        {
            Dictionary<ActuationPoint, Ellipse> components = new Dictionary<ActuationPoint, Ellipse>()
            {
                 {ActuationPoint.Index, IndexThimble},{ActuationPoint.Thumb, ThumbThimble},{ActuationPoint.Middle, MiddleThimble},
                 {ActuationPoint.Annular, AnnularThimbel}, {ActuationPoint.Pinky, PinkyThimble}, {ActuationPoint.Palm, PalmNode}
            };

            if (!components.ContainsKey(ap))
                return;

            components[ap].Fill = new SolidColorBrush(connected ? (ok ? Colors.Green : Colors.Red) : Colors.Black);
            components[ap].Opacity = connected ? 1 : 0.5;
        }
    }
}
