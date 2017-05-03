using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

using BlueFire;

namespace Demo.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		//
		// This method is invoked when the application has loaded and is ready to run. In this 
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
            UIUserNotificationSettings Settings = UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Alert | UIUserNotificationType.Sound, null);
            UIApplication.SharedApplication.RegisterUserNotificationSettings(Settings);

            global::Xamarin.Forms.Forms.Init ();
			LoadApplication (new Demo.App ());

			return base.FinishedLaunching (app, options);
		}

        public override void OnActivated(UIApplication application)
        {
            API.IsActive = true;

            API.RaiseAppEvent(AppEventIds.IsActive);
        }

        public override void WillEnterForeground(UIApplication application)
        {
            API.IsBackground = false;

            API.RaiseAppEvent(AppEventIds.IsForeground);
        }

        public override void OnResignActivation(UIApplication application)
        {
            API.IsActive = false;

            API.RaiseAppEvent(AppEventIds.IsInactive);
        }

        public override void DidEnterBackground(UIApplication application)
        {
            API.IsBackground = true;

            API.RaiseAppEvent(AppEventIds.IsBackground);
        }

        // not guaranteed that this will run
        public override void WillTerminate(UIApplication application)
        {
            API.IsTerminating = true;

            API.RaiseAppEvent(API.AppEventIds.IsTerminating);
        }
    }
}
