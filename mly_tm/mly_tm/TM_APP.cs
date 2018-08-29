#define DEBUG
#define EMULATE_GW

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO.Ports;

/*
 TODO:
     - Interruption timers for transmission/reception
     - UART send/receive message
     - Initialisation phase
     - Protocol commands template
     */

namespace mly_tm
{


    // TM Application
#if EMULATE_GW
    public class TM_APP
#else
    class TM_APP
#endif
    {
        private double _speed_target;
        public double speed_target { get { return _speed_target; } set { _speed_target = value; } }

        private double _speed_MIN;
        public double speed_MIN { get { return _speed_MIN; } set { _speed_MIN = value; } }

        private double _speed_MAX;
        public double speed_MAX { get { return _speed_MAX; } set { _speed_MAX = value; } }

        private int _incline_target;
        public int incline_target { get { return _incline_target; } set { _incline_target = value; } }

        private int _incline_MIN;
        public int incline_MIN { get { return _incline_MIN; } set { _incline_MIN = value; } }

        private int _incline_MAX;
        public int incline_MAX { get { return _incline_MAX; } set { _incline_MAX = value; } }

        private int _pressure_target;
        public int pressure_target { get { return _pressure_target; } set { _pressure_target = value; } }

        private int _heartbeat_limit;
        public int heartbeat_limit { get { return _heartbeat_limit; } set { _heartbeat_limit = value; } }

        private int _fan_target;
        public int fan_target { get { return _fan_target; } set { _fan_target = value; } }


        protected int watt_target = 200;

        private bool _is_reversion = false;
        public bool is_reversion { get { return _is_reversion; } set { _is_reversion = value; } }

        private bool _is_running = false;
        public bool is_running { get { return _is_running; } set { _is_running = value; } }

        private int _running_time;
        public int running_time { get { return _running_time; } set { _running_time = value; } }
        protected DateTime running_start;

        private bool _bIs_cardio_mode;
        public bool bIs_cardio_mode { get { return _bIs_cardio_mode; } set { _bIs_cardio_mode = value; } }

        protected static DispatcherTimer writeConsoleTimer;
        private string _console_msg;
        public string console_msg { get { return _console_msg; } set { _console_msg = value; } }
#if DEBUG
        private ETM_MODE tm_mode = ETM_MODE.DEBUG;
#else
        private ETM_MODE tm_mode = ETM_MODE.DEFAULT;
#endif
        private ETM_STATE _tm_state = ETM_STATE.DISCONNECTED;
        public ETM_STATE tm_state  { get { return _tm_state; }set { _tm_state = value;WriteLineToConsole(value.ToString()); } }

        private SerialPort port = new SerialPort(Constants.COM_PORT_NAME, Constants.COM_BAUDRATE, Constants.COM_PARITY, Constants.COM_DATABITS, Constants.COM_STOPBITS);

        private UserData _user;
        public UserData user { get { return _user; } set { _user = value; } }

        protected string machine_producer = "";
        protected string machine_type = "";
        protected string model_name = "";
        protected string sw_name = "";
        protected string sw_version = "";
        protected string comsw_version = "";
        protected double wheel_dimension = -1.0f;
        protected string distance_unit = "";
        protected string reserved_parameter_1 = "";
        protected string reserved_parameter_2 = "";
        protected string reserved_parameter_3 = "";
        protected float disconnection_time = 0.0f;
        protected DateTime disconnection_start;
        protected int pressure;
        // W7
        W7 w7 = new W7();
        MainWindow windowParent;
        // WS
        WS wS = new WS();

        DispatcherTimer disconnection_clock;
        protected List<W7> w7s = new List<W7>();
        protected List<WS> wSs = new List<WS>();

        protected INLINE_MANUFACTURED_MODE inline_mode = INLINE_MANUFACTURED_MODE.AUTO_CALIBRATE_SLOPE;

        public TM_APP(MainWindow mw) {
            windowParent = mw;
            incline_MIN = Constants.MIN_INCLINE;
            incline_MAX = Constants.MAX_INCLINE;
            speed_MIN = Constants.MIN_SPEED;
            speed_MAX = Constants.MAX_SPEED;
            fan_target = 0;
            is_reversion = false;
            user = new UserData(25, 80, false);

            Reset();
            // Open the port for communications
            // Get a list of serial port names.

            string[] ports = SerialPort.GetPortNames();

            string res ="The following serial ports were found:\n";

            // Display each port name to the console.
            foreach (string port in ports)
            {
                res+= port+ "\n";
            }
            WriteConsole(res, 3);
            if (ports.Length > 0)
            {
                port.Open();
                // Attach a method to be called when there
                // is data waiting in the port's buffer
                port.DataReceived += new SerialDataReceivedEventHandler(receiveCommand);
            }
            TryConnect();
        }

        public void Reset()
        {
            speed_target = speed_MIN;
            w7.speed = speed_MIN;

            incline_target = incline_MIN;
            w7.incline = incline_MIN;

            //pressure_target = Constants.MIN_PRESSURE;
            //pressure = Constants.MIN_PRESSURE;

            //heartbeat_limit = Constants.MIN_LIM_HEARTBEAT;
            //w7.heartbeat = Constants.MIN_LIM_HEARTBEAT;

            is_running = false;
            running_time = 0;
            bIs_cardio_mode = false;
            console_msg = "TM "+ tm_state;
        }

        private void TryConnect()
        {
            disconnection_clock = new DispatcherTimer();
            disconnection_clock.Tick += new EventHandler(Handle_TryConnect);
            disconnection_clock.Interval = new TimeSpan(0, 0, 1);
            disconnection_clock.Start();
            disconnection_start = DateTime.Now;
        }

        private void Handle_TryConnect(object source, EventArgs e)
        {
            disconnection_time = (int)(DateTime.Now - disconnection_start).TotalSeconds;

            if (disconnection_time > 15)
            {
                return;
            }

            if (tm_state == ETM_STATE.DISCONNECTED)
            {
                sendCommand(ECOMMAND_TYPE.EA);
            }else
            {
                disconnection_clock.Stop();
            }
        }

        public void UpdateFromW7()
        {
            if (w7s.Count == 0 || !w7s[w7s.Count - 1].isValid) { return; }

            //TODO update values from memo avg for now take last
            w7 = w7s[w7s.Count - 1];
        }
        public void UpdateFromWS()
        {
            if (wSs.Count == 0 || !wSs[wSs.Count - 1].isValid) { return; }

            //TODO update values from memo avg for now take last
            wS = wSs[wSs.Count - 1];

        }
        public override string ToString()
        {
            List<string> res = new List<string>();
            res.Add("MODE " + tm_mode + "\n");

            if (is_running)
            {
                running_time = (int) (DateTime.Now - running_start).TotalSeconds;
            }

            TimeSpan time = TimeSpan.FromSeconds(running_time);
            res.Add("Running Time " + time.ToString(@"hh\:mm\:ss"));

            res.Add(w7.ToString());
            
            res.Add("Pressure " + pressure.ToString() + " dpi");

            res.Add(wS.ToString());

            return string.Join("\n", res);
        }

        public string ToStringPart1()
        {
            List<string> res = new List<string>();
            res.Add("MODE " + tm_mode + "\n");

            if (is_running)
            {
                running_time = (int)(DateTime.Now - running_start).TotalSeconds;
            }

            TimeSpan time = TimeSpan.FromSeconds(running_time);
            res.Add("Running Time " + time.ToString(@"hh\:mm\:ss"));

            res.Add(w7.ToString());

            res.Add("Pressure " + pressure.ToString() + "dpi");

            return string.Join("\n", res);
        }

        public string ToStringPart2()
        {
            List<string> res = new List<string>();
            res.Add(wS.ToString());
            return string.Join("\n", res);
        }

        public void WriteConsole(string msg, int seconds)
        {
            console_msg = msg;
            writeConsoleTimer = new DispatcherTimer();
            writeConsoleTimer.Tick += new EventHandler(ResetMsg);
            writeConsoleTimer.Interval = new TimeSpan(0, 0, seconds);
            writeConsoleTimer.Start();
        }

        private void ResetMsg(object source, EventArgs e)
        {
            console_msg = "TM " + tm_state;
            writeConsoleTimer.Stop();
        }

        public void changeSpeedTarget(double delta)
        {
            if (!is_running) { return; }
            speed_target = Math.Min(speed_MAX, Math.Max(speed_MIN, speed_target + delta));
        }

        public void changeInclineTarget(int delta)
        {
            if (!is_running) { return; }
            incline_target = Math.Min(incline_MAX, Math.Max(incline_MIN, incline_target + delta));
        }

        public void changePressureTarget(double value)
        {
//            if (!is_running) { return; }
            pressure_target = (int)(value * (Constants.MAX_PRESSURE - Constants.MIN_PRESSURE) + Constants.MIN_PRESSURE);
        }

        public void changeMaxHeartbeatTarget(double value)
        {
  //          if (!is_running) { return; }
            heartbeat_limit = (int)(value * (Constants.MAX_LIM_HEARTBEAT - Constants.MIN_LIM_HEARTBEAT) + Constants.MIN_LIM_HEARTBEAT);
        }

        public void ToggleCardioMode(bool b)
        {
            bIs_cardio_mode = b;
            if (bIs_cardio_mode)
            {
                WriteConsole("Cardio mode turned On", 2);
            }
            else
            {
                WriteConsole("Cardio mode turned Off", 2);
            }
        }

        public void StartRunning()
        {
            running_start = DateTime.Now;
            sendCommand(ECOMMAND_TYPE.CP);
        }

        public void StopRunning()
        {
            sendCommand(ECOMMAND_TYPE.CP);
        }

        private void writeCommand(string msg, bool bWrittingToConsole = true)
        {
            if (bWrittingToConsole)
            {
                WriteLineToConsole("APP - " + msg);
            }
            if (port.IsOpen) { port.Write(msg); }
#if EMULATE_GW
            windowParent.emulatedGW.receiveCommand(msg);
#endif // Emulate_GW
            
        }

        public void sendCommand(ECOMMAND_TYPE type)
        {
            switch (type)
            {
                case ECOMMAND_TYPE.EA:
                    writeCommand("<EA_>");
                    break;

                case ECOMMAND_TYPE.CS:
                    if (!is_running || tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand(String.Format("<CS_{0:D3}>", (int)(speed_target * 10)));
                    break;

                case ECOMMAND_TYPE.CR:
                    if (!is_running || tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand(String.Format("<CR_{0:D3},{1:D3}>", incline_target * 10, Constants.SPEEDRATIO));
                    break;

                case ECOMMAND_TYPE.CC:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand("<CC_>");
                    break;

                case ECOMMAND_TYPE.CU:
                    if (!is_running || tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand(String.Format("<CU_{0:D2},{1:D3},{2:D1}>", user.age, user.weight, user.isMale ? 1 : 0));
                    break;

                case ECOMMAND_TYPE.CT:
                    if (!is_running || tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand(String.Format("<CT_{0:D3},{1:D3}>", heartbeat_limit, watt_target));
                    break;

                case ECOMMAND_TYPE.CP:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    float tmp = 0;
                    if (is_running) { tmp = Constants.stop_tm_in_x_sec; }
                    is_running = !is_running;
                    writeCommand(String.Format("<CP_{0:N0}>", 10*tmp));
                    break;

                case ECOMMAND_TYPE.WT:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand("<WT_7>");
                    break;

                case ECOMMAND_TYPE.CF:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand(String.Format("<CF_{0:D3},{1:D3}>", fan_target, Constants.SPEEDRATIO));
                    break;

                case ECOMMAND_TYPE.CG:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand(String.Format("<CG_{0:D1}>", is_reversion ? 1 : 0));
                    break;

                case ECOMMAND_TYPE.CM:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand(String.Format("<CM_{0:D3}>", pressure_target));
                    break;

                case ECOMMAND_TYPE.FF:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand(String.Format("<FF_{0:D2}>", inline_mode));
                    break;

                case ECOMMAND_TYPE.AT:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand("<AT_>");
                    break;

                case ECOMMAND_TYPE.SLEEP:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand("<SLEEP_>");
                    break;

                case ECOMMAND_TYPE.DLOAD:
                    if (tm_state != ETM_STATE.CONNECTED) { break; }
                    writeCommand("<DLOAD_>");
                    break;

                default:
                    WriteLineToConsole("cmd "+type+" not implemented");
                    break;
            }
        }

        public void receiveCommand(object sender, SerialDataReceivedEventArgs e)
        {
            string cmd = port.ReadExisting();
            receiveCommand(cmd);
        }

        public void receiveCommand(string cmd) { 
            string[] tokens = cmd.Replace("<","").Replace(">","").Split('_');

            bool is_receptionToken = tokens[tokens.Length-1] == "OK";
            ECOMMAND_TYPE type;
            if (!Enum.TryParse(tokens[0], out type))
            {
                WriteLineToConsole("RECEIVED INVALID COMMAND: " + cmd);
                return;
            }
            if (type != ECOMMAND_TYPE.W7 & type != ECOMMAND_TYPE.WS)
            {
                windowParent.console_text.Text += "APP receive" + cmd + "\n";
                windowParent.console_user_text.Text += "APP receive" + cmd + "\n";
            }

            switch (type)
            {
                case ECOMMAND_TYPE.EA:
                    if (is_receptionToken)
                    {
                        tm_state = ETM_STATE.INITIALISATION;
                    }
                    break;
                case ECOMMAND_TYPE.EP:
                    if (!is_receptionToken)
                    {
                        machine_producer = tokens[1];
                        writeCommand("<EP_OK>");
                    }
                    break;
                case ECOMMAND_TYPE.ET:
                    if (!is_receptionToken)
                    {
                        if (!Constants.MachineTypes.TryGetValue(tokens[1], out machine_type))
                        {
                            WriteLineToConsole("cmd " + type + " wrong machinetype "+ cmd);
                        }
                        writeCommand("<ET_OK>");
                    }
                    break;
                case ECOMMAND_TYPE.EM:
                    if (!is_receptionToken)
                    {
                        model_name = tokens[1];
                        writeCommand("<EM_OK>");
                    }
                    break;
                case ECOMMAND_TYPE.ER:
                    if (!is_receptionToken)
                    {
                        string[] inclineBounds = tokens[1].Split(',');
                        int res = -1;
                        if (int.TryParse(inclineBounds[0], out res)) { incline_MIN = res; }
                        if (int.TryParse(inclineBounds[1], out res)) { incline_MAX = res; }

                        writeCommand("<ER_OK>");

                    }
                    break;
                case ECOMMAND_TYPE.ES:
                    if (!is_receptionToken)
                    {
                        string[] speedBounds = tokens[1].Split(',');
                        double res = Constants.MIN_SPEED;
                        if (double.TryParse(speedBounds[0], out res)) { speed_MIN = res; }
                        if (double.TryParse(speedBounds[1], out res)) { speed_MAX = res; }

                        writeCommand("<ES_OK>");

                    }
                    break;
                case ECOMMAND_TYPE.EV:
                    if (!is_receptionToken)
                    {
                        string[] versions = tokens[1].Split(',');
                        sw_name = versions[0];
                        sw_version = versions[1];
                        comsw_version = versions[2];

                        writeCommand("<EV_OK>");
                    }
                    break;
                case ECOMMAND_TYPE.ED:
                    if (!is_receptionToken)
                    {
                        double res = 0.0f;
                        if (double.TryParse(tokens[1], out res)) { wheel_dimension = res; }

                        writeCommand("<ED_OK>");
                    }
                    break;
                case ECOMMAND_TYPE.EU:
                    if (!is_receptionToken)
                    {
                        distance_unit = tokens[1];
                        writeCommand("<EU_OK>");
                    }
                    break;

                case ECOMMAND_TYPE.EI:
                    if (!is_receptionToken)
                    {
                        string[] parameters = tokens[1].Split(',');
                        reserved_parameter_1 = parameters[0];
                        reserved_parameter_2 = parameters[1];
                        reserved_parameter_3 = parameters[2];
                        writeCommand("<EI_OK>");
                    }
                    break;

                case ECOMMAND_TYPE.EZ:
                    if (!is_receptionToken)
                    {
                        writeCommand("<EZ_OK>");
                        tm_state = ETM_STATE.CONNECTED;
                    }
                    break;

                case ECOMMAND_TYPE.WS:
                    if (wSs.Count >= 10)
                    {
                        wSs.RemoveAt(0);
                    }
                    WS nWS = new WS(tokens[1]);
                    wSs.Add(nWS);
                    UpdateFromWS();
                    writeCommand("<WS_OK>",false);
                    break;

                case ECOMMAND_TYPE.W7:
                    if (w7s.Count>=10)
                    {
                        w7s.RemoveAt(0);
                    }
                    W7 nW7 = new W7(tokens[1]);
                    w7s.Add(nW7);
                    UpdateFromW7();

                    writeCommand("<W7_OK>",false);
                    break;

                case ECOMMAND_TYPE.WT:
                    if (!is_receptionToken)
                    {
                        tm_state = ETM_STATE.DISCONNECTED;
                        writeCommand("<WT_OK>");
                    }
                    break;

                case ECOMMAND_TYPE.AT:
                    if (is_receptionToken)
                    {
                        tm_state = ETM_STATE.DISCONNECTED;
                    }
                    break;

                case ECOMMAND_TYPE.CP:
                    if (is_receptionToken)
                    {
                        writeCommand("<WT_7>");
                    }
                    break;

                case ECOMMAND_TYPE.CC:
                case ECOMMAND_TYPE.CU:
                case ECOMMAND_TYPE.CT:
                case ECOMMAND_TYPE.CR:
                case ECOMMAND_TYPE.CS:
                
                case ECOMMAND_TYPE.CF:
                case ECOMMAND_TYPE.CG:
                case ECOMMAND_TYPE.CM:
                case ECOMMAND_TYPE.FF:
                case ECOMMAND_TYPE.SLEEP:
                case ECOMMAND_TYPE.DLOAD:
                    if (is_receptionToken)
                    {
                        WriteLineToConsole("GW - " + cmd);
                    }
                    break;


                default:
                    WriteLineToConsole("cmd " + type + " not implemented");
                    break;
            }
        }

        public void WriteLineToConsole(string msg)
        {
#if DEBUG
            Console.WriteLine(msg);
            windowParent.console_text.Text += msg + "\n";
            windowParent.console_user_text.Text += msg + "\n";
#endif
        }
    }
}
