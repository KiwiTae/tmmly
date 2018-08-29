using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace mly_tm
{
    public class GW_emulator
    {
        MainWindow windowParent;
        // W7
        W7 w7 = new W7();
        // WS
        WS wS = new WS();
        DispatcherTimer clock;

        bool bIsConnected = false;
        bool bIsSendingW7 = false;
        bool bIsSendingWS = false;

        string machine_producer = "ENDEX";
        string machine_type = "00";
        string model_name = "00TM01";
        int min_incline = 0;
        int max_incline = 17;
        float min_speed = 0.0f;
        float max_speed = 15.0f;
        string sw_name = "xxx";
        string sw_version = "yyyyyy";
        string comsw_version = "zzzzzz";
        float wheel_dim = 12.45f;
        string unit_type = "M";
        string reserved_param_1 = "PAR1";
        string reserved_param_2 = "PAR2";
        string reserved_param_3 = "PAR3";
        
        public GW_emulator(MainWindow w)
        {
            windowParent = w;
            clock = new DispatcherTimer();
            clock.Tick += new EventHandler(Tick_GW_Emulator);
            clock.Interval = new TimeSpan(200);
            clock.Start();

        }

        private void Tick_GW_Emulator(object source, EventArgs e)
        {
            if (!bIsConnected) { return; }
            if (bIsSendingWS)
            {
                
                wS.serial_number = (wS.serial_number + 1) % 10;
                sendCommand(ECOMMAND_TYPE.WS);
            }
            else
            {
                if (bIsSendingW7)
                {
                    sendCommand(ECOMMAND_TYPE.W7);
                }
            }
            bIsSendingWS = !bIsSendingWS;
        }

        private void writeCommand(string cmd)
        {
            windowParent.tm_app.receiveCommand(cmd);
        }

        void sendCommand(ECOMMAND_TYPE type)
        {
            switch (type)
            {
                case ECOMMAND_TYPE.EP:
                    writeCommand(String.Format("<EP_{0}>", machine_producer));
                    break;
                case ECOMMAND_TYPE.ET:
                    writeCommand("<ET_"+ machine_type + ">");
                    break;
                case ECOMMAND_TYPE.EM:
                    writeCommand("<EM_" + model_name + ">");
                    break;
                case ECOMMAND_TYPE.ER:
                    writeCommand(String.Format("<ER_{0:D2},{1:D2}>", min_incline, max_incline));
                    break;
                case ECOMMAND_TYPE.ES:
                    writeCommand(String.Format("<ES_{0:D3},{1:D3}>", 10*min_speed, 10*max_speed));
                    break;
                case ECOMMAND_TYPE.EV:
                    writeCommand("<EV_" + sw_name +","+ sw_version +","+ comsw_version + ">");
                    break;
                case ECOMMAND_TYPE.ED:
                    writeCommand(String.Format("<ED_{0:D4}>", (int)(100*wheel_dim)));
                    break;
                case ECOMMAND_TYPE.EU:
                    writeCommand("<EU_" + unit_type + ">");
                    break;
                case ECOMMAND_TYPE.EI:
                    writeCommand("<EI_" + reserved_param_1 + "," + reserved_param_2 + "," + reserved_param_3 + ">");
                    break;
                case ECOMMAND_TYPE.EZ:
                    writeCommand("<EZ_Q>");
                    break;
                case ECOMMAND_TYPE.CP:
                    writeCommand("<CP_OK>");
                    break;
                case ECOMMAND_TYPE.WT:
                    writeCommand("<WT_>");
                    break;
                case ECOMMAND_TYPE.WS:
                    writeCommand(wS.ToCmdString());
                    break;
                case ECOMMAND_TYPE.W7:
                    writeCommand(w7.ToCmdString());
                    break;
                default:
                    WriteLineToConsole("SENDING INVALID COMMAND: " + type);
                    break;
            }

        }

        public void receiveCommand(string cmd)
        {

            string[] tokens = cmd.Replace("<", "").Replace(">", "").Split('_');

            bool is_receptionToken = tokens[tokens.Length - 1] == "OK";
            ECOMMAND_TYPE type;
            if (!Enum.TryParse(tokens[0], out type))
            {
                WriteLineToConsole("RECEIVED INVALID COMMAND: " + cmd);
                return;
            }

            if (type != ECOMMAND_TYPE.W7 & type != ECOMMAND_TYPE.WS)
            {
                windowParent.console_text.Text += "GW receive" + cmd + "\n";
                windowParent.console_user_text.Text += "GW receive" + cmd + "\n";
            }

            switch (type)
            {
                case ECOMMAND_TYPE.EA:
                    writeCommand("<EA_OK>");
                    sendCommand(ECOMMAND_TYPE.EP);
                    break;
                case ECOMMAND_TYPE.EP:
                    sendCommand(ECOMMAND_TYPE.ET);
                    break;
                case ECOMMAND_TYPE.ET:
                    sendCommand(ECOMMAND_TYPE.EM);
                    break;
                case ECOMMAND_TYPE.EM:
                    sendCommand(ECOMMAND_TYPE.ER);
                    break;
                case ECOMMAND_TYPE.ER:
                    sendCommand(ECOMMAND_TYPE.EV);
                    break;
                case ECOMMAND_TYPE.EV:
                    sendCommand(ECOMMAND_TYPE.ED);
                    break;
                case ECOMMAND_TYPE.ED:
                    sendCommand(ECOMMAND_TYPE.EU);
                    break;
                case ECOMMAND_TYPE.EU:
                    sendCommand(ECOMMAND_TYPE.EI);
                    break;
                case ECOMMAND_TYPE.EI:
                    sendCommand(ECOMMAND_TYPE.EZ);
                    break;
                case ECOMMAND_TYPE.EZ:
                    bIsSendingWS = true;
                    bIsConnected = true;
                    break;
                case ECOMMAND_TYPE.CP:
                    int res;
                    if (int.TryParse(tokens[1], out res)) {
                        if (res > 0)
                        {
                            w7 = new W7();
                            bIsSendingW7 = false;
                        }
                        else if (res == 0)
                        {
                            bIsSendingW7 = true;
                        }
                    }

                    sendCommand(ECOMMAND_TYPE.CP);
                    break;

                case ECOMMAND_TYPE.WT:
                    writeCommand("<WT_OK>");
                    break;
                case ECOMMAND_TYPE.AT:
                    bIsConnected = false;
                    writeCommand("<AT_OK>");
                    break;
                case ECOMMAND_TYPE.CR:
                    string[] str_res = tokens[1].Split(',');
                    int cr = Constants.MIN_INCLINE;
                    if (int.TryParse(str_res[0], out cr)) { w7.incline = cr/10; }
                    break;
                case ECOMMAND_TYPE.CS:
                    str_res = tokens[1].Split(',');
                    double cs = Constants.MIN_SPEED;
                    if (double.TryParse(str_res[0], out cs)) { w7.speed = cs/10; }
                    break;
                case ECOMMAND_TYPE.CM:
                    str_res = tokens[1].Split(',');
                    int cm = Constants.MIN_PRESSURE;
                    if (int.TryParse(str_res[0], out cm)) { wS.reserve_value_3 = cm; }
                    break;

                case ECOMMAND_TYPE.W7:
                case ECOMMAND_TYPE.WS:
                    break;

                default:
                    WriteLineToConsole("GW reception - " + type + " not implemented");
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
