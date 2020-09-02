using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace KdSoft.MailSlot
{
    public class MailSlot: IDisposable
    {
        #region DLL Imports

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        static extern SafeFileHandle CreateMailslot(
            string lpName,
            uint nMaxMessageSize,
            uint lReadTimeout,
            IntPtr lpSecurityAttributes
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        static extern bool GetMailslotInfo(
            SafeFileHandle hMailslot,
            IntPtr lpMaxMessageSize,
            out uint lpNextSize,
            out uint lpMessageCount,
            IntPtr lpReadTimeout
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        static extern SafeFileHandle CreateFile(
            string lpFileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr SecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        const int FileFlagOverlapped = 0x40000000;

        #endregion

        public static FileStream CreateMailSlotClient(string name, string domain = ".") {
            var mailSlotUncName = $@"\\{domain}\mailslot\{name}";
            var handle = CreateFile(mailSlotUncName, FileAccess.Write, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileFlagOverlapped, IntPtr.Zero);
            if (handle.IsInvalid)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            try {
                return new FileStream(handle, FileAccess.Write, 4096, true);
            }
            catch {
                handle.Dispose();
                throw;
            }
        }

        readonly FileStream _stream;

        public MailSlot(string name) {
            var handle = CreateMailSlotHandle(name);
            try {
                _stream = new FileStream(handle, FileAccess.Read, 4096, true);
            }
            catch {
                handle.Dispose();
                throw;
            }
        }

        SafeFileHandle CreateMailSlotHandle(string name) {
            var mailSlotUncName = $@"\\.\mailslot\{name}";
            var handle = CreateMailslot(mailSlotUncName, 0, unchecked((uint)-1), IntPtr.Zero);
            if (handle.IsInvalid)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            return handle;
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) {
            return _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public int Read(byte[] buffer, int offset, int count) {
            return _stream.Read(buffer, offset, count);
        }

        public void Dispose() {
            _stream?.Dispose();
        }
    }
}
