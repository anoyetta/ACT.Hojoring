using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VoiceTextWebAPI.Client
{
    public class VoiceTextException : InvalidOperationException
    {
        public HttpStatusCode StatusCode { get; set; }

        public VoiceTextException()
            : base()
        {
        }

        public VoiceTextException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            this.StatusCode = statusCode;
        }
    }
}
