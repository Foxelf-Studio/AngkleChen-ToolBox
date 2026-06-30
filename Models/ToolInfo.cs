namespace 陈叔叔工具箱.Models;

public record ToolInfo(
    string Name,
    string Category,
    string Icon,
    string Description,
    string RelativePath,
    string Detail = ""
);
