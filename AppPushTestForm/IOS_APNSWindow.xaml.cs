using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using AppPush.Data;
using FCM.Json;
using Microsoft.Win32;
using PushSharp.Sender;
using Xattacker.Utility.Json;

namespace AppPush
{
    /// <summary>
    /// IOS_APNSWindow.xaml 的互動邏輯
    /// </summary>
    public partial class IOS_APNSWindow : Window
    {
        private const string RECORD_NAME = "apns_reocrd.json";
        private IOS_APNSRecord record;

        public IOS_APNSWindow()
        {
            InitializeComponent();

            this.waitingProgressBar.Visibility = Visibility.Hidden;

            string path = System.IO.Path.Combine(AppProperties.AppPath, RECORD_NAME);
            if (File.Exists(path))
            {
                // load last record
                try
                {
                    this.record = Xattacker.Utility.Json.JsonUtility.DeserializeFromJson<IOS_APNSRecord>(File.ReadAllText(path));
                    this.certFilePathTextBox.Text = this.record.CertificateFilePath;
                    this.certPwdTextBox.Text = this.record.CertificatePassword;

                    if (this.record.ServerMode)
                    {
                        this.productionRadioButton.IsChecked = true;
                    }
                    else
                    {
                        this.sandboxRadioButton.IsChecked = true;
                    }

                    if (this.record.DeviceTokens != null)
                    {
                        foreach (string token in this.record.DeviceTokens)
                        {
                            TokenItem item = new TokenItem();
                            item.Token = token;
                            this.tokenListView.Items.Add(item);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private void SaveRecord()
        {
            if (this.record == null)
            {
                this.record = new IOS_APNSRecord();
            }

            this.record.CertificateFilePath = this.certFilePathTextBox.Text;
            this.record.CertificatePassword = this.certPwdTextBox.Text;
            this.record.ServerMode = this.productionRadioButton.IsChecked ?? false;


            List<string> tokens = new List<string>();

            foreach (var obj in this.tokenListView.Items)
            {
                TokenItem item = (TokenItem)obj;
                tokens.Add(item.Token);
            }

            this.record.DeviceTokens = tokens;


            string json = this.record.SerializeToJson();
            string path = System.IO.Path.Combine(AppProperties.AppPath, RECORD_NAME);

            File.WriteAllText(path, json);
        }

        private void Send()
        {
            Exception ex = null;
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += delegate
                                {
                                    try
                                    {
                                        // 產生 push playload
                                        PushMessage msg = new PushMessage();
                                        msg.Aps.Badge = 1;
                                        msg.Aps.Sound = "default";
                                        msg.Aps.Alert = "有一則推播通知";
                                        // msg.Aps.ContentAvailable = 0;

                                        PushSender sender = new PushSender();

                                        sender.PushMessage
                                        (
                                        this.record.DeviceTokens, // device token
                                        msg, // push message, 不可超過2kb
                                        this.record.CertificateFilePath, // p12授權檔路徑
                                        this.record.CertificatePassword, // 授權檔密碼
                                        this.record.ServerMode // 推送到測試(false)或正式(true)server 
                                        );
                                    }
                                    catch (Exception ex2)
                                    {
                                        ex = ex2;
                                    }
                                };

            worker.RunWorkerCompleted += delegate
                                        {
                                            this.ShowWaitingBar(false);

                                            if (ex != null)
                                            {
                                                MessageBox.Show("error happen:" + ex.ToString());
                                            }
                                        };

            worker.RunWorkerAsync();

            this.ShowWaitingBar(true);
        }

        private void ShowWaitingBar(bool show)
        {
            if (show)
            {
                this.IsEnabled = false;
                this.waitingProgressBar.Visibility = Visibility.Visible;
            }
            else
            {
                this.IsEnabled = true;
                this.waitingProgressBar.Visibility = Visibility.Hidden;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // click add device token
            DeviceTokenEditWindow win = new DeviceTokenEditWindow();
            win.Callback = (string token) =>
                            {
                                TokenItem item = new TokenItem();
                                item.Token = token;
                                this.tokenListView.Items.Add(item);
                            };

            win.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // click send
            if (this.certFilePathTextBox.Text.Length == 0 || this.certPwdTextBox.Text.Length == 0 || this.tokenListView.Items.Count == 0)
            {
                MessageBox.Show("Cert file, Cert password and DeviceToken could not be empty !!");

                return;
            }


            this.SaveRecord();
            this.Send();
        }

        // click ListView item
        private void onListViewItemClick(object sender, MouseButtonEventArgs e)
        {
            if (this.tokenListView.SelectedIndex >= 0)
            {
                TokenItem item = (TokenItem)this.tokenListView.SelectedItem;

                DeviceTokenEditWindow win = new DeviceTokenEditWindow(item.Token);
                win.Callback = (string token) =>
                                {
                                    item.Token = token;
                                    this.tokenListView.Items.Refresh();
                                };

                win.ShowDialog();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // click select certificate file path
            this.OpenFileDialog
            (
            (string filePath) =>
            {
                this.certFilePathTextBox.Text = filePath;
            }
            );
        }

        private void OpenFileDialog(Action<string> callback)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "select file (*.p12)|*.p12";

            if (dialog.ShowDialog() == true)
            {
                callback(dialog.FileName);
            }
        }
    }
}
