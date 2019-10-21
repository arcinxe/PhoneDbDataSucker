using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneDbDataSucker {
    class Downloader {
        private static readonly Queue<Task> WaitingTasks = new Queue<Task>();
        private static readonly Dictionary<int, Task> RunningTasks = new Dictionary<int, Task>();
        public static int MaxRunningTasks = 20; // vary this to dynamically throttle launching new tasks 
        private static int _totalPages = 1;
        private static int _downloadedPages = 0;

        private readonly static HttpClient _client = new HttpClient();

        /// <summary>
        /// Gets a chosen page with up to 29 devices from PhoneDb.net and saves it to the Output/Pages folder
        /// </summary>
        /// <param name="firstDeviceNumber">Inclusive number of first device to include in page</param>
        public static void GetPage(int firstDeviceNumber) {
            // Console.ForegroundColor = ConsoleColor.Yellow;
            Console.ResetColor();
            System.Console.WriteLine($"\rpage-{(firstDeviceNumber):00000}.html started download" + new string(' ', 60));
            UpdateConsole();
            if (!Directory.Exists("Output/Pages")) {
                Directory.CreateDirectory("Output/Pages");
            }
            File.Create($"Output/Pages/page-{(firstDeviceNumber):00000}.temp");
            var pageResponse = _client.GetAsync($"http://phonedb.net/index.php?m=device&s=list&filter={firstDeviceNumber}").Result;
            // pageResponse.EnsureSuccessStatusCode();
            var pageContent = pageResponse.Content.ReadAsStringAsync().Result;
            File.WriteAllText($"Output/Pages/page-{(firstDeviceNumber):00000}.html", pageContent);
            File.Delete($"Output/Pages/page-{(firstDeviceNumber):00000}.temp");
            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"\rpage-{(firstDeviceNumber):00000}.html downloaded" + new string(' ', 60));
            Console.ResetColor();
            _downloadedPages++;
            UpdateConsole();
            // return pageContent.Result;
        }

        /// <summary>
        /// Fetches the main page of the PhoneDB.net and checks the amount of available devices
        /// </summary>
        /// <returns>The current number of devices at PhoneDB.net</returns>
        public static async Task<int> GetNumberOfDevices() {
            var mainPageResponse = await _client.GetAsync("http://phonedb.net/index.php?m=device&s=list");
            var mainPageContent = mainPageResponse.Content.ReadAsStringAsync().Result;
            var match = Regex.Match(mainPageContent, @"(\d+)-(\d+)<\/a> *\| <a rel=""nofollow"" +title=""Next page""");
            System.Console.WriteLine();
            var amountOfDevices = int.Parse(match.Groups[2].Value);

            return amountOfDevices;

        }

        /// <summary>
        /// Asynchronously calls GetPage method to retrive all links to datasheets of all devices from PhoneDb.net
        /// </summary>
        public static void GetAllPages() {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Worker.Done = new Worker.DoneDelegate(WorkerDone);

            _totalPages = (int) Math.Ceiling(((double) GetNumberOfDevices().Result / 29));
            _downloadedPages = 0;

            System.Console.WriteLine($"Total pages: {_totalPages}");
            for (int i = 0; i < _totalPages; i++) {
                var currentDevice = i * 29;
                WaitingTasks.Enqueue(new Task(id => new Worker().DoWork((int) id, () => GetPage(currentDevice), token), i, token));
            }
            LaunchTasks();
            // Console.ReadKey();
            while (RunningTasks.Count != 0)
                System.Threading.Thread.Sleep(500);
            UpdateConsole();
            if (RunningTasks.Count > 0) {
                lock(WaitingTasks) WaitingTasks.Clear();
                tokenSource.Cancel();
                // Console.ReadKey();
            }
        }

        /// <summary>
        /// Launches the enqueued tasks
        /// </summary>
        /// <returns></returns>
        static async void LaunchTasks() {
            // keep checking until we're done
            while ((WaitingTasks.Count > 0) || (RunningTasks.Count > 0)) {
                // launch tasks when there's room
                while ((WaitingTasks.Count > 0) && (RunningTasks.Count < MaxRunningTasks)) {
                    Task task = WaitingTasks.Dequeue();
                    lock(RunningTasks) RunningTasks.Add((int) task.AsyncState, task);
                    task.Start();
                }
                UpdateConsole();
                await Task.Delay(300); // wait before checking again
            }
            UpdateConsole(); // all done
        }

        static void UpdateConsole() {
            Console.Write($"\rProgress: {((double)_downloadedPages/_totalPages).ToString("P2", new NumberFormatInfo { PercentPositivePattern = 1, PercentNegativePattern = 1 })} [{_downloadedPages}/{_totalPages}], Waiting downloads: {WaitingTasks.Count:##0}  Running downloads: {RunningTasks.Count:##0} ");
        }

        // callback from finished worker
        static void WorkerDone(int id) {
            lock(RunningTasks) RunningTasks.Remove(id);
        }
    }

    internal class Worker {
        public delegate void DoneDelegate(int taskId);
        public static DoneDelegate Done { private get; set; }
        private static readonly Random Rnd = new Random();

        public async void DoWork(object id, Action action, CancellationToken token) {
            await Task.Run(action);
            Done((int) id);
        }
    }
}
