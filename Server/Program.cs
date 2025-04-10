using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


namespace Server
{
    public class Program
    {
        static void Main()
        {
            var server = new Server();
            server.Start();
        }
    }
}