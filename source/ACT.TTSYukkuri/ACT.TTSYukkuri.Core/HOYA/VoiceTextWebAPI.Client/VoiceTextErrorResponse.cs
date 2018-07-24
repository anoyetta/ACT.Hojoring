using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceTextWebAPI.Client.Internal
{
    public class VoiceTextErrorResponse
    {
        public class VoiceTextError
        {
            public string message { get; set; }
        } 

        public VoiceTextError error { get; set; }
    }
}
