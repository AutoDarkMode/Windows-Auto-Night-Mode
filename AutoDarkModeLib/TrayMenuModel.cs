using System;
using System.Collections.Generic;

namespace AutoDarkModeLib;

public static class TrayMenuProtocol
{
    public const string Argument = "--tray-menu-host";
    public const string WindowTitle = "Auto Dark Mode tray menu";
    public static string PipeName(int ownerPid) => $"adm-winui-tray-{Environment.UserName}-{ownerPid}";
}

public sealed class TrayMenuModel
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Dark { get; set; }
    public List<TrayMenuEntry> Items { get; set; } = new();
}

public sealed class TrayMenuEntry
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string Glyph { get; set; } = "";
    public bool Separator { get; set; }
    public bool Checkable { get; set; }
    public bool Checked { get; set; }
    public bool Enabled { get; set; } = true;
}
