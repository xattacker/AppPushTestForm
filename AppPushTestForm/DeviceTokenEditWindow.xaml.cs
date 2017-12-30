using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AppPush
{
    /// <summary>
    /// DeviceTokenEditWindow.xaml 的互動邏輯
    /// </summary>
    public partial class DeviceTokenEditWindow : Window
    {
        public DeviceTokenEditWindow(string deviceToken = "")
        {
            InitializeComponent();

            this.tokenTextBox.Text = deviceToken;
        }

        ~DeviceTokenEditWindow()
        {
            this.Callback = null;
        }

        public Action<string> Callback { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // click send
            if (this.tokenTextBox.Text.Length == 0)
            {
                return;
            }

            if (this.Callback != null)
            {
                this.Callback(this.tokenTextBox.Text);
            }

            this.Close();
        }
    }
}
