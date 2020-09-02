using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.MailSlot.TestServer {
    class Program {
        static async Task Main(string[] args) {
            var buffer = new byte[16384];
            using (var server = MailSlot.CreateServer("test1")) {
                while (true) {
                    var count = await server.ReadAsync(buffer, 0, buffer.Length);
                    var msg = Encoding.UTF8.GetString(buffer, 0, count);
                    Console.WriteLine(msg);
                }
            }
        }

        // Synchronous Version, not recommended
        //static void Main(string[] args) {
        //    var buffer = new byte[16384];
        //    using (var server = MailSlot.CreateServer("test1")) {
        //        while (true) {
        //            int timeout;
        //            var (size, msgCount) = MailSlot.GetServerInfo(server.SafeFileHandle, out timeout);
        //            if (size == null)
        //                Thread.Sleep(1000);
        //            var count = server.Read(buffer);
        //            var msg = Encoding.UTF8.GetString(buffer, 0, count);
        //            Console.WriteLine(msg);
        //        }
        //    }
        //}
    }
}
