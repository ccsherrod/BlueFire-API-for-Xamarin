# BlueFire-API-for-Xamarin
Microsoft Xamarin API for direct connection to the BlueFire J1939/J1708 Bluetooth Data Adapter. Documentation is available upon request from [BlueFire Support](mailto:support@bluefire-llc.com).

This API requires development under the Xamarin Platform.

Requirements for using the API are:
	<ul><li> Visual Studio 2015 or greater.</ul>
	<ul><li> A BlueFire Data Adapter. You can purchase an adapter from Amazon or the BlueFire store.</ul>

To build the demo, follow these steps:
    <ul><li> Replace missing References (Android, iOS, UWP) with their Library counterparts (BlueFire.Android, BlueFire.iOS, BlueFire.UWP). </ul>

Version 1:<ul>
	<li>Initial version.
</ul>

Version 1.1:<ul>
	<li>Changed the Demo App to retrieve truck data more efficiently.
	<li>Uses the most recent BlueFire code libraries.
</ul>

Version 1.2:<ul>
	<li>Renamed IsVersionIncompatible to IsCompatible and removed the check for it in GetData (see below).
	<li>Moved the incompatible version alert to the data event handler's IncompatibleVersion ConnectionState.
	<li>Updated DisconnectAdapter and OnBackButtonPressed.
	<li>Removed the await BlueFire.Disconnect from the data event handler (the API now does this).
	<li>Added setting the default Minimum Interval of 500ms in the API for Android apps.
	<li>Replaced IsKeyOn with BlueFire.IsKeyOn in AdapterConnected.
	<li>Improved connection and reconnection.
</ul>
	