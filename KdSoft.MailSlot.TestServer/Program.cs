using System;
using System.Text;
using System.Threading.Tasks;

namespace KdSoft.MailSlot.TestServer
{
    class Program
    {
        static async Task Main(string[] args) {
            var buffer = new byte[16384];
            using (var server = new MailSlot("test1")) {
                while (true) {
                    var count = await server.ReadAsync(buffer, 0, buffer.Length);
                    if (count == 0)
                        break;
                    var msg = Encoding.UTF8.GetString(buffer, 0, count);
                    Console.WriteLine(msg);
                }
            }
        }
    }
}
