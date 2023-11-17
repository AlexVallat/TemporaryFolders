using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TemporaryFolders;

internal class NativeMethods
{
    public static HICON GetStockIcon(SHSTOCKICONID iconId)
    {
        SHSTOCKICONINFO info = new SHSTOCKICONINFO();
        info.cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO));

        PInvoke.SHGetStockIconInfo(iconId, SHGSI_FLAGS.SHGSI_ICON | SHGSI_FLAGS.SHGSI_SHELLICONSIZE, ref info).ThrowOnFailure();
        return info.hIcon;
    }
}
