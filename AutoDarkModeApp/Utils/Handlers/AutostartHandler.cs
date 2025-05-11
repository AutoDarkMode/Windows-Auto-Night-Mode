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
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Services;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using Microsoft.UI.Xaml;

namespace AutoDarkModeApp.Utils.Handlers;

internal static class AutostartHandler
{
    private static readonly IErrorService _errorService = App.GetService<IErrorService>();

    public static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

    public static void EnsureAutostart(XamlRoot xamlRoot)
    {
        ApiResponse result = new()
        {
            StatusCode = StatusCode.Err,
            Message = "error in frontend: EnsureAutostart()"
        };
        try
        {
            _ = MessageHandler.Client.SendMessageAndGetReply(Command.ValidateAutostart);
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessageFromApi(result, ex, xamlRoot);
        }
    }

    public static async void EnableAutoStart(XamlRoot xamlRoot)
    {
        ApiResponse result = new()
        {
            StatusCode = StatusCode.Err,
            Message = "error in frontend: EnableAutostart()"
        };
        try
        {
            result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.AddAutostart));
            if (result.StatusCode != StatusCode.Ok)
            {
                throw new AddAutoStartException($"Could not add Auto Dark Mode to autostart", "AutoCheckBox_Checked");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessageFromApi(result, ex, xamlRoot);
        }
    }
}
