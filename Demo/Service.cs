using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using BlueFire;

namespace Demo
{
    public class Service
    {

    #region Declaratives

        private API BlueFire;

        private event API.EventHandler APIDataHandlerEvent;

        private ConnectionStates ConnectionState = ConnectionStates.NotConnected;

        private Int32 GroupNo;
        private const Int32 MaxGroupNo = 7;

        private Int32 RetrievalInterval;
        private RetrievalMethods RetrievalMethod;

        private Boolean IsConnected;
        private Boolean IsConnecting;

        private String ErrorMessage = "";
        private Exception ErrorException = null;

        // Adapter variables
        private Byte LedBrightness = 100;
        private Boolean IgnoreJ1939 = false;
        private Boolean IgnoreJ1708 = false;

        private Boolean ServiceIsRunning;
        private CancellationTokenSource ServiceToken;

    #endregion

    #region Constructor

        public Service()
        {
            APIDataHandlerEvent += new API.EventHandler(DataEventHandler);

            BlueFire = new API(APIDataHandlerEvent, null);
        }

    #endregion

    #region Simulate Service

        public async Task StartService()
        {
            // Simulate a service
            ServiceIsRunning = true;

            ServiceToken = null; // free up any previous token
            ServiceToken = new CancellationTokenSource(); // must always be new
            await Task.Factory.StartNew(async () => await SimulateService(), ServiceToken.Token);
        }

        private async Task SimulateService()
        {
            await Initialize();

            await Connect();

            while (ServiceIsRunning) ;
        }

        public async Task StopService()
        {
            ServiceIsRunning = false;
            ServiceToken.Cancel();

            await Disconnect();

            await BlueFire.EndApplication();
        }

        private async Task Initialize()
        {
            // Connect button
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

            // Set Bluetooth settings
            BlueFire.UseBT2 = false;
            BlueFire.UseBLE = true;

            // Set adapter databus settings
            BlueFire.IgnoreJ1939 = false;
            BlueFire.IgnoreJ1708 = true;

            // Set data retrieval method for testing.
            //RetrievalMethod = RetrievalMethods.OnChange;
            RetrievalMethod = RetrievalMethods.OnInterval;
            //RetrievalMethod = RetrievalMethods.Synchronized;

            // Or set the retrieval interval if using OnInterval.
            //RetrievalInterval = 1000; // default is BlueFire.MinInterval
        }

    #endregion

    #region Connect / Disconnect

        private async Task Connect()
        {
            IsConnecting = true;
            IsConnected = false;

            WriteLog("Connecting...");

            Boolean Synchronized = true; // test synchronized connection
            //BlueFire.ConnectTimeout = 2000; // default is 1000 (one second)

            await BlueFire.Connect(Synchronized);
        }

        private async Task Disconnect()
        {
            WriteLog("Disconnecting...");

            await DisconnectAdapter(true);
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

    #region Data Event Handler

        private async void DataEventHandler(ConnectionStates State)
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
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.NotConnected:
                    if (IsConnecting || IsConnected)
                    {
                        AdapterNotConnected();
                        LogStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.Connecting:
                    if (BlueFire.IsReconnecting)
                        if (!IsConnecting)
                            AdapterReconnecting();
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.Ready:
                    if (!IsConnected)
                    {
                        await AdapterConnected();
                        LogStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.KeyTurnedOn:
                    LogKeyState();
                    await GetTruckData(); // get data if key is turned on after app is started
                    break;

                case ConnectionStates.KeyTurnedOff:
                    LogKeyState();
                    break;

                case ConnectionStates.Disconnected:
                    if ((IsConnecting || IsConnected) && !BlueFire.IsReconnecting)
                        AdapterDisconnected();
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.Reconnecting:
                    if (!IsConnecting)
                        AdapterReconnecting();
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.Reconnected:
                    if (IsConnecting)
                    {
                        AdapterReconnected();
                        LogStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.NotReconnected:
                    if (IsConnecting)
                        AdapterNotReconnected();
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.DataAvailable:
                    if (IsConnected)
                    {
                        CheckData();
                        LogStatus(State.ToString());
                    }
                    break;

                case ConnectionStates.CANFilterFull:
                    LogMessage("The CAN Filter is Full. Some data will not be retrieved.");
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.ConnectTimeout:
                case ConnectionStates.AdapterTimeout:
                    if (IsConnecting || IsConnected)
                    {
                        AdapterNotConnected();
                        LogStatus(State.ToString());
                        LogMessage("The Adapter Timed Out.");
                    }
                    break;

                case ConnectionStates.DataError:
                    LogError();
                    break;

                case ConnectionStates.SystemError:
                    LogError();
                    AdapterNotConnected();
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.NotAuthenticated:
                    LogMessage("You are not authorized to access this adapter. Check your adapter security settings.");
                    AdapterNotConnected();
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.NotFound:
                    LogMessage("A valid adapter was not found. Check your adapter connection settings.");
                    AdapterNotConnected();
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.IncompatibleVersion:
                    LogMessage("The Adapter is not compatible with this API.");
                    AdapterNotConnected();
                    LogStatus(State.ToString());
                    break;

                case ConnectionStates.NoAdapter:
                case ConnectionStates.BluetoothNA:
                    AdapterNotConnected();
                    LogStatus(State.ToString());
                    break;
            }
        }

    #endregion

    #region Connection Status

        private void LogStatus(String Status)
        {
            WriteLog("Status=" + Status);
        }

        private void LogMessage(String Message)
        {
            WriteLog("Message=" + Message);
        }

    #endregion

    #region Adapter Connection

        private async Task AdapterConnected()
        {
            WriteLog("Adapter connected.");

            IsConnected = true;
            IsConnecting = false;

            // Test the adapter id filter feature.
            // Note, this will allow the intial connection to a single adapter
            // but then not any more connection attempts until the app is restarted
            // because the filter is cleared when the app is started.
            //BlueFire.AdapterIdFilter.Add(BlueFire.AdapterId);

            // Start retrieving truck data
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

            LogKeyState(); // key off
        }

        private void AdapterReconnecting()
        {
            WriteLog("Adapter re-connecting.");

            IsConnected = false;
            IsConnecting = true;

            WriteLog("App reconnecting to the Adapter. Reason is " + BlueFire.ReconnectReason + ".");
        }

        private void AdapterReconnected()
        {
            WriteLog("Adapter re-connected.");
        }

        private void AdapterNotReconnected()
        {
            WriteLog("Adapter not re-connected.");

            AdapterNotConnected();
        }

    #endregion

    #region Key State

        private void LogKeyState()
        {
            if (BlueFire.IsKeyOn)
                WriteLog("Key is On");
            else
                WriteLog("Key is Off");
        }

    #endregion

    #region Check Data

        // This is called whenever the DataAvailable event is triggered. You don't know which data
        // triggered the event so you need to check all data that you requested.

        private void CheckData()
        {
            // Check adapter data
            CheckAdapterData();

            // Check truck data
            CheckTruckData();

            // Check ELD data
            if (ELD.IsStarted)
                GetELDData();
        }

     #endregion

    #region Check Adapter Data

        private void CheckAdapterData()
        {
            // Check adapter settings
            if (LedBrightness != BlueFire.LedBrightness)
            {
                LedBrightness = BlueFire.LedBrightness;
                // Save this if you need to
            }

            // Check if ignore databuses have changed
            if (IgnoreJ1939 != BlueFire.IgnoreJ1939)
            {
                IgnoreJ1939 = BlueFire.IgnoreJ1939;
                // Save this if need be
            }

            if (IgnoreJ1708 != BlueFire.IgnoreJ1708)
            {
                IgnoreJ1708 = BlueFire.IgnoreJ1708;
                // Save this if need be
            }
        }

        #endregion

    #region Truck Data Processing

        #region Get Truck Data

        // Send data request to the adapter to retrieve truck ECM data.
        // Note, be careful not to request too much data at one time otherwise you
        // run the risk of filling up the CAN Filter buffer.
        // This routine uses GroupNo to select a data group to retrieve from. This is just
        // a funky way of separating out data so the CAN Filter does not fill up. You can
        // experiement with combining data retrievals to determine how much you can request
        // before filling the CAN Filter buffer (you get an error if you do).

        private async Task GetTruckData()
        {
            // Set the retrieval method and interval.
            // Note, this is here for demo-ing the different methods.
            RetrievalMethod = RetrievalMethods.OnChange; // default
#if (__ANDROID__)
            RetrievalMethod = RetrievalMethods.OnInterval; // recommended for Android
#endif
            //RetrievalInterval = 5000; // default is MinInterval, only required if RetrievalMethod is OnInterval

            //RetrievalMethod = RetrievalMethods.Synchronized;
            BlueFire.SyncTimeout = 2000; // default is 1000, only required if RetrievalMethod is Synchronized

            // Clear any previous adapter data
            await BlueFire.ClearData();

            switch (GroupNo)
            {
                case 0:
                    //await BlueFire.GetEngineData1(); // default RetrievalMethods.OnChange
                    //await BlueFire.GetEngineData1(RetrievalMethods.OnInterval); // default Interval = MinInterval
                    //await BlueFire.GetEngineData1(RetrievalMethods.Synchronized); // blocks until data is retrieved

                    await BlueFire.GetEngineData1(RetrievalMethod, RetrievalInterval); // RPM, Percent Torque, Driver Torque, Torque Mode
                    await BlueFire.GetEngineData2(RetrievalMethod, RetrievalInterval); // Percent Load, Accelerator Pedal Position
                    await BlueFire.GetEngineData3(RetrievalMethod, RetrievalInterval); // Vehicle Speed, Max Set Speed, Brake Switch, Clutch Switch, Park Brake Switch, Cruise Control Settings and Switches

                    break;

                case 1:
                    await BlueFire.GetDistance(RetrievalMethod, RetrievalInterval); // Distance and Odometer

                    break;

                case 2:
                    await BlueFire.GetBrakeData(RetrievalMethod, RetrievalInterval); // Application Pressure, Primary Pressure, Secondary Pressure
                    await BlueFire.GetEngineHours(RetrievalMethod, RetrievalInterval); // Total Engine Hours, Total Idle Hours
                    await BlueFire.GetTransmissionGears(RetrievalMethod, RetrievalInterval); // Selected and Current Gears
                    await BlueFire.GetBatteryVoltage(RetrievalMethod, RetrievalInterval); // Battery Voltage

                    break;

                case 3:
                    await BlueFire.GetFuelData(RetrievalMethod, RetrievalInterval); // Fuel Used, Idle Fuel Used, Fuel Rate, Instant Fuel Economy, Avg Fuel Economy, Throttle Position

                    break;

                case 4:
                    await BlueFire.GetPressures(RetrievalMethod, RetrievalInterval); // Oil Pressure, Coolant Pressure, Intake Manifold(Boost) Pressure
                    await BlueFire.GetTemperatures(RetrievalMethod, RetrievalInterval); // Oil Temp, Coolant Temp, Intake Manifold Temperature
                    await BlueFire.GetCoolantLevel(RetrievalMethod, RetrievalInterval); // Coolant Level

                    break;

                case 5:
                    await BlueFire.GetEngineData3(RetrievalMethod, RetrievalInterval); // Vehicle Speed, Max Set Speed, Brake Switch, Clutch Switch, Park Brake Switch, Cruise Control Settings and Switches

                    break;

                case 6:
                    if (!BlueFire.Truck.VINExists)
                        await BlueFire.GetVehicleIdSync(); // VIN synchronously
                                                           //await BlueFire.GetVehicleId(); // VIN asynchronously

                    if (!BlueFire.Truck.Engine.IdExists)
                        await BlueFire.GetVehicleData(); // Make, Model, Serial No asynchronously

                    break;

                case 7:
                    // Faults
                    await BlueFire.GetFaults();

                    break;
            }
        }

        #endregion

        #region Check Truck Data

        // Check the data you requested to see which one changed that triggered the DataAvailable
        // event. If you're not concerned with data throughput for processing the data, you can just
        // process all the data whether it changed or not.
        // Again, the GroupNo is used to segment out the data groups used for testing.

        private void CheckTruckData()
        {
            String Data1Text = "";
            String Data2Text = "";
            String Data3Text = "";
            String Data4Text = "";
            String Data5Text = "";
            String Data6Text = "";
            String Data7Text = "";

            switch (GroupNo)
            {
                case 0:
                    Data1Text = FormatInt32(BlueFire.Truck.RPM);
                    Data2Text = FormatSingle(BlueFire.Truck.Speed, 0);
                    Data3Text = FormatSingle(BlueFire.Truck.AccPedPos, 2);
                    Data4Text = FormatInt32(BlueFire.Truck.PctLoad);
                    Data5Text = FormatInt32(BlueFire.Truck.PctTorque);
                    Data6Text = FormatInt32(BlueFire.Truck.DrvPctTorque);
                    Data7Text = BlueFire.Truck.TorqueMode.ToString();
                    break;

                case 1:
                    Data1Text = FormatSingle(BlueFire.Truck.Distance, 0);
                    Data2Text = FormatSingle(BlueFire.Truck.HiResDistance, 0);
                    Data3Text = FormatSingle(BlueFire.Truck.LoResDistance, 0);
                    Data4Text = "";
                    Data5Text = FormatSingle(BlueFire.Truck.Odometer, 0);
                    Data6Text = FormatSingle(BlueFire.Truck.HiResOdometer, 0);
                    Data7Text = FormatSingle(BlueFire.Truck.LoResOdometer, 0);
                    break;

                case 2:
                    Data1Text = FormatSingle(BlueFire.Truck.TotalHours, 2);
                    Data2Text = FormatSingle(BlueFire.Truck.IdleHours, 2);
                    Data3Text = FormatSingle(BlueFire.Truck.BrakeApplicationPressure, 2);
                    Data4Text = FormatSingle(BlueFire.Truck.Brake1AirPressure, 2);
                    Data5Text = FormatInt32(BlueFire.Truck.TransCurrentGear);
                    Data6Text = FormatInt32(BlueFire.Truck.TransSelectedGear);
                    Data7Text = FormatSingle(BlueFire.Truck.BatteryPotential, 2);
                    break;

                case 3:
                    Data1Text = FormatSingle(BlueFire.Truck.FuelRate, 2);
                    Data2Text = FormatSingle(BlueFire.Truck.TotalFuelUsed, 2);
                    Data3Text = FormatSingle(BlueFire.Truck.HiResFuelUsed, 2);
                    Data4Text = FormatSingle(BlueFire.Truck.IdleFuelUsed, 2);
                    Data5Text = FormatSingle(BlueFire.Truck.AvgFuelEcon, 2);
                    Data6Text = FormatSingle(BlueFire.Truck.InstFuelEcon, 2);
                    Data7Text = FormatSingle(BlueFire.Truck.ThrottlePos, 2);
                    break;

                case 4:
                    Data1Text = FormatSingle(BlueFire.Truck.OilTemp, 2);
                    Data2Text = FormatSingle(BlueFire.Truck.OilPressure, 2);
                    Data3Text = FormatSingle(BlueFire.Truck.IntakeTemp, 2);
                    Data4Text = FormatSingle(BlueFire.Truck.IntakePressure, 2);
                    Data5Text = FormatSingle(BlueFire.Truck.CoolantTemp, 2);
                    Data6Text = FormatSingle(BlueFire.Truck.CoolantPressure, 2);
                    Data7Text = FormatSingle(BlueFire.Truck.CoolantLevel, 2);
                    break;

                case 5:
                    Data1Text = BlueFire.Truck.BrakeSwitch.ToString();
                    Data2Text = BlueFire.Truck.ClutchSwitch.ToString();
                    Data3Text = BlueFire.Truck.ParkBrakeSwitch.ToString();
                    Data4Text = BlueFire.Truck.CruiseSwitch.ToString();
                    Data5Text = BlueFire.Truck.CruiseState.ToString();
                    Data6Text = FormatSingle(BlueFire.Truck.CruiseSpeed, 0);
                    Single MaxSpeed = BlueFire.Truck.MaxSpeed;
                    if (BlueFire.Truck.HiResMaxSpeed > 0)
                        MaxSpeed = BlueFire.Truck.HiResMaxSpeed;
                    Data7Text = FormatSingle(MaxSpeed, 0);
                    break;

                case 6:
                    Data1Text = BlueFire.Truck.EngineVIN;
                    Data2Text = BlueFire.Truck.Engine.Make;
                    Data3Text = BlueFire.Truck.Engine.Model;
                    Data4Text = BlueFire.Truck.Engine.SerialNo;
                    Data5Text = BlueFire.Truck.Engine.UnitNo;
                    //Data1Text = BlueFire.Truck.CabBodyVIN;
                    //Data2Text = BlueFire.Truck.CabBody.Make;
                    //Data3Text = BlueFire.Truck.CabBody.Model;
                    //Data4Text = BlueFire.Truck.CabBody.SerialNo;
                    //Data5Text = BlueFire.Truck.CabBody.UnitNo;
                    Data6Text = "";

                    if (BlueFire.Truck.EngineVINExists && BlueFire.Truck.Engine.IdExists)
                        Data7Text = "";

                    break;

                case 7:

                    String Source = "";
                    String SPN = "";
                    String FMI = "";
                    String Occurrence = "";
                    String Conversion = "";

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
                    }

                    Data1Text = Source;
                    Data2Text = SPN;
                    Data3Text = FMI;
                    Data4Text = Occurrence;
                    Data5Text = Conversion;
                    Data6Text = "";
                    Data7Text = "";

                    break;
            }

            // Do something when data has changed and been retrieved from the adapter.
            // Note, unless you programmatically save the previous value and check it you don't
            // know which data changed and triggered the DataAvailable event.

            WriteLog("Data1Text=" + Data1Text);
            WriteLog("Data2Text=" + Data2Text);
            WriteLog("Data3Text=" + Data3Text);
            WriteLog("Data4Text=" + Data4Text);
            WriteLog("Data5Text=" + Data5Text);
            WriteLog("Data6Text=" + Data6Text);
            WriteLog("Data7Text=" + Data7Text);
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

    #endregion

    #region ELD Processing

        #region Variables

        // ELD variables
        private Int32 CurrentRecordNo;
        private Boolean IsUploading;
        private Int32 UploadFrom;

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

        #region Initialize

        private void ELDInitialize()
        {
            // ELD settings
            ELD.DriverId = "Test Driver Id";

            ELD.ELDInterval = 60;
            ELD.IFTAInterval = 15;
            ELD.StatsInterval = 30;

            ELD.RecordIFTA = true;
            ELD.RecordStats = true;

            ELD.AlignELD = true;
            ELD.AlignIFTA = true;
            ELD.AlignStats = true;

            ELD.IsSecured = false;

            ELD.IsStreaming = true;
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
                    LogMessage("There is not emough adapter memory to record ELD data. You must change your ELD Interval or Duration in Settings.");
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
                // Check for any recording set
                if (!ELD.IsStreaming && ELD.RecordingMode == ELD.RecordingModes.RecordNever)
                {
                    LogMessage("You have not set any ELD recording.");
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

        #region Get ELD Data

        private void GetELDData()
        {
            if (!ELD.IsCompatibleAdapter)
                return;

            // Check for adapter or local recording, or adapter upload
            if (ELD.CurrentRecordNo > 0 || ELD.IsRecordingLocally)
                if (ELD.RecordNo > 0 && ELD.RecordNo != CurrentRecordNo)
                {
                    CurrentRecordNo = ELD.RecordNo;

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
        }

        #endregion

        #region Remaining Memory

        private void GetRemaingMemory()
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

                LogMessage("The ELD upload has finished.");
            }
            else
                ELD.GetNextRecord();
        }

        #endregion

        #region Save ELD Data

        private void SaveELDData()
        {
            // Save the locallly retrieved ELD records

            LogMessage("ELD Records have been Saved.");
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

            LogMessage("The ELD records have been deleted.");
        }

        #endregion

        #region Set Show NA

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

#if (WINDOWS_UWP)
            Debug.WriteLine(Message);
#else
            Debug.Print(Message);
#endif
        }

        #endregion

    #region Dispose

        public async Task Dispose()
        {
            if (ServiceIsRunning)
                await StopService();
        }

    #endregion

    }
}
