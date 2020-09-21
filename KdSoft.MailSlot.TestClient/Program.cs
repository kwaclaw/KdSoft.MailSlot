using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace KdSoft.MailSlot.TestClient {
    class Program {
        const byte MessageSeparator = 0x03;

        static async Task Main(string[] args) {
            var buffer = new byte[1024];
            try {
                using (var client = MailSlot.CreateClient("test1")) {
                    for (int indx = 0; indx < 50; indx++) {
                        var msg = $"Writing line #{indx}.";
                        // var count = Encoding.UTF8.GetBytes(msg, 0, msg.Length, buffer, 0);
                        var count = Encoding.UTF8.GetBytes(msg, buffer);
                        buffer[count++] = MessageSeparator;
                        await Task.Delay(100);  // small delay between messages
                        await client.WriteAsync(buffer, 0, count);

                        // This leads to "Incorrect Function" error on full framework:
                        //     client.Flush(true) or await client.FlushAsync();
                        client.Flush(false);
                    }
                }
            }
            catch (EndOfStreamException) {
                Console.WriteLine("Server closed mail slot.");
                Console.ReadLine();
            }
        }
    }
}
