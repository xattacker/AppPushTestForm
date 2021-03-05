using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Xattacker.Utility.Json;

namespace FCM.Sender
{
    public enum DeviceType
    {
        Android,
        iOS
    }


    public class FCMSender
    {
        public void SendPushNotification<T>
        (
        FCMAttributes attribue,
        string deviceToken,
       // DeviceType deviceType,
        T noticationData,
        out string response
        ) where T : FCMNotificationData
        {
            List<string> tokens = new List<string>();
            tokens.Add(deviceToken);

            this.SendPushNotification(attribue, tokens, noticationData, out response);
        }

        public void SendPushNotification<T>
        (
        FCMAttributes attribue,
        List<string> deviceTokens,
       // DeviceType deviceType,
        T noticationData,
        out string response
        ) where T : FCMNotificationData
        {
            try
            {
                WebRequest tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
                tRequest.Method = "POST";
                tRequest.ContentType = "application/json";

                FCMNotification<T> notification = new FCMNotification<T>();
                notification.RegIds = deviceTokens;
                notification.Data = noticationData;
                notification.Priority = attribue.Priority;
                notification.CollapseKey = attribue.CollapseKey;

                noticationData.Category = attribue.CollapseKey;

               // if (deviceType == DeviceType.iOS)
               // {
                    FCMSubNotification sub_noti = new FCMSubNotification();
                    sub_noti.Title = noticationData.Title;
                    sub_noti.Body = noticationData.Body;
                    sub_noti.Sound = "default";
                    notification.Notification = sub_noti;
               // }

                var json = JsonUtility.SerializeToJson(notification);
  
                byte[] byteArray = Encoding.UTF8.GetBytes(json);
                tRequest.Headers.Add(string.Format("Authorization: key={0}", attribue.AppId));
                tRequest.Headers.Add(string.Format("Sender: id={0}", attribue.SenderId));
                tRequest.ContentLength = byteArray.Length;

                using (Stream dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);

                    using (Stream dataStreamResponse = tRequest.GetResponse().GetResponseStream())
                    {
                        using (StreamReader tReader = new StreamReader(dataStreamResponse))
                        {
                            response = tReader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }


    public class FCMAttributes
    {
        public string AppId { get; set; }

        public string SenderId { get; set; }

        public string Priority { get; set; }

        public string CollapseKey { get; set; }
    }
}
