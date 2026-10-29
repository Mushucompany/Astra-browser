using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

class Installer
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        string[] files = {
            "Мой браузер.exe",
            "Microsoft.Web.WebView2.Core.dll",
            "Microsoft.Web.WebView2.WinForms.dll",
            "WebView2Loader.dll",
            "browser.ico",
            "home.html",
            "welcome.html",
            "logo.png"
        };

        string installDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Мой браузер");

        var result = MessageBox.Show(
            "Установить «Мой браузер»?\n\n" +
            "Место установки:\n" + installDir + "\n\n" +
            "Будут созданы ярлыки на рабочем столе и в меню «Пуск».\n\n" +
            "Продолжить?",
            "Установка браузера",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        if (result != DialogResult.OK) return;

        try
        {
            Directory.CreateDirectory(installDir);

            foreach (string f in files)
            {
                string src = Path.Combine(baseDir, f);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(installDir, f), true);
            }

            string exePath = Path.Combine(installDir, "Мой браузер.exe");
            string iconPath = Path.Combine(installDir, "browser.ico");

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            CreateShortcut(exePath, Path.Combine(desktop, "Мой браузер.lnk"), iconPath);
            CreateShortcut(exePath, Path.Combine(startMenu, "Мой браузер.lnk"), iconPath);

            MessageBox.Show("Установка завершена!\n\n«Мой браузер» установлен в:\n" + installDir +
                "\n\nЯрлыки созданы на рабочем столе и в меню «Пуск».",
                "Установка браузера", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (MessageBox.Show("Запустить браузер сейчас?", "Установка",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Process.Start(exePath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка установки:\n" + ex.Message, "Установка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Создание ярлыка через IShellLink (COM)
    static void CreateShortcut(string targetPath, string lnkPath, string iconPath)
    {
        try
        {
            Type shellType = Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046"));
            dynamic shell = Activator.CreateInstance(shellType);
            dynamic lnk = shell.CreateLink(targetPath, lnkPath, string.Empty, Path.GetDirectoryName(targetPath), 0);
            // Если интерфейс не поддерживает CreateLink, используем IShellLinkW
        }
        catch
        {
            CreateShortcutViaIShellLink(targetPath, lnkPath, iconPath);
        }
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    class ShellLink { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("000214F9-0000-0000-C000-000000000046")]
    interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("0000010B-0000-0000-C000-000000000046")]
    interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    static void CreateShortcutViaIShellLink(string targetPath, string lnkPath, string iconPath)
    {
        ShellLink link = new ShellLink();
        IShellLinkW sl = (IShellLinkW)link;
        sl.SetPath(targetPath);
        sl.SetWorkingDirectory(Path.GetDirectoryName(targetPath));
        sl.SetIconLocation(iconPath, 0);
        sl.SetDescription("Мой браузер");

        IPersistFile pf = (IPersistFile)link;
        pf.Save(lnkPath, true);
    }
}