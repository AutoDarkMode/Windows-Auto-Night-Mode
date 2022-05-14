using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers.ThemeFiles
{
    internal class Desktop
    {
        public (string, int) Section { get; } = (@"[Control Panel\Desktop]", 0);
        public string Wallpaper { get; set; } = "";
        public string Pattern { get; set; } = "";
        public int MultimonBackgrounds { get; set; } = 0;
        public List<(string, string)> MultimonWallpapers { get; set; } = new();
    }

    internal class VisualStyles
    {
        public (string, int) Section { get; } = (@"[VisualStyles]", 0);
        public (string, int) Path { get; set; } = (@"Path=%SystemRoot%\resources\themes\Aero\Aero.msstyles", 1);
        public (string, int) ColorStyle { get; set; } = ("NormalColor", 2);
        public (string, int) Size { get; set; } = ("NormalSize", 3);
        public (string, int) AutoColorization { get; set; } = ("0", 4);
        public (string, int) ColorizationColor { get; set; } = ("0XC45D5A58", 5);
        public (string, int) VisualStyleVersion { get; set; } = ("10", 6);
        public (string, int) AppMode { get; set; } = ("Light", 7);
        public (string, int) SystemMode { get; set; } = ("Dark", 8);
    }

    internal class Colors
    {
        public (string, int) Section { get; } = (@"[Control Panel\Colors", 0);
        public (string, int) Background = ("0 0 0", 1);
    }

    internal class ServerIcon
    {
        public (string, int) Description { get; } = ("; Computer - SHIDI_SERVER", 0);
        public (string, int) Section { get; } = (@"[CLSID\{20D04FE0-3AEA-1069-A2D8-08002B30309D}\DefaultIcon]", 1);
        public (string, int) DefaultValue { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-109", 2);
    }

    internal class UserFilesIcon
    {
        public readonly (string, int) Description = ("; UsersFiles - SHIDI_USERFILES", 0);
        public readonly (string, int) Section = (@"[CLSID\{59031A47-3F72-44A7-89C5-5595FE6B30EE}\DefaultIcon]", 1);
        public (string, int) DefaultValue { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-123", 2);
    }

    internal class MyNetworkIcon
    {
        public (string, int) Description { get; } = ("; UsersFiles - SHIDI_USERFILES", 0);
        public (string, int) Section { get; } = (@"[CLSID\{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}\DefaultIcon]", 1);
        public (string, int) DefaultValue { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-25", 2);
    }

    internal class RecycleBinIcon
    {
        public (string, int) Description { get; } = ("; UsersFiles - SHIDI_USERFILES", 0);
        public (string, int) Tag { get; } = (@"[CLSID\{645FF040-5081-101B-9F08-00AA002F954E}\DefaultIcon]", 1);
        public (string, int) Full { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-54", 2);
        public (string, int) Empty { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-55", 3);
    }

    internal class Cursors
    {
        public (string, int) Section { get; } = (@"[Control Panel\Cursors]", 0);
        public (string, int) AppStarting { get; set; } = (@"%SystemRoot%\cursors\aero_working.ani", 1);
        public (string, int) Arrow { get; set; } = (@"%SystemRoot%\cursors\aero_arrow.cur", 2);
        public (string, int) Hand { get; set; } = (@"%SystemRoot%\cursors\aero_link.cur", 3);
        public (string, int) Help { get; set; } = (@"%SystemRoot%\cursors\aero_helpsel.cur", 4);
        public (string, int) No { get; set; } = (@"%SystemRoot%\cursors\aero_unavail.cur", 5);
        public (string, int) NWPen { get; set; } = (@"%SystemRoot%\cursors\aero_pen.cur", 6);
        public (string, int) SizeAll { get; set; } = (@"%SystemRoot%\cursors\aero_move.cur", 7);
        public (string, int) SizeNESW { get; set; } = (@"%SystemRoot%\cursors\aero_nesw.cur", 8);
        public (string, int) SizeNS { get; set; } = (@"%SystemRoot%\cursors\aero_ns.cur", 9);
        public (string, int) SizeNWSE { get; set; } = (@"%SystemRoot%\cursors\aero_nwse.cur", 10);
        public (string, int) SizeWE { get; set; } = (@"%SystemRoot%\cursors\aero_ew.cur", 11);
        public (string, int) UpArrow { get; set; } = (@"%SystemRoot%\cursors\aero_up.cur", 12);
        public (string, int) Wait { get; set; } = (@"%SystemRoot%\cursors\aero_busy.ani", 13);
        public (string, int) DefaultValue { get; set; } = (@"Windows Default", 14);
    }
}
