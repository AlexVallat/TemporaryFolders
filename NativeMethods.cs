using System.Runtime.InteropServices;

namespace TemporaryFolders;

public class NativeMethods
{
    [Flags]
    private enum SHGSI : uint
    {
        SHGSI_ICON = 0x000000100,
        SHGSI_SHELLICONSIZE = 0x000000004
    }

    private const int MAX_PATH = 260;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHSTOCKICONINFO
    {
        public UInt32 cbSize;
        public IntPtr hIcon;
        public Int32 iSysIconIndex;
        public Int32 iIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string szPath;
    }

    [DllImport("Shell32.dll", SetLastError = false)]
    private static extern Int32 SHGetStockIconInfo(uint siid, SHGSI uFlags, ref SHSTOCKICONINFO psii);

    public static IntPtr GetStockIcon(uint iconId)
    {
        SHSTOCKICONINFO info = new SHSTOCKICONINFO();
        info.cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO));

        Marshal.ThrowExceptionForHR(SHGetStockIconInfo(iconId, SHGSI.SHGSI_ICON | SHGSI.SHGSI_SHELLICONSIZE, ref info));
        return info.hIcon;
    }
}
