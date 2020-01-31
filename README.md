# Windows 10 Auto Dark Mode
Automatically switches between the dark and light theme of Windows 10 at sheduled times
![Screenshot](https://github.com/Armin2208/Windows-Auto-Night-Mode/blob/master/screenshot.png)

## Description
Microsoft provides a dark theme in Windows 10. You can switch manually between the implemented dark and white theme in the Settings app. On the other hand some programs or operating systems are allowing a automatic switch at a specific time. While it is bright outside, you have a bright and clearly readable OS. At afternoon the sun starts to set and your operating system also switches to a darker look to take care of your eyes.

I wanted to have this kind of solution in Windows, so I wrote this little program.

You can set your own preferred start-times in the user interface.

## Features
- Easy to use and clean user-interface.
- Theme switch based on custom times.
- Theme switch based on the sunrise and sunset of your location.
- Desktop background switch.
- Easy theme switch with the Jump-List from the Startmenu or Taskbar.
- You can choose if only Apps should change their theme, or both apps and the system.
- Support for Accent Color on the Taskbar and other system elements.
- Lightweight with 100% clean uninstall. No admin-rights needed.

## Download
[Click here to download the newest version of Windows Auto Dark Mode!](https://github.com/Armin2208/Windows-Auto-Night-Mode/releases)

#### A note about the download
Windows Smartscreen and some antivirus software may warn you from downloading this program. This is caused to the small user base and missing certificate. Sadly I can't change anything about that.

## Telegram Group
[Join my Telegram group to get early access to new versions](https://t.me/autodarkmode)

# For developers: adding new modules

In case you want to contribute and add a new module, here's how:

### Understanding how a module works

AutoDarkMode uses a modular timer based system. Each module is registered or deregistered to a specific timer when it is enabled or disabled. The first step therefore usually consists of creating an `Enabled` property or config class for your module in `Config/AutoDarkModeConfig.cs`.
In order to then create a module let's take a look at what a module class looks like:
```C#
namespace AutoDarkModeSvc.Modules
{
  class MyModule : AutoDarkModeModule
  {
     private AutoDarkModeConfigBuilder ConfigBuilder { get; }
     public override string TimerAffinity { get; } = TimerName.Main;
     public MyModule(string name)
     {
         Name = name;
         //uncomment the line below if you want a module to execute immediately after it has been registered
         //Fire()
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
A module needs to have a constructor with exactly one string parameter which is set as `Name`.

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
    AutoDarkModeConfig config = ConfigBuilder.Config;
    AutoManageModule(typeof(TimeSwitchModule).Name, typeof(TimeSwitchModule), config.AutoThemeSwitchingEnabled);
    AutoManageModule(typeof(GeopositionUpdateModule).Name, typeof(GeopositionUpdateModule), config.Location.Enabled);
  }
```

To add a module, call AutoManageModule with type signature `AutoManageModule#(String, Type, Bool)` and takes the following parameters:
- Name: Derived from the className so you can use `typeof(MyModule).Name`
- Type: The module's class used for object instantiation, this is always `typeof(MyModule)`
- Enabled: A boolean value that indicates whether the module should be running currently. Point it to your `Enabled` Property that you created in the configuration file or use an existing one if it fits your needs

Our final call then looks like this:

`AutoManageModule(typeof(MyModule).Name, typeof(MyModule), config.MyModuleProperty.Enabled);`

And that's it. Your module will now be managed automatically. Next steps would be providing a user interface element that controls your module.
