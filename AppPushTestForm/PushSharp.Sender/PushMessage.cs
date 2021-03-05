using System.Runtime.Serialization;

namespace PushSharp.Sender
{
    // 用來產生推播json 的類別
    [DataContract]
    public class PushMessage
    {
        public PushMessage()
        {
            this.Aps = new PushAps();
        }

        [DataMember(Name = "aps")]
        public PushAps Aps { get; set; }
    }

    [DataContract]
    public class PushAps
    {
        // 顯示於app icon 上的未讀數量
        [DataMember(Name = "badge")]
        public int Badge { get; set; }

        // 推播聲音提示模式, 有 default 或是可指定特定聲音檔(聲音檔需存在於app端或是iOS系統), 帶空字串則為無聲
        // https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Chapters/IPhoneOSClientImp.html#//apple_ref/doc/uid/TP40008194-CH103-SW6
        [DataMember(Name = "sound")]
        public string Sound { get; set; }

        // 推播訊息內容, 會呈現在iOS裝置的通知列上
        [DataMember(Name = "alert")]
        public string Alert { get; set; }

      //  [DataMember(Name = "content-available")]
       // public int ContentAvailable { get; set; }

        // 推播類型, 可自定義 Category, 讓app收到時根據此值來做對應的處理, 預設可不填
        [DataMember(Name = "category", IsRequired = false, EmitDefaultValue = false)]
        public string Category { get; set; }
    }
}
