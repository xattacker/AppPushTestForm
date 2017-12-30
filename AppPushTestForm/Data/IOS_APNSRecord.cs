using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AppPush.Data
{
    [DataContract]
    public class IOS_APNSRecord : DataRecord
    {
        [DataMember(Name = "certFilePath")]
        public string CertificateFilePath { get; set; }

        [DataMember(Name = "certPwd")]
        public string CertificatePassword { get; set; }

        // true = production server, false = sandbox server
        [DataMember(Name = "serverMode")]
        public bool ServerMode { get; set; }

    }
}
