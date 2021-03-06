﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

using Xamarin.Forms;

using Plugin.Permissions.Abstractions;

using BlueFire;

namespace Demo
{
	public partial class MainPage : ContentPage
	{

    #region Declaratives

        private Service DemoService;

        private Int32 GroupNo;
        private const Int32 MaxGroupNo = 8;

        private BFPIDs[] Pids; // for sending J1939/J1587 Pids to the adapter

        private Int32 RetrievalInterval;
        private RequestTypes RequestType; 

        private API BlueFire;

        private Boolean IsConnected;
        private Boolean IsConnecting;
        private Boolean IsConnectButton;

        private Int32 Heartbeat;

        private Int32 PGN;
        private Boolean IsSendingPGN;
        private Boolean IsMonitoringPGN;

        private Byte LedBrightness = 100;
        private Boolean ConnectLastAdapter = false;

        private Boolean IgnoreJ1939 = false;
        private Boolean IgnoreJ1708 = false;
        private Boolean IgnoreOBD2 = false;

        private String ErrorMessage = "";
        private Exception ErrorException = null;

        private ConnectionStates ConnectionState = ConnectionStates.NotConnected;

        private event API.EventHandler APIHandlerEvent;

        private event API.AppEventHandler AppHandlerEvent;

        private Boolean IsServiceRunning;
        private Boolean IsStressTesting;
        private Boolean IsRetrievingVINInfo;

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

            APIHandlerEvent += new API.EventHandler(APIEventHandler);
            AppHandlerEvent += new API.AppEventHandler(AppEventHandler);

            BlueFire = new API(APIHandlerEvent, AppHandlerEvent);

            Title.Text = "API Demo v-" + BlueFire.GetAPIVersion();

            InitializeApp();
        }

    #endregion

    #region Initialize Controls

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
            StartServiceButton.IsEnabled = true;
            StopServiceButton.IsEnabled = false;
        }

    #endregion

    #region Initialize App

        private async void InitializeApp()
        {
            // Connect button
            IsConnectButton = true;

            // Keyboards
            LedBrightnessEntry.Keyboard = Keyboard.Numeric;
            PGNEntry.Keyboard = Keyboard.Numeric;

            ClearMessages();

            ShowStatus(ConnectionStates.NotConnected.ToString());

            // Set to kill app on exiting
            BlueFire.KillAppService = true;

            // Check permissions.
            // Note, this must be before BlueFire.Initialize.
            if (!await CheckPermissions())
            {
                EndApplication(); // you may or may not want to do this
                return;
            }

            // Initialize BlueFire API
            await BlueFire.Initialize();

            // Clear adapter id filter
            BlueFire.DeviceAddressFilter.Clear();

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

            ConnectLastAdapterSwitch.IsEnabled = true;

            // Security settings
            SecureDeviceSwitch.IsEnabled = true;
            SecureAdapterSwitch.IsEnabled = true;

            UserNameEntry.Text = "";
            PasswordEntry.Text = "";

            UpdateButton.IsEnabled = true;

            // Proprietary PGNs
            PGNEntry.Text = "";
            PGNDataEntry.Text = "";

            NextButton.IsVisible = false;
            PrevButton.IsVisible = false;
            SendButton.IsEnabled = false;

            // ELD settings
            DriverIdEntry.Text = ELD.DriverId;

            ELDIntervalEntry.Text = ELD.ELDInterval.ToString();
            IFTAIntervalEntry.Text = ELD.IFTAInterval.ToString();
            StatsIntervalEntry.Text = ELD.StatsInterval.ToString();

            IFTAIntervalLayout.IsVisible = false;
            StatsIntervalLayout.IsVisible = false;

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
        }

    #endregion

    #region Check Permissions

        private async Task<Boolean> CheckPermissions()
        {
            // Check storage permission
            if (!await BlueFire.CheckPermission(Permission.Storage, false))
            {
                // Note, an alert to the user explaining why this is needed should be issued here.
                //if (!await ShowAlert("Storage Permission", "My app needs the following permissions to save and access settings and files.", "Cancel"))
                //    return false;

                if (!await BlueFire.CheckPermission(Permission.Storage))
                {
                    ShowMessages("You must allow Storage permissions to save and access settings and files.");
                    return false;
                }
            }

#if (__ANDROID__) // for BLE
            // Check location permission (BLE (just Android) and GPS (if your App is using GPS)).
            if (!await BlueFire.CheckPermission(Permission.Location, false))
            {
                // Note, an alert to the user explaining why this is needed should be issued here.
                //if (!await ShowAlert("Location Permission", "My app needs the following permissions for the Bluetooth connection.", "Cancel"))
                //    return false;

                if (!await BlueFire.CheckPermission(Permission.Location))
                {
                    ShowMessages("You must allow Location access for the Bluetooth connection.");
                    return false;
                }
            }
#endif
            return true;
        }

    #endregion

    #region Page Events

        protected override void OnAppearing()
        {
            base.OnAppearing();
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

    #region API Event Handler

        private void APIEventHandler(ConnectionStates State)
        {
            // Run on the UI thread
            Xamarin.Forms.Device.BeginInvokeOnMainThread(async() => await APIHandlerUI(State));
        }

        private async Task APIHandlerUI(ConnectionStates State)
        {
            ConnectionState = State;

            switch (ConnectionState)
            {
                case ConnectionStates.Initializing:
                case ConnectionStates.Initialized:
                case ConnectionStates.Discovering:
                case ConnectionStates.Authenticating:
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

                case ConnectionStates.Authenticated:
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

                case ConnectionStates.CANStarting:
                    ShowMessages("The Adapter is connected to the " + BlueFire.CanBusToString() + ".");
                    await GetTruckData();
                    ShowData();
                    break;

                case ConnectionStates.J1708Restarting:
                    ShowMessages("J1708 is restarting.");
                    await GetTruckData();
                    ShowData();
                    break;

                case ConnectionStates.Heartbeat:
                    ShowHeartbeat();
                    break;

                case ConnectionStates.DataAvailable:
                    if (IsConnected)
                    {
                        ShowData();
                        ShowStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.CANFilterFull:
                    ShowMessages("The CAN Filter is Full. Some data will not be retrieved.");
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.ConnectTimeout:
                case ConnectionStates.AdapterTimeout:
                    if (IsConnecting || IsConnected)
                    {
                        AdapterNotConnected();
                        ShowStatus(State.ToString());
                        ShowMessages("The Adapter Timed Out.");
                    }
                    break;

                case ConnectionStates.Notification:
                    LogNotification();
                    break;

                case ConnectionStates.AdapterMessage:
                    LogAdapterMessage();
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
                    ShowMessages("You are not authorized to access this adapter. Check your adapter security settings.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.NotFound:
                    ShowMessages("A valid adapter was not found. Check your adapter connection settings.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.IncompatibleAPI:
                    ShowMessages("The API is not compatible with this App.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.IncompatibleAdapter:
                    ShowMessages("The Adapter is not compatible with this API.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case ConnectionStates.IncompatibleSecurity:
                    ShowMessages("App Security is not compatible with this API.");
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

    #endregion

    #region Show Heartbeat

        private void ShowHeartbeat()
        {
            Heartbeat++;

            HeartbeatText.Text = Heartbeat.ToString();
        }

    #endregion

    #region Show Messages

        private void ShowMessages(String Message)
        {
            if (MessageText.Text == "")
                MessageText.Text = Message;
            else
                MessageText.Text += Const.CrLf + Message;

            MessageText.IsVisible = true;
        }

        private void ClearMessages()
        {
            MessageText.Text = "";
            MessageText.IsVisible = false;
        }

    #endregion

    #region Show Key State

        private void ShowKeyState()
        {
            if (BlueFire.Truck.IsKeyOn)
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

            ClearMessages();

            ConnectLastAdapterSwitch.IsEnabled = false;
            SecureDeviceSwitch.IsEnabled = false;
            SecureAdapterSwitch.IsEnabled = false;
            UpdateButton.IsEnabled = false;

            SendButton.IsEnabled = true;

            ConnectButton.Focus();

            // Check for API setting the adapter type
            BT2Switch.IsToggled = BlueFire.UseBT2;
            BLESwitch.IsToggled = BlueFire.UseBLE;

            HardwareText.Text = BlueFire.HardwareVersion;
            FirmwareText.Text = BlueFire.FirmwareVersion;

            // Test adapter id filter.
            // Note, this will allow the intial connection to a single adapter
            // but then not any more connection attempts until the app is restarted
            // because the filter is cleared when the app is started.
            //BlueFire.AdapterIdFilter.Add(BlueFire.AdapterId);

            // Start retrieving truck data if key is on when app is connecting
            if (BlueFire.Truck.IsKeyOn)
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

            ConnectLastAdapterSwitch.IsEnabled = true;
            SecureDeviceSwitch.IsEnabled = true;
            SecureAdapterSwitch.IsEnabled = true;
            UpdateButton.IsEnabled = true;

            SendButton.IsEnabled = false;

            ConnectButton.Focus();
        }

        private void AdapterReconnecting()
        {
            WriteLog("Adapter re-connecting.");

            IsConnected = false;
            IsConnecting = true;

            ConnectLastAdapterSwitch.IsEnabled = false;
            SecureDeviceSwitch.IsEnabled = false;
            SecureAdapterSwitch.IsEnabled = false;
            UpdateButton.IsEnabled = false;

            SendButton.IsEnabled = false;

            WriteLog("App reconnecting to the Adapter. Reason is " + BlueFire.ReconnectReason + ".");

            ShowMessages("Lost connection to the Adapter, reconnecting.");
        }

        private void AdapterReconnected()
        {
            WriteLog("Adapter re-connected.");

            ShowMessages("Adapter reconnected.");
        }

        private void AdapterNotReconnected()
        {
            WriteLog("Adapter not re-connected.");

            AdapterNotConnected();

            ShowMessages("The Adapter did not reconnect.");
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

            if (IgnoreOBD2 != BlueFire.IgnoreOBD2)
            {
                IgnoreOBD2 = BlueFire.IgnoreOBD2;
                OBD2Switch.IsToggled = !IgnoreOBD2; // switch is opposite
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

            if (ConnectLastAdapter != BlueFire.ConnectLastAdapter)
                ConnectLastAdapterSwitch.IsToggled = BlueFire.ConnectLastAdapter;

            // Check for SendPGN response
            if ((IsSendingPGN || IsMonitoringPGN) && BlueFire.PGN == PGN)
            {
                IsSendingPGN = false; // only show sending data once
                PGNDataEntry.Text = BitConverter.ToString(BlueFire.PGNData);
            }
        }

    #endregion

    #region Connect Button

        private async void ConnectButton_Clicked(object sender, EventArgs e)
        {
            ConnectButton.IsEnabled = false;

            StartServiceButton.IsEnabled = false;
            StopServiceButton.IsEnabled = false;

            ConnectLastAdapterSwitch.IsEnabled = false;
            SecureDeviceSwitch.IsEnabled = false;
            SecureAdapterSwitch.IsEnabled = false;
            UpdateButton.IsEnabled = false;

            SendButton.IsEnabled = false;

            // Check for connecting
            if (IsConnectButton)
            {
                IsConnecting = true;
                IsConnected = false;

                Heartbeat = 0;
                ShowHeartbeat();

                ShowStatus("Connecting...");

                ClearMessages();

                ShowDisconnectButton();

                BlueFire.UseBLE = BLESwitch.IsToggled;
                BlueFire.UseBT2 = BT2Switch.IsToggled;

                BlueFire.IgnoreJ1939 = !J1939Switch.IsToggled; // is opposite
                BlueFire.IgnoreJ1708 = !J1708Switch.IsToggled; // is opposite
                BlueFire.IgnoreOBD2 = !OBD2Switch.IsToggled; // is opposite

                BlueFire.ConnectLastAdapter = ConnectLastAdapterSwitch.IsToggled;

                BlueFire.SendAllPackets = SendAllPacketsSwitch.IsToggled;

                BlueFire.IsNotificationsOn = false;
                BlueFire.IsPerformanceModeOn = false;

                BlueFire.OptimizeDataRetrieval = true; // recommended

                await BlueFire.Connect();
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
            StartServiceButton.IsEnabled = true;
            StopServiceButton.IsEnabled = true;
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
                    await BlueFire.CancelConnecting();
                    break;

                // Check for already connected
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

    #region Service Buttons

        private async void StartServiceButton_Clicked(object sender, EventArgs e)
        {
            if (DemoService == null)
                DemoService = new Service();

            StartServiceButton.IsEnabled = false;

            IsServiceRunning = true;

            await DemoService.StartService();

            StopServiceButton.IsEnabled = true;

            ELDButton.IsEnabled = false;
            StressTestButton.IsEnabled = false;

            ConnectButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            SendButton.IsEnabled = false;
        }

        private async void StopServiceButton_Clicked(object sender, EventArgs e)
        {
            if (DemoService == null)
                return;

            StopServiceButton.IsEnabled = false;

            await DemoService.StopService();

            DemoService = null;

            IsServiceRunning = false;

            // Re-initialze for app processing
            InitializeApp();

            StartServiceButton.IsEnabled = true;

            ELDButton.IsEnabled = true;
            StressTestButton.IsEnabled = true;

            ConnectButton.IsEnabled = true;
            UpdateButton.IsEnabled = true;
            SendButton.IsEnabled = true;
        }

    #endregion

    #region Update Button

        // Adapter settings
        private void UpdateButton_Clicked(object sender, EventArgs e)
        {
            ClearMessages();

            // Security
            Boolean SecureDevice = SecureDeviceSwitch.IsEnabled;
            Boolean SecureAdapter = SecureAdapterSwitch.IsToggled;

            String UserName = UserNameEntry.Text.Trim();
            String Password = PasswordEntry.Text.Trim();

            if (UserName.Length > 20)
            {
                ShowMessages("Invalid User Name");
                return;
            }
            if (Password.Length > 12)
            {
                ShowMessages("Invalid Password");
                return;
            }

            BlueFire.UpdateSecurity(SecureDevice, SecureAdapter, UserName, Password);
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
                ShowMessages("PGN must be numeric.");
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
                    ShowMessages("PGN Data must be 16 hex characters (8 bytes).");
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
                    ShowMessages("PGN must be numeric.");
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
            // Clear messages
            ClearMessages();

            // Clear any previous adapter data retrieval
            if (!IsServiceRunning && !IsStressTesting)
                await BlueFire.ClearData();

            await ShowTruckLayout();
        }

    #endregion

    #region Show Truck Layout

        private async Task<Boolean> ShowTruckLayout()
        {
            // Go back to the adapter page
            if (TruckLayout.IsVisible)
            {
                NextButton.IsVisible = false;
                PrevButton.IsVisible = false;

                ShowAdapterPage();
                return false;
            }

            // Show the truck layout page
            HideELDPage();

            AdapterLayout.IsVisible = false;
            TruckLayout.IsVisible = true;

            NextButton.IsVisible = true;
            PrevButton.IsVisible = true;

            GroupNo = -1; // so it increments to 0

            await ShowTruckPage();

            return true;
        }

    #endregion

    #region Show Truck Page

        private async Task ShowTruckPage(Boolean ShowPrevious = false)
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

    #region Next/Prev Buttons

        // Next Truck Data
        private async void NextButton_Clicked(object sender, EventArgs e)
        {
            // Clear message
            ClearMessages();

            // Show next page
            await ShowTruckPage();
        }

        // Previous Truck Data
        private async void PrevButton_Clicked(object sender, EventArgs e)
        {
            // Clear message
            ClearMessages();

            // Show previous pge
            await ShowTruckPage(true);
        }

        #endregion

    #region Get Truck Data

        private async Task GetTruckData()
        {
            if (!TruckLayout.IsVisible)
                return;

            DataView1.Text = "NA";
            DataView2.Text = "NA";
            DataView3.Text = "NA";
            DataView4.Text = "NA";
            DataView5.Text = "NA";
            DataView6.Text = "NA";
            DataView7.Text = "NA";

            FaultLayout.IsVisible = false;

            // Clear any previous adapter data
            if (!IsServiceRunning && !IsStressTesting)
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

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    if (!IsStressTesting)
                    {
                        Pids = new BFPIDs[7];
                        Pids[0] = BFPIDs.RPM;
                        Pids[1] = BFPIDs.Speed;
                        Pids[2] = BFPIDs.AccPedalPos;
                        Pids[3] = BFPIDs.PctLoad;
                        Pids[4] = BFPIDs.PctTorque;
                        Pids[5] = BFPIDs.DrvPctTorque;
                        Pids[6] = BFPIDs.TorqueMode;
                        await BlueFire.GetPIDs(Pids);
                    }

                    break;

                case 1:
                    TextView1.Text = "Distance";
                    TextView2.Text = "     HiRes";
                    TextView3.Text = "     LoRes";
                    TextView4.Text = "";
                    TextView5.Text = "Odometer";
                    TextView6.Text = "     HiRes";
                    TextView7.Text = "     LoRes";

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    if (!IsStressTesting)
                    {
                        Pids = new BFPIDs[3];
                        Pids[0] = BFPIDs.Odometer;
                        Pids[1] = BFPIDs.Distance;
                        Pids[2] = BFPIDs.HiResDistance;
                        await BlueFire.GetPIDs(Pids);
                    }

                    break;

                case 2:
                    TextView1.Text = "Total Hours";
                    TextView2.Text = "Idle Hours";
                    TextView3.Text = "Brake Pres";
                    TextView4.Text = "Brake Air";
                    TextView5.Text = "Current Gear";
                    TextView6.Text = "Selected Gear";
                    TextView7.Text = "Battery Volts";

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    if (!IsStressTesting)
                    {
                        Pids = new BFPIDs[6];
                        Pids[0] = BFPIDs.TotalHours;
                        Pids[1] = BFPIDs.IdleHours;
                        Pids[2] = BFPIDs.BrakeAppPressure;
                        Pids[3] = BFPIDs.BrakeAirPressure;
                        Pids[4] = BFPIDs.Transmission2;
                        Pids[5] = BFPIDs.BatteryVoltage;
                        await BlueFire.GetPIDs(Pids);
                    }

                    break;

                case 3:
                    TextView1.Text = "Fuel Rate";
                    TextView2.Text = "Fuel Used";
                    TextView3.Text = "HiRes Fuel";
                    TextView4.Text = "Idle Fuel Used";
                    TextView5.Text = "Avg Fuel Econ";
                    TextView6.Text = "Inst Fuel Econ";
                    TextView7.Text = "Throttle Pos";

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    if (!IsStressTesting)
                    {
                        Pids = new BFPIDs[7];
                        Pids[0] = BFPIDs.FuelRate;
                        Pids[1] = BFPIDs.FuelUsed;
                        Pids[2] = BFPIDs.HiResFuelUsed;
                        Pids[3] = BFPIDs.IdleFuelUsed;
                        Pids[4] = BFPIDs.AvgFuelEcon;
                        Pids[5] = BFPIDs.InstFuelEcon;
                        Pids[6] = BFPIDs.ThrottlePos;
                        await BlueFire.GetPIDs(Pids);
                    }

                    break;

                case 4:
                    TextView1.Text = "Oil Temp";
                    TextView2.Text = "Oil Pressure";
                    TextView3.Text = "Intake Temp";
                    TextView4.Text = "Intake Pres";
                    TextView5.Text = "Coolant Temp";
                    TextView6.Text = "Coolant Pres";
                    TextView7.Text = "Coolant Level";

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    if (!IsStressTesting)
                    {
                        Pids = new BFPIDs[7];
                        Pids[0] = BFPIDs.OilTemp;
                        Pids[1] = BFPIDs.OilPressure;
                        Pids[2] = BFPIDs.IntakeTemp;
                        Pids[3] = BFPIDs.IntakePressure;
                        Pids[4] = BFPIDs.CoolantTemp;
                        Pids[5] = BFPIDs.CoolantPressure;
                        Pids[6] = BFPIDs.CoolantLevel;
                        await BlueFire.GetPIDs(Pids);
                    }

                    break;

                case 5:
                    TextView1.Text = "Brake Switch";
                    TextView2.Text = "Clutch Switch";
                    TextView3.Text = "Park Switch";
                    TextView4.Text = "Cruise Switch";
                    TextView5.Text = "Cruise State";
                    TextView6.Text = "Cruise Speed";
                    TextView7.Text = "Max Speed";

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    // Restart stress test after retrieving VIN/Info data
                    if (IsStressTesting && IsRetrievingVINInfo)
                    {
                        IsRetrievingVINInfo = false;
                        await StartStressTest();
                    }
                    else
                    {
                        Pids = new BFPIDs[5];
                        Pids[0] = BFPIDs.BrakeSwitch;
                        Pids[1] = BFPIDs.ClutchSwitch;
                        Pids[2] = BFPIDs.ParkingBrake;
                        Pids[3] = BFPIDs.CruiseControl;
                        Pids[4] = BFPIDs.MaxSpeed;
                        await BlueFire.GetPIDs(Pids);
                    }

                    break;

                case 6: // special for ELD
                    TextView1.Text = "RPM";
                    TextView2.Text = "Speed";
                    TextView3.Text = "Distance";
                    TextView4.Text = "Odometer";
                    TextView5.Text = "Total Hours";
                    TextView6.Text = "";
                    TextView7.Text = "";

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    if (!IsStressTesting)
                    {
                        // Set RPM and Speed to always return data on a one second interval
                        await BlueFire.GetPID(BFPIDs.RPM, Const.OneSecond, RequestTypes.OnInterval);
                        await BlueFire.GetPID(BFPIDs.Speed, Const.OneSecond, RequestTypes.OnInterval);

                        // Set Distance and hours to return only on change at longer intervals.
                        // Note, odometer is the same as distance just from a different source (OEM ECM).
                        await BlueFire.GetPID(BFPIDs.Distance, 5 * Const.OneSecond, RequestTypes.OnChange);
                        await BlueFire.GetPID(BFPIDs.HiResDistance, 5 * Const.OneSecond, RequestTypes.OnChange);
                        await BlueFire.GetPID(BFPIDs.TotalHours, 10 * Const.OneSecond, RequestTypes.OnChange);
                    }

                    break;

                case 7:
                    TextView1.Text = "VIN";
                    TextView2.Text = "Make";
                    TextView3.Text = "Model";
                    TextView4.Text = "Serial No";
                    TextView5.Text = "Unit No";
                    TextView6.Text = "";
                    TextView7.Text = "";

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    IsRetrievingVINInfo = true;

                    // Stop stress test to retrieve VIN/Info data
                    if (IsStressTesting)
                        await BlueFire.ClearData();

                    if (!BlueFire.Truck.VINExists)
                        await BlueFire.Truck.GetVIN();

                    if (!BlueFire.Truck.Engine.IdExists)
                        await BlueFire.Truck.GetInfo(); // Make, Model, Serial No

                    if (!BlueFire.Truck.VINExists || !BlueFire.Truck.Engine.IdExists)
                        DataView7.Text = "Retrieving Data ...";

                    break;

                case 8:
                    TextView1.Text = "Source";
                    TextView2.Text = "SPN";
                    TextView3.Text = "FMI";
                    TextView4.Text = "Occurrence";
                    TextView5.Text = "Conversion";
                    TextView6.Text = "";
                    TextView7.Text = "";

                    if (IsServiceRunning)
                    {
                        ShowTruckData();
                        break;
                    }

                    // Restart stress test after retrieving VIN/Info data
                    if (IsStressTesting && IsRetrievingVINInfo)
                    {
                        IsRetrievingVINInfo = false;
                        await StartStressTest();
                    }
                    else
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
                    DataView2.Text = FormatSingle(BlueFire.Truck.HiResDistance, 0);
                    DataView3.Text = FormatSingle(BlueFire.Truck.LoResDistance, 0);
                    DataView4.Text = "";
                    DataView5.Text = FormatSingle(BlueFire.Truck.Odometer, 0);
                    DataView6.Text = FormatSingle(BlueFire.Truck.HiResOdometer, 0);
                    DataView7.Text = FormatSingle(BlueFire.Truck.LoResOdometer, 0);
                    break;

                case 2:
                    DataView1.Text = FormatSingle(BlueFire.Truck.TotalHours, 2);
                    DataView2.Text = FormatSingle(BlueFire.Truck.IdleHours, 2);
                    DataView3.Text = FormatSingle(BlueFire.Truck.BrakeApplicationPressure, 2);
                    DataView4.Text = FormatSingle(BlueFire.Truck.Brake1AirPressure, 2);
                    DataView5.Text = BlueFire.Truck.TransCurrentGear;
                    DataView6.Text = BlueFire.Truck.TransSelectedGear;
                    DataView7.Text = FormatSingle(BlueFire.Truck.BatteryPotential, 2);
                    break;

                case 3:
                    DataView1.Text = FormatSingle(BlueFire.Truck.FuelRate, 2);
                    DataView2.Text = FormatSingle(BlueFire.Truck.TotalFuelUsed, 2);
                    DataView3.Text = FormatSingle(BlueFire.Truck.HiResFuelUsed, 2);
                    DataView4.Text = FormatSingle(BlueFire.Truck.IdleFuelUsed, 2);
                    DataView5.Text = FormatSingle(BlueFire.Truck.AvgFuelEcon, 2);
                    DataView6.Text = FormatSingle(BlueFire.Truck.InstFuelEcon, 2);
                    DataView7.Text = FormatSingle(BlueFire.Truck.ThrottlePos, 2);
                    break;

                case 4:
                    DataView1.Text = FormatSingle(BlueFire.Truck.OilTemp, 2);
                    DataView2.Text = FormatSingle(BlueFire.Truck.OilPressure, 2);
                    DataView3.Text = FormatSingle(BlueFire.Truck.IntakeTemp, 2);
                    DataView4.Text = FormatSingle(BlueFire.Truck.IntakePressure, 2);
                    DataView5.Text = FormatSingle(BlueFire.Truck.CoolantTemp, 2);
                    DataView6.Text = FormatSingle(BlueFire.Truck.CoolantPressure, 2);
                    DataView7.Text = FormatSingle(BlueFire.Truck.CoolantLevel, 2);
                    break;

                case 5:
                    DataView1.Text = BlueFire.Truck.BrakeSwitch.ToString();
                    DataView2.Text = BlueFire.Truck.ClutchSwitch.ToString();
                    DataView3.Text = BlueFire.Truck.ParkBrakeSwitch.ToString();
                    DataView4.Text = BlueFire.Truck.CruiseSwitch.ToString();
                    DataView5.Text = BlueFire.Truck.CruiseState.ToString();
                    DataView6.Text = FormatSingle(BlueFire.Truck.CruiseSpeed, 0);
                    Single MaxSpeed = BlueFire.Truck.MaxSpeed;
                    if (BlueFire.Truck.HiResMaxSpeed > 0)
                        MaxSpeed = BlueFire.Truck.HiResMaxSpeed;
                    DataView7.Text = FormatSingle(MaxSpeed, 0);
                    break;

                case 6: // special for ELD
                    DataView1.Text = FormatInt32(BlueFire.Truck.RPM);
                    DataView2.Text = FormatSingle(BlueFire.Truck.Speed, 0);
                    DataView3.Text = FormatSingle(BlueFire.Truck.Distance, 0);
                    DataView4.Text = FormatSingle(BlueFire.Truck.Odometer, 0);
                    DataView5.Text = FormatSingle(BlueFire.Truck.TotalHours, 2);
                    DataView6.Text = "";
                    DataView7.Text = "";
                    break;

                case 7:
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

                case 8:

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

    #region Stress Test Button

        // Test System Load
        private async void StressTestButton_Clicked(object sender, EventArgs e)
        {
            // Request as much data from the adapter as possible in order to
            // stress test the connection.
            // Note, you will get an Adapter Filter Full message if you request too 
            // much data.

            // Clear any previous adapter data
            await BlueFire.ClearData();

            // Show truck layout or adapter page
            if (!await ShowTruckLayout())
            {
                IsStressTesting = false;
                return;
            }

            IsStressTesting = true;

            await StartStressTest();
        }

        private async Task StartStressTest()
        {
            // Clear any previous adapter data
            await BlueFire.ClearData();

            // Start monitoring for faults.
            // Note, this clears the CAN Filter so it must be before any other requests for data.
            await BlueFire.GetFaults();

            // Start monitoring all other truck data

            Pids = new BFPIDs[7];
            Pids[0] = BFPIDs.RPM;
            Pids[1] = BFPIDs.Speed;
            Pids[2] = BFPIDs.AccPedalPos;
            Pids[3] = BFPIDs.PctLoad;
            Pids[4] = BFPIDs.PctTorque;
            Pids[5] = BFPIDs.DrvPctTorque;
            Pids[6] = BFPIDs.TorqueMode;
            await BlueFire.GetPIDs(Pids);

            Pids = new BFPIDs[3];
            Pids[0] = BFPIDs.Odometer;
            Pids[1] = BFPIDs.Distance;
            Pids[2] = BFPIDs.HiResDistance;
            await BlueFire.GetPIDs(Pids);

            Pids = new BFPIDs[6];
            Pids[0] = BFPIDs.TotalHours;
            Pids[1] = BFPIDs.IdleHours;
            Pids[2] = BFPIDs.BrakeAppPressure;
            Pids[3] = BFPIDs.BrakeAirPressure;
            Pids[4] = BFPIDs.Transmission2;
            Pids[5] = BFPIDs.BatteryVoltage;
            await BlueFire.GetPIDs(Pids);

            Pids = new BFPIDs[7];
            Pids[0] = BFPIDs.FuelRate;
            Pids[1] = BFPIDs.FuelUsed;
            Pids[2] = BFPIDs.HiResFuelUsed;
            Pids[3] = BFPIDs.IdleFuelUsed;
            Pids[4] = BFPIDs.AvgFuelEcon;
            Pids[5] = BFPIDs.InstFuelEcon;
            Pids[6] = BFPIDs.ThrottlePos;
            await BlueFire.GetPIDs(Pids);

            Pids = new BFPIDs[7];
            Pids[0] = BFPIDs.OilTemp;
            Pids[1] = BFPIDs.OilPressure;
            Pids[2] = BFPIDs.IntakeTemp;
            Pids[3] = BFPIDs.IntakePressure;
            Pids[4] = BFPIDs.CoolantTemp;
            Pids[5] = BFPIDs.CoolantPressure;
            Pids[6] = BFPIDs.CoolantLevel;
            await BlueFire.GetPIDs(Pids);

            Pids = new BFPIDs[5];
            Pids[0] = BFPIDs.BrakeSwitch;
            Pids[1] = BFPIDs.ClutchSwitch;
            Pids[2] = BFPIDs.ParkingBrake;
            Pids[3] = BFPIDs.CruiseControl;
            Pids[4] = BFPIDs.MaxSpeed;
            await BlueFire.GetPIDs(Pids);
        }

        #endregion

    #region ELD Button

        // ELD Recording
        private async void ELDButton_Clicked(object sender, EventArgs e)
        {
            // Clear message
            ClearMessages();

            // Clear any previous adapter data retrieval
            await BlueFire.ClearData();

            if (ELDLayout.IsVisible)
            {
                ShowAdapterPage();
                return;
            }

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
                ShowMessages("ELD is not available with your current adapter.");
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

    #region ELD

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

            // Not connected
            if (!IsConnected)
            {
                EnableELDInput(true);

                StartELDButton.IsEnabled = false;
                DeleteELDButton.IsEnabled = false;
                UploadELDButton.IsEnabled = false;

                return;
            }

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
                UploadELDButton.IsEnabled = (ELD.CurrentRecordNo > 0);

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

                if (ELD.UploadFrom <= 1)
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
                    ShowMessages("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
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
                    ShowMessages("You have not set any ELD recording.");
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
                ShowMessages("Driver Id length is too long for ELD custom records.");
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
                ShowMessages("ELD Interval must be numeric.");
                return false;
            }

            if (ELDInterval == 0)
                ELDInterval = ELD.DefaultELDInterval;

            if (AlignELDSwitch.IsToggled && !ELD.IsHourAligned(ELDInterval))
            {
                ShowMessages("ELD Interval cannot be aligned to the hour.");
                AlignELDSwitch.IsToggled = false;
                return false;
            }

            ELD.ELDInterval = ELDInterval;

            ELDIntervalEntry.Text = ELDInterval.ToString();

            if (ELD.MaxRecords > 0 && !ELD.IsTotalMemoryAvailable)
            {
                ShowMessages("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
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
                ShowMessages("IFTA Interval must be numeric.");
                return false;
            }

            if (ELDIFTAInterval == 0)
                ELDIFTAInterval = ELD.DefaultIFTAInterval;

            if (AlignIFTASwitch.IsToggled && !ELD.IsHourAligned(ELDIFTAInterval))
            {
                ShowMessages("IFTA Interval cannot be aligned to the hour.");
                AlignIFTASwitch.IsToggled = false;
                return false;
            }

            ELD.IFTAInterval = ELDIFTAInterval;

            IFTAIntervalEntry.Text = ELDIFTAInterval.ToString();

            if (ELD.MaxRecords > 0 && !ELD.IsTotalMemoryAvailable)
            {
                ShowMessages("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
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
                ShowMessages("Stats Interval must be numeric.");
                return false;
            }

            if (ELDStatsInterval == 0)
                ELDStatsInterval = ELD.DefaultStatsInterval;

            if (AlignStatsSwitch.IsToggled && !ELD.IsHourAligned(ELDStatsInterval))
            {
                ShowMessages("Stats Interval cannot be aligned to the hour.");
                AlignStatsSwitch.IsToggled = false;
                return false;
            }

            ELD.StatsInterval = ELDStatsInterval;

            StatsIntervalEntry.Text = ELDStatsInterval.ToString();

            if (ELD.MaxRecords > 0 && !ELD.IsTotalMemoryAvailable)
            {
                ShowMessages("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
                return false;
            }

            FormatRemainingText();

            return true;
        }

        #endregion

        #region Upload ELD Data

        private void StartELDUpload()
        {
            ShowMessages("ELD upload has started.");

            ELD.StartUpload(); // required

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

                ELD.StopUpload(); // required

                ShowMessages("ELD upload has finished.");
            }
            else
                ELD.GetNextRecord();
        }

        #endregion

        #region Save ELD Data

        private void SaveELDData()
        {
            // Save the locallly retrieved ELD records

            ShowMessages("ELD Records have been Saved.");
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
                { 
                    if (ELD.UploadTo == 0)
                        ELD.Delete(ELD.CurrentRecordNo);
                    else
                        ELD.Delete(ELD.UploadTo);
                }

                IsUploading = false;
            }

            CurrentRecordNo = 0;
            ELD.UploadRecordNo = 0; // must be cleared for both local and adapter recording

            ClearELDPage();

            ShowMessages("The ELD records have been deleted.");
        }

        #endregion

        #region IFTA/Stats Switches

        private void IFTASwitch_Toggled(object sender, EventArgs e)
        {
            IFTAIntervalLayout.IsVisible = IFTASwitch.IsToggled;
        }

        private void StatsSwitch_Toggled(object sender, EventArgs e)
        {
            StatsIntervalLayout.IsVisible = StatsSwitch.IsToggled;
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
            if (!IsConnected)
                return;

            // Pause uploading
            if (IsUploading)
            {
                IsUploading = false;
                ShowMessages("ELD Uploading has been cancelled.");
            }

            // Stop local recording
            else if (ELD.IsRecordingLocally) // recording locally
            {
                if (ELD.LocalRecordNo > 0)
                {
                    ELD.Stop();

                    ShowMessages("Local ELD Recording has been stopped.");

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

    #region Switches

        // BLE Switch
        private void BLESwitch_Toggled(object sender, EventArgs e)
        {
            // Update API
            BlueFire.UseBLE = BLESwitch.IsToggled;
        }

        // BT2 Switch
        private void BT2Switch_Toggled(object sender, EventArgs e)
        {
            // Update API
            BlueFire.UseBT2 = BT2Switch.IsToggled;
        }

        // J1939 Switch
        private void J1939Switch_Toggled(object sender, EventArgs e)
        {
            if (IsTogglingOBD2)
                return;

            J1708Switch.IsToggled = !J1939Switch.IsToggled;

            OBD2Switch.IsToggled = false;

            // Update API
            BlueFire.IgnoreJ1939 = !J1939Switch.IsToggled; // opposite
            BlueFire.IgnoreJ1708 = !J1708Switch.IsToggled; // opposite
            BlueFire.IgnoreOBD2 = !OBD2Switch.IsToggled; // opposite
        }

        // J1708 Switch
        private void J1708Switch_Toggled(object sender, EventArgs e)
        {
            if (IsTogglingOBD2)
                return;

            J1939Switch.IsToggled = !J1708Switch.IsToggled;

            OBD2Switch.IsToggled = false;

            // Update API
            BlueFire.IgnoreJ1939 = !J1939Switch.IsToggled; // opposite
            BlueFire.IgnoreJ1708 = !J1708Switch.IsToggled; // opposite
            BlueFire.IgnoreOBD2 = !OBD2Switch.IsToggled; // opposite
        }

        private Boolean IsTogglingOBD2;

        // OBD2 Switch
        private void OBD2Switch_Toggled(object sender, EventArgs e)
        {
            IsTogglingOBD2 = true;

            if (OBD2Switch.IsToggled)
            {
                J1939Switch.IsToggled = false;
                J1708Switch.IsToggled = false;
            }
            else
            {
                J1939Switch.IsToggled = true;
                J1708Switch.IsToggled = false;
            }

            IsTogglingOBD2 = false;

            // Update API
            BlueFire.IgnoreJ1939 = !J1939Switch.IsToggled; // opposite
            BlueFire.IgnoreJ1708 = !J1708Switch.IsToggled; // opposite
            BlueFire.IgnoreOBD2 = !OBD2Switch.IsToggled; // opposite
        }

    #endregion

    #region LED Brightness

        private void LedBrightness_Completed(object sender, EventArgs e)
        {
            // LED Brightness
            Byte LedBrightness;

            ClearMessages();

            if (!Byte.TryParse(LedBrightnessEntry.Text, out LedBrightness) || LedBrightness < 5 || LedBrightness > 100)
            {
                ShowMessages("Invalid LED Brightness");
                return;
            }

            // Update API
            BlueFire.LedBrightness = LedBrightness;
        }

    #endregion

    #region Log Notification

        private void LogNotification()
        {
            String NotificationMessage = BlueFire.NotificationMessage;
            ErrorException = BlueFire.ErrorException;

            if (NotificationMessage != "")
            {
                if (BlueFire.NotificationLocation != "")
                    NotificationMessage = BlueFire.NotificationLocation + " - " + NotificationMessage;

                BlueFire.ClearNotificationMessage();

                WriteLog(NotificationMessage);
            }
        }

    #endregion

    #region Log Adapter Message

        private void LogAdapterMessage()
        {
            String AdapterMessage = BlueFire.AdapterMessage;

            BlueFire.ClearAdapterMessage();

            WriteLog(AdapterMessage);
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

            ShowMessages(Message);

#if (DEBUG)
    #if (WINDOWS_UWP)
            Debug.WriteLine(Message);
    #else
            Debug.Print(Message);
    #endif
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
            try
            {
                if (DemoService != null)
                    await DemoService.Dispose();

                // Set switch settings
                BlueFire.UseBLE = BLESwitch.IsToggled;
                BlueFire.UseBT2 = BT2Switch.IsToggled;

                BlueFire.IgnoreJ1939 = !J1939Switch.IsToggled;
                BlueFire.IgnoreJ1708 = !J1708Switch.IsToggled;
            }
            catch { }

            await BlueFire.EndAppService(); 
        }

    #endregion

    }
}
