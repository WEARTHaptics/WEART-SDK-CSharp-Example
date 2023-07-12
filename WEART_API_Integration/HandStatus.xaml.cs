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

        public DeviceStatus? Device { get; set; }

        public HandStatus()
        {
            this.InitializeComponent();
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
            ScaleTransform handScale = new ScaleTransform();
            handScale.ScaleX = HandSide == HandSide.Left ? 1 : -1;
            HandCanvas.RenderTransform = handScale;

            // Set connection status
            HandImage.Opacity = Connected ? 1 : 0.5;

            // Set mac address
            MacAddressText.Visibility = Connected ? Visibility.Visible : Visibility.Collapsed;
            MacAddressText.Text = Device?.MacAddress ?? "";

            // Render battery
            bool charging = Device?.Charging ?? false;
            BatteryPanel.Visibility = Connected ? Visibility.Visible : Visibility.Collapsed;
            BatteryIconCharging.Visibility = charging ? Visibility.Visible : Visibility.Collapsed;
            BatteryIconNotCharging.Visibility = !charging ? Visibility.Visible : Visibility.Collapsed;
            BatteryLevelText.Text = Device != null ? Device?.BatteryLevel.ToString() : "";

            // Set Thimbles
            if (!Connected)
            {
                RenderThimble(ActuationPoint.Thumb, false, true);
                RenderThimble(ActuationPoint.Index, false, true);
                RenderThimble(ActuationPoint.Middle, false, true);
            }
            else
            {
                foreach (ThimbleStatus thimble in Device?.Thimbles)
                {
                    RenderThimble(thimble.Id, thimble.Connected, thimble.StatusCode == 0);
                }
            }
        }

        private void RenderThimble(ActuationPoint ap, bool connected, bool ok)
        {
            Dictionary<ActuationPoint, Ellipse> components = new Dictionary<ActuationPoint, Ellipse>()
            {
                 {ActuationPoint.Index, IndexThimble},{ActuationPoint.Thumb, ThumbThimble},{ActuationPoint.Middle, MiddleThimble},
            };

            if (!components.ContainsKey(ap))
                return;

            components[ap].Fill = new SolidColorBrush(connected ? (ok ? Colors.Green : Colors.Red) : Colors.Black);
            components[ap].Opacity = connected ? 1 : 0.5;
        }
    }
}
