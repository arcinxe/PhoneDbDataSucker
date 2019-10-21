using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneDbDataSucker
{
    class Program
    {

        static void Main(string[] args)
        {
            Downloader.GetAllPages();
            Console.WriteLine("\ngud");
        }

    }
}
