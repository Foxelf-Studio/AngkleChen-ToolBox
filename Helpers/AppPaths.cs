using System.IO;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 程序目录与常用路径的唯一出口。
///
/// 此前有 6 处各自用 <c>Environment.ProcessPath</c> 或
/// <c>AppDomain.CurrentDomain.BaseDirectory</c> 现算路径。.NET 5+ 的单文件发布下两者结果一致，
/// 所以没出过 bug，但散落各处容易在新增功能时写错，也让人误以为两者有区别。
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// 程序所在目录。单文件发布时返回 exe 自身所在目录（不是解压临时目录）。
    /// </summary>
    public static string Root { get; } =
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    public static string Config => Path.Combine(Root, "config.json");
    public static string Settings => Path.Combine(Root, "settings.json");
    public static string Log => Path.Combine(Root, "debug.log");
    public static string IconCache => Path.Combine(Root, "IconCache");
    public static string Tools => Path.Combine(Root, "工具");
    public static string Trash => Path.Combine(Root, "回收站");
}
