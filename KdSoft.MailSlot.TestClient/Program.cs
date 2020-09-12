using System.Text;
using System.Threading.Tasks;

namespace KdSoft.MailSlot.TestClient {
    class Program {
        const byte MessageSeparator = 0x03;

        static async Task Main(string[] args) {
            var buffer = new byte[16384];
            using (var client = MailSlot.CreateClient("test1")) {
                for (int indx = 0; indx < 50; indx++) {
                    var count = Encoding.UTF8.GetBytes($"Writing line #{indx}.", buffer);
                    buffer[count++] = MessageSeparator;
                    await client.WriteAsync(buffer, 0, count);
                }
            }
        }
    }
}
