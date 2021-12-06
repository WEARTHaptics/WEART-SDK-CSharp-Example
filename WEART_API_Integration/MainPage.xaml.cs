using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using WeArt.Components;
using WeArt.Core;
using WeArt.Utils;

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
        private WeArtThimbleTrackingObject _thimbleTrackingObject;

        public MainPage()
        {
            this.Loaded += PageLoaded;
            this.InitializeComponent();
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            WeArtController weArtController = new WeArtController();
            _weartClient = weArtController.Client;
            _weartClient.OnConnectionStatusChanged += OnConnectionChanged;

            _effect = new TouchEffect();

            _hapticObject = new WeArtHapticObject(_weartClient);
            _hapticObject.HandSides = HandSideFlags.Right; // HandSideFlags.Left;
            _hapticObject.ActuationPoints = ActuationPointFlags.Index;  //ActuationPointFlags.Middle| ActuationPointFlags.Thumb

            _thimbleTrackingObject = new WeArtThimbleTrackingObject(_weartClient);
            _thimbleTrackingObject.HandSide = HandSide.Right;
            _thimbleTrackingObject.ActuationPoint = ActuationPoint.Index;

            // schedule timer tracking
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 1000; //Milliseconds
            timer.AutoReset = true;
            timer.Elapsed += async (sender_, e_) =>
            {
                WeArtLog.Log($"WeArtThimbleTrackingObject Right-Index closure: {_thimbleTrackingObject.Closure.Value}");
            };
            timer.Start();

        }

        private void OnConnectionChanged(bool connected)
        {
            if (connected)
            {

                CreateEffect();
            }
        }

        private void CreateEffect()
        {
            Temperature temperature = Temperature.Default;
            Force force = Force.Default;
            Texture texture = Texture.Default;

            _effect.Set(temperature, force, texture);

            _hapticObject.AddEffect(_effect);
        }

        /// <summary>
        /// Internal class used to create the haptic effet on collision.
        /// </summary>
        internal class TouchEffect : IWeArtEffect
        {

            #region Events

            /// <summary>
            /// Defines the OnUpdate.
            /// </summary>
            public event Action OnUpdate;

            #endregion

            #region Properties

            /// <summary>
            /// Gets the Temperature.
            /// </summary>
            public Temperature Temperature { get; private set; } = Temperature.Default;

            /// <summary>
            /// Gets the Force.
            /// </summary>
            public Force Force { get; private set; } = Force.Default;

            /// <summary>
            /// Gets the Texture.
            /// </summary>
            public Texture Texture { get; private set; } = Texture.Default;

            #endregion

            #region Methods

            /// <summary>
            /// The Set.
            /// </summary>
            /// <param name="temperature">The temperature<see cref="Temperature"/>.</param>
            /// <param name="force">The force<see cref="Force"/>.</param>
            /// <param name="texture">The texture<see cref="Texture"/>.</param>
            public void Set(Temperature temperature, Force force, Texture texture)
            {
                // Need to clone these, or the internal arrays will point to the same data
                force = (Force)force.Clone();
                texture = (Texture)texture.Clone();


                bool changed = false;

                // Temperature
                changed |= !Temperature.Equals(temperature);
                Temperature = temperature;

                // Force
                changed |= !Force.Equals(force);
                Force = force;

                // Texture
                texture.VelocityZ = 0.5f;

                changed |= !Texture.Equals(texture);
                Texture = texture;

                if (changed)
                    OnUpdate?.Invoke();
            }

            #endregion

        }


        private void StartClient_Click(object sender, RoutedEventArgs e)
        {
            _weartClient.Start();
        }

        private void StopClient_Click(object sender, RoutedEventArgs e)
        {
            _weartClient.Stop();
        }

        private void AddEffectSample1_Click(object sender, RoutedEventArgs e)
        {
            Temperature temperature = Temperature.Default;
            temperature.Active = true;
            temperature.Value = 0.2f;

            Force force = Force.Default;
            force.Active = true;
            force.Value = 0.7f;

            Texture texture = Texture.Default;
            texture.Active = true;
            texture.TextureType = TextureType.ProfiledAluminiumMeshFast;

            _effect.Set(temperature, force, texture);

            if (_hapticObject.ActiveEffect == null)
                _hapticObject.AddEffect(_effect);
        }


        private void AddEffectSample2_Click(object sender, RoutedEventArgs e)
        {
            Temperature temperature = Temperature.Default;
            temperature.Active = true;
            temperature.Value = 0.6f;

            Force force = Force.Default;
            force.Active = true;
            force.Value = 0.2f;

            Texture texture = Texture.Default;
            texture.Active = true;
            texture.TextureType = TextureType.ProfiledAluminiumMeshFast;

            _effect.Set(temperature, force, texture);

            if (_hapticObject.ActiveEffect == null)
                _hapticObject.AddEffect(_effect);
        }

        private void AddEffectSample3_Click(object sender, RoutedEventArgs e)
        {
            Temperature temperature = Temperature.Default;
            temperature.Active = true;
            temperature.Value = 1.0f;

            Force force = Force.Default;
            force.Active = true;
            force.Value = 1.0f;

            Texture texture = Texture.Default;
            texture.Active = true;
            texture.TextureType = TextureType.ProfiledAluminiumMeshFast;

            _effect.Set(temperature, force, texture);

            if (_hapticObject.ActiveEffect == null)
                _hapticObject.AddEffect(_effect);
        }

        private void RemoveEffects_Click(object sender, RoutedEventArgs e)
        {
            _hapticObject.RemoveEffect(_effect);
        }
    }
}
