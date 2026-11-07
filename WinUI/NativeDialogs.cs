using System.Runtime.InteropServices;
using System.Text;

namespace AngkleChen;

/// <summary>
/// 文件 / 文件夹选择框，直接调 Win32 通用对话框（comdlg32 / shell32）。
///
/// 为什么不用 Windows.Storage.Pickers：非打包 + 单文件发布下，
/// FileOpenPicker 会让进程以 0xc000027b 直接终止，托管层的 try/catch 根本拦不住
/// （实测崩溃日志落在 CoreMessagingXP.dll）。comdlg32 是纯 Win32，
/// 没有 WinRT 激活这一层，不存在这个问题。
/// </summary>
internal static class NativeDialogs
{
    private const int OfnExplorer = 0x00080000;
    private const int OfnFileMustExist = 0x00001000;
    private const int OfnPathMustExist = 0x00000800;
    private const int OfnNoChangeDir = 0x00000008;

    private const int BifReturnOnlyFsDirs = 0x00000001;
    private const int BifNewDialogStyle = 0x00000040;

    public static string? PickFile(IntPtr owner, string filter)
    {
        // 用 StringBuilder 而不是 char[]：char[] 要靠 SizeParamIndex 推导长度，
        // 索引写错会越界写内存（直接崩进程），StringBuilder 按 Capacity 分配最稳
        var buffer = new StringBuilder(4096);
        var ofn = new OpenFileName
        {
            lStructSize = Marshal.SizeOf<OpenFileName>(),
            hwndOwner = owner,
            lpstrFilter = filter,
            lpstrFile = buffer,
            nMaxFile = buffer.Capacity,
            lpstrTitle = "选择工具程序",
            Flags = OfnExplorer | OfnFileMustExist | OfnPathMustExist | OfnNoChangeDir
        };

        if (!GetOpenFileNameW(ref ofn)) return null;

        var path = buffer.ToString().TrimEnd('\0');
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    public static string? PickFolder(IntPtr owner, string title)
    {
        var display = new StringBuilder(260);
        var browse = new BrowseInfo
        {
            hwndOwner = owner,
            pszDisplayName = display,
            lpszTitle = title,
            ulFlags = BifReturnOnlyFsDirs | BifNewDialogStyle
        };

        var pidl = SHBrowseForFolderW(ref browse);
        if (pidl == IntPtr.Zero) return null;

        var pathBuffer = new char[4096];
        try
        {
            if (!SHGetPathFromIDListW(pidl, pathBuffer)) return null;

            var path = new string(pathBuffer).TrimEnd('\0');
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        finally
        {
            Marshal.FreeCoTaskMem(pidl);
        }
    }

    /// <summary>把过滤器写成 comdlg32 要求的双 null 结尾格式</summary>
    public static string BuildFilter(params (string Label, string Pattern)[] entries)
    {
        var parts = new List<string>();
        foreach (var (label, pattern) in entries)
        {
            parts.Add(label);
            parts.Add(pattern);
        }

        return string.Join('\0', parts) + "\0\0";
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
        public StringBuilder? lpstrFile;
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
        public StringBuilder? pszDisplayName;
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
    private static extern bool SHGetPathFromIDListW(IntPtr pidl, char[] path);
}
