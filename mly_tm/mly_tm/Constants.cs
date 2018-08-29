using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace mly_tm
{
    // Physical constraints on TM
    public static class Constants
    {
        public static readonly Dictionary<string, string> MachineTypes = new Dictionary<string, string>
        {
            { "00", "Treadmill" },
            { "10", "Bike" },
            { "20", "Elliptical"}
        };
        public const double MIN_SPEED = 0.0f;
        public const double MAX_SPEED = 25.0f;
        public const int MIN_INCLINE = 0;
        public const int MAX_INCLINE = 15;
        public const int MIN_PRESSURE = 100;
        public const int MAX_PRESSURE = 200;
        public const int MIN_LIM_HEARTBEAT = 80;
        public const int MAX_LIM_HEARTBEAT = 140;

        public const int SPEEDRATIO = 100;

        //https://web.archive.org/web/20130709121945/http://msmvps.com/blogs/coad/archive/2005/03/23/SerialPort-_2800_RS_2D00_232-Serial-COM-Port_2900_-in-C_2300_-.NET.aspx
        public const string COM_PORT_NAME = "COM1";
        public const int COM_PORT_NUMBER = 6000;
        public const int COM_BAUDRATE = 19200;
        public const Parity COM_PARITY = Parity.None;
        public const int COM_DATABITS = 8;
        public const StopBits COM_STOPBITS = StopBits.One;

        public const float stop_tm_in_x_sec = 20.0f;

        static public bool VerifyCheckSum()
        {
            return true;
        }
    }

    public struct UserData
    {
        public int age { get; set; }
        public int weight { get; set; }
        public bool isMale { get; set; }

        public UserData(int a, int w, bool s)
        {
            age = a;
            weight = w;
            isMale = s;
        }

        public override string ToString()
        {
            return "Age: " + age.ToString() + " years\n"
                + "Weight: " + weight.ToString() + " kg\n"
                + "Sex:" + (isMale ? " Male" : " Female");
        }
    }

    public struct W7
    {
        public int serial_number { get; set; }
        public double distance { get; set; }
        public double speed { get; set; }
        public int heartbeat { get; set; }
        public double incline { get; set; }
        public int calories { get; set; }
        public int watt { get; set; }
        public int checkSum { get; set; }
        public bool isValid;

        public W7(int sn, double d,
            double s, int hb, double i,
            int cal, int w, int cs)
        {
            serial_number = sn;
            distance = d;
            speed = s;
            heartbeat = hb;
            incline = i;
            calories = cal;
            watt = w;
            checkSum = cs;
            isValid = false;
        }

        public W7(string token)
        {
            string[] tmp = token.Split(',');
            serial_number = int.Parse(tmp[0]);
            distance = double.Parse(tmp[1]) / 10;
            speed = double.Parse(tmp[2]) / 10;
            heartbeat = int.Parse(tmp[3]);
            incline = double.Parse(tmp[4]) / 10;
            calories = int.Parse(tmp[5]);
            watt = int.Parse(tmp[6]);
            checkSum = int.Parse(tmp[7]);
            isValid = false;
            Validate();
        }

        public void Validate()
        {
            isValid = true;
        }

        public string ToCmdString()
        {
            return String.Format("<W7_" +
                serial_number.ToString("D1") + "," +
                ((int)(distance*10)).ToString("D4") + "," +
                ((int)(speed * 10)).ToString("D3") + "," +
                heartbeat.ToString("D3") + "," +
                ((int)(incline * 10)).ToString("D3") + "," +
                calories.ToString("D6") + "," +
                watt.ToString("D3") + "," +
                checkSum.ToString("D2") + ">"
                );
        }

        public override string ToString()
        {
            return "Distance: " + distance.ToString("N1") + " km\n" +
                "Speed: " + speed.ToString("N1") + " km/h\n" +
                "Heartbeat: " + heartbeat + " b/m\n" +
                "Incline: " + incline + "\n" +
                "Calories: " + calories + " kcal\n" +
                "Watt: " + watt;
        }
    }
    public struct WS
    {
        public int serial_number { get; set; }
        public int reserve_value_1 { get; set; }
        public int machine_value_1 { get; set; }
        public int machine_key { get; set; }
        public int reserve_value_2 { get; set; }
        public int reserve_value_3 { get; set; }
        public int machine_error_id { get; set; }
        public int machine_io_status { get; set; }
        public int checkSum { get; set; }
        public bool isValid;

        public WS(int sn, int rv1,
            int mv1, int mk, int rv2,
            int rv3, int me, int mi, int cs)
        {
            serial_number = sn;
            reserve_value_1 = rv1;
            machine_value_1 = mv1;
            machine_key = mk;
            reserve_value_2 = rv2;
            reserve_value_3 = rv3;
            machine_error_id = me;
            machine_io_status = mi;
            checkSum = cs;
            isValid = false;
        }

        public WS(string token)
        {
            string[] tmp = token.Split(',');
            serial_number = int.Parse(tmp[0]);
            reserve_value_1 = int.Parse(tmp[1]);
            machine_value_1 = int.Parse(tmp[2]);
            machine_key = int.Parse(tmp[3]);
            reserve_value_2 = int.Parse(tmp[4]);
            reserve_value_3 = int.Parse(tmp[5]);
            machine_error_id = int.Parse(tmp[6]);
            machine_io_status = int.Parse(tmp[7]);
            checkSum = int.Parse(tmp[8]);
            isValid = false;
            Validate();
        }

        public void Validate()
        {
            isValid = true;
        }


        public string ToCmdString()
        {
            return String.Format("<WS_"+
                serial_number.ToString("D1")+","+
                reserve_value_1.ToString("D4") + "," +
                machine_value_1.ToString("D3") + "," +
                machine_key.ToString("D3") + "," +
                reserve_value_2.ToString("D3") + "," +
                reserve_value_3.ToString("D3") + "," +
                machine_error_id.ToString("D6") + "," +
                machine_io_status.ToString("D3") + "," +
                checkSum.ToString("D2")+">"
                );
        }

        public override string ToString()
        {
            return "reserve_value_1: " + reserve_value_1 + "\n" +
                "reserve_value_2: " + reserve_value_2 + "\n" +
                "reserve_value_3: " + reserve_value_3 + "\n" +
                "machine_value_1: " + machine_value_1 + "\n" +
                "machine_key: " + machine_key + "\n" +
                "machine_error_id: " + machine_error_id + "\n" +
                "machine_io_status: " + machine_io_status.ToString("D3");
        }
    }

    public enum ETM_MODE
    {
        DEFAULT = 0,
        DEBUG = 1,
    }

    public enum INLINE_MANUFACTURED_MODE
    {
        UP = 1,
        DOWN = 2,
        AUTO_CALIBRATE_SLOPE = 3,
        IS_RESERVED = 4,
    }

    public enum ETM_STATE
    {
        DISCONNECTED = 0,
        CONNECTED = 1,
        INITIALISATION = 2,
    }

    public enum ECOMMAND_TYPE
    {
        // Initialisation commands
        EA,//APP_INFORMATION,
        EP,//INFO_GW_PRODUCER,
        ET,//INFO_GW_MACHINE_TYPE,
        EM,//INFO_GW_MODEL,
        ER,//INFO_GW_INCLINE,
        ES,//INFOSPEED
        EV,//INFO_GW_HWVERSION,
        ED,//INFO_GW_WHEELDIMENSION,
        EU,//INFO_GW_UNIT,
        EI, // reserved parameters
        EZ,//GW_INFORMATION_END,
        // APP commands
        CC,//APP_CLEAR,
        CU,//APP_USERDATA,
        CT,//APP_TARGET,
        CR,//APP_INCLINE,
        CS,//APP_SPEED,
        CP,//APP_STARTSTOP_TM,
        WT,//APP_STARTUP_TM,//Disconnect in reception
        CF,//APP_FAN,
        CG,//APP_DIRECTION,
        CM,//APP_PRESSURE,
        FF,//APP_FF,

        // TM commands
        W7,//GW_W7,
        WS,//GW_WS,
        //Connection commands
        AT,//APp disconnect
        SLEEP,//APP_SLEEP,
        DLOAD,//APP_DOWNLOAD,
    }
}
