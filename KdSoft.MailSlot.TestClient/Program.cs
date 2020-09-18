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
                        var count = Encoding.UTF8.GetBytes($"Writing line #{indx}.", buffer);
                        buffer[count++] = MessageSeparator;
                        await Task.Delay(100);  // small delay between messages
                        await client.WriteAsync(buffer, 0, count);
                        await client.FlushAsync();
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
