using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.MailSlot.TestServer {
    class Program {
        const byte MessageSeparator = 0x03;

        static async Task Main(string[] args) {
            if (args.Length > 0 && args[0] == "async")
                await ListenAsync();
            else
                await Listen();
        }

        static int msgCount;
        static CancellationTokenSource tcs;

        static void ProcessMessage(ref ReadOnlySequence<byte> msgBytes) {
            if (msgCount++ > 20)
                tcs.Cancel();
            var msg = Encoding.UTF8.GetString(ref msgBytes);
            Console.WriteLine(msg);
        }

        static Task Listen() {
            msgCount = 0;
            tcs = new CancellationTokenSource();
            var listener = new MailSlotListener("test1", ProcessMessage, MessageSeparator, tcs.Token);
            return listener.Task;
        }

        static async Task ListenAsync() {
            var listener = new AsyncMailSlotListener("test1", 3);
            int msgCount = 0;
            var tcs = new CancellationTokenSource();

            await foreach (var msgBytes in listener.GetNextMessage(tcs.Token)) {
                if (msgCount++ > 20)
                    tcs.Cancel();
                var msg = Encoding.UTF8.GetString(msgBytes);
                Console.WriteLine(msg);
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
