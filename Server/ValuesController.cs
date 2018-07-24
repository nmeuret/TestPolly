using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;

namespace Server
{
    [RoutePrefix("api/test")]
    public class ValuesController : ApiController
    {
        static int _callCounter = 0;
        static int _failCounter = 0;
        const int FAILSTEP = 5;
        const int FAILNBR = 3;

        // GET api/values/code
        [System.Diagnostics.DebuggerHidden]
        [HttpGet]
        [Route("retries")]
        public string Retries()
        {
            if (++_callCounter > FAILSTEP)
            {
                if (++_failCounter > FAILNBR)
                {
                   _callCounter = 0;
                   _failCounter = 0;
                }
                else
                {
                    Console.WriteLine("Retries : Max Fails, Exception !");
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

            }

            Console.WriteLine($"Retries : OK, ");

            return "Retry OK";
        }

        [HttpGet]
        [Route("timeout")]
        public string Timeout()
        {
            Thread.Sleep(10000000);

            return "Timeout OK";
        }


        [System.Diagnostics.DebuggerHidden]
        [HttpGet]
        [Route("failure")]
        public string Failure()
        {
            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
