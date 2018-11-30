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
        [DataMember(Name = "certificateInfo")]
        public APNSCertificateInfo CertificateInfo { get; set; }

        [DataMember(Name = "authInfo")]
        public APNSAuthInfo AuthInfo { get; set; }

        // true = production server, false = sandbox server
        [DataMember(Name = "serverMode")]
        public bool ServerMode { get; set; }
    }

    [DataContract]
    public class APNSCertificateInfo : DataRecord
    {
        [DataMember(Name = "certFilePath")]
        public string CertificateFilePath { get; set; }

        [DataMember(Name = "certPwd")]
        public string CertificatePassword { get; set; }
    }

    [DataContract]
    public class APNSAuthInfo : DataRecord
    {
        [DataMember(Name = "keyFilePath")]
        public string KeyFilePath { get; set; }

        [DataMember(Name = "keyID")]
        public string KeyID { get; set; }

        [DataMember(Name = "teamID")]
        public string TeamID { get; set; }

        [DataMember(Name = "bundleID")]
        public string BundleID { get; set; }
    }
}
