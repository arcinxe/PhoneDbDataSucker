using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace PhoneDbDataSucker.App {
    public static class Extractor {
        public static void ExtractAllDeviceSpecificationLinks() {
            File.Delete("Output/DeviceLinks.txt");
            var files = Directory.GetFiles("Output/Pages").OrderBy(n => n);
            var urls = new List<string>();
            foreach (var file in files) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine($"Extracting links from {file}");
                Console.ResetColor();
                var doc = new HtmlDocument();
                doc.Load(file);
                var devices = doc.DocumentNode.SelectNodes("/html/body/div[5]/div");

                foreach (var device in devices) {
                    try {
                        // Skip processing the ads
                        if (device.InnerHtml.Contains("adsbygoogle"))
                            continue;
                        var firstDiv = device.SelectSingleNode("div[1]/a");

                        var url = $"http://phonedb.net/{firstDiv.Attributes["href"].Value}";
                        var match = Regex.Match(url, @"&c=([a-z-_0-9,\.]+)$");
                        System.Console.WriteLine(match.Groups[1].Value);
                        urls.Add(url);

                    } catch (System.Exception e) {
                        System.Console.WriteLine(e.Message);
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"Extracted total of {urls.Count} links!");
            Console.ResetColor();
            File.AppendAllLines("Output/DeviceLinks.txt", urls);
        }
    }
}
