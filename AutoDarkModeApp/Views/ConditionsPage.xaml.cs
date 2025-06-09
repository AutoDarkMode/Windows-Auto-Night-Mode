using System.Collections.ObjectModel;
using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace AutoDarkModeApp.Views;

public sealed partial class ConditionsPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public ConditionsViewModel ViewModel { get; }

    public ConditionsPage()
    {
        ViewModel = App.GetService<ConditionsViewModel>();
        InitializeComponent();

        _ = BuildProcessListAsync();
    }

    private async Task BuildProcessListAsync()
    {
        if (ViewModel.ProcessListItemSource == null)
            ViewModel.ProcessListItemSource = [];

        ViewModel.ProcessListItemSource.Clear();

        ViewModel.ProcessBlockListItemSource ??= new ObservableCollection<string>(_builder.Config.ProcessBlockList.ProcessNames);

        var processes = await Task.Run(() => Process.GetProcesses());

        var filteredProcesses = await Task.Run(() =>
        {
            var blockList = new HashSet<string>(_builder.Config.ProcessBlockList.ProcessNames);
            var uniqueProcesses = new SortedSet<string>();

            foreach (var process in processes)
            {
                if (process.MainWindowHandle == IntPtr.Zero || blockList.Contains(process.ProcessName))
                    continue;

                uniqueProcesses.Add(process.ProcessName);
            }

            return uniqueProcesses.ToList();
        });

        foreach (var process in filteredProcesses)
        {
            ViewModel.ProcessListItemSource.Add(process);
        }
    }

    private void ProcessTokenizingTextBox_TokenItemChanged(CommunityToolkit.WinUI.Controls.TokenizingTextBox sender, object args)
    {
        if (ViewModel.ProcessBlockListItemSource == null)
            return;

        _builder.Config.ProcessBlockList.ProcessNames.Clear();
        foreach (var i in ViewModel.ProcessBlockListItemSource)
        {
            _builder.Config.ProcessBlockList.ProcessNames.Add(i);
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesPage");
        }
    }
}
