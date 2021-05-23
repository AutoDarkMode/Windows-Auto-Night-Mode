
# Windows 10 Auto Dark Mode
![App Logo](https://github.com/Armin2208/Windows-Auto-Night-Mode/blob/master/Readme/logo.png)  
Automatically switches between the dark and light theme of Windows 10 at scheduled times.

[Overview](#overview) • [Features](#features) • [Download](#download-and-installing) • [Release Notes](https://github.com/Armin2208/Windows-Auto-Night-Mode/releases) • [Wiki](https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki) • [Telegram Group](#telegram-group)

## Overview
![Screenshot showing Time-Page](https://github.com/Armin2208/Windows-Auto-Night-Mode/blob/master/Readme/screenshot1.png)
Android, GNOME Shell, iOS and MacOS already offer the possibility of changing the system design based on the time of day. So why not Windows too?

Auto Dark Mode helps you to be more productive. When it is light outside, we enable you to have a bright and clearly readable OS. After the sun starts to set, your operating system switches to a matching theme to take care of your eyes. This app saves you from the annoying way to switch the design manually in the Windows settings.

By enabling the automatic theme switcher in the app, a task in the Task Scheduler of Windows is created. This task will launch the app and set your theme. No background task, no interruption of a CMD-window, no footprint and no reliability issues. You can find many settings in the easy-to-understand user interface.

## Features
![Screenshot showing Apps-Page](https://github.com/Armin2208/Windows-Auto-Night-Mode/blob/master/Readme/screenshot2.png)  
- Easy to use and clean user-interface.
- Theme switch based on custom times or the suntimes of your location + time offset.
- Desktop wallpaper switch.
- Windows .Theme-File switch, to also change Accent Color and Mouse Cursor.
- Office theme switch
- You can choose if only Apps should change their theme, or both apps and system.
- Support for accent color on the Taskbar and other system elements.
- Ability to automatically enable the grayscale color filter of Windows 10.
- Easy theme switch with a Jump-List, accessible from the Startmenu or Taskbar.
- Lightweight with 100% clean uninstall. No admin-rights needed.

## Download and installing
#### [Click here to download the newest version of Auto Dark Mode!](https://github.com/Armin2208/Windows-Auto-Night-Mode/releases)

#### Annotation
Windows SmartScreen-Filter, your webbrowser or some antivirus software may warn you from downloading or starting this app. This is due to the missing signature license, which unfortunately I cannot afford. But from the numerous github stars you can see that many people use the program without problems.  
[Click here to see a VirusTotal test of AutoDarkMode_Setup.exe](https://www.virustotal.com/gui/file/fea01593ebcd7aeec3a4d7566e4c449a486c8c9fecd0b7941ebb036fb0fe2797/)

#### Installation
Installing is pretty easy as you only need to run the setup file provided as .exe. If you want to deploy Auto Dark Mode on multiple machines, you can use the argument _/verysilent_ to skip the installer window.

#### Via WinGet
Download Auto Dark Mode from [WinGet](https://github.com/microsoft/winget-cli/releases).
```powershell
winget install "Auto Dark Mode"
```

#### Via Chocolatey
Download Auto Dark Mode from [Chocolatey](https://chocolatey.org/packages/auto-dark-mode) (unofficial entry).
```powershell
choco install auto-dark-mode
```

#### Via Scoop
Download Auto Dark Mode from [Scoop](https://scoop.sh) (unofficial entry).
- Via portable
```powershell
scoop bucket add dorado https://github.com/chawyehsu/dorado
scoop install autodarkmode
```
- Via non-portable
```powershell
scoop bucket add nonportable
scoop install auto-dark-mode-np
```

## Telegram group
[Join our official Telegram group to get early access to new beta versions](https://t.me/autodarkmode)

## More information
You still have open questions? [Check out our wiki!](https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki)


# For developers: adding new modules

In case you want to contribute and add a new module, here's how:

### Understanding how a module works

AutoDarkMode uses a modular timer based system. Each module is registered or deregistered to a specific timer when it is enabled or disabled. The first step therefore usually consists of creating an `Enabled` property or config class for your module in `Config/AdmConfig.cs`.
Only read operations are allowed to this config file. If you need write access, you will need to add a new configuration class and file and save it separately.
In order to then create a module let's take a look at what a module class looks like:
```C#
namespace AutoDarkModeSvc.Modules
{
  class MyModule : AutoDarkModeModule
  {
     public override string TimerAffinity { get; } = TimerName.Main; 
     private AdmConfigBuilder ConfigBuilder { get; }
     public MyModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration) 
     {
       ConfigBuilder = AdmConfigBuilder.Instance();
       //do constructor stuff here
     }
     public override void Fire()
     {
         Task.Run(() =>
         {
             // call your logic here
         });
     }   
     // implement as usual
    }
}
```
A module needs to have a constructor that calls its base constructor with exactly one string parameter (name) and one bool parameter (should the module be fired when it was enabled in the config file). 

Each module has access to the configuration builder in case it needs to retrieve values from the global configuration. You can call it by invoking the `ConfigBuilder` singleton instance.

A module must inherit from the `AutoDarkModeModule` base class. The base class ensures that modules are comparable and implements the `IAutoDarkModeModule` interface. This ensures that all modules can be controlled by only using the interface.
You will then need to override 
- `Fire()`, which is called by the timer and
- `TimerAffinity`, which is the unique name of a timer this module should run on. 

There are preconfigured timer names in `Timers/TimerName.cs` that tick at different intervals. An example on how to add new timers will come at a later point in time.

### Adding a module to the module warden
Each module is automatically controlled by the module warden, which is a module itself that runs by default. It manages enabling and disabling modules on any timer based on the current configuration file state. You can add your modules to the software by changing the `Fire()` method in `Modules/ModuleWardenModule.cs`

It looks similar to this one:
```C#
public override void Fire()
  {
    AdmConfig config = ConfigBuilder.Config;
    AutoManageModule(typeof(TimeSwitchModule).Name, typeof(TimeSwitchModule), false, config.AutoThemeSwitchingEnabled);
    AutoManageModule(typeof(GeopositionUpdateModule).Name, typeof(GeopositionUpdateModule), true, config.Location.Enabled);
  }
```

To add a module, call AutoManageModule with type signature `AutoManageModule#(String, Type, Bool, Bool)` and takes the following parameters:
- Name: Derived from the className so you can use `typeof(MyModule).Name`
- Type: The module's class used for object instantiation, this is always `typeof(MyModule)`
- FireOnRegistration: The module event should be triggered as soon as it is registered to a timer, boolean `true/false`
- Enabled: A boolean value that indicates whether the module should be running currently. Point it to your `Enabled` Property that you created in the configuration file or use an existing one if it fits your needs

Our final call then looks like this:

`AutoManageModule(typeof(MyModule).Name, typeof(MyModule), true, config.MyModuleProperty.Enabled);`

And that's it. Your module will now be managed automatically. Next steps would be providing a user interface element that controls your module.
