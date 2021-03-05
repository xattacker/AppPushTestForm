using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FCM.Sender
{
    // 推播用的訊息結構
    // 資料參考: https://developers.google.com/cloud-messaging/http-server-ref#downstream-http-messages-json
    [DataContract]
    public class FCMNotification<T> where T : FCMNotificationData
    {
        public FCMNotification()
        {
        }

        // 要推播的device token
        [DataMember(Name = "registration_ids")]
        public List<string> RegIds { get; set; }

        // 對應同樣名稱的訊息 Server只會留存最新的一個, 並且Server同時最多保留4組不同key的資料, 超過時會刪那一組無法確定
        [DataMember(Name = "collapse_key")]
        public string CollapseKey { get; set; }

        // 推播內容, 最多不可超過4k
        [DataMember(Name = "data")]
        public T Data { get; set; }

        // for iOS 推播顯示
        [DataMember(Name = "notification")]
        public FCMSubNotification Notification { get; set; }

        // 訊息優先權, 有 "normal", "high" 兩種, high的模式會試著讓休眠中的裝置去開啟網路來接收訊息, normal則不會如此, 若不帶此值預設為normal
        [DataMember(Name = "priority", IsRequired = false, EmitDefaultValue = false)]
        public string Priority { get; set; } 
    }

    [DataContract]
    public class FCMNotificationData
    {
        [DataMember(Name = "body")]
        public string Body { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "sound")]
        public string Sound { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; }
    }

    [DataContract]
    public class FCMSubNotification
    {
        [DataMember(Name = "body")]
        public string Body { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "sound")]
        public string Sound { get; set; }
    }
}
