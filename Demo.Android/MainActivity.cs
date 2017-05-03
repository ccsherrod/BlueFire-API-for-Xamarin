using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using BlueFire;

namespace Demo.Droid
{
    [Activity(MainLauncher = true, Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate (bundle);

            // ****************************************************************************
            // **********************  REQUIRED FOR BLUEFIRE API  *************************
            //
            // Also, ensure that the Demo.Android project properties Android Options/Linker 
            // Additional supported encodings West is checked.
            //
            API.Activity = this;
            API.Context = this.ApplicationContext;

            // ****************************************************************************

            global::Xamarin.Forms.Forms.Init (this, bundle);
			LoadApplication (new Demo.App ());
		}

        protected override void OnStart()
        {
            base.OnStart();

            API.IsActive = true;

            API.RaiseAppEvent(AppEventIds.IsActive);
        }

        protected override void OnStop()
        {
            base.OnStop();

            API.IsBackground = true;

            API.RaiseAppEvent(AppEventIds.IsBackground);
        }

        protected override void OnResume()
        {
            base.OnResume();

            API.IsBackground = false;

            API.RaiseAppEvent(AppEventIds.IsForeground);
        }

        // not guaranteed that this will run
        protected override void OnDestroy()
        {
            base.OnDestroy();

            API.IsTerminating = true;

            API.RaiseAppEvent(AppEventIds.IsTerminating);
        }
    }
}

