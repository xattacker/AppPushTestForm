using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Xattacker.Utility;
using Xattacker.Utility.Json;
using System.IO;
using FCM.Sender;

namespace AppPush
{
    /// <summary>
    /// AndroidFCMWindow.xaml 的互動邏輯
    /// </summary>
    public partial class AndroidFCMWindow : Window
    {
        private const string RECORD_NAME = "fcm_reocrd.json";
        private AndroidFCMRecord record;

        public AndroidFCMWindow()
        {
            InitializeComponent();

            string path = System.IO.Path.Combine(AppProperties.AppPath, RECORD_NAME);
            if (File.Exists(path))
            {
                // load last record
                try
                {
                    this.record = JsonUtility.DeserializeFromJson<AndroidFCMRecord>(File.ReadAllText(path));
                    this.senderIdTextBox.Text = this.record.SenderId;
                    this.appIdTextBox.Text = this.record.AppId;

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
                this.record = new AndroidFCMRecord();
            }

            this.record.SenderId = this.senderIdTextBox.Text;
            this.record.AppId = this.appIdTextBox.Text;


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
            try
            {
                FCMAttributes attribute = new FCMAttributes();
                attribute.SenderId = this.record.SenderId;
                attribute.AppId = this.record.AppId;

                FCMSender fcm_sender = new FCMSender();

                FCMNotificationData data = new FCMNotificationData();
                data.Title = "更新通知";
                data.Body = "有一則推播通知";

                string response = null; // 回應結果內容
                fcm_sender.SendPushNotification<FCMNotificationData>(attribute, this.record.DeviceTokens, data, out response);

                MessageBox.Show(response);
            }
            catch (Exception ex)
            {
                MessageBox.Show("error happen:" + ex.ToString());
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // click send
            if (this.senderIdTextBox.Text.Length == 0 || this.appIdTextBox.Text.Length == 0 || this.tokenListView.Items.Count == 0)
            {
                MessageBox.Show("SenderId, AppId and DeviceToken could not be empty !!");

                return;
            }


            this.SaveRecord();
            this.Send();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // click add device token event
            DeviceTokenEditWindow win = new DeviceTokenEditWindow();
            win.Callback = (string token) =>
                            {
                                TokenItem item = new TokenItem();
                                item.Token = token;
                                this.tokenListView.Items.Add(item);
                            };

            win.ShowDialog();
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
    }
}
