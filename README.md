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
</ul>

Version 2.1:<ul>
	<li>Fixed J1708 not retrieving all data requested.
    <li>Added SignalStrength (RSSI) property.
    <li>Removed the need for Android Activity.
    <li>ELD.IsStreaming no longer starts or stops streaming. Use Start/Stop Streaming methods.
</ul>

Version 2.2:<ul>
	<li>Requires Adapter Firmware Beta 3.10.9.
	<li>ELD rules are sent to the Adapter from the API.
	<li>The Adapter will reboot after ELD StopEngine if the App is not connected.
</ul>

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
</ul>

Version 2.4:<ul>
	<li>Added a Service class that simulates the API used with a service.
	<li>Added Start and Stop Service buttons to the Demo app.
	<li>Added method GetDistance which is the same as GetOdometer (GetOdometer actually calls GetDistance).
	<li>Added properties Truck.HiResDistance, LoResDistance, HiResOdometer, and LoResOdometer.
	<li>Truck.Odometer will return -1 if the OEM distance is not available (e.g. Volvo trucks).
	<li>Truck.Distance and Truck.Odometer returns the hi-resolution value unless it is not available in which case it returns the lo-resolution value.
	<li>Note that hi-resolution distance is at a 1 second ECM refresh rate while lo-resolution is at a 100 ms ECM refresh rate.
	<li>Modified the Demo app to reflect the above changes.
	<li>Fixed issue with API attempting to reconnect when disconnecting immediately after connecting.
	<li>Uses the latest BlueFire 4.8 core libraries.
</ul>
	
Version 3.0:<ul>
	<li>Removed all truck data methods and replaced them with the GetPIDs method.
	<li>Renamed AdapterId property to DeviceAddress (to be consistent with the BlueFire core libraries).
	<li>Renamed AdapterIdFilter property to DeviceAddressFilter.
	<li>Renamed PerformanceMode property to IsPerformanceModeOn.
	<li>Added PerformanceInterval property that sets the interval for PerformanceMode. The default is 500 ms.
	<li>Added IsJ1708Available property that indicates if the J1708 data bus is available.
	<li>Added HardwareType property with values HardwareTypes.HW_1_1 (old adapter), HW_6_Pin, and HW_9_Pin.
	<li>Added IsHeartbeatOn property that will turn the Adapter heartbeat on/off. Use with caution. The default is On.
	<li>Added IsNotificationsOn property that will turn Adapter and API notifications on/off. The default is Off.
	<li>Added OptimizeDataRetrieval property that optimizes retrieval of data when the same data is available from both J1939 and J1708 ECMs. The default is Off. Recommend On.
    <li>Added BluetoothRecycleAttempt property that will recycle (turn off/on) Bluetooth at the specified connection and reconnection attempt. The default is 2 (second attempt).
	<li>Added Notification ConnectionState that will return any API notifications.
    <li>Added ELDConnected ConnectionState that will be raised after the API receives ELD startup data from the adapter.
	<li>Added AdapterMessage ConnectionState that will be raised when there is a message from the Adapter.
	<li>Added IncompatibleAPI ConnectionState that will be raised if the API is not compatible with the Adapter.
	<li>Added J1939Starting and J1708Restarting ConnectionStates that will be raised when adapter is starting J1939 or restarting J1708.
	<li>Renamed IncompatibleVersion ConnectionState to IncompatibleAdapter.
	<li>Removed Connected ConnectionState as this was confusing because it only appled to the Bluetooth connection and not the Adapter connection.
	<li>Removed Ready ConnectionState since the Authenticated ConnectionState is raised when Ready use to be.
    <li>The Authenticated ConnectionState will now be raised after the API receives startup data from the adapter. This data includes PerformanceMode, SleepMode, LEDBrightness, IgnoreJ1939, IgnoreJ1708, HardwareType and any messages.
    <li>The IgnoreJ1939, and IgnoreJ1708 settings will be set appropriately if the HardwareType is HW_6_Pin.
	<li>The MaxConnectAttempts property now works for BLE adapters.
    <li>The MaxConnectAttempts property default value is changed from 10 to 5;
	<li>Removed the ELD Waiting RecordId as it is no longer sent by the Adapter.
	<li>Added ELD StartUpload and StopUpload methods that must be called prior to and after retrieving ELD records (only for Firmware 3.11-13).
    <li>Added Security setting Secure Device which secures the device (phone, tablet, etc) to an adapter. One device can be secured to many adapters (one to many relationship).
    <li>Security setting Secure Adapter remains unchanged and secures the device to a single adapter and secures the adapter to the one device (one to one relationship).)
    <li>Security setting UserName and Password secures the device to an adapter. A device can be secured to many adapters and many adapters can be secured to a device (many to many relationship).
    <li>Security (UserName, Password, Adapter Id, Device Id) are all encrypted with AES encryption.
	<li>Security authentication is performed in the adapter with Adapter Firmware 3.14.
    <li>Requires Adapter Firmware 3.14 for all security updates.
	<li>Better J1708 data retrieval with Firmware 3.13+.
	<li>More reliable BLE connection.
	<li>Faster BLE re-connections.
	<li>Faster BLE initial connection if using ConnectToLastAdapter.
	<li>Improved API and Adapter error reporting to the app.
	<li>Updated the Demo app and Service to demonstrate the above changes.
	<li>The ConnectionState Ready in the Demo app is replaced with ConnectionState Authenticated.
	<li>Added a Stress Test button to the Demo app that retrieves all the data to test loading the connection.
	<li>Uses the latest BlueFire 4.10 core libraries.
</ul>
