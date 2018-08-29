#define EMULATE_GW

using System;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;


namespace mly_tm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


#if EMULATE_GW
        public TM_APP tm_app;
        public GW_emulator emulatedGW;
#else //Emulate GW
        TM_APP tm_app;
#endif
        UserData user;

        DispatcherTimer clock;

        public MainWindow()
        {
            InitializeComponent();
            
#if EMULATE_GW
            emulatedGW = new GW_emulator(this);
#endif //Emulate GW

            tm_app = new TM_APP(this);

            resetView();
            clock = new DispatcherTimer();
            clock.Tick += new EventHandler(OnConsoleUpdate);
            clock.Interval = new TimeSpan(0, 0, 1);
            clock.Start();
            user = new UserData();

            DateTime today = DateTime.Now;
            date_label.Content = today.ToShortDateString();

            for (int i = today.Year-1; i > today.Year-120; i--)
            {
                ComboBoxItem bi = new ComboBoxItem();
                bi.Content = (today.Year - i).ToString();
                tm_user_age_comboBox.Items.Add(bi);
            }
            for (int i = 1; i <= 150; i++)
            {
                ComboBoxItem bi = new ComboBoxItem();
                bi.Content = (i).ToString();
                tm_user_weight_comboBox.Items.Add(bi);
            }

        }

        private void resetView()
        {
            resetViewTargets();
            tm_state_text.Text = tm_app.ToStringPart1();
            tm_state_text2.Text = tm_app.ToStringPart2();
            debugLog_text.Text = tm_app.console_msg;
            HEARTBEAT_GRID.Visibility = (tm_app.bIs_cardio_mode) ? Visibility.Visible:Visibility.Hidden;
        }

        private void resetViewTargets()
        {
            speed_label_value.Content = tm_app.speed_MIN.ToString("N1") + " km/h";
            incline_label_value.Content = tm_app.incline_MIN + " deg";
            pressure_label_value.Content = Constants.MIN_PRESSURE.ToString("N1") + " dpi";
            max_heartbeat_label_value.Content = Constants.MIN_LIM_HEARTBEAT.ToString("N1") + " bpm";
            heartbeat_slider.Value = 0;
            pressure_slider.Value = 0;
        }

        private void OnConsoleUpdate(object source, EventArgs e)
        {
            tm_state_text.Text = tm_app.ToStringPart1();
            tm_state_text2.Text = tm_app.ToStringPart2();
            debugLog_text.Text = tm_app.console_msg;
            tm_user.Text = user.ToString();
            tm_loggedUser.Text = user.ToString();
        }


        private void speed_increase(object sender, RoutedEventArgs e)
        {
            tm_app.changeSpeedTarget(0.1f);
            speed_label_value.Content = tm_app.speed_target.ToString("N1") + " km/h";
        }

        private void speed_decrease(object sender, RoutedEventArgs e)
        {
            tm_app.changeSpeedTarget(-0.1f);
            speed_label_value.Content = tm_app.speed_target.ToString("N1") + " km/h";
        }

        private void incline_increase(object sender, RoutedEventArgs e)
        {
            tm_app.changeInclineTarget(1);
            incline_label_value.Content = tm_app.incline_target.ToString() + " deg";
        }

        private void incline_decrease(object sender, RoutedEventArgs e)
        {
            tm_app.changeInclineTarget(-1);
            incline_label_value.Content = tm_app.incline_target.ToString() + " deg";
        }

        private void ONOFF(object sender, RoutedEventArgs e)
        {
            if (!tm_app.is_running)
            {
                onoff_button.Content = "STOP";
                tm_app.StartRunning();
            }
            else
            {
                onoff_button.Content = "START";
                tm_app.StopRunning();
                tm_app.Reset();
                resetViewTargets();
            }
        }

        private void OnPressureTargetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tm_app.changePressureTarget(pressure_slider.Value*0.1);
            pressure_label_value.Content = tm_app.pressure_target.ToString() + " dpi";
        }

        private void OnMaxHeartbeatTargetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tm_app.changeMaxHeartbeatTarget(heartbeat_slider.Value*0.1);
            max_heartbeat_label_value.Content = tm_app.heartbeat_limit.ToString() + " bpm";
        }

        private void cardioMode_checkBox_Checked(object sender, RoutedEventArgs e)
        {
            bool b = (sender as CheckBox).IsChecked.Value;
            HEARTBEAT_GRID.Visibility = b ? Visibility.Visible : Visibility.Hidden; 
            tm_app.ToggleCardioMode(b);
        }


        // Commands events
        //Incline
        private void OnUpdateIncline(object sender, TouchEventArgs e)
        {
            tm_app.sendCommand(ECOMMAND_TYPE.CR);
        }
        private void OnUpdateIncline(object sender, MouseButtonEventArgs e)
        {
            tm_app.sendCommand(ECOMMAND_TYPE.CR);
        }
        //Speed
        private void OnUpdateSpeed(object sender, TouchEventArgs e)
        {
            tm_app.sendCommand(ECOMMAND_TYPE.CS);
        }
        private void OnUpdateSpeed(object sender, MouseButtonEventArgs e)
        {
            tm_app.sendCommand(ECOMMAND_TYPE.CS);
        }
        //Pressure
        private void OnUpdatePressure(object sender, TouchEventArgs e)
        {
            tm_app.sendCommand(ECOMMAND_TYPE.CM);
        }
        private void OnUpdatePressure(object sender, MouseButtonEventArgs e)
        {
            tm_app.sendCommand(ECOMMAND_TYPE.CM);
        }

        //Heartbeat 
        private void OnUpdateHeartbeat(object sender, TouchEventArgs e)
        {
            tm_app.sendCommand(ECOMMAND_TYPE.CM);
        }
        private void OnUpdateHeartbeat(object sender, MouseButtonEventArgs e)
        {
            tm_app.sendCommand(ECOMMAND_TYPE.CT);
        }

        public class RadioButtonCheckedConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                return value.Equals(parameter);
            }

            public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                return value.Equals(true) ? parameter : Binding.DoNothing;
            }
        }

        private void sex_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton r = (RadioButton) sender;
            user.isMale = r.Content.ToString() == "Male";
        }

        private void tm_user_age_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            user.age = int.Parse(((ComboBoxItem)((ComboBox)sender).SelectedValue).Content.ToString());
        }

        private void tm_user_weight_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            user.weight = int.Parse(((ComboBoxItem)((ComboBox)sender).SelectedValue).Content.ToString());
        }

        private void tm_user_pass_button_Click(object sender, RoutedEventArgs e)
        {
           // if (tm_app.tm_state != ETM_STATE.CONNECTED) { return; }
            user_grid.Visibility = Visibility.Hidden;
        }
        private void tm_user_validate_button_Click(object sender, RoutedEventArgs e)
        {
            //if (tm_app.tm_state != ETM_STATE.CONNECTED) { return; }
            tm_app.user = user;
            tm_app.sendCommand(ECOMMAND_TYPE.CU);
            user_grid.Visibility = Visibility.Hidden;
        }

        private void console_text_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox) sender).ScrollToEnd();
        }
    }


}
