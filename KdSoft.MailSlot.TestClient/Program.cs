using System.Text;

namespace KdSoft.MailSlot.TestClient
{
    class Program
    {
        static void Main(string[] args) {
            var buffer = new byte[16384];
            using (var client = MailSlot.CreateMailSlotClient("test1")) {
                for (int indx = 0; indx < 50; indx++) {
                    var bytes = Encoding.UTF8.GetBytes($"Writing line #{indx}.\n");
                    client.Write(bytes);
                }
            }
        }
    }
}
