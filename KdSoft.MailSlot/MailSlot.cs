using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace KdSoft.MailSlot
{
    public class MailSlot
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

        public static FileStream CreateClient(string name, string domain = ".") {
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

        static SafeFileHandle CreateMailSlotHandle(string name) {
            var mailSlotUncName = $@"\\.\mailslot\{name}";
            var handle = CreateMailslot(mailSlotUncName, 0, unchecked((uint)-1), IntPtr.Zero);
            if (handle.IsInvalid)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            return handle;
        }

        public static FileStream CreateServer(string name) {
            var handle = CreateMailSlotHandle(name);
            try {
                return new FileStream(handle, FileAccess.Read, 4096, true);
            }
            catch {
                handle.Dispose();
                throw;
            }
        }
    }
}
