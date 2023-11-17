using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace TemporaryFolders
{
    internal static class Junction
    {
        // CsWin32 can't generate REPARSE_DATA_BUFFER correctly due to https://github.com/microsoft/CsWin32/issues/387
        [StructLayout(LayoutKind.Sequential)]
        private struct REPARSE_DATA_BUFFER
        {
            /// <summary>
            ///     Reparse point tag. Must be a Microsoft reparse point tag.
            /// </summary>
            public uint ReparseTag;

            /// <summary>
            ///     Size, in bytes, of the data after the Reserved member. This can be calculated by:
            ///     (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength +
            ///     (namesAreNullTerminated ? 2 * sizeof(char) : 0);
            /// </summary>
            public ushort ReparseDataLength;

            /// <summary>
            ///     Reserved; do not use.
            /// </summary>
            public ushort Reserved;

            /// <summary>
            ///     Offset, in bytes, of the substitute name string in the PathBuffer array.
            /// </summary>
            public ushort SubstituteNameOffset;

            /// <summary>
            ///     Length, in bytes, of the substitute name string. If this string is null-terminated,
            ///     SubstituteNameLength does not include space for the null character.
            /// </summary>
            public ushort SubstituteNameLength;

            /// <summary>
            ///     Offset, in bytes, of the print name string in the PathBuffer array.
            /// </summary>
            public ushort PrintNameOffset;

            /// <summary>
            ///     Length, in bytes, of the print name string. If this string is null-terminated,
            ///     PrintNameLength does not include space for the null character.
            /// </summary>
            public ushort PrintNameLength;

            /// <summary>
            ///     A buffer containing the unicode-encoded path string. The path string contains
            ///     the substitute name string and print name string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)] public byte[] PathBuffer;
        }

        /// <summary>
        ///     This prefix indicates to NTFS that the path is to be treated as a non-interpreted
        ///     path in the virtual file system.
        /// </summary>
        private const string NonInterpretedPathPrefix = @"\??\";

        /// <summary>
        ///     Creates a junction point from the specified directory to the specified target directory.
        /// </summary>
        /// <remarks>
        ///     Only works on NTFS.
        /// </remarks>
        /// <param name="junctionPoint">The junction point path</param>
        /// <param name="targetDir">The target directory</param>
        /// <param name="overwrite">If true overwrites an existing reparse point or empty directory</param>
        /// <exception cref="IOException">
        ///     Thrown when the junction point could not be created or when
        ///     an existing directory was found and <paramref name="overwrite" /> if false
        /// </exception>
        public unsafe static void Create(string junctionPoint, string targetDir, bool overwrite)
        {
            targetDir = Path.GetFullPath(targetDir);

            if (!Directory.Exists(targetDir))
                throw new IOException("Target path does not exist or is not a directory.");

            if (Directory.Exists(junctionPoint))
            {
                if (!overwrite)
                    throw new IOException("Directory already exists and overwrite parameter is false.");
            }
            else
            {
                Directory.CreateDirectory(junctionPoint);
            }

            using (var handle = OpenReparsePoint(junctionPoint, GENERIC_ACCESS_RIGHTS.GENERIC_WRITE))
            {
                var targetDirBytes = Encoding.Unicode.GetBytes(NonInterpretedPathPrefix + targetDir);

                var reparseDataBuffer =
                    new REPARSE_DATA_BUFFER
                    {
                        ReparseTag = PInvoke.IO_REPARSE_TAG_MOUNT_POINT,
                        ReparseDataLength = (ushort)(targetDirBytes.Length + 12),
                        SubstituteNameOffset = 0,
                        SubstituteNameLength = (ushort)targetDirBytes.Length,
                        PrintNameOffset = (ushort)(targetDirBytes.Length + 2),
                        PrintNameLength = 0,
                        PathBuffer = new byte[0x3ff0]
                    };

                Array.Copy(targetDirBytes, reparseDataBuffer.PathBuffer, targetDirBytes.Length);

                var inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                var inBuffer = Marshal.AllocHGlobal(inBufferSize);
                try
                {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    uint bytesReturned;
                    var result = PInvoke.DeviceIoControl(handle, PInvoke.FSCTL_SET_REPARSE_POINT,
                        inBuffer.ToPointer(), (uint)targetDirBytes.Length + 20, null, 0, &bytesReturned, null);

                    if (!result)
                    {
                        ThrowLastWin32Error($"Unable to create junction point '{junctionPoint}' -> '{targetDir}'.");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(inBuffer);
                }
            }
        }

        private static SafeFileHandle OpenReparsePoint(string reparsePoint, GENERIC_ACCESS_RIGHTS accessMode)
        {
            var reparsePointHandle = PInvoke.CreateFile(reparsePoint, (uint)accessMode,
                FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
                null, FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT, null);

            if (reparsePointHandle.IsInvalid)
            {
                ThrowLastWin32Error($"Unable to open reparse point '{reparsePoint}'.");
            }

            return reparsePointHandle;
        }

        private static void ThrowLastWin32Error(string message)
        {
            throw new IOException(message, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
        }
    }
}
