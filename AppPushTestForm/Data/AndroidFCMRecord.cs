using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AppPush.Data
{
    [DataContract]
    public class AndroidFCMRecord : DataRecord
    {
        [DataMember(Name = "appId")]
        public string AppId { get; set; }

        [DataMember(Name = "senderId")]
        public string SenderId { get; set; }
    }
}
