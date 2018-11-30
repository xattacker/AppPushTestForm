using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AppPush.Data
{
    [DataContract]
    public class DataRecord
    {
        [DataMember(Name = "tokens")]
        public List<string> DeviceTokens { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}
