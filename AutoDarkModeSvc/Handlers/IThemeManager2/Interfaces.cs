using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using static AutoDarkModeSvc.Handlers.IThemeManager2.Flags;

namespace AutoDarkModeSvc.Handlers.IThemeManager2
{
    public class Interfaces
    {
        [Guid("26e4185f-0528-475f-acaf-abe89ba6017d")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ITheme
        {
            public string DisplayName { get; set; }
            public string VisualStyle1 { get; set; }
            public string VisualStyle2 { get; set; }
        }

        [Guid("c1e8c83e-845d-4d95-81db-e283fdffc000")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IThemeManager2
        {
            void Init(InitializationFlags initFlags);
            void InitAsync(IntPtr hwnd, int unk1);
            void Refresh();
            void RefreshAsync(IntPtr hwnd, int unk1);
            void RefreshComplete();
            int GetThemeCount(out int count);
            void GetTheme(int index, out ITheme theme);
            void IsThemeDisabled(int index, out int disabled);
            void GetCurrentTheme(out int index);

            int SetCurrentTheme(IntPtr parent, int themeIndex, int applyNow, ThemeApplyFlags applyFlags,
                ThemePackFlags packFlags);

            void GetCustomTheme(out int index);
            void GetDefaultTheme(out int index);
            void CreateThemePack(IntPtr hwnd, string unk1, ThemePackFlags packFlags);
            void CloneAndSetCurrentTheme(IntPtr hwnd, string unk1, out string unk2);

            void InstallThemePack(IntPtr hwnd, string unk1, int unk2, ThemePackFlags packFlags, out string unk3,
                out ITheme unk4);

            void DeleteTheme(string unk1);
            int OpenTheme(IntPtr hwnd, string path, ThemePackFlags packFlags);
            void AddAndSelectTheme(IntPtr hwnd, string path, ThemeApplyFlags applyFlags, ThemePackFlags packFlags);
            void SQMCurrentTheme();
            void ExportRoamingThemeToStream(IStream stream, int unk1);
            void ImportRoamingThemeFromStream(IStream stream, int unk1);
            void UpdateColorSettingsForLogonUI();
            void GetDefaultThemeId(out Guid guid);
            void UpdateCustomTheme();
        }
    }
}
