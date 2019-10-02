using AppPush.Data;
using Microsoft.Win32;
using PushSharp.Sender;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
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
            this.LoadRecord();
        }

        private void LoadRecord()
        {
            string path = System.IO.Path.Combine(AppProperties.AppPath, RECORD_NAME);
            if (File.Exists(path))
            {
                // load last record
                try
                {
                    this.record = Xattacker.Utility.Json.JsonUtility.DeserializeFromJson<IOS_APNSRecord>(File.ReadAllText(path));

                    if (this.record.CertificateInfo != null)
                    {
                        this.certFilePathTextBox.Text = this.record.CertificateInfo.CertificateFilePath;
                        this.certPwdTextBox.Text = this.record.CertificateInfo.CertificatePassword;
                    }

                    if (this.record.AuthInfo != null)
                    {
                        this.keyFilePathTextBox.Text = this.record.AuthInfo.KeyFilePath;
                        this.keyIDTextBox.Text = this.record.AuthInfo.KeyID;
                        this.teamIDTextBox.Text = this.record.AuthInfo.TeamID;
                        this.bundleIDTextBox.Text = this.record.AuthInfo.BundleID;
                    }


                    this.messageTextBox.Text = this.record.Message;


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
                    MessageBox.Show("Load Record Failed");
                }
            }
        }

        private void SaveRecord()
        {
            if (this.record == null)
            {
                this.record = new IOS_APNSRecord();
            }


            APNSCertificateInfo cert_info = new APNSCertificateInfo();
            cert_info.CertificateFilePath = this.certFilePathTextBox.Text;
            cert_info.CertificatePassword = this.certPwdTextBox.Text;
            this.record.CertificateInfo = cert_info;

            APNSAuthInfo auth_info = new APNSAuthInfo();
            auth_info.KeyFilePath = this.keyFilePathTextBox.Text;
            auth_info.KeyID = this.keyIDTextBox.Text;
            auth_info.TeamID = this.teamIDTextBox.Text;
            auth_info.BundleID = this.bundleIDTextBox.Text;
            this.record.AuthInfo = auth_info;

            this.record.ServerMode = this.productionRadioButton.IsChecked ?? false;

            this.record.Message = this.messageTextBox.Text;


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

        private void SendByP12()
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
                                        msg.Aps.Alert = this.record.Message;
                                        // msg.Aps.ContentAvailable = 0;

                                        if (this.record.CertificateInfo != null)
                                        { 
                                            PushSender sender = new PushSender();

                                            sender.PushMessage
                                            (
                                            this.record.DeviceTokens, // device token
                                            msg, // push message, 不可超過2kb
                                            this.record.CertificateInfo.CertificateFilePath, // p12授權檔路徑
                                            this.record.CertificateInfo.CertificatePassword, // 授權檔密碼
                                            this.record.ServerMode // 推送到測試(false)或正式(true)server 
                                            );
                                        }
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

        private void SendByP8()
        {
            try
            {
                // 產生 push playload
                PushMessage msg = new PushMessage();
                msg.Aps.Badge = 1;
                msg.Aps.Sound = "default";
                msg.Aps.Alert = this.record.Message;
                // msg.Aps.ContentAvailable = 0;

                ApnsTBASender sender = new ApnsTBASender();

                ApnsAuthParas paras = new ApnsAuthParas();
                paras.KeyPath = this.record.AuthInfo.KeyFilePath;
                paras.KeyID = this.record.AuthInfo.KeyID;
                paras.TeamID = this.record.AuthInfo.TeamID;
                paras.BundleID = this.record.AuthInfo.BundleID;

                sender.Push(
                    paras,
                    msg, 
                    this.record.DeviceTokens,
                    (bool succeed, string response) =>
                    {
                        // delegate invoke for async callback
                        Application.Current.Dispatcher.Invoke(
                            new Action(() => {
                                this.ShowWaitingBar(false);

                                MessageBox.Show(response);
                            }));
                    },
                    (Exception ex) =>
                    {
                        // delegate invoke for async callback
                        Application.Current.Dispatcher.Invoke(
                            new Action(() => {
                                this.ShowWaitingBar(false);

                                 MessageBox.Show("error happen:" + ex.ToString());
                            }));
                    }
                    );

                this.ShowWaitingBar(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("error happen:" + ex.ToString());
            }
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

        // click to send
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.typeTabCtrl.SelectedIndex == 0)
            {
                if (
                   this.certFilePathTextBox.Text.Length == 0 || 
                   this.certPwdTextBox.Text.Length == 0 ||
                   this.messageTextBox.Text.Length == 0 || 
                   this.tokenListView.Items.Count == 0
                   )
                {
                    MessageBox.Show("Cert file, Cert password, Message and DeviceToken could not be empty !!");

                    return;
                }


                this.SaveRecord();
                this.SendByP12();
            }
            else
            {
                if (
                   this.keyFilePathTextBox.Text.Length == 0 || 
                   this.keyIDTextBox.Text.Length == 0 ||
                   this.teamIDTextBox.Text.Length == 0 ||
                   this.bundleIDTextBox.Text.Length == 0 ||
                   this.messageTextBox.Text.Length == 0 ||
                   this.tokenListView.Items.Count == 0
                   )
                {
                    MessageBox.Show("Auth Key file, KeyID, TeamID, BundleID, Message and DeviceToken could not be empty !!");

                    return;
                }


                this.SaveRecord();
                this.SendByP8();
            }
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
           "select file (*.p12)|*.p12",
            (string filePath) =>
            {
                this.certFilePathTextBox.Text = filePath;
            }
            );
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // click select certificate file path
            this.OpenFileDialog
            (
            "select file (*.p8)|*.p8",
            (string filePath) =>
            {
                this.keyFilePathTextBox.Text = filePath;
            }
            );
        }

        private void OpenFileDialog(string filter, Action<string> callback)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = filter;

            if (dialog.ShowDialog() == true)
            {
                callback(dialog.FileName);
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
