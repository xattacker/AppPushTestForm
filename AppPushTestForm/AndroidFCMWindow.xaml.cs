using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.IO;

using AppPush.Data;
using Xattacker.Utility.Json;
using FCM.Sender;

namespace AppPush
{
    /// <summary>
    /// AndroidFCMWindow.xaml 的互動邏輯
    /// </summary>
    public partial class AndroidFCMWindow : Window
    {
        private AndroidFCMRecord record;

        public AndroidFCMWindow()
        {
            InitializeComponent();

            this.waitingProgressBar.Visibility = Visibility.Hidden;
            this.Title = "Android FCM";
            this.LoadRecord();
        }

        private void LoadRecord()
        {
            string path = this.RecordPath;
            if (File.Exists(path))
            {
                // load last record
                try
                {
                    this.record = JsonUtility.DeserializeFromJson<AndroidFCMRecord>(File.ReadAllText(path));
                    this.senderIdTextBox.Text = this.record.SenderId;
                    this.appIdTextBox.Text = this.record.AppId;
                    this.messageTextBox.Text = this.record.Message;

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

            this.record.Message = this.messageTextBox.Text;


            List<string> tokens = new List<string>();

            foreach (var obj in this.tokenListView.Items)
            {
                TokenItem item = (TokenItem)obj;
                tokens.Add(item.Token);
            }

            this.record.DeviceTokens = tokens;


            string json = this.record.SerializeToJson();
            string path = this.RecordPath;
            File.WriteAllText(path, json);
        }

        private void Send()
        {
           this.SendFCM();
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

        private void SendFCM()
        {
            Exception ex = null;
            string response = null; // 回應結果內容
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += delegate
                            {
                                try
                                {
                                    FCMAttributes attribute = new FCMAttributes();
                                    attribute.SenderId = this.record.SenderId;
                                    attribute.AppId = this.record.AppId;

                                    FCMSender fcm_sender = new FCMSender();

                                    FCMNotificationData data = new FCMNotificationData();
                                    data.Title = "push notification";
                                    data.Body = this.record.Message;

                                    fcm_sender.SendPushNotification<FCMNotificationData>(attribute, this.record.DeviceTokens, data, out response);
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
                                            else if (!string.IsNullOrEmpty(response))
                                            {
                                                MessageBox.Show(response);
                                            }
                                        };

            worker.RunWorkerAsync();

            this.ShowWaitingBar(true);
        }

        private string RecordPath
        {
            get
            {
                string name = "fcm_reocrd.json";

                return System.IO.Path.Combine(AppProperties.AppPath, name);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // click send
            if (
               this.senderIdTextBox.Text.Length == 0 ||
               this.appIdTextBox.Text.Length == 0 ||
               this.messageTextBox.Text.Length == 0 ||
               this.tokenListView.Items.Count == 0
               )
            {
                MessageBox.Show("SenderId, AppId, Message and DeviceToken could not be empty !!");

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

        private void tokenListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.tokenListView.SelectedIndex >= 0)
            {
                MessageBoxResult result = MessageBox.Show("delete this token?", "", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    TokenItem item = (TokenItem)this.tokenListView.SelectedItem;
                    this.tokenListView.Items.Remove(item);
                }
            }
        }
    }
}
