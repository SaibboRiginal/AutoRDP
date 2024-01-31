using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace SpikeAutoRDP
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Process.Start("cmd", "/c mstsc /v:192.168.56.104 /w:1920 /h:1080");

            // Add a .txt file with the credentials to login
            // with property "Copy to Output directory" set to "copy",
            // the content is expected to have 2 rows:
            // [0] user name
            // [1] password
            var creds = File.ReadAllLines("credentials.txt");

            var autoRDP = new AutoRDP.AutoRDP();
            await autoRDP.LoginAsync(creds[0], creds[1], 10);
        }
    }
}
