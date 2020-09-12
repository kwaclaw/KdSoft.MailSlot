using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace KdSoft.MailSlot.TestServer
{
    class Program {
        const byte MessageSeparator = 0x03;

        static async Task Main(string[] args) {
            var pipe = new Pipe();

            var readTask = ReadMail(pipe.Reader);

            using (var server = MailSlot.CreateServer("test1")) {
                var writer = pipe.Writer;

                while (true) {
                    var memory = writer.GetMemory(4096);
                    var count = await server.ReadAsync(memory);
                    if (count == 0)
                        break;

                    writer.Advance(count);
                    var writeResult = await writer.FlushAsync();
                    if (writeResult.IsCompleted)
                        break;
                }

                writer.Complete();
            }

            await readTask;
        }

        static async Task ReadMail(PipeReader reader) {
            while (true) {
                var readResult = await reader.ReadAsync();
                var buffer = readResult.Buffer;

                while (TryReadMessage(ref buffer, out var msgBytes)) {
                    ProcessMessage(ref msgBytes);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }

        static bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> msgBytes) {
            var pos = buffer.PositionOf(MessageSeparator);
            if (pos == null) {
                msgBytes = default;
                return false;
            }

            msgBytes = buffer.Slice(0, pos.Value);

            var nextStart = buffer.GetPosition(1, pos.Value);
            buffer = buffer.Slice(nextStart);
            return true;
        }

        static void ProcessMessage(ref ReadOnlySequence<byte> msgBytes) {
            var msg = Encoding.UTF8.GetString(ref msgBytes);
            Console.WriteLine(msg);
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
