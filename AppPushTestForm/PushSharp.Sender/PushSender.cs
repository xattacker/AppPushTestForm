using System;
using System.Collections.Generic;
using System.Threading;

using Newtonsoft.Json.Linq;

using PushSharp.Apple;
using Xattacker.Utility.Json;

namespace PushSharp.Sender
{
    // 負責推播的類別
    public class PushSender
    {
        #region 使用 PushSharp API 發送 push notification

        public void PushMessage <T>
        (
        string deviceToken,
        T message,
        string certificateFile,
        string certificatePwd,
        bool serverMode // true = production server, false = sandbox server
        ) where T : PushMessage
        {
            List<string> tokens = new List<string>();
            tokens.Add(deviceToken);

            this.PushMessage(tokens, message, certificateFile, certificatePwd, serverMode);
        }

        public void PushMessage <T>
        (
        List<string> deviceTokens,
        T message,
        string certificateFile,
        string certificatePwd,
        bool serverMode // true = production server, false = sandbox server
        ) where T : PushMessage
        {
            int UNIT = 100;
            int count = deviceTokens.Count;
            ApnsHttp2Configuration.ApnsServerEnvironment mode = serverMode ?
                                                                  ApnsHttp2Configuration.ApnsServerEnvironment.Production :
                                                                ApnsHttp2Configuration.ApnsServerEnvironment.Sandbox;

            string playload = JsonUtility.SerializeToJson(message);

            if (count < UNIT)
            {
                // 少於一定數量, 只建立一個連線來送
                this.PushMessage1Pri
                (
                deviceTokens,
                playload,
                certificateFile,
                certificatePwd,
                mode
                );
            }
            else
            {
                int size = count / UNIT;
                if (count % UNIT > 0)
                {
                    size += 1;
                }

                int added = 0;

                for (int i = 0; i < size; i++)
                {
                    int current_size =  (added + UNIT > count) ? count - added : UNIT;
                    List<string> temp = deviceTokens.GetRange(i * UNIT, current_size);

                    this.PushMessage1Pri
                    (
                    temp,
                    playload,
                    certificateFile,
                    certificatePwd,
                    mode
                    );

                    added += current_size;
                }
            }
        }

        // 取得已失效 device token 清單
        public void FeedbackCheck
        (
        string certificateFile,
        string certificatePwd,
        bool serverMode // true = production server, false = sandbox server
        )
        {
            lock (this)
            {
                try
                {
                    var mode = serverMode ? ApnsHttp2Configuration.ApnsServerEnvironment.Production : ApnsHttp2Configuration.ApnsServerEnvironment.Sandbox;
                    // Configuration
                    var config = new ApnsHttp2Configuration
                                 (
                                 mode, // app使用那一種profile就只能用那一模式去推, 否則會出錯
                                 certificateFile, // p12檔的完整路徑
                                 certificatePwd // p12檔匯出時輸入的密碼
                                 );

                    // Create a new broker
                    var broker = new ApnsHttp2ServiceBroker(config);

                    broker.OnNotificationFailed += (ApnsHttp2Notification notification, AggregateException exception) =>
                    {
                        Console.WriteLine("invalid token: " + notification.DeviceToken);
                    };

                    //broker.OnNotificationSucceeded += (ApnsHttp2Notification notification) =>
                    //{
                    //    Console.WriteLine("send succeed: " + notification.DeviceToken);
                    //};

                    broker.Start();

                    Monitor.Wait(this); // lock the current thread
                }
                catch (Exception ex)
                {
                    // send finish
                    lock (this)
                    {
                        Monitor.Pulse(this); // unlock the locked thread
                    }

                    throw ex;
                }
            }
        }

        private void Broker_OnNotificationFailed(ApnsHttp2Notification notification, AggregateException exception)
        {
            throw new NotImplementedException();
        }

        private void PushMessage1Pri
        (
        List<string> deviceTokens,
        string jsonPlayload,
        string certificateFile,
        string certificatePwd,
        ApnsHttp2Configuration.ApnsServerEnvironment mode
        )
        {
            lock (this)
            {
                ApnsHttp2ServiceBroker broker = null;
                int count = deviceTokens.Count;
                int index = 0;

                try
                {
                    // Configuration
                    var config = new ApnsHttp2Configuration
                                    (
                                    mode, // app使用那一種profile就只能用那一模式去推, 否則會出錯
                                    certificateFile, // p12檔的完整路徑
                                    certificatePwd // p12檔匯出時輸入的密碼
                                    );

                    // Create a new broker
                    broker = new ApnsHttp2ServiceBroker(config);

                    // Wire up events
                    broker.OnNotificationFailed += (notification, aggregateEx) =>
                    {
                        aggregateEx.Handle(ex =>
                        {
                            // 傳送失敗有錯誤
                            // See what kind of exception it was to further diagnose
                            if (ex is ApnsNotificationException)
                            {
                                var apnsEx = ex as ApnsNotificationException;

                                // Deal with the failed notification
                                var n = apnsEx.Notification;

                                Console.WriteLine("Notification Failed: ID={n.Identifier}, Code={apnsEx.ErrorStatusCode}");
                                Console.WriteLine(apnsEx.ErrorStatusCode);
                            }
                            else if (ex is ApnsConnectionException)
                            {
                                // Something failed while connecting (maybe bad cert?)
                                Console.WriteLine("Notification Failed (Bad APNS Connection)!");
                            }
                            else
                            {
                                Console.WriteLine("Notification Failed (Unknown Reason)!");
                            }

                            if (index < count - 1)
                            {
                                // send to next device token 
                                Console.WriteLine("send to next device token");

                                index++;
                                this.SendPlayload(broker, deviceTokens[index], jsonPlayload);
                            }
                            else
                            {
                                // send finish
                                lock (this)
                                {
                                    Monitor.Pulse(this); // unlock the locked thread
                                }
                            }

                            // Mark it as handled
                            return true;
                        });
                    };


                    broker.OnNotificationSucceeded += (notification) =>
                    {
                        // 傳送成功
                        Console.WriteLine("Notification Sent!");

                        if (index < count - 1)
                        {
                            // send to next device token 
                            Console.WriteLine("send to next device token");

                            index++;
                            this.SendPlayload(broker, deviceTokens[index], jsonPlayload);
                        }
                        else
                        {
                            // send finish
                            lock (this)
                            {
                                Monitor.Pulse(this); // unlock the locked thread
                            }
                        }
                    };

                    // Start the broker
                    broker.Start();

                    // send playload
                    this.SendPlayload(broker, deviceTokens[index], jsonPlayload);

                    Monitor.Wait(this); // lock the current thread
                }
                catch (Exception ex)
                {
                    // send finish
                    lock (this)
                    {
                        Monitor.Pulse(this); // unlock the locked thread
                    }

                    throw ex;
                }

                // Stop the broker, wait for it to finish   
                // This isn't done after every message, but after you're
                // done with the broker
                if (broker != null)
                {
                    broker.Stop();
                }
            }
        }

        private void SendPlayload(ApnsHttp2ServiceBroker broker, string deviceToken, string deviceTokens)
        {
            // Queue a notification to send
            ApnsHttp2Notification notification = new ApnsHttp2Notification();
            notification.DeviceToken = deviceToken;
            notification.Payload = JObject.Parse(deviceTokens);
            broker.QueueNotification(notification);
        }

        #endregion
    }
}
