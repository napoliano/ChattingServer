using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public static class Log
    {
        public static void Debug(string log)
        {
            Console.WriteLine(log);
        }

        public static void Error(in Exception ex, string log)
        {
            Console.WriteLine(ex.Message);
            Error(log);
        }

        public static void Error(string log)
        {
            Console.WriteLine(log);
        }
    }
}
