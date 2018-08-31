using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using BlueFire;

namespace Demo.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static Application thisApp;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            thisApp = this;

            this.InitializeComponent();

            Resuming += OnResuming;
            Suspending += OnSuspending;

            EnteredBackground += OnEnteredBackground;
            LeavingBackground += OnLeavingBackground;

            // Ensure general app exceptions are handled
            AppDomain.CurrentDomain.UnhandledException += HandleUnCaughtException;
        }

        private void HandleUnCaughtException(object sender, System.UnhandledExceptionEventArgs args)
        {
            //args.Handled = true;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = false;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                Xamarin.Forms.Forms.Init(e);

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            API.IsBackground = true;

            API.RaiseAppEvent(AppEventIds.IsBackground);
        }

        private void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            API.IsBackground = false;

            API.RaiseAppEvent(AppEventIds.IsForeground);
        }

        private void OnResuming(object sender, object e)
        {
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral Deferral = e.SuspendingOperation.GetDeferral();

            API.IsTerminating = true;

            API.RaiseAppEvent(AppEventIds.IsTerminating);

            await WaitForAppToEnd();

            Deferral.Complete();
        }

        private async Task WaitForAppToEnd()
        {
            while (!API.HasEnded)
                await Task.Delay(10);
        }

    }
}
