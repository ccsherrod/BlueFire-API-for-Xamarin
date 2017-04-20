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
        private const Int32 MaxGroupNo = 6;

        private API BlueFire;

        private Boolean IsConnected;
        private Boolean IsConnecting;
        private Boolean IsConnectButton;

        private Boolean IsKeyOn;

        private Int32 PGN;
        private Boolean IsSendingPGN;
        private Boolean IsMonitoringPGN;

        private Byte LedBrightness = 100;
        private Boolean IgnoreJ1939 = false;
        private Boolean IgnoreJ1708 = false;

        private String ErrorMessage = "";
        private Exception ErrorException = null;

        private API.ConnectionStates ConnectionState = API.ConnectionStates.NotConnected;

        private event API.EventHandler APIDataHandlerEvent;

        private event API.AppEventHandler APIAppHandlerEvent;

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
#elif (WINDOWS_UWP)
            LedBrightnessLayout.Padding = new Thickness(0, -4, 0, 0);
            LedBrightnessLabel.WidthRequest = 160;
            LedBrightnessText.WidthRequest = 54;

            UserNameLayout.Padding = new Thickness(0, -4, 0, 0);
            UserNameLabel.WidthRequest = 130;
            UserNameText.WidthRequest = 300;

            PasswordLayout.Padding = new Thickness(0, -4, 0, 0);
            PasswordLabel.WidthRequest = 130;
            PasswordText.WidthRequest = 300;

            PGNLayout.Padding = new Thickness(0, -4, 0, 0);
            PGNLabel.WidthRequest = 54;
            PGNText.WidthRequest = 100;

            PGNDataLayout.Padding = new Thickness(0, -4, 0, 0);
            PGNDataLabel.WidthRequest = 54;
            PGNDataText.WidthRequest = 300;
#endif
        }

        private async void Initialize()
        {
            // Keyboards
            LedBrightnessText.Keyboard = Keyboard.Numeric;
            PGNText.Keyboard = Keyboard.Numeric;

            // Clear messages and status
            ClearEditMessages();

            ShowStatus(API.ConnectionStates.NotConnected.ToString());

            // Initialize BlueFire API
            await BlueFire.Initialize();

            // Set BlueFire settings
            BT2Switch.IsToggled = BlueFire.UseBT2;
            BLESwitch.IsToggled = BlueFire.UseBLE;

            J1939Switch.IsToggled = !BlueFire.IgnoreJ1939;
            J1708Switch.IsToggled = !BlueFire.IgnoreJ1708;

            // Security settings
            UserNameText.Text = "";
            PasswordText.Text = "";
            UpdateButton.IsEnabled = false;

            // Proprietary PGNs
            PGNText.Text = "";
            PGNDataText.Text = "";
            SendButton.IsEnabled = false;

            // Show initial key state (key off)
            ShowKeyState();

            // Show Data
            GroupNo = 0;
            NextButton.IsEnabled = false;

            await ShowTruckText();
            ShowData();

            // Show connect button
            ShowConnectButton();
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

        internal async void AppEventHandler(API.AppEventIds EventId)
        {
            switch (EventId)
            {
                // Check for app becoming inactive (iOS).
                // Note, when Bluetooth is connecting, the app will be set inactive.
                case API.AppEventIds.IsInactive:
                    break;

                // Check for app going to the background
                case API.AppEventIds.IsBackground:

                    OnDisappearing();  // this will invoke Page Event OnDisappearing

                    // Adjust the data connection when in the background.
                    // Note, this will remove all app data retrieval.
                    if (!await BlueFire.SendToBackground())
                        return; // app is ending

                    // Re-retrieve data if on external power
                    if (BlueFire.IsDevicePowered)
                        await GetData();

                    // Not on external power, disconnect the adapter
                    else if (IsConnected)
                        await BlueFire.Disconnect();

                    break;

                // Check for app is coming back to the foreground
                case API.AppEventIds.IsForeground:

                    // Restore the app to the foreground.
                    // Note, this will remove all app data retrieval.
                    if (!await BlueFire.RestoreForeground())
                        return; // app is ending

                    // Re-retrieve app data
                    await GetData();

                    OnAppearing(); // this will invoke Page Event OnAppearing

                    break;

                // Check for app being terminated.
                // Note, iOS will terminate the app during the execution of any code here so
                // there is no sense in putting any code here.
                case API.AppEventIds.IsTerminating:

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

        private void DataEventHandler(API.ConnectionStates State)
        {
            // Run on the UI thread
            Xamarin.Forms.Device.BeginInvokeOnMainThread(async() => await DataDataHandlerUI(State));
        }

        private async Task DataDataHandlerUI(API.ConnectionStates State)
        {
            ConnectionState = State;

            switch (ConnectionState)
            {
                case API.ConnectionStates.Initializing:
                case API.ConnectionStates.Initialized:
                case API.ConnectionStates.Discovering:
                case API.ConnectionStates.Connected:
                case API.ConnectionStates.Authenticating:
                case API.ConnectionStates.Authenticated:
                case API.ConnectionStates.RetrievingData:
                case API.ConnectionStates.Disconnecting:
                    ShowStatus(State.ToString());
                    break;

                case API.ConnectionStates.NotConnected:
                    if (IsConnecting || IsConnected)
                    {
                        AdapterNotConnected();
                        ShowStatus(State.ToString());
                    }
                    break;

                case API.ConnectionStates.Connecting:
                    if (BlueFire.IsReconnecting)
                        if (!IsConnecting)
                        {
                            AdapterReconnecting();
                            ShowStatus(State.ToString());
                        }
                    break;

                case API.ConnectionStates.Ready:
                    if (!IsConnected)
                    {
                        await AdapterConnected();
                        ShowStatus(State.ToString());
                    }
                    break;

                case API.ConnectionStates.KeyTurnedOn:
                    ShowKeyState();
                    await GetData(); // get data if key is turned on after app is started
                    break;

                case API.ConnectionStates.KeyTurnedOff:
                    ShowKeyState();
                    break;

                case API.ConnectionStates.Disconnected:
                    if (IsConnecting || IsConnected)
                        AdapterDisconnected();
                    ShowStatus(State.ToString());
                    break;

                case API.ConnectionStates.Reconnecting:
                    if (!IsConnecting)
                    {
                        AdapterReconnecting();
                        ShowStatus(State.ToString());
                    }
                    break;

                case API.ConnectionStates.Reconnected:
                    if (IsConnecting)
                    {
                        await AdapterReconnected();
                        ShowStatus(State.ToString());
                    }
                    break;

                case API.ConnectionStates.NotReconnected:
                    if (IsConnecting)
                    {
                        AdapterNotReconnected();
                        ShowStatus(State.ToString());
                    }
                    break;

                case API.ConnectionStates.DataAvailable:
                    if (IsConnected)
                    {
                        ShowData();
                        ShowStatus(State.ToString());
                    }
                    break;

                case API.ConnectionStates.ConnectTimeout:
                case API.ConnectionStates.AdapterTimeout:
                    if (IsConnecting || IsConnected)
                    {
                        AdapterNotConnected();
                        ShowStatus(State.ToString());
                        ShowMessage("The Adapter Timed Out.");
                    }
                    break;

                case API.ConnectionStates.DataError:
                    ShowError();
                    break;

                case API.ConnectionStates.SystemError:
                    if (IsConnecting || IsConnected)
                    {
                        AdapterNotConnected();
                        ShowStatus(State.ToString());
                        ShowError();
                    }
                    break;

                case API.ConnectionStates.NotAuthenticated:
                    ShowMessage("Your User Name and Password do not match the Adapter's User Name and Password.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case API.ConnectionStates.IncompatibleVersion:
                    ShowMessage("The Adapter is not compatible with this API.");
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;

                case API.ConnectionStates.NoAdapter:
                case API.ConnectionStates.BluetoothNA:
                    AdapterNotConnected();
                    ShowStatus(State.ToString());
                    break;
            }

            // Check reset button enable
            if (!IsConnected)
                ResetButton.IsEnabled = false; // because it can be enabled in ShowData
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

        private void ShowError()
        {
            ErrorMessage = BlueFire.ErrorMessage;
            ErrorException = BlueFire.ErrorException;

            if (ErrorException != null)
                ErrorMessage += @"/r/n" + @"/r/n" + ErrorException.Message;

            if (ErrorMessage == "")
                return;

            ShowMessage(ErrorMessage);

            ShowConnectButton();
        }

        private void ClearEditMessages()
        {
            MessageText.IsVisible = false;
            EditPGNText.IsVisible = false;
            EditSettingsText.IsVisible = false;
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
            LogNotifications("Adapter connected.");

            IsConnected = true;
            IsConnecting = false;

            ClearEditMessages();

            ShowDisconnectButton();

            NextButton.IsEnabled = true;
            UpdateButton.IsEnabled = true;
            SendButton.IsEnabled = true;

            ConnectButton.Focus();

            // Get data if key is on when app is connecting
            if (IsKeyOn)
                await GetData();
        }

        private void AdapterDisconnected()
        {
            LogNotifications("Adapter disconnected.");

            AdapterNotConnected();
        }

        private void AdapterNotConnected()
        {
            LogNotifications("Adapter not connected.");

            IsConnected = false;
            IsConnecting = false;

            ShowKeyState(); // key off

            ShowConnectButton();

            BT2Switch.IsEnabled = true;
            BLESwitch.IsEnabled = true;

            J1939Switch.IsEnabled = true;
            J1708Switch.IsEnabled = true;

            UpdateButton.IsEnabled = true;
            SendButton.IsEnabled = false;

            ConnectButton.Focus();
        }

        private void AdapterReconnecting()
        {
            LogNotifications("Adapter re-connecting.");

            IsConnected = false;
            IsConnecting = true;

            NextButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            SendButton.IsEnabled = false;

            LogNotifications("App reconnecting to the Adapter. Reason is " + BlueFire.ReconnectReason + ".");

            ShowMessage("Lost connection to the Adapter, reconnecting.");
        }

        private async Task AdapterReconnected()
        {
            LogNotifications("Adapter re-connected.");

            await AdapterConnected();

            ShowMessage("Adapter reconnected.");
        }

        private void AdapterNotReconnected()
        {
            LogNotifications("Adapter not re-connected.");

            AdapterNotConnected();

            ShowMessage("The Adapter did not reconnect.");
        }

    #endregion

    #region Get Data

        // Start retrieving data after connecting to the adapter
        private async Task GetData()
        {
            // Check for API setting the adapter type
            BT2Switch.IsToggled = BlueFire.UseBT2;
            BLESwitch.IsToggled = BlueFire.UseBLE;

            // Start retrieving initial truck data
            await ShowTruckText();

            // Start retrieving truck info
            BlueFire.GetVehicleData(); // VIN, Make, Model, Serial no
        }

        private async Task ClearData()
        {
            await BlueFire.ClearData();

            BlueFire.GetFaults(); // Faults
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

            // Show truck data
            ShowTruckData();

            // Show truck faults
            if (BlueFire.Truck.ActiveFaultsCount == 0)
            {
                FaultText.Text = "NA";
                ResetButton.IsEnabled = false;
            }
            else
            {
                FaultText.Text = BlueFire.Faults.Items[0].ToString();
                ResetButton.IsEnabled = true;
            }

            // Check adapter settings
            if (LedBrightness != BlueFire.LedBrightness)
            {
                LedBrightness = BlueFire.LedBrightness;
                LedBrightnessText.Text = LedBrightness.ToString();
            }

            // Check for SendPGN response
            if ((IsSendingPGN || IsMonitoringPGN) && BlueFire.PGN == PGN)
            {
                IsSendingPGN = false; // only show sending data once
                PGNDataText.Text = BitConverter.ToString(BlueFire.PGNData);
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
                    DataView1.Text = FormatSingle(BlueFire.Truck.MaxSpeed, 0);
                    DataView2.Text = FormatSingle(BlueFire.Truck.HiResMaxSpeed, 0);
                    //DataView3.Text = FormatInt32(BlueFire.Truck.CurrentGear);
                    //DataView4.Text = FormatInt32(BlueFire.Truck.SelectedGear);
                    DataView5.Text = FormatSingle(BlueFire.Truck.BatteryPotential, 2);
                    DataView6.Text = "";
                    DataView7.Text = "";
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
                    DataView7.Text = "";
                    break;

                case 6:
                    DataView1.Text = BlueFire.Truck.VIN;
                    DataView2.Text = BlueFire.Truck.Engine.Make;
                    DataView3.Text = BlueFire.Truck.Engine.Model;
                    DataView4.Text = BlueFire.Truck.Engine.SerialNo;
                    DataView5.Text = BlueFire.Truck.Engine.UnitNo;
                    DataView6.Text = "";
                    DataView7.Text = "";
                    break;
            }
        }

        private async Task ShowTruckText()
        {
            switch (GroupNo)
            {
                case 0:
                    await ClearData();
                    BlueFire.GetEngineData1(); // RPM, Percent Torque, Driver Torque, Torque Mode
                    BlueFire.GetEngineData2(); // Percent Load, Accelerator Pedal Position
                    BlueFire.GetEngineData3(); // Vehicle Speed, Max Set Speed, Brake Switch, Clutch Switch, Park Brake Switch, Cruise Control Settings and Switches

                    TextView1.Text = "RPM";
                    TextView2.Text = "Speed";
                    TextView3.Text = "Accel Pedal";
                    TextView4.Text = "Pct Load";
                    TextView5.Text = "Pct Torque";
                    TextView6.Text = "Driver Torque";
                    TextView7.Text = "Torque Mode";
                    break;

                case 1:
                    await ClearData();
                    BlueFire.GetOdometer(); // Odometer (Engine Distance)
                    BlueFire.GetBrakeData(); // Application Pressure, Primary Pressure, Secondary Pressure
                    BlueFire.GetEngineHours(); // Total Engine Hours, Total Idle Hours

                    TextView1.Text = "Distance";
                    TextView2.Text = "Odometer";
                    TextView3.Text = "Total Hours";
                    TextView4.Text = "Idle Hours";
                    TextView5.Text = "Brake Pres";
                    TextView6.Text = "Brake Air";
                    TextView7.Text = "";
                    break;

                case 2:
                    await ClearData();
                    BlueFire.GetFuelData(); // Fuel Used, Idle Fuel Used, Fuel Rate, Instant Fuel Economy, Avg Fuel Economy, Throttle Position

                    TextView1.Text = "Fuel Rate";
                    TextView2.Text = "Fuel Used";
                    TextView3.Text = "HiRes Fuel";
                    TextView4.Text = "Idle Fuel Used";
                    TextView5.Text = "Avg Fuel Econ";
                    TextView6.Text = "Inst Fuel Econ";
                    TextView7.Text = "Throttle Pos";
                    break;

                case 3:
                    await ClearData();
                    BlueFire.GetEngineData3(); // Vehicle Speed, Max Set Speed, Brake Switch, Clutch Switch, Park Brake Switch, Cruise Control Settings and Switches
                    BlueFire.GetBatteryVoltage(); // Battery Voltage
                    BlueFire.GetTransmissionGears(); // Selected and Current Gears

                    TextView1.Text = "Max Speed";
                    TextView2.Text = "HiRes Max";
                    TextView3.Text = "Current Gear";
                    TextView4.Text = "Selected Gear";
                    TextView5.Text = "Battery Volts";
                    TextView6.Text = "";
                    TextView7.Text = "";

                    break;

                case 4:
                    await ClearData();
                    BlueFire.GetPressures(); // Oil Pressure, Coolant Pressure, Intake Manifold(Boost) Pressure
                    BlueFire.GetTemperatures(); // Oil Temp, Coolant Temp, Intake Manifold Temperature
                    BlueFire.GetCoolantLevel(); // Coolant Level

                    TextView1.Text = "Oil Temp";
                    TextView2.Text = "Oil Pressure";
                    TextView3.Text = "Intake Temp";
                    TextView4.Text = "Intake Pres";
                    TextView5.Text = "Coolant Temp";
                    TextView6.Text = "Coolant Pres";
                    TextView7.Text = "Coolant Level";
                    break;

                case 5:
                    await ClearData();
                    BlueFire.GetEngineData3(); // Vehicle Speed, Max Set Speed, Brake Switch, Clutch Switch, Park Brake Switch, Cruise Control Settings and Switches

                    TextView1.Text = "Brake Switch";
                    TextView2.Text = "Clutch Switch";
                    TextView3.Text = "Park Switch";
                    TextView4.Text = "Cruise Switch";
                    TextView5.Text = "Cruise State";
                    TextView6.Text = "Cruise Speed";
                    TextView7.Text = "";
                    break;

                case 6:
                    await ClearData();
                    BlueFire.GetVehicleData(); // VIN, Make, Model, Serial no

                    TextView1.Text = "VIN";
                    TextView2.Text = "Make";
                    TextView3.Text = "Model";
                    TextView4.Text = "Serial No";
                    TextView5.Text = "Unit No";
                    TextView6.Text ="";
                    TextView7.Text ="";
                    break;
            }
            DataView1.Text = "NA";
            DataView2.Text = "NA";
            DataView3.Text = "NA";
            DataView4.Text = "NA";
            DataView5.Text = "NA";
            DataView6.Text = "NA";
            DataView7.Text = "NA";
        }

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

    #region Log Notification

        private void LogNotifications(String Notification)
        {
#if (WINDOWS_UWP)
            Debug.WriteLine(Notification);
#else
            Debug.Print(Notification);
#endif
        }

    #endregion

    #region Connect Button

        private async void ConnectButton_Clicked(object sender, EventArgs e)
        {
            ConnectButton.IsEnabled = false;

            // Check for connecting
            if (IsConnectButton)
            {
                IsConnecting = true;
                IsConnected = false;

                ShowStatus("Connecting...");

                NextButton.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                ResetButton.IsEnabled = false;
                SendButton.IsEnabled = false;

                ClearEditMessages();

                ShowDisconnectButton();

                BlueFire.UseBLE = BLESwitch.IsToggled;
                BlueFire.UseBT2 = BT2Switch.IsToggled;

                BlueFire.IgnoreJ1939 = !J1939Switch.IsToggled; // is opposite
                BlueFire.IgnoreJ1708 = !J1708Switch.IsToggled; // is opposite

                //BlueFire.PerformanceMode = true;

                await BlueFire.Connect();
            }
            else // Disconnecting
                await DisconnectAdapter(true);
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
                case API.ConnectionStates.Discovering:
                case API.ConnectionStates.Connecting:
                case API.ConnectionStates.Reconnecting:
                case API.ConnectionStates.Ready:
                    await BlueFire.CancelConnecting();
                    break;

                // Check for already connected
                case API.ConnectionStates.Connected:
                case API.ConnectionStates.Reconnected:
                case API.ConnectionStates.Authenticating:
                case API.ConnectionStates.Authenticated:
                case API.ConnectionStates.RetrievingData:
                case API.ConnectionStates.DataAvailable:
                    await BlueFire.Disconnect(WaitForDisconnect);
                    break;

                // already disconnecting or not connected
                default:
                    AdapterNotConnected();
                    break;
            }
        }

    #endregion

    #region Next Button

        // Next Truck Data
        private async void NextButton_Clicked(object sender, EventArgs e)
        {
            GroupNo++;
            if (GroupNo > MaxGroupNo)
                GroupNo = 0;

            await ShowTruckText();
        }

    #endregion

    #region Update Button

        // Adapter settings
        private void UpdateButton_Clicked(object sender, EventArgs e)
        {
            // LED Brightness
            Byte LedBrightness;

            EditSettingsText.IsVisible = false;

            if (!Byte.TryParse(LedBrightnessText.Text, out LedBrightness) || LedBrightness < 5 || LedBrightness > 100)
            {
                EditSettingsText.Text = "Invalid LED Brightness";
                EditSettingsText.IsVisible = true;
                return;
            }
            BlueFire.LedBrightness = LedBrightness;

            // Security
            String UserName = UserNameText.Text.Trim();
            String Password = PasswordText.Text.Trim();

            if (UserName.Length > 20)
            {
                EditSettingsText.Text = "Invalid User Name";
                EditSettingsText.IsVisible = true;
                return;
            }
            if (Password.Length > 12)
            {
                EditSettingsText.Text = "Invalid Password";
                EditSettingsText.IsVisible = true;
                return;
            }

            BlueFire.UpdateSecurity(UserName, Password);
        }

    #endregion

    #region Reset Button

        // Fault Reset
        private void ResetButton_Clicked(object sender, EventArgs e)
        {
            BlueFire.ResetFaults();
        }

    #endregion

    #region Send Button

        // Proprietary PGN Monitoring/Sending
        private void SendButton_Clicked(object sender, EventArgs e)
        {
            IsSendingPGN = false;
            IsMonitoringPGN = false;
            EditPGNText.IsVisible = false;

            // Get PGN
            if (!Int32.TryParse("0" + PGNText.Text.Trim(), out PGN))
            {
                EditPGNText.Text = "PGN must be numeric.";
                EditPGNText.IsVisible = true;
                return;
            }

            // Ignore if no PGN
            if (PGN == 0)
                return;

            // Get PGN Data
            Byte[] PGNBytes = new Byte[8];

            String PGNData = PGNDataText.Text.Trim();

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
                    EditPGNText.Text = "PGN Data must be 16 hex characters (8 bytes).";
                    EditPGNText.IsVisible = true;
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
                    EditPGNText.Text = "PGN Data must be 16 hex characters (8 bytes).";
                    EditPGNText.IsVisible = true;
                    return;
                }

                // Send the PGN
                IsSendingPGN = true;
                BlueFire.SendPGN(PGN, PGNBytes);
            }
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
