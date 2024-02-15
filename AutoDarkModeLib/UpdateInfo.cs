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
using System.Globalization;
using System.Runtime.InteropServices;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeLib
{
    public class UpdateInfo
    {
        public string Tag { get; set; }
        public string PathFile { get; set; }
        public string PathFileArm { get; set; }
        public string PathChecksum { get; set; }
        public string PathChecksumArm { get; set; }
        public bool AutoUpdateAvailable { get; set; }
        public string UpdaterVersion { get; set; }
        public string Message { get; set; }
        public string ChangelogUrl { get; set; }
        public static UpdateInfo Deserialize(string data)
        {
            var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().IgnoreUnmatchedProperties().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            UpdateInfo deserialized = yamlDeserializer.Deserialize<UpdateInfo>(data);
            return deserialized;
        }

        public string Serialize()
        {
            YamlDotNet.Serialization.ISerializer yamlSerializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            return yamlSerializer.Serialize(this);
        }

        public string GetUpdateUrl(string url, bool custom = false)
        {
            if (custom)
            {
                return url;
            }
            else
            {
                string fileUrl;
                if (PathFileArm != null && RuntimeInformation.OSArchitecture == Architecture.Arm64)
                {
                    fileUrl = $"{url}{PathFileArm}";
                }
                else
                {
                    fileUrl = $"{url}{PathFile}";
                }
                return fileUrl;
            }
        }

        public string GetUpdateInfoPage()
        {
            return ChangelogUrl;
        }

        public string GetUpdateHashUrl(string url, bool custom = false)
        {
            if (custom)
            {
                return url;
            }
            else
            {
                string hashUrl;

                if (PathChecksumArm != null && RuntimeInformation.OSArchitecture == Architecture.Arm64)
                {
                    hashUrl = $"{url}{PathChecksumArm}";
                }
                else
                {
                    hashUrl = $"{url}{PathChecksum}";
                }
                return hashUrl;
            }
        }
    }
}
