using System.Runtime.InteropServices;
using System.Text;

namespace AngkleChen;

/// <summary>
/// 文件 / 文件夹选择框，直接调 Win32 通用对话框（comdlg32 / shell32）。
///
/// 为什么不用 Windows.Storage.Pickers：非打包 + 单文件发布下，
/// FileOpenPicker 会让进程以 0xc000027b 直接终止，托管层的 try/catch 拦不住。
///
/// 为什么缓冲区用 IntPtr 手工分配而不是 StringBuilder：
/// StringBuilder 只能作为 P/Invoke 的直接参数，放进结构体字段后
/// Marshal.SizeOf 会直接抛 ArgumentException（"cannot be marshaled as an
/// unmanaged structure"）——这正是上一版报错的来源。
/// </summary>
internal static class NativeDialogs
{
    private const int OfnExplorer = 0x00080000;
    private const int OfnFileMustExist = 0x00001000;
    private const int OfnPathMustExist = 0x00000800;
    private const int OfnNoChangeDir = 0x00000008;

    private const int BifReturnOnlyFsDirs = 0x00000001;
    private const int BifNewDialogStyle = 0x00000040;

    private const int PathCapacity = 4096;

    public static string? PickFile(IntPtr owner, string filter)
    {
        var fileBuffer = Marshal.AllocHGlobal(PathCapacity * 2);

        try
        {
            Zero(fileBuffer, PathCapacity * 2);

            var ofn = new OpenFileName
            {
                lStructSize = Marshal.SizeOf<OpenFileName>(),
                hwndOwner = owner,
                lpstrFilter = filter,
                lpstrFile = fileBuffer,
                nMaxFile = PathCapacity,
                lpstrTitle = "选择工具程序",
                Flags = OfnExplorer | OfnFileMustExist | OfnPathMustExist | OfnNoChangeDir
            };

            if (!GetOpenFileNameW(ref ofn)) return null;

            var path = Marshal.PtrToStringUni(fileBuffer);
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        finally
        {
            Marshal.FreeHGlobal(fileBuffer);
        }
    }

    public static string? PickFolder(IntPtr owner, string title)
    {
        var display = Marshal.AllocHGlobal(260 * 2);
        var pathBuffer = Marshal.AllocHGlobal(PathCapacity * 2);

        try
        {
            Zero(display, 260 * 2);
            Zero(pathBuffer, PathCapacity * 2);

            var browse = new BrowseInfo
            {
                hwndOwner = owner,
                pszDisplayName = display,
                lpszTitle = title,
                ulFlags = BifReturnOnlyFsDirs | BifNewDialogStyle
            };

            var pidl = SHBrowseForFolderW(ref browse);
            if (pidl == IntPtr.Zero) return null;

            try
            {
                if (!SHGetPathFromIDListW(pidl, pathBuffer)) return null;

                var path = Marshal.PtrToStringUni(pathBuffer);
                return string.IsNullOrWhiteSpace(path) ? null : path;
            }
            finally
            {
                Marshal.FreeCoTaskMem(pidl);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(display);
            Marshal.FreeHGlobal(pathBuffer);
        }
    }

    /// <summary>把过滤器写成 comdlg32 要求的双 null 结尾格式</summary>
    public static string BuildFilter(params (string Label, string Pattern)[] entries)
    {
        var builder = new StringBuilder();

        foreach (var (label, pattern) in entries)
        {
            builder.Append(label).Append('\0').Append(pattern).Append('\0');
        }

        builder.Append('\0');
        return builder.ToString();
    }

    private static void Zero(IntPtr buffer, int bytes)
    {
        for (var i = 0; i < bytes; i++)
            Marshal.WriteByte(buffer, i, 0);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string? lpstrFilter;
        public string? lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public IntPtr lpstrFile;
        public int nMaxFile;
        public string? lpstrFileTitle;
        public int nMaxFileTitle;
        public string? lpstrInitialDir;
        public string? lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string? lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string? lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BrowseInfo
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public IntPtr pszDisplayName;
        public string? lpszTitle;
        public int ulFlags;
        public IntPtr lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetOpenFileNameW(ref OpenFileName openFileName);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHBrowseForFolderW(ref BrowseInfo browseInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SHGetPathFromIDListW(IntPtr pidl, IntPtr path);
}
