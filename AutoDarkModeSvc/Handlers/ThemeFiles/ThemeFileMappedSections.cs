#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers.ThemeFiles
{
    public class Desktop
    {
        public (string, int) Section { get; } = (@"[Control Panel\Desktop]", 0);
        public string Wallpaper { get; set; } = "";
        public string Pattern { get; set; } = "";
        public int PicturePosition { get; set; } = 4;
        public int MultimonBackgrounds { get; set; } = 0;
        public int WindowsSpotlight { get; set; } = 0;
        public List<(string, string)> MultimonWallpapers { get; set; } = new();
    }

    public class MasterThemeSelector
    {
        public (string, int) Section { get; } = (@"[MasterThemeSelector]", 0);
        public string MTSM { get; set; } = "RJSPBS";
    }

    public class VisualStyles
    {
        public (string, int) Section { get; } = (@"[VisualStyles]", 0);
        public (string, int) Path { get; set; } = (@"%SystemRoot%\resources\themes\Aero\Aero.msstyles", 1);
        public (string, int) ColorStyle { get; set; } = ("NormalColor", 2);
        public (string, int) Size { get; set; } = ("NormalSize", 3);
        public (string, int) AutoColorization { get; set; } = ("0", 4);
        public (string, int) ColorizationColor { get; set; } = ("0XC45D5A58", 5);
        public (string, int) VisualStyleVersion { get; set; } = ("10", 6);
        public (string, int) AppMode { get; set; } = ("Light", 7);
        public (string, int) SystemMode { get; set; } = ("Dark", 8);
    }

    public class Colors
    {
        public (string, int) Section { get; } = (@"[Control Panel\Colors]", 0);
        public (string, int) Background { get; set; } = ("0 0 0", 1);
        public (string, int) InfoText { get; set; } = ("0 0 0", 1);
    }

    public class ServerIcon
    {
        public (string, int) Description { get; } = ("; Computer - SHIDI_SERVER", 0);
        public (string, int) Section { get; } = (@"[CLSID\{20D04FE0-3AEA-1069-A2D8-08002B30309D}\DefaultIcon]", 1);
        public (string, int) DefaultValue { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-109", 2);
    }

    public class UserFilesIcon
    {
        public readonly (string, int) Description = ("; UsersFiles - SHIDI_USERFILES", 0);
        public readonly (string, int) Section = (@"[CLSID\{59031A47-3F72-44A7-89C5-5595FE6B30EE}\DefaultIcon]", 1);
        public (string, int) DefaultValue { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-123", 2);
    }

    public class MyNetworkIcon
    {
        public (string, int) Description { get; } = ("; UsersFiles - SHIDI_USERFILES", 0);
        public (string, int) Section { get; } = (@"[CLSID\{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}\DefaultIcon]", 1);
        public (string, int) DefaultValue { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-25", 2);
    }

    public class RecycleBinIcon
    {
        public (string, int) Description { get; } = ("; UsersFiles - SHIDI_USERFILES", 0);
        public (string, int) Tag { get; } = (@"[CLSID\{645FF040-5081-101B-9F08-00AA002F954E}\DefaultIcon]", 1);
        public (string, int) Full { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-54", 2);
        public (string, int) Empty { get; set; } = (@"%SystemRoot%\System32\imageres.dll,-55", 3);
    }

    public class Cursors
    {
        public (string, int) Section { get; } = (@"[Control Panel\Cursors]", 0);
        public (string, int) AppStarting { get; set; } = (@"%SystemRoot%\cursors\aero_working.ani", 3);
        public (string, int) Crosshair { get; set; } = ("", 5);
        public (string, int) Arrow { get; set; } = (@"%SystemRoot%\cursors\aero_arrow.cur", 1);
        public (string, int) Hand { get; set; } = (@"%SystemRoot%\cursors\aero_link.cur", 15);
        public (string, int) Help { get; set; } = (@"%SystemRoot%\cursors\aero_helpsel.cur", 2);
        public (string, int) IBeam { get; set; } = ("", 6);
        public (string, int) No { get; set; } = (@"%SystemRoot%\cursors\aero_unavail.cur", 8);
        public (string, int) NWPen { get; set; } = (@"%SystemRoot%\cursors\aero_pen.cur", 7);
        public (string, int) Person { get; set; } = (@"%SystemRoot%\cursors\aero_person.cur", 17);
        public (string, int) Pin { get; set; } = (@"%SystemRoot%\cursors\aero_pin.cur", 16);
        public (string, int) SizeAll { get; set; } = (@"%SystemRoot%\cursors\aero_move.cur", 13);
        public (string, int) SizeNESW { get; set; } = (@"%SystemRoot%\cursors\aero_nesw.cur", 12);
        public (string, int) SizeNS { get; set; } = (@"%SystemRoot%\cursors\aero_ns.cur", 9);
        public (string, int) SizeNWSE { get; set; } = (@"%SystemRoot%\cursors\aero_nwse.cur", 11);
        public (string, int) SizeWE { get; set; } = (@"%SystemRoot%\cursors\aero_ew.cur", 10);
        public (string, int) UpArrow { get; set; } = (@"%SystemRoot%\cursors\aero_up.cur", 14);
        public (string, int) Wait { get; set; } = (@"%SystemRoot%\cursors\aero_busy.ani", 4);
        public (string, int) DefaultValue { get; set; } = (@"Windows Default", 18);
    }

    public class Slideshow
    {
        public (string, int) Section { get; } = (@"[Slideshow]", 0);
        public bool Enabled { get; set;  }
        public int Interval { get; set; } = 1337000;
        public int Shuffle { get; set; } = 1;
        public string ImagesRootPath { get; set; } = null;
        public string ImagesRootPIDL { get; set; } = null;
        public List<(string, string)> ItemPaths { get; set; } = new();
        public string RssFeed { get; set; } = null;
    }
}
