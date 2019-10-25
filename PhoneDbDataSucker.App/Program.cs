using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace PhoneDbDataSucker.App {
    class Program {

        static void Main(string[] args) {

            if (args.Contains("-hl")) {
                var message = "--full\n--get-pages\n--update-list\n--get-all-details\n--get-failed-details";
                System.Console.WriteLine(message);
            }
            
            var everything = args.Contains("--full");

            if (everything || args.Contains("--get-pages"))
                Downloader.GetAllPages();

            if (everything || args.Contains("--update-list"))
                Extractor.ExtractAllDeviceSpecificationLinks();

            if (everything || args.Contains("--get-all-details"))
                Downloader.DownloadAllDeviceSpecificationPages("Output/DeviceLinks.txt");

            if (everything || args.Contains("--get-failed-details"))
                Downloader.DownloadAllDeviceSpecificationPages("Output/FailedDeviceLinks.txt");

            Console.WriteLine("\ngud");
        }

    }
}
