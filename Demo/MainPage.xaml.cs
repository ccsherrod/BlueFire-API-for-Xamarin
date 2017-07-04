using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Diagnostics;

using Xamarin.Forms;

using BlueFire;

namespace Demo
{
	public partial class MainPage : ContentPage
	{

    #region Declaratives

        private Int32 GroupNo;
        private const Int32 MaxGroupNo = 7;

        private RetrievalMethods RetrievalMethod; 
        private Int32 RetrievalInterval;

        private API BlueFire;

        private Boolean IsConnected;
        private Boolean IsConnecting;
        private Boolean IsConnectButton;

        private Int32 PGN;
        private Boolean IsSendingPGN;
        private Boolean IsMonitoringPGN;

        private Byte LedBrightness = 100;
        private Boolean IgnoreJ1939 = false;
        private Boolean IgnoreJ1708 = false;

        private String ErrorMessage = "";
        private Exception ErrorException = null;

        private ConnectionStates ConnectionState = ConnectionStates.NotConnected;

        private event API.EventHandler APIDataHandlerEvent;

        private event API.AppEventHandler APIAppHandlerEvent;

        // ELD variables
        private Int32 CurrentRecordNo;
        private Boolean IsUploading;
        private Int32 UploadFrom;

        private const String VINTitle = "VIN";
        private const String DriverIdTitle = "DriverId";

        private const String StartTitle = "Start";
        private const String StopTitle = "Stop";
        private const String StreamLocalTitle = "Stream Locally";
        private const String StreamRecordTitle = "Stream and Record Locally";
        private const String UploadRecordsTitle = "Upload";
        private const String SaveRecordsTitle = "Save";

        private Boolean DistanceShowNA = false;
        private Boolean OdometerShowNA = false;
        private Boolean TotalHoursShowNA = false;
        private Boolean IdleHoursShowNA = false;
        private Boolean TotalFuelShowNA = false;
        private Boolean IdleFuelShowNA = false;
        private Boolean LatLongShowNA = false;

        public enum CustomRecordIds : byte
        {
            StartedELD,
            StoppedELD
        }

    #endregion

    #region Constructor

        public MainPage()
        {
            InitializeComponent();

            InitializeControls();

            APIDataHandlerEvent += new API.EventHandler(DataEventHandler);
            APIAppHandlerEvent += new API.AppEventHandler(AppEventHandler);

            BlueFire = new API(APIDataHandlerEvent, APIAppHandlerEvent);

            Title.Text = "API Demo v-" + BlueFire.GetAPIVersion();

            Initialize();
        }

        private void InitializeControls()
        {
#if (__IOS__)
            TitleLayout.Padding = new Thickness(0, 20, 0, 0);

            LedBrightnessLayout.Padding = new Thickness(0, -4, 0, 0);
            UserNameLayout.Padding = new Thickness(0, -4, 0, 0);
            PasswordLayout.Padding = new Thickness(0, -4, 0, 0);
            PGNLayout.Padding = new Thickness(0, -4, 0, 0);
            PGNDataLayout.Padding = new Thickness(0, -4, 0, 0);

            DriverIdEntryLayout.Padding = new Thickness(0, -4, 0, 0);
            ELDIntervalEntryLayout.Padding = new Thickness(0, -4, 0, 0);
            IFTAIntervalEntryLayout.Padding = new Thickness(0, -4, 0, 0);
            StatsIntervalEntryLayout.Padding = new Thickness(0, -4, 0, 0);

#elif (WINDOWS_UWP)
            LedBrightnessLayout.Padding = new Thickness(0, -4, 0, 0);
            LedBrightnessLabel.WidthRequest *= 1.2; // = 160; // 130
            LedBrightnessEntry.WidthRequest *= 1.2; // = 54; //50

            UserNameLayout.Padding = new Thickness(0, -4, 0, 0);
            UserNameLabel.WidthRequest *= 1.2; // = 130;
            UserNameEntry.WidthRequest *= 1.2; // = 300;

            PasswordLayout.Padding = new Thickness(0, -4, 0, 0);
            PasswordLabel.WidthRequest *= 1.2; // = 130;
            PasswordEntry.WidthRequest *= 1.2; // = 300;

            PGNLayout.Padding = new Thickness(0, -4, 0, 0);
            PGNLabel.WidthRequest *= 1.2; // = 54;
            PGNEntry.WidthRequest *= 1.2; // = 100;

            PGNDataLayout.Padding = new Thickness(0, -4, 0, 0);
            PGNDataLabel.WidthRequest *= 1.2; // = 54;
            PGNDataEntry.WidthRequest *= 1.2; // = 300;

            DriverIdEntryLayout.Padding = new Thickness(0, -4, 0, 0);
            DriverIdLabel.WidthRequest *= 1.2;
            DriverIdEntry.WidthRequest *= 1.2;

            ELDIntervalEntryLayout.Padding = new Thickness(0, -4, 0, 0);
            ELDIntervalLabel.WidthRequest *= 1.2;
            ELDIntervalEntry.WidthRequest *= 1.2;

            IFTAIntervalEntryLayout.Padding = new Thickness(0, -4, 0, 0);
            IFTAIntervalLabel.WidthRequest *= 1.2;
            IFTAIntervalEntry.WidthRequest *= 1.2;

            StatsIntervalEntryLayout.Padding = new Thickness(0, -4, 0, 0);
            StatsIntervalLabel.WidthRequest *= 1.2;
            StatsIntervalEntry.WidthRequest *= 1.2;
#endif
        }

        private async void Initialize()
        {
            // Connect button
            IsConnectButton = true;

            // Keyboards
            LedBrightnessEntry.Keyboard = Keyboard.Numeric;
            PGNEntry.Keyboard = Keyboard.Numeric;

            ClearMessage();

            ShowStatus(ConnectionStates.NotConnected.ToString());

            // Initialize BlueFire API
            await BlueFire.Initialize();

            // Clear adapter id filter
            BlueFire.AdapterIdFilter.Clear();

            // Set initial Bluetooth discovery timeout.
            // Note, this will be adjusted by the API after a connection is made.
            BlueFire.DiscoveryTimeout = 30;

            // Instruct the API not to do any reconnections
            //BlueFire.MaxReconnectAttempts = 0;

            // Set performance mode if need to
            //BlueFire.PerformanceMode = true;

            // Set BlueFire settings
            BT2Switch.IsToggled = BlueFire.UseBT2;
            BLESwitch.IsToggled = BlueFire.UseBLE;

            J1939Switch.IsToggled = !BlueFire.IgnoreJ1939;
            J1708Switch.IsToggled = !BlueFire.IgnoreJ1708;

            // Security settings
            UserNameEntry.Text = "";
            PasswordEntry.Text = "";

            SecureAdapterSwitch.IsEnabled = false;

            // Proprietary PGNs
            PGNEntry.Text = "";
            PGNDataEntry.Text = "";

            NextButton.IsVisible = false;
            PrevButton.IsVisible = false;

            UpdateButton.IsEnabled = false;
            SendButton.IsEnabled = false;

            // ELD settings
            DriverIdEntry.Text = ELD.DriverId;

            ELDIntervalEntry.Text = ELD.ELDInterval.ToString();
            IFTAIntervalEntry.Text = ELD.IFTAInterval.ToString();
            StatsIntervalEntry.Text = ELD.StatsInterval.ToString();

            IFTASwitch.IsToggled = ELD.RecordIFTA;
            StatsSwitch.IsToggled = ELD.RecordStats;

            AlignELDSwitch.IsToggled = ELD.AlignELD;
            AlignIFTASwitch.IsToggled = ELD.AlignIFTA;
            AlignStatsSwitch.IsToggled = ELD.AlignStats;

            SecureELDSwitch.IsToggled = ELD.IsSecured;

            StreamingSwitch.IsToggled = ELD.IsStreaming;
            RecordConnectedSwitch.IsToggled = ELD.IsRecordingConnected;
            RecordDisconnectedSwitch.IsToggled = ELD.IsRecordingDisconnected;

            // Show initial key state (key off)
            ShowKeyState();

            // Set data retrieval method for testing.
            //RetrievalMethod = RetrievalMethods.OnChange;
            RetrievalMethod = RetrievalMethods.OnInterval;
            //RetrievalMethod = RetrievalMethods.Synchronized;

            // Or set the retrieval interval if using OnInterval.
            //RetrievalInterval = 1000; // default is BlueFire.MinInterval
        }

    #endregion

    #region Page Events

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Stop ELD recording if not set to record when app is connected
            if (!ELD.IsRecordingConnected)
            {
                // Stop any streaming
                if (ELD.IsStreaming)
                    ELD.StopStreaming();

                // Stop any recording
                if (ELD.IsStarted)
                    ELD.Stop();
            }

            // Get truck data if key is on when app is connecting
            if (TruckLayout.IsVisible)
            {
                if (BlueFire.IsKeyOn)
                    GetTruckData();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (ELDLayout.IsVisible)
                OnELDPageDisappearing();
        }

        protected override void OnSizeAllocated(Double Width, Double Height)
        {
            base.OnSizeAllocated(Width, Height);
        }

#if (!__IOS__)
        protected override Boolean OnBackButtonPressed()
        {
            // Disconnect the adapter
            DisconnectAdapter(false);

            base.OnBackButtonPressed();

            // Remove all pages from the stack so the app can end
            while (Navigation.NavigationStack.Count > 1)
                Navigation.RemovePage(Navigation.NavigationStack[0]);
#if (WINDOWS_UWP)
            // Windows Mobile does not raise the suspend event which raises the app terminate event
            // so we have to set to kill the app here.
            if (BlueFire.IsDeviceMobile)
                BlueFire.KillApp = true;
#endif
            // End the application.
            // Note, this will save the settings.
            EndApplication();

            // Note, returning false without calling the base will go back.
            return false;
        }
#endif
    #endregion

    #region App Event Handler

        internal async void AppEventHandler(AppEventIds EventId)
        {
            switch (EventId)
            {
                // Check for app becoming inactive (iOS).
                // Note, when Bluetooth is connecting, the app will be set inactive.
                case AppEventIds.IsInactive:
                    break;

                // Check for app going to the background
                case AppEventIds.IsBackground:

                    OnDisappearing();  // this will invoke Page Event OnDisappearing

                    // Adjust the data connection when in the background.
                    // Note, this will remove all app data retrieval.
                    if (!await BlueFire.SendToBackground())
                        return; // app is ending
                    
                    if (IsConnected)
                    {
                        // Re-retrieve data if on external power
                        if (BlueFire.IsDevicePowered)
                            await GetTruckData();

                        // Not on external power, disconnect the adapter
                        else
                            await BlueFire.Disconnect();
                    }

                    break;

                // Check for app is coming back to the foreground
                case AppEventIds.IsForeground:

                    // Restore the app to the foreground.
                    // Note, this will remove all app data retrieval.
                    if (!await BlueFire.RestoreForeground())
                        return; // app is ending

                    // Re-retrieve app data
                    await GetTruckData();

                    OnAppearing(); // this will invoke Page Event OnAppearing

                    break;

                // Check for app being terminated.
                // Note, iOS will terminate the app during the execution of any code here so
                // there is no sense in putting any code here.
                case AppEventIds.IsTerminating:

                    if (!await BlueFire.TerminateApp())
                        return;

                    // End the application.
                    // Note, this will save the settings.
                    EndApplication();

                    break;
            }
        }

    #endregion

    #region Data Event Handler

        private void DataEventHandler(ConnectionStates State)
        {
            // Run on the UI thread
            Xamarin.Forms.Device.BeginInvokeOnMainThread(async() => await DataDataHandlerUI(State));
        }

        private async Task DataDataHandlerUI(ConnectionStates State)
        {
            ConnectionState = State;

            switch (ConnectionState)
            {
                case ConnectionStates.Initializing:
                case ConnectionStates.Initialized:
                case ConnectionStates.Discovering:
                case ConnectionStates.Connected:
                case ConnectionStates.Authenticating:
                case ConnectionStates.Authenticated:
                case ConnectionStates.RetrievingData:
                case ConnectionStates.Disconnecting:
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.NotConnected:
                    if (IsConnecting || IsConnected)
                    {
                        AdapterNotConnected();
                        ShowStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.Connecting:
                    if (BlueFire.IsReconnecting)
                        if (!IsConnecting)
                            AdapterReconnecting();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.Ready:
                    if (!IsConnected)
                    {
                        await AdapterConnected();
                        ShowStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.KeyTurnedOn:
                    ShowKeyState();
                    await GetTruckData(); // get data if key is turned on after app is started
                    break;

                case ConnectionStates.KeyTurnedOff:
                    ShowKeyState();
                    break;

                case ConnectionStates.Disconnected:
                    if ((IsConnecting || IsConnected) && !BlueFire.IsReconnecting)
                        AdapterDisconnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.Reconnecting:
                    if (!IsConnecting)
                        AdapterReconnecting();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.Reconnected:
                    if (IsConnecting)
                    {
                        AdapterReconnected();
                        ShowStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.NotReconnected:
                    if (IsConnecting)
                        AdapterNotReconnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.DataAvailable:
                    if (IsConnected)
                    {
                        ShowData();
                        ShowStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.CANFilterFull:
                    ShowMessage("The CAN Filter is Full. Some data will not be retrieved.");
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.ConnectTimeout:
                case ConnectionStates.AdapterTimeout:
                    if (IsConnecting || IsConnected)
                    {
                        AdapterNotConnected();
                        ShowStatus(State.ToString());
                        ShowMessage("The Adapter Timed Out.");
                    }
                    break;

                case ConnectionStates.DataError:
                    LogError();
                    break;

                case ConnectionStates.SystemError:
                    LogError();
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.NotAuthenticated:
                    ShowMessage("You are not authorized to access this adapter. Check your adapter security settings.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.NotFound:
                    ShowMessage("A valid adapter was not found. Check your adapter connection settings.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.IncompatibleVersion:
                    ShowMessage("The Adapter is not compatible with this API.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.NoAdapter:
                case ConnectionStates.BluetoothNA:
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;
            }
        }

    #endregion

    #region Show Status

        private void ShowStatus(String Status)
        {
            StatusText.Text = Status;
        }

        private void ShowMessage(String Message)
        {
            MessageText.Text = Message;
            MessageText.IsVisible = true;
        }

        private void ClearMessage()
        {
            MessageText.Text = "";
            MessageText.IsVisible = false;
        }

    #endregion

    #region Show Key State

        private void ShowKeyState()
        {
            if (BlueFire.IsKeyOn)
                KeyStateText.Text = "Key On";
            else
                KeyStateText.Text = "Key Off";
        }

    #endregion

    #region Adapter Connection

        private async Task AdapterConnected()
        {
            WriteLog("Adapter connected.");

            IsConnected = true;
            IsConnecting = false;

            ClearMessage();

            SecureAdapterSwitch.IsEnabled = true;

            UpdateButton.IsEnabled = true;
            SendButton.IsEnabled = true;

            ConnectButton.Focus();

            // Check for API setting the adapter type
            BT2Switch.IsToggled = BlueFire.UseBT2;
            BLESwitch.IsToggled = BlueFire.UseBLE;

            // Test adapter id filter.
            // Note, this will allow the intial connection to a single adapter
            // but then not any more connection attempts until the app is restarted
            // because the filter is cleared when the app is started.
            //BlueFire.AdapterIdFilter.Add(BlueFire.AdapterId);

            // Get data if key is on when app is connecting
            if (BlueFire.IsKeyOn)
                await GetTruckData(); 
        }

        private void AdapterDisconnected()
        {
            WriteLog("Adapter disconnected.");

            AdapterNotConnected();
        }

        private void AdapterNotConnected()
        {
            WriteLog("Adapter not connected.");

            IsConnected = false;
            IsConnecting = false;

            ShowKeyState(); // key off

            ShowConnectButton();

            BT2Switch.IsEnabled = true;
            BLESwitch.IsEnabled = true;

            J1939Switch.IsEnabled = true;
            J1708Switch.IsEnabled = true;

            SecureAdapterSwitch.IsEnabled = false;

            UpdateButton.IsEnabled = false;
            SendButton.IsEnabled = false;

            ConnectButton.Focus();
        }

        private void AdapterReconnecting()
        {
            WriteLog("Adapter re-connecting.");

            IsConnected = false;
            IsConnecting = true;

            SecureAdapterSwitch.IsEnabled = false;

            UpdateButton.IsEnabled = false;
            SendButton.IsEnabled = false;

            WriteLog("App reconnecting to the Adapter. Reason is " + BlueFire.ReconnectReason + ".");

            ShowMessage("Lost connection to the Adapter, reconnecting.");
        }

        private void AdapterReconnected()
        {
            WriteLog("Adapter re-connected.");

            ShowMessage("Adapter reconnected.");
        }

        private void AdapterNotReconnected()
        {
            WriteLog("Adapter not re-connected.");

            AdapterNotConnected();

            ShowMessage("The Adapter did not reconnect.");
        }

    #endregion

    #region Show Data

        private void ShowData()
        {
            // Check if ignore databuses have changed
            if (IgnoreJ1939 != BlueFire.IgnoreJ1939)
            {
                IgnoreJ1939 = BlueFire.IgnoreJ1939;
                J1939Switch.IsToggled = !IgnoreJ1939; // switch is opposite
            }

            if (IgnoreJ1708 != BlueFire.IgnoreJ1708)
            {
                IgnoreJ1708 = BlueFire.IgnoreJ1708;
                J1708Switch.IsToggled = !IgnoreJ1708; // switch is opposite
            }

            // Show adapter data
            if (AdapterLayout.IsVisible)
                ShowAdapterData();

            // Show ELD data
            else if (ELDLayout.IsVisible)
                ShowELDData();

            // Show truck data
            else if (TruckLayout.IsVisible)
                ShowTruckData();
        }

    #endregion

    #region Show Adapter Page

        private void ShowAdapterPage()
        {
            HideELDPage();
            TruckLayout.IsVisible = false;
            AdapterLayout.IsVisible = true;

            ShowAdapterData();
        }

        private void ShowAdapterData()
        {
            // Check adapter settings
            if (LedBrightness != BlueFire.LedBrightness)
            {
                LedBrightness = BlueFire.LedBrightness;
                LedBrightnessEntry.Text = LedBrightness.ToString();
            }

            // Check for SendPGN response
            if ((IsSendingPGN || IsMonitoringPGN) && BlueFire.PGN == PGN)
            {
                IsSendingPGN = false; // only show sending data once
                PGNDataEntry.Text = BitConverter.ToString(BlueFire.PGNData);
            }
        }

        #endregion

    #region Get Truck Data

        private async Task GetTruckData()
        {
            DataView1.Text = "NA";
            DataView2.Text = "NA";
            DataView3.Text = "NA";
            DataView4.Text = "NA";
            DataView5.Text = "NA";
            DataView6.Text = "NA";
            DataView7.Text = "NA";

            FaultLayout.IsVisible = false;

            // Set the retrieval method and interval.
            // Note, this is here for demo-ing the different methods.
            RetrievalMethod = RetrievalMethods.OnChange; // default
#if (__ANDROID__)
            RetrievalMethod = RetrievalMethods.OnInterval; // recommended for Android
#endif
            //RetrievalInterval = 5000; // default is MinInterval, only required if RetrievalMethod is OnInterval

            //RetrievalMethod = RetrievalMethods.Synchronized;
            //BlueFire.SyncTimeout = 2000; // default is 1000, only required if RetrievalMethod is Synchronized

            // Clear any previous adapter data
            await BlueFire.ClearData();

            switch (GroupNo)
            {
                case 0:
                    TextView1.Text = "RPM";
                    TextView2.Text = "Speed";
                    TextView3.Text = "Accel Pedal";
                    TextView4.Text = "Pct Load";
                    TextView5.Text = "Pct Torque";
                    TextView6.Text = "Driver Torque";
                    TextView7.Text = "Torque Mode";

                    //await BlueFire.GetEngineData1(); // default RetrievalMethods.OnChange
                    //await BlueFire.GetEngineData1(RetrievalMethods.OnInterval); // default Interval = MinInterval
                    //await BlueFire.GetEngineData1(RetrievalMethods.Synchronized); // blocks until data is retrieved

                    await BlueFire.GetEngineData1(RetrievalMethod, RetrievalInterval); // RPM, Percent Torque, Driver Torque, Torque Mode
                    await BlueFire.GetEngineData2(RetrievalMethod, RetrievalInterval); // Percent Load, Accelerator Pedal Position
                    await BlueFire.GetEngineData3(RetrievalMethod, RetrievalInterval); // Vehicle Speed, Max Set Speed, Brake Switch, Clutch Switch, Park Brake Switch, Cruise Control Settings and Switches

                    break;

                case 1:
                    TextView1.Text = "Distance";
                    TextView2.Text = "Odometer";
                    TextView3.Text = "Total Hours";
                    TextView4.Text = "Idle Hours";
                    TextView5.Text = "Brake Pres";
                    TextView6.Text = "Brake Air";
                    TextView7.Text = "";

                    await BlueFire.GetOdometer(RetrievalMethod, RetrievalInterval); // Odometer (Engine Distance)
                    await BlueFire.GetBrakeData(RetrievalMethod, RetrievalInterval); // Application Pressure, Primary Pressure, Secondary Pressure
                    await BlueFire.GetEngineHours(RetrievalMethod, RetrievalInterval); // Total Engine Hours, Total Idle Hours

                    break;

                case 2:
                    TextView1.Text = "Fuel Rate";
                    TextView2.Text = "Fuel Used";
                    TextView3.Text = "HiRes Fuel";
                    TextView4.Text = "Idle Fuel Used";
                    TextView5.Text = "Avg Fuel Econ";
                    TextView6.Text = "Inst Fuel Econ";
                    TextView7.Text = "Throttle Pos";

                    await BlueFire.GetFuelData(RetrievalMethod, RetrievalInterval); // Fuel Used, Idle Fuel Used, Fuel Rate, Instant Fuel Economy, Avg Fuel Economy, Throttle Position

                    break;

                case 3:
                    TextView1.Text = "Oil Temp";
                    TextView2.Text = "Oil Pressure";
                    TextView3.Text = "Intake Temp";
                    TextView4.Text = "Intake Pres";
                    TextView5.Text = "Coolant Temp";
                    TextView6.Text = "Coolant Pres";
                    TextView7.Text = "Coolant Level";

                    await BlueFire.GetPressures(RetrievalMethod, RetrievalInterval); // Oil Pressure, Coolant Pressure, Intake Manifold(Boost) Pressure
                    await BlueFire.GetTemperatures(RetrievalMethod, RetrievalInterval); // Oil Temp, Coolant Temp, Intake Manifold Temperature
                    await BlueFire.GetCoolantLevel(RetrievalMethod, RetrievalInterval); // Coolant Level

                    break;

                case 4:
                    TextView1.Text = "Brake Switch";
                    TextView2.Text = "Clutch Switch";
                    TextView3.Text = "Park Switch";
                    TextView4.Text = "Cruise Switch";
                    TextView5.Text = "Cruise State";
                    TextView6.Text = "Cruise Speed";
                    TextView7.Text = "";

                    await BlueFire.GetEngineData3(RetrievalMethod, RetrievalInterval); // Vehicle Speed, Max Set Speed, Brake Switch, Clutch Switch, Park Brake Switch, Cruise Control Settings and Switches

                    break;

                case 5:
                    TextView1.Text = "Max Speed";
                    TextView2.Text = "HiRes Max";
                    TextView3.Text = "Current Gear";
                    TextView4.Text = "Selected Gear";
                    TextView5.Text = "Battery Volts";
                    TextView6.Text = "";
                    TextView7.Text = "";

                    await BlueFire.GetEngineData3(RetrievalMethod, RetrievalInterval); // Vehicle Speed, Max Set Speed, Brake Switch, Clutch Switch, Park Brake Switch, Cruise Control Settings and Switches
                    await BlueFire.GetBatteryVoltage(RetrievalMethod, RetrievalInterval); // Battery Voltage
                    await BlueFire.GetTransmissionGears(RetrievalMethod, RetrievalInterval); // Selected and Current Gears

                    break;

                case 6:
                    TextView1.Text = "VIN";
                    TextView2.Text = "Make";
                    TextView3.Text = "Model";
                    TextView4.Text = "Serial No";
                    TextView5.Text = "Unit No";
                    TextView6.Text = "";
                    TextView7.Text = "";

                    if (!BlueFire.Truck.VINExists)
                    {
                        DataView7.Text = "Retrieving VIN ...";
                        await BlueFire.GetVehicleIdSync(); // VIN synchronously
                                                           //await BlueFire.GetVehicleId(); // VIN asynchronously
                    }

                    if (!BlueFire.Truck.Engine.IdExists)
                    {
                        DataView7.Text = "Retrieving Vehicle Data ...";
                        await BlueFire.GetVehicleData(); // Make, Model, Serial No asynchronously
                    }

                    break;

                case 7:
                    TextView1.Text = "Source";
                    TextView2.Text = "SPN";
                    TextView3.Text = "FMI";
                    TextView4.Text = "Occurrence";
                    TextView5.Text = "Conversion";
                    TextView6.Text = "";
                    TextView7.Text = "";

                    // Faults
                    await BlueFire.GetFaults();

                    break;
            }
        }

    #endregion

    #region Show Truck Data

        private void ShowTruckData()
        {
            switch (GroupNo)
            {
                case 0:
                    DataView1.Text = FormatInt32(BlueFire.Truck.RPM);
                    DataView2.Text = FormatSingle(BlueFire.Truck.Speed, 0);
                    DataView3.Text = FormatSingle(BlueFire.Truck.AccPedPos, 2);
                    DataView4.Text = FormatInt32(BlueFire.Truck.PctLoad);
                    DataView5.Text = FormatInt32(BlueFire.Truck.PctTorque);
                    DataView6.Text = FormatInt32(BlueFire.Truck.DrvPctTorque);
                    DataView7.Text = BlueFire.Truck.TorqueMode.ToString();
                    break;

                case 1:
                    DataView1.Text = FormatSingle(BlueFire.Truck.Distance, 0);
                    DataView2.Text = FormatSingle(BlueFire.Truck.Odometer, 0); // HiRes Distance
                    DataView3.Text = FormatSingle(BlueFire.Truck.TotalHours, 2);
                    DataView4.Text = FormatSingle(BlueFire.Truck.IdleHours, 2);
                    DataView5.Text = FormatSingle(BlueFire.Truck.BrakeApplicationPressure, 2);
                    DataView6.Text = FormatSingle(BlueFire.Truck.Brake1AirPressure, 2);
                    DataView7.Text = "";
                    break;

                case 2:
                    DataView1.Text = FormatSingle(BlueFire.Truck.FuelRate, 2);
                    DataView2.Text = FormatSingle(BlueFire.Truck.TotalFuelUsed, 2);
                    DataView3.Text = FormatSingle(BlueFire.Truck.HiResFuelUsed, 2);
                    DataView4.Text = FormatSingle(BlueFire.Truck.IdleFuelUsed, 2);
                    DataView5.Text = FormatSingle(BlueFire.Truck.AvgFuelEcon, 2);
                    DataView6.Text = FormatSingle(BlueFire.Truck.InstFuelEcon, 2);
                    DataView7.Text = FormatSingle(BlueFire.Truck.ThrottlePos, 2);
                    break;

                case 3:
                    DataView1.Text = FormatSingle(BlueFire.Truck.OilTemp, 2);
                    DataView2.Text = FormatSingle(BlueFire.Truck.OilPressure, 2);
                    DataView3.Text = FormatSingle(BlueFire.Truck.IntakeTemp, 2);
                    DataView4.Text = FormatSingle(BlueFire.Truck.IntakePressure, 2);
                    DataView5.Text = FormatSingle(BlueFire.Truck.CoolantTemp, 2);
                    DataView6.Text = FormatSingle(BlueFire.Truck.CoolantPressure, 2);
                    DataView7.Text = FormatSingle(BlueFire.Truck.CoolantLevel, 2);
                    break;

                case 4:
                    DataView1.Text = BlueFire.Truck.BrakeSwitch.ToString();
                    DataView2.Text = BlueFire.Truck.ClutchSwitch.ToString();
                    DataView3.Text = BlueFire.Truck.ParkBrakeSwitch.ToString();
                    DataView4.Text = BlueFire.Truck.CruiseSwitch.ToString();
                    DataView5.Text = BlueFire.Truck.CruiseState.ToString();
                    DataView6.Text = FormatSingle(BlueFire.Truck.CruiseSpeed, 0);
                    DataView7.Text = "";
                    break;

                case 5:
                    DataView1.Text = FormatSingle(BlueFire.Truck.MaxSpeed, 0);
                    DataView2.Text = FormatSingle(BlueFire.Truck.HiResMaxSpeed, 0);
                    //DataView3.Text = FormatInt32(BlueFire.Truck.CurrentGear);
                    //DataView4.Text = FormatInt32(BlueFire.Truck.SelectedGear);
                    DataView5.Text = FormatSingle(BlueFire.Truck.BatteryPotential, 2);
                    DataView6.Text = "";
                    DataView7.Text = "";
                    break;

                case 6:
                    DataView1.Text = BlueFire.Truck.EngineVIN;
                    DataView2.Text = BlueFire.Truck.Engine.Make;
                    DataView3.Text = BlueFire.Truck.Engine.Model;
                    DataView4.Text = BlueFire.Truck.Engine.SerialNo;
                    DataView5.Text = BlueFire.Truck.Engine.UnitNo;
                    //DataView1.Text = BlueFire.Truck.CabBodyVIN;
                    //DataView2.Text = BlueFire.Truck.CabBody.Make;
                    //DataView3.Text = BlueFire.Truck.CabBody.Model;
                    //DataView4.Text = BlueFire.Truck.CabBody.SerialNo;
                    //DataView5.Text = BlueFire.Truck.CabBody.UnitNo;
                    DataView6.Text = "";

                    if (BlueFire.Truck.EngineVINExists && BlueFire.Truck.Engine.IdExists)
                        DataView7.Text = "";

                    break;

                case 7:

                    String Source = "";
                    String SPN = "";
                    String FMI = "";
                    String Occurrence = "";
                    String Conversion = "";

                    FaultLayout.IsVisible = false;
                    
                    // Show truck faults
                    if (BlueFire.Truck.ActiveFaultsCount > 0)
                    {
                        // Check if fault count changed
                        if (FaultIndex >= BlueFire.Truck.ActiveFaultsCount)
                            FaultIndex = BlueFire.Truck.ActiveFaultsCount - 1;

                        Source = BlueFire.Faults.Items[FaultIndex].Source.ToString();
                        SPN = BlueFire.Faults.Items[FaultIndex].SPN.ToString();
                        FMI = BlueFire.Faults.Items[FaultIndex].FMI.ToString();
                        Occurrence = BlueFire.Faults.Items[FaultIndex].Occurrence.ToString();
                        Conversion = BlueFire.Faults.Items[FaultIndex].Conversion.ToString();

                        FaultLayout.IsVisible = true;

                        if (BlueFire.Truck.ActiveFaultsCount > 1)
                            NextFaultButton.IsVisible = true;
                        else
                            NextFaultButton.IsVisible = false;
                    }

                    DataView1.Text = Source;
                    DataView2.Text = SPN;
                    DataView3.Text = FMI;
                    DataView4.Text = Occurrence;
                    DataView5.Text = Conversion;
                    DataView6.Text = "";
                    DataView7.Text = "";

                    break;
            }
        }

        private Int32 FaultIndex = 0;

        private String FormatInt32(Int32 Data)
        {
            if (Data < 0)
                return "NA";
            else
                return Data.ToString();
        }
        private String FormatSingle(Single Data, Int32 Precision)
        {
            if (Data < 0)
                return "NA";

            return Math.Round(Data, Precision).ToString();
        }

    #endregion

    #region Show ELD Page

        #region Clear ELD Page

        private void ClearELDPage()
        {
            CurrentRecordNo = -1;

            FormatRemainingText();

            RecordNoText.Text = "";
            RecordIdText.Text = "";
            TimeText.Text = "";
            VINText.Text = "";
            DistanceText.Text = "";
            OdometerText.Text = "";
            TotalHoursText.Text = "";
            IdleHoursText.Text = "";
            TotalFuelText.Text = "";
            IdleFuelText.Text = "";
            LatitudeText.Text = "";
            LongitudeText.Text = "";

            RefreshELDPage();
        }

        #endregion

        #region Set ELD Buttons

        private void SetELDButtons()
        {
            // Streaming text
            if (ELD.IsStreaming)
            {
                if (ELD.IsRecordingConnected)
                    StreamingLabel.Text = StreamRecordTitle;
                else
                    StreamingLabel.Text = StreamLocalTitle;
            }

            // Check for third party securing access
            if (ELD.IsAccessSecured)
            {
                EnableELDInput(false);

                StartELDButton.IsVisible = false;
                UploadELDButton.IsVisible = false;
                DeleteELDButton.IsVisible = false;

                return;
            }

            // Start button text
            if (ELD.IsStarted)
                StartELDButton.Text = StopTitle;
            else
                StartELDButton.Text = StartTitle;

            // Uploading
            if (IsUploading)
            {
                EnableELDInput(true);

                StartELDButton.IsEnabled = false;
                DeleteELDButton.IsEnabled = false;
                UploadELDButton.IsEnabled = false;

                return;
            }

            // Recording
            if (ELD.IsStarted)
            {
                EnableELDInput(false);

                StartELDButton.IsEnabled = true; // for stop
                DeleteELDButton.IsEnabled = false;
                UploadELDButton.IsEnabled = false;

                return;
            }

            // Not recording and local records available
            if (ELD.LocalRecordNo > 0)
            {
                // Enable the email and delete buttons
                UploadELDButton.Text = SaveRecordsTitle;
                UploadELDButton.IsEnabled = true;

                DeleteELDButton.IsEnabled = true;
                StartELDButton.IsEnabled = false;
                return;
            }

            // Not recording and adapter records available
            if (ELD.CurrentRecordNo > 0)
            {
                EnableELDInput(true);

                if (ELD.IsRecordingLocally || ELD.RemainingPercent == 0)  // recording locally or memory full
                    StartELDButton.IsEnabled = false;
                else
                    StartELDButton.IsEnabled = true; // for start

                if (ELD.UploadFrom == 1)
                    DeleteELDButton.IsEnabled = true;

                UploadELDButton.Text = UploadRecordsTitle;
                UploadELDButton.IsEnabled = true;

                return;
            }

            // Not recording and no records
            EnableELDInput(true);

            StartELDButton.IsEnabled = true; // for start

            UploadELDButton.Text = UploadRecordsTitle;
            UploadELDButton.IsEnabled = false;

            DeleteELDButton.IsEnabled = false;
        }

        private void EnableELDInput(Boolean IsEnabled)
        {
            DriverIdEntry.IsEnabled = IsEnabled;

            ELDIntervalEntry.IsEnabled = IsEnabled;
            AlignELDSwitch.IsEnabled = IsEnabled;

            IFTASwitch.IsEnabled = IsEnabled;
            IFTAIntervalEntry.IsEnabled = IsEnabled;
            AlignIFTASwitch.IsEnabled = IsEnabled;

            StatsSwitch.IsEnabled = IsEnabled;
            StatsIntervalEntry.IsEnabled = IsEnabled;
            AlignStatsSwitch.IsEnabled = IsEnabled;

            SecureELDSwitch.IsEnabled = IsEnabled;

            StreamingSwitch.IsEnabled = IsEnabled;
            RecordConnectedSwitch.IsEnabled = IsEnabled;
            RecordDisconnectedSwitch.IsEnabled = IsEnabled;
        }

        #endregion

        #region Start/Stop Recording

        private void StartStop()
        {
            // Check for starting a new ELD recording
            if (!ELD.IsStarted && ELD.IsNewRecording)
            {
                // Check if not enough memory to record all ELD records
                if (!ELD.IsTotalMemoryAvailable)
                {
                    ShowMessage("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
                    return;
                }
                // Check if not enough memory remaining to record all ELD records
                if (!ELD.IsMemoryAvailable)
                    // Memory available only if reset to the beginning of memory
                    ELD.Reset();
            }

            // Start or stop recording

            if (!ELD.IsStarted) // start recording
            {
                // Check ELD data
                if (!EditELDData())
                    return;

                // Check for any recording set
                if (!ELD.IsStreaming && ELD.RecordingMode == ELD.RecordingModes.RecordNever)
                {
                    ShowMessage("You have not set any ELD recording.");
                    return;
                }

                BlueFire.SetAdapterTime();

                CheckStreaming();

                if (ELD.IsRecordingLocally) // recording locally
                {
                    ELD.RecordNo = 0;
                }

                SendCustomRecord((ELD.RecordIds)CustomRecordIds.StartedELD);

                ELD.Start();

                ELD.UploadRecordNo = 0;
            }
            else // stop recording
            {
                SendCustomRecord((ELD.RecordIds)CustomRecordIds.StoppedELD);

                ELD.Stop();
            }
        }

        private void CheckStreaming()
        {
            // Start streaming ELD records
            if (ELD.IsStreaming)
                ELD.StartStreaming();
            else
                ELD.StopStreaming();
        }

        #endregion

        #region Custom ELD Record

        private void SendCustomRecord(ELD.RecordIds RecordId)
        {
            Byte CustomId = (Byte)((Byte)ELD.RecordIds.Custom + (Byte)RecordId);

            // Set the data to whatever
            Byte[] CustomData = new Byte[ELD.CustomDataLength];
            CustomData[0] = 1;
            CustomData[1] = 2;

            // Send the custom record to the adapter
            ELD.WriteRecord(CustomId, CustomData);
        }

        #endregion

        #region Show ELD Data

        private void ShowELDData()
        {
            if (!ELD.IsCompatibleAdapter)
                return;

            // Check for adapter or local recording, or adapter upload
            if (ELD.CurrentRecordNo > 0 || ELD.IsRecordingLocally)
                if (ELD.RecordNo > 0 && ELD.RecordNo != CurrentRecordNo)
                {
                    CurrentRecordNo = ELD.RecordNo;

                    RefreshELDPage(CurrentRecordNo);

                    if (ELD.IsRecordingLocally && !IsUploading) // recording locally
                    {
                        if (ELD.IsStarted || ELD.LocalRecordNo > 0)
                        {
                            ELD.LocalRecordNo = ELD.RecordNo;

                            ELD.UploadFrom = 0;
                            ELD.UploadTo = ELD.RecordNo;
                            ELD.UploadRecordNo = ELD.RecordNo;

                            WriteELDRecord();
                        }
                    }
                    else // recording or uploading from the adapter
                    {
                        if (IsUploading) // upload from adapter
                            UploadELDRecord(); // will get next record
                    }
                }

            SetELDButtons();
        }

        #endregion

        #region Refresh ELD Page

        private void RefreshELDPage(Int32 RecordNo = 0)
        {
            const String DateFormat = "M-dd-yy h:mm:ss tt";

            if (RecordNo > 0)
            {
                FormatRemainingText();

                ELD.RecordIds RecordId = (ELD.RecordIds)ELD.RecordId;

                // Record No
                if (RecordId == ELD.RecordIds.Waiting)
                    RecordNoText.Text = "";
                else
                    RecordNoText.Text = RecordNo.ToString();

                // Record Id
                if (RecordId >= ELD.RecordIds.Custom)
                    RecordIdText.Text = ((CustomRecordIds)(RecordId - ELD.RecordIds.Custom)).ToString();
                else
                    RecordIdText.Text = RecordId.ToString();

                // Driver Id
                if (ELD.RecordId == (Byte)ELD.RecordIds.DriverId)
                {
                    VINLabel.Text = DriverIdTitle;
                    VINText.Text = ELD.DriverId;
                }

                // VIN
                else if (ELD.RecordId == (Byte)ELD.RecordIds.VIN)
                {
                    VINLabel.Text = VINTitle;
                    VINText.Text = ELD.VIN;
                }
                else if (RecordId >= ELD.RecordIds.Custom || ELD.RecordId == (Byte)ELD.RecordIds.Waiting)
                {
                    VINLabel.Text = VINTitle;
                    VINText.Text = "";
                }
                else // ELD, IFTA or Stats
                {
                    VINLabel.Text = VINTitle;
                    if (ELD.VIN == Const.NA)
                        VINText.Text = "";
                    else
                        VINText.Text = ELD.VIN;
                }

                SetShowNA();

                TimeText.Text = Helper.FormatDate(ELD.Time.Ticks, DateFormat, true);
                DistanceText.Text = Helper.FormatDistance(ELD.Distance, DistanceShowNA, true);
                OdometerText.Text = Helper.FormatDistance(ELD.Odometer, OdometerShowNA, true);
                TotalHoursText.Text = Helper.FormatEngineHours(ELD.TotalHours, TotalHoursShowNA);
                IdleHoursText.Text = Helper.FormatEngineHours(ELD.IdleHours, IdleHoursShowNA);
                TotalFuelText.Text = Helper.FormatFuelUsed(ELD.TotalFuel, TotalFuelShowNA, true);
                IdleFuelText.Text = Helper.FormatFuelUsed(ELD.IdleFuel, IdleFuelShowNA, true);
                LatitudeText.Text = Helper.FormatLatLong(ELD.Latitude, LatLongShowNA);
                LongitudeText.Text = Helper.FormatLatLong(ELD.Longitude, LatLongShowNA);

                SetTextColor((ELD.RecordIds)ELD.RecordId, RecordIdText);
                SetTextColor(ELD.Time, TimeText);
                if (ELD.RecordId == (Byte)ELD.RecordIds.DriverId)
                    SetTextColor(ELD.DriverId, VINText);
                else
                    SetTextColor(ELD.VIN, VINText);
                SetTextColor(ELD.Distance, DistanceText);
                SetTextColor(ELD.Odometer, OdometerText);
                SetTextColor(ELD.TotalHours, TotalHoursText);
                SetTextColor(ELD.IdleHours, IdleHoursText);
                SetTextColor(ELD.TotalFuel, TotalFuelText);
                SetTextColor(ELD.IdleFuel, IdleFuelText);
                SetTextColor(ELD.Latitude, LatitudeText);
                SetTextColor(ELD.Longitude, LongitudeText);
            }

            SetELDButtons();
        }

        private void FormatRemainingText()
        {
            Single RemainingPercent;
            String RemaingTimeRecordText;

            if (IsUploading)
            {
                Int32 RecordNo = ELD.RecordNo;
                if (RecordNo == 0)
                    RecordNo = ELD.UploadRecordNo;

                RemainingPercent = ((Single)(ELD.CurrentRecordNo - RecordNo) / ELD.CurrentRecordNo) * 100F;
                RemaingTimeRecordText = " (" + (ELD.CurrentRecordNo - RecordNo) + " records)";
            }
            else
            {
                RemainingPercent = ELD.RemainingPercent;

                RemaingTimeRecordText = "";
                //if (ELD.RemainingTime <= Const.HoursPerWeek) // one week
                RemaingTimeRecordText = " (" + Helper.FormatNumber(ELD.RemainingTime, 2) + Helper.GetUM(Helper.UnitOfMeasures.Hours) + ")";
            }

            RemainingText.Text = Helper.FormatNumber(RemainingPercent, 2) + Helper.GetUM(Helper.UnitOfMeasures.Percent) + RemaingTimeRecordText;
        }

        #region Set ShowNA

        private void SetShowNA()
        {
            DistanceShowNA = false;
            OdometerShowNA = false;
            TotalHoursShowNA = false;
            IdleHoursShowNA = false;
            TotalFuelShowNA = false;
            IdleFuelShowNA = false;
            LatLongShowNA = false;

            switch ((ELD.RecordIds)ELD.RecordId)
            {
                case ELD.RecordIds.DriverId:
                    break;

                case ELD.RecordIds.VIN:
                    break;

                case ELD.RecordIds.IFTA:
                    DistanceShowNA = true;
                    OdometerShowNA = true;
                    TotalFuelShowNA = true;
                    LatLongShowNA = true;
                    break;

                case ELD.RecordIds.Stats:
                    DistanceShowNA = true;
                    TotalHoursShowNA = true;
                    TotalFuelShowNA = true;
                    IdleHoursShowNA = true;
                    IdleFuelShowNA = true;
                    break;

                // ELD 
                case ELD.RecordIds.StartEngine:
                case ELD.RecordIds.StartDriving:
                case ELD.RecordIds.Driving:
                case ELD.RecordIds.StopDriving:
                case ELD.RecordIds.StopEngine:
                    DistanceShowNA = true;
                    OdometerShowNA = true;
                    TotalHoursShowNA = true;
                    LatLongShowNA = true;
                    break;

                // Custom 
                default:
                    break;
            }
        }

        #endregion

        #region Set Text Color

        public static void SetTextColor(ELD.RecordIds Value, Label Control)
        {
            if (Value == ELD.RecordIds.Waiting)
                Control.TextColor = Color.Red;
            else
                Control.TextColor = Color.Green;
        }

        public static void SetTextColor(Single Value, Label Control)
        {
            if (Value == 0)
                Control.TextColor = Color.Gray;
            else
                Control.TextColor = Color.Green;
        }

        public static void SetTextColor(Double Value, Label Control)
        {
            if (Value == 0)
                Control.TextColor = Color.Gray;
            else
                Control.TextColor = Color.Green;
        }

        public static void SetTextColor(String Value, Label Control)
        {
            if (Value == Const.NA)
                Control.TextColor = Color.Gray;
            else
                Control.TextColor = Color.Green;
        }

        public static void SetTextColor(DateTime Value, Label Control)
        {
            if (Value.Year <= Const.BaseYear)
                Control.TextColor = Color.Gray;
            else
                Control.TextColor = Color.Green;
        }

        #endregion

        #endregion

        #region Edit ELD Data

        private Boolean EditELDData()
        {
            if (!EditDriver())
                return false;

            if (!EditELDInterval())
                return false;

            if (IFTASwitch.IsToggled)
                if (!EditIFTAInterval())
                return false;

            if (StatsSwitch.IsToggled)
                if (!EditStatsInterval())
                return false;

            // Save ELD data
            ELD.RecordIFTA = IFTASwitch.IsToggled;
            ELD.RecordStats = StatsSwitch.IsToggled;

            ELD.IsSecured = SecureELDSwitch.IsToggled;

            ELD.IsStreaming = StreamingSwitch.IsToggled;

            ELD.SetRecordingMode(RecordConnectedSwitch.IsToggled, RecordDisconnectedSwitch.IsToggled);

            SetELDButtons();

            return true;
        }

        private Boolean EditDriver()
        {
            String DriverId = DriverIdEntry.Text.Trim();

            if (DriverId.Length > ELD.CustomDataLength) // driver id is treated as a custom record
            {
                ShowMessage("Driver Id length is too long for ELD custom records.");
                return false;
            }

            ELD.DriverId = DriverId;

            return true;
        }

        private Boolean EditELDInterval()
        {
            Single ELDInterval;
            if (!Single.TryParse("0" + ELDIntervalEntry.Text, out ELDInterval))
            {
                ShowMessage("ELD Interval must be numeric.");
                return false;
            }

            if (ELDInterval == 0)
                ELDInterval = ELD.DefaultELDInterval;

            if (AlignELDSwitch.IsToggled && !ELD.IsHourAligned(ELDInterval))
            {
                ShowMessage("ELD Interval cannot be aligned to the hour.");
                AlignELDSwitch.IsToggled = false;
                return false;
            }

            ELD.ELDInterval = ELDInterval;

            ELDIntervalEntry.Text = ELDInterval.ToString();

            if (ELD.MaxRecords > 0 && !ELD.IsTotalMemoryAvailable)
            {
                ShowMessage("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
                return false;
            }

            FormatRemainingText();

            return true;
        }

        private Boolean EditIFTAInterval()
        {
            Single ELDIFTAInterval;
            if (!Single.TryParse("0" + IFTAIntervalEntry.Text, out ELDIFTAInterval))
            {
                ShowMessage("IFTA Interval must be numeric.");
                return false;
            }

            if (ELDIFTAInterval == 0)
                ELDIFTAInterval = ELD.DefaultIFTAInterval;

            if (AlignIFTASwitch.IsToggled && !ELD.IsHourAligned(ELDIFTAInterval))
            {
                ShowMessage("IFTA Interval cannot be aligned to the hour.");
                AlignIFTASwitch.IsToggled = false;
                return false;
            }

            ELD.IFTAInterval = ELDIFTAInterval;

            IFTAIntervalEntry.Text = ELDIFTAInterval.ToString();

            if (ELD.MaxRecords > 0 && !ELD.IsTotalMemoryAvailable)
            {
                ShowMessage("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
                return false;
            }

            FormatRemainingText();

            return true;
        }

        private Boolean EditStatsInterval()
        {
            Single ELDStatsInterval;
            if (!Single.TryParse("0" + StatsIntervalEntry.Text, out ELDStatsInterval))
            {
                ShowMessage("Stats Interval must be numeric.");
                return false;
            }

            if (ELDStatsInterval == 0)
                ELDStatsInterval = ELD.DefaultStatsInterval;

            if (AlignStatsSwitch.IsToggled && !ELD.IsHourAligned(ELDStatsInterval))
            {
                ShowMessage("Stats Interval cannot be aligned to the hour.");
                AlignStatsSwitch.IsToggled = false;
                return false;
            }

            ELD.StatsInterval = ELDStatsInterval;

            StatsIntervalEntry.Text = ELDStatsInterval.ToString();

            if (ELD.MaxRecords > 0 && !ELD.IsTotalMemoryAvailable)
            {
                ShowMessage("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
                return false;
            }

            FormatRemainingText();

            return true;
        }

        #endregion

        #region Upload ELD Data

        private void StartELDUpload()
        {
            ELD.UploadFrom = 1;
            ELD.UploadTo = ELD.CurrentRecordNo;

            ELD.UploadRecordNo = 0;

            CurrentRecordNo = 0;
            UploadFrom = ELD.UploadFrom;

            // Reset ELD VIN prior to retrieving the first row.
            // Note, this is for showing VIN only with and after the VIN record.
            ELD.VIN = "";

            ELD.GetFirstRecord();

            IsUploading = true;
        }

        private void UploadELDRecord()
        {
            WriteELDRecord();

            // Check for end of upload
            if (ELD.RecordNo == ELD.UploadTo)
            {
                IsUploading = false;

                ELD.UploadFrom = UploadFrom;

                ELD.UploadRecordNo = CurrentRecordNo;

                RefreshELDPage(CurrentRecordNo);

                ShowMessage("The ELD upload has finished.");
            }
            else
                ELD.GetNextRecord();
        }

        #endregion

        #region Save ELD Data

        private void SaveELDData()
        {
            // Save the locallly retrieved ELD records

            ShowMessage("ELD Records have been Saved.");
        }

        #endregion

        #region Write ELD Record

        private Boolean WriteELDRecord()
        {
            String ELDRecord = "";
            const String DateFormat = "M/d/yyyy H:mm:ss";

            String RecordIdText;
            ELD.RecordIds _RecordId = (ELD.RecordIds)ELD.RecordId;

            if (_RecordId == ELD.RecordIds.Waiting)
                return true;

            if (_RecordId >= ELD.RecordIds.Custom)
                RecordIdText = ((CustomRecordIds)(_RecordId - ELD.RecordIds.Custom)).ToString();
            else
                RecordIdText = _RecordId.ToString();

            String DriverVIN = "";
            if (_RecordId < ELD.RecordIds.Custom)
                if (_RecordId == ELD.RecordIds.DriverId)
                    DriverVIN = ELD.DriverId;
                else
                    DriverVIN = ELD.VIN;

            ELDRecord += ELD.RecordNo + "," +
                         ELD.RecordId + "," +
                         RecordIdText + "," +
                         DriverVIN + "," +
                         Helper.FormatDate(ELD.Time.Ticks, DateFormat, true) + "," +
                         Helper.FormatLatLong(ELD.Latitude, LatLongShowNA) + "," +
                         Helper.FormatLatLong(ELD.Longitude, LatLongShowNA) + "," +
                         Helper.FormatDistance(ELD.Distance, DistanceShowNA, false, true) + "," +
                         Helper.FormatDistance(ELD.Odometer, OdometerShowNA, false, true) + "," +
                         Helper.FormatEngineHours(ELD.TotalHours, TotalHoursShowNA, true) + "," +
                         Helper.FormatEngineHours(ELD.IdleHours, IdleHoursShowNA, true) + "," +
                         Helper.FormatFuelUsed(ELD.TotalFuel, TotalFuelShowNA, false, true) + "," +
                         Helper.FormatFuelUsed(ELD.IdleFuel, IdleFuelShowNA, false, true);

            // Write log record
            Debug.WriteLine(ELDRecord);

            return true;
        }

        #endregion

        #region Delete ELD Data

        private void DeleteELDData()
        {
            // Check for deleting local records first
            if (ELD.LocalRecordNo > 0)
            {
                ELD.RecordNo = 0;
                ELD.LocalRecordNo = 0;
            }

            //  Check adapter records
            else if (ELD.CurrentRecordNo > 0)
            {
                if (ELD.ResetRecords)
                    ELD.Reset();
                else
                    ELD.Delete(ELD.UploadTo);

                IsUploading = false;
            }

            CurrentRecordNo = 0;
            ELD.UploadRecordNo = 0; // must be cleared for both local and adapter recording

            ClearELDPage();

            ShowMessage("The ELD records have been deleted.");
        }

        #endregion

        #region ELD Buttons (Start, Update, Delete)

        private void StartELDButton_Clicked(object sender, System.EventArgs e)
        {
            StartELDButton.IsEnabled = false;

            StartStop();

            SetELDButtons();
        }

        private void UpdateELDButton_Clicked(object sender, System.EventArgs e)
        {
            UploadELDButton.IsEnabled = false;

            if (ELD.IsRecordingLocally)
                SaveELDData();
            else
                StartELDUpload();

            SetELDButtons();
        }

        private void DeleteELDButton_Clicked(object sender, System.EventArgs e)
        {
            DeleteELDButton.IsEnabled = false;

            DeleteELDData();

            SetELDButtons();
        }

        #endregion

        #region OnELDPage Disappearing

        private void OnELDPageDisappearing()
        {
            // Pause uploading
            if (IsUploading)
            {
                IsUploading = false;
                ShowMessage("ELD Uploading has been cancelled.");
            }

            // Stop local recording
            else if (ELD.IsRecordingLocally) // recording locally
            {
                if (ELD.LocalRecordNo > 0)
                {
                    ELD.Stop();

                    ShowMessage("Local ELD Recording has been cancelled.");

                    // Restart ELD for disconnected recording
                    if (ELD.IsRecordingDisconnected)
                    {
                        if (!ELD.IsStarted)
                            ELD.Start();
                    }

                }
            }

            // Stop streaming
            if (ELD.IsStreaming)
                ELD.StopStreaming();
        }

        #endregion

    #endregion

    #region Connect Button

        private async void ConnectButton_Clicked(object sender, EventArgs e)
        {
            ConnectButton.IsEnabled = false;

            SecureAdapterSwitch.IsEnabled = false;

            UpdateButton.IsEnabled = false;
            SendButton.IsEnabled = false;

            // Check for connecting
            if (IsConnectButton)
            {
                IsConnecting = true;
                IsConnected = false;

                ShowStatus("Connecting...");

                ClearMessage();

                ShowDisconnectButton();

                BlueFire.UseBLE = BLESwitch.IsToggled;
                BlueFire.UseBT2 = BT2Switch.IsToggled;

                BlueFire.IgnoreJ1939 = !J1939Switch.IsToggled; // is opposite
                BlueFire.IgnoreJ1708 = !J1708Switch.IsToggled; // is opposite

                Boolean Synchronized = true; // test synchronized connection
                //BlueFire.ConnectTimeout = 2000; // default is 1000 (one second)

                await BlueFire.Connect(Synchronized);
            }
            else // Disconnecting
            {
                await DisconnectAdapter(true);
            }
        }

        private void ShowConnectButton()
        {
            IsConnectButton = true;
            ConnectButton.Text = "Connect";
            ConnectButton.IsEnabled = true;
        }

        private void ShowDisconnectButton()
        {
            IsConnectButton = false;
            ConnectButton.Text = "Disconnect";
            ConnectButton.IsEnabled = true;
        }

        private async Task DisconnectAdapter(Boolean WaitForDisconnect)
        {
            switch (ConnectionState)
            {
                // Check for cancelling a connection attempt
                case ConnectionStates.Discovering:
                case ConnectionStates.Connecting:
                case ConnectionStates.Reconnecting:
                case ConnectionStates.Ready:
                    await BlueFire.CancelConnecting();
                    break;

                // Check for already connected
                case ConnectionStates.Connected:
                case ConnectionStates.Reconnected:
                case ConnectionStates.Authenticating:
                case ConnectionStates.Authenticated:
                case ConnectionStates.RetrievingData:
                case ConnectionStates.DataAvailable:
                    await BlueFire.Disconnect(WaitForDisconnect);
                    break;

                // already disconnecting or not connected
                default:
                    AdapterNotConnected();
                    break;
            }
        }

    #endregion

    #region Update Button

        // Adapter settings
        private void UpdateButton_Clicked(object sender, EventArgs e)
        {
            // LED Brightness
            Byte LedBrightness;

            ClearMessage();

            if (!Byte.TryParse(LedBrightnessEntry.Text, out LedBrightness) || LedBrightness < 5 || LedBrightness > 100)
            {
                ShowMessage("Invalid LED Brightness");
                return;
            }
            BlueFire.LedBrightness = LedBrightness;

            // Security
            Boolean SecureAdapter = SecureAdapterSwitch.IsToggled;

            String UserName = UserNameEntry.Text.Trim();
            String Password = PasswordEntry.Text.Trim();

            if (UserName.Length > 20)
            {
                ShowMessage("Invalid User Name");
                return;
            }
            if (Password.Length > 12)
            {
                ShowMessage("Invalid Password");
                return;
            }

            BlueFire.UpdateSecurity(SecureAdapter, UserName, Password);
        }

        #endregion

    #region Send Button

        // Proprietary PGN Monitoring/Sending
        private void SendButton_Clicked(object sender, EventArgs e)
        {
            IsSendingPGN = false;
            IsMonitoringPGN = false;

            // Get PGN
            if (!Int32.TryParse("0" + PGNEntry.Text.Trim(), out PGN))
            {
                ShowMessage("PGN must be numeric.");
                return;
            }

            // Ignore if no PGN
            if (PGN == 0)
                return;

            // Get PGN Data
            Byte[] PGNBytes = new Byte[8];

            String PGNData = PGNDataEntry.Text.Trim();

            if (PGNData.Length == 0) // Monitor a PGN
            {
                Int32 Source = 0; // engine
                IsMonitoringPGN = true;
                BlueFire.MonitorPGN(Source, PGN);
            }
            else // Send a PGN
            {
                // Edit the PGN Data to be 16 hex characters (8 bytes)
                if (PGNData.Length != 16)
                {
                    ShowMessage("PGN Data must be 16 hex characters (8 bytes).");
                    return;
                }

                // Convert the PGN Data hex string to bytes
                try
                {
                    for (int i = 0; i < 8; i++)
                        PGNBytes[i] = Byte.Parse(PGNData.Substring(i * 2, 2), NumberStyles.HexNumber);
                }
                catch
                {
                    ShowMessage("PGN must be numeric.");
                    return;
                }

                // Send the PGN
                IsSendingPGN = true;
                BlueFire.SendPGN(PGN, PGNBytes);
            }
        }

    #endregion

    #region Next Fault Button

        private void NextFaultButton_Clicked(object sender, EventArgs e)
        {
            FaultIndex++;
            if (FaultIndex == BlueFire.Truck.ActiveFaultsCount)
                FaultIndex = 0;
        }

    #endregion

    #region Reset Fault Button

        private async void ResetFaultButton_Clicked(object sender, EventArgs e)
        {
            await BlueFire.ResetFaults();
        }

    #endregion

    #region Truck Button

        // Truck Data
        private async void TruckButton_Clicked(object sender, EventArgs e)
        {
            // Clear message
            ClearMessage();

            // Clear any previous adapter data retrieval
            await BlueFire.ClearData();

            HideELDPage();

            AdapterLayout.IsVisible = false;
            TruckLayout.IsVisible = true;

            NextButton.IsVisible = true;
            PrevButton.IsVisible = true;

            GroupNo = 0; // so it increments to 0

            await GetTruckData();
        }

        #endregion

    #region Next/Prev Buttons

        // Next Truck Data
        private async void NextButton_Clicked(object sender, EventArgs e)
        {
            // Clear message
            ClearMessage();

            // Clear any previous adapter data retrieval
            await BlueFire.ClearData();

            // Show next page
            await ShowTruckPages();
        }

        // Previous Truck Data
        private async void PrevButton_Clicked(object sender, EventArgs e)
        {
            // Clear message
            ClearMessage();

            // Clear any previous adapter data retrieval
            await BlueFire.ClearData();

            // Show previous pge
            await ShowTruckPages(true);
        }

    #region Show Truck Pages

        private async Task ShowTruckPages(Boolean ShowPrevious = false)
        {
            if (ShowPrevious)
                GroupNo--;
            else
                GroupNo++;

            if (GroupNo > MaxGroupNo)
                GroupNo = 0;

            else if (GroupNo < 0)
                GroupNo = MaxGroupNo;

            await GetTruckData();
        }

    #endregion

    #endregion

    #region ELD Button

        // ELD Recording
        private async void ELDButton_Clicked(object sender, EventArgs e)
        {
            // Clear message
            ClearMessage();

            // Clear any previous adapter data retrieval
            await BlueFire.ClearData();

            AdapterLayout.IsVisible = false;
            TruckLayout.IsVisible = false;

            NextButton.IsVisible = false;
            PrevButton.IsVisible = false;

            ShowELDPage();
        }

    #endregion

    #region Show ELD Page

        private void ShowELDPage()
        {
            AdapterLayout.IsVisible = false;
            TruckLayout.IsVisible = false;

            ELDLayout.IsVisible = true;

            ClearELDPage();

            CheckStreaming();

            if (!ELD.IsCompatibleAdapter)
            {
                ShowMessage("ELD is not available with your current adapter.");
                return;
            }

            // Get current record
            CurrentRecordNo = -1;

            ELD.GetCurrentRecord();

            SetELDButtons();
        }

        private void HideELDPage()
        {
            if (!ELDLayout.IsVisible)
                return;

            EditELDData();

            ELDLayout.IsVisible = false;

            OnELDPageDisappearing();
        }

    #endregion

    #region Log Error

        private void LogError()
        {
            ErrorMessage = BlueFire.ErrorMessage;
            ErrorException = BlueFire.ErrorException;

            if (ErrorException != null)
                ErrorMessage += @"/r/n" + @"/r/n" + ErrorException.Message;

            if (ErrorMessage == "")
                return;

            WriteLog(ErrorMessage);
        }

    #endregion

    #region Write Log

        private void WriteLog(String Message)
        {
            // Write the message to a log file.
            // TODO - must implement this in an actual app.

#if (WINDOWS_UWP)
            Debug.WriteLine(Message);
#else
            Debug.Print(Message);
#endif
        }

    #endregion

    #region End Application

        private void EndApplication()
        {
            // Must be like this for a quick exit
            Task.Factory.StartNew(() => Xamarin.Forms.Device.BeginInvokeOnMainThread(async () => await EndApplicationUI()));
        }

        public async Task EndApplicationUI()
        {
            // Set switch settings
            BlueFire.UseBLE = BLESwitch.IsToggled;
            BlueFire.UseBT2 = BT2Switch.IsToggled;

            BlueFire.IgnoreJ1939 = !J1939Switch.IsToggled;
            BlueFire.IgnoreJ1708 = !J1708Switch.IsToggled;

            await BlueFire.EndApplication(); 
        }

    #endregion

    }
}
