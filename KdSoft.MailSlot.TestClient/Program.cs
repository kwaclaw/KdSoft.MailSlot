using System.Text;
using System.Threading.Tasks;

namespace KdSoft.MailSlot.TestClient {
    class Program {
        static async Task Main(string[] args) {
            var buffer = new byte[16384];
            using (var client = MailSlot.CreateClient("test1")) {
                for (int indx = 0; indx < 50; indx++) {
                    var bytes = Encoding.UTF8.GetBytes($"Writing line #{indx}.\n");
                    await client.WriteAsync(bytes);
                }
            }
        }
    }
}
