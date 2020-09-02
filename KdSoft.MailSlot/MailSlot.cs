using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace KdSoft.MailSlot {
    public class MailSlot {
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
            out int lpNextSize,
            out int lpMessageCount,
            out int lpReadTimeout
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

        const uint FileFlagOverlapped = 0x40000000;
        const int MailSlotNoMessage = -1;

        #endregion

        static SafeFileHandle CreateFileHandle(string name, string domain) {
            var mailSlotUncName = $@"\\{domain}\mailslot\{name}";
            var handle = CreateFile(mailSlotUncName, FileAccess.Write, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileFlagOverlapped, IntPtr.Zero);
            if (handle.IsInvalid)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            return handle;
        }

        /// <summary>
        /// Creates write only <see cref="FileStream"/> that can write to a mailslot.
        /// </summary>
        public static FileStream CreateClient(string name, string domain = ".") {
            var handle = CreateFileHandle(name, domain);
            try {
                return new FileStream(handle, FileAccess.Write, 4096, true);
            }
            catch {
                handle.Dispose();
                throw;
            }
        }

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getmailslotinfo".
        /// Only useful for synchronous access.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="timeout">Returns read timeout set when mailslot was created.</param>
        /// <returns>Next messages total size and count. Size = <c>null</c> when no messages queued.</returns>
        public static (int? size, int count) GetServerInfo(SafeFileHandle handle, out int timeout) {
            var success = GetMailslotInfo(handle, IntPtr.Zero, out var ret, out var count, out timeout);
            if (!success)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            if (ret == MailSlotNoMessage) {
                return (null, 0);
            }
            return unchecked(((int)ret, (int)count));
        }

        static SafeFileHandle CreateMailSlotHandle(string name) {
            var mailSlotUncName = $@"\\.\mailslot\{name}";
            var handle = CreateMailslot(mailSlotUncName, 0, unchecked((uint)-1), IntPtr.Zero);
            if (handle.IsInvalid)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            return handle;
        }

        /// <summary>
        /// Creates a read-only FileStream that allows reading from a mailslot.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
