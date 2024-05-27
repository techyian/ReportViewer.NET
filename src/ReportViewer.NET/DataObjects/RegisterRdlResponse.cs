using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportViewer.NET.DataObjects
{
    public class RegisterRdlResponse
    {
        public short ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public ReportRDL ParsedRdl { get; set; }
    }
}
