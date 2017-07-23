# BlueFire-API-for-Xamarin
Microsoft Xamarin API for direct connection to the BlueFire J1939/J1708 Bluetooth Data Adapter.

This API requires development under the Xamarin Platform.

Requirements for using the API are:
	<ul><li> Visual Studio 2015 or greater.</ul>
	<ul><li> A BlueFire Data Adapter. You can purchase an adapter from Amazon or the BlueFire store.</ul>

To build the demo, follow these steps:
    <ul><li> Replace missing References (Android, iOS, UWP) with their Library counterparts (BlueFire.Android, BlueFire.iOS, BlueFire.UWP).
	<li> For Android apps, ensure you include the BlueFire code that is in the Demo.Android MainActivity.cs. </ul>

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

Version 1.3:<ul>
	<li>Requires Adapter Firmware Beta 3.10.5.
	<li>Added getEngineVIN method.
	<li>Added synchronization to Truck Data methods.
	<li>Added RetrievalMethod parameter to Truck Data methods.
	<li>API documentation has been updated to reflect the above changes.
</ul>

Version 1.4:<ul>
	<li>Requires Adapter Firmware Beta 3.10.6.
    <li>All enums are now outside of the API reference.
    <li>Added enum RetrievalMethods.
	<li>Added RetrievalMethods to Truck Data methods.
    <li>Added property SyncTimeout.
    <li>Added property AdapterIdFilter.
    <li>Added property MaxReconnectAttempts.
    <li>Added property ConnectionState.
	<li>Added ConnectionState ConnectTimeout.
    <li>Added Synchronized option to the Connect method.
	<li>Added ConnectionState CANFilterFull.
	<li>Renamed property DiscoveryTimeOut to DiscoveryTimeout.
    <li>Renamed property MaxConnectRetrys to MaxReconnectAttempts.
	<li>API documentation has been updated to reflect the above changes.
    <li>API documentation is now included in the GitHub update.
</ul>

Version 2.0:<ul>
	<li>Compatible with Adapter Firmware Beta 3.10.6+
    <li>Added ELD Recording.
	<li>Fixed a few data retrieval bugs.
	<li>Added a section in the API doc for Adapter LEDs.
	<li>Rearranged the sections in the API documentation.

Version 2.1:<ul>
	<li>Fixed J1708 not retrieving all data requested.
    <li>Added SignalStrength (RSSI) property.
    <li>Removed the need for Android Activity.
    <li>ELD.IsStreaming no longer starts or stops streaming. Use Start/Stop Streaming methods.

Version 2.2:<ul>
	<li>Requires Adapter Firmware Beta 3.10.9.
	<li>ELD rules are sent to the Adapter from the API.
	<li>The Adapter will reboot after ELD StopEngine if the App is not connected.

Version 2.3:<ul>
	<li>Compatible with Adapter Firmware 3.11. Note, Firmware 3.10 is broken and must be updated to 3.11.
	<li>Changed property VIN to EngineVIN.
	<li>Added property CabBodyVIN.
	<li>Added properties CabBody.Make, Model, SerialNo, and UnitNo.
	<li>Renamed method GetEngineVIN to GetVehicleId and removed all parameters. This method will retrieve the VIN asynchronously.
	<li>Added a GetVehicleIdSync method for retrieving the VIN synchronously.
	<li>Added property AdvertisementTimeout.
	<li>For BLE adapters, if the ConnectToLastAdapter and UpdateSecurity (SecureAdapter) are not set, the API will connect to the adapter with the strongest signal.
	<li>Uses the latest BlueFire 4.7 core libraries.

Version 2.4:<ul>
	<li>Added method GetDistance which is the same as GetOdometer (GetOdometer actually calls GetDistance).
	<li>Added properties Truck.HiResDistance, LoResDistance, HiResOdometer, and LoResOdometer.
	<li>Truck.Odometer will return -1 if the OEM distance is not available (e.g. Volvo trucks).
	<li>Truck.Distance and Truck.Odometer returns the hi-resolution value unless it is not available in which case it returns the lo-resolution value.
	<li>Note that hi-resolution distance is at a 1 second ECM refresh rate while lo-resolution is at a 100 ms ECM refresh rate.
	<li>Modified the Demo app to reflect the above changes.
	<li>Fixed issue with API attempting to reconnect when disconnecting immediately after connecting.
	<li>Uses the latest BlueFire 4.8 core libraries.
