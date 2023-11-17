using System.Diagnostics;
using SHDocVw;
using Microsoft.VisualBasic.FileIO;
using TemporaryFolders.Properties;

namespace TemporaryFolders;
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var ctrlDownAtStart = Control.ModifierKeys == Keys.Control;

        Application.EnableVisualStyles();

        var tempFile = Path.GetTempFileName();
        File.Delete(tempFile);
        var tempFolder = Directory.CreateDirectory(Path.Combine(tempFile, Resources.TemporaryFolderName)).FullName;
        try
        {
            Clipboard.SetText(tempFolder);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // Couldn't set the clipboard, ignore that
        }

        foreach (var arg in args)
        {
            var destinationFileName = Path.Combine(tempFolder, Path.GetFileName(arg));

            if (File.Exists(arg))
            {
                if (ctrlDownAtStart)
                {
                    FileSystem.CopyFile(arg, destinationFileName, UIOption.AllDialogs, UICancelOption.DoNothing);
                }
                else
                {
                    FileSystem.MoveFile(arg, destinationFileName, UIOption.AllDialogs, UICancelOption.DoNothing);
                }
            }
            else if(Directory.Exists(arg))
            {
                if (ctrlDownAtStart)
                {
                    FileSystem.CopyDirectory(arg, destinationFileName, UIOption.AllDialogs, UICancelOption.DoNothing);
                }
                else
                {
                    FileSystem.MoveDirectory(arg, destinationFileName, UIOption.AllDialogs, UICancelOption.DoNothing);
                }
            }
        }

        var shellWindows = new ShellWindows();
        
        void ShowFolder()
        {
            shellWindows.WindowRegistered += OnExplorerWindowOpened;

            void OnExplorerWindowOpened(int lCookie)
            {
                var window = (InternetExplorer)shellWindows.Item(shellWindows.Count - 1);

                if (new Uri(window.LocationURL).LocalPath.Equals(tempFolder, StringComparison.OrdinalIgnoreCase))
                {
                    var hwnd = new IntPtr(window.HWND);
                    window.OnQuit += () => OnExploreWindowClosed(hwnd);
                    Task.Run(() => shellWindows.WindowRegistered -= OnExplorerWindowOpened);
                }
            }

            Process.Start("explorer.exe", $"/separate /root,\"{tempFolder}\"");
            Directory.SetCurrentDirectory(tempFolder);

            // Also update a junction to the folder
            var junctionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Most Recent Temporary Folder");

            try
            {
                var overwriteJunction = false;

                if (!Directory.Exists(junctionPath)) 
                { 
                    overwriteJunction = true;
                }
                else if (File.GetAttributes(junctionPath).HasFlag(FileAttributes.Directory | FileAttributes.ReparsePoint))
                {
                    var symlink = new DirectoryInfo(junctionPath);
                    if (symlink.LinkTarget != null) // Only delete the existing symlink if it *is* a symlink
                    {
                        overwriteJunction = true;
                    }
                }

                if (overwriteJunction)
                {
                    Junction.Create(junctionPath, tempFolder, true);
                }
            }
            catch (IOException)
            {
                // Ignore it if we can't overwrite the junction
            }
        }

        ShowFolder();
        Application.Run();

        void OnExploreWindowClosed(IntPtr explorerWindowHandle)
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());

            if (!Directory.EnumerateFileSystemEntries(tempFolder).Any())
            {
                // Nothing in the folder, don't bother asking
                try
                {
                    Directory.Delete(tempFile, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not delete folder: {ex.Message}");
                }
                Application.Exit();
            }

            // Prompt on a different thread so as not to block Explorer process
            Task.Run(() => PromptForDelete(explorerWindowHandle));
            Thread.Sleep(200); // Wait a little bit for the dialog to be shown before allowing the handle to go away
        }

        void PromptForDelete(IntPtr explorerWindowHandle)
        {
            var deleteButton = new TaskDialogCommandLinkButton(Resources.DeleteButton, Resources.DeleteDescription);
            var result = TaskDialog.ShowDialog(explorerWindowHandle, new TaskDialogPage
            {
                AllowCancel = true,
                Caption = Resources.DeleteConfirmationTitle,
                Icon = new TaskDialogIcon(NativeMethods.GetStockIcon(Windows.Win32.UI.Shell.SHSTOCKICONID.SIID_RECYCLERFULL)),
                Text = Resources.DeleteConfirmationPrompt,
                SizeToContent = true,
                Buttons = new TaskDialogButtonCollection
            {
                deleteButton,
                new TaskDialogCommandLinkButton(Resources.NoDeleteButton, Resources.NoDeleteDescription),
                TaskDialogButton.Cancel
            }});

            if (result == deleteButton)
            {
                try
                {
                    if (Control.ModifierKeys == Keys.Shift)
                    {
                        try
                        {
                            FileSystem.DeleteDirectory(tempFolder, UIOption.AllDialogs, RecycleOption.DeletePermanently, UICancelOption.ThrowException);
                        }
                        catch (OperationCanceledException)
                        {
                            // Re-open the folder
                            ShowFolder();
                            return;
                        }
                    }
                    else
                    {
                        FileSystem.DeleteDirectory(tempFolder, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
                    }
                    Directory.Delete(tempFile, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not delete folder: {ex.Message}");
                }
                Application.Exit();
            }
            else if (result == TaskDialogButton.Cancel)
            {
                // Re-open the folder
                Task.Run(() => ShowFolder());
                return;
            }
            else // Do Nothing button
            {
                Application.Exit();
            }
        }
    }

    private static void Window_OnQuit()
    {
    }

    private static void ShellWindows_WindowRevoked(int lCookie)
    {
    }

    private static void Window_WindowClosing(bool IsChildWindow, ref bool Cancel)
    {
    }
}