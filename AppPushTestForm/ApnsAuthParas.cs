using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppPush
{
    public class ApnsAuthParas
    {
        public string KeyPath { get; set; }

        public string KeyID { get; set; }
        public string TeamID { get; set; }
        public string BundleID { get; set; }

        // true = production server, false = sandbox server
        public bool ServerMode { get; set; }
    }
}
