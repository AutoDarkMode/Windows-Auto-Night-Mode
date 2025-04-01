
# Development Note

## About this project

This project base on [Auto Dark Mode](https://github.com/AutoDarkMode/Windows-Auto-Night-Mode), this project upgrades it from WPF to WinUI 3. The project start at 23.02.2025.

## What's changed?

 1. The project is developed based on Windows App SDK 1.6 and .Net8.0. Thanks to WinUI, the UI style is more modern now. The program adopts MWWM framework, use [Xaml Styler](https://marketplace.visualstudio.com/items?itemName=TeamXavalon.XAMLStyler2022) for styling Xaml.
 2. The localization code is similar to the official WinUI example, I use the `string.GetLocalized`, it is code of [Template Studio for WinUI](https://github.com/microsoft/TemplateStudio). The x:Uid is not used, because the source code of Auto Dark Mode does not match it, localization is a huge project, and it is best to improve it as much as possible, rather than reinventing it.
 3. The format of some pages may change a little, because the MWWM framework makes the interface interaction more natural and may not need some additional acitons.
 4. Some localization resources have been added, which is due to the above-mentioned interface changes, and some interface changes are aimed at the consistency of interface style. Such as **TimePage**'s first settingscard: IsAutoThemeSwitching doesn't have header textblock.
 5. Also thanks to the MWWM architecture, I think every page can be updated with the Config file in real time, which does not require developers to add too much code, but only to initialize the page "reasonably".
 6. Delete some redundant code, such as the how to get localization part in AutoDarkMode.Lib.
 7. Replace the traditional png icon, because Windows App SDK does not directly support SVG icon. Use it after converting SVG icon into font resource file.
 8. We can still use the previous localization resx, but it needs to be converted into resw file. At the same time, there are some new key:

- PostponeButtonUnDelay
- EnterToApply
- UpdatesChannelBeta
- UpdatesChannelStable
- SettingsPageLanguage
- lblScripts
- lblSet

## Task progress

- [ ] App
- [ ] Localization (Finished: en-us, fr, ja, zh-hans)
- [x] TimePage (LackFunction: Postpone)
- [x] SwitchModesPage (LackFunction: BatteryDarkMode)
- [ ] AppsPage
- [ ] PersonalizationPage
- [ ] ScriptsPage
- [ ] DonationPage
- [ ] AboutPage
- [x] SettingsPage

## What needs constant attention?

1. Need the unification of variable naming, the source code is full of naming methods with different styles, which easily leads to difficulties in subsequent code maintenance. *[Code quality]*
2. The name of localized text is need to updated for the same reason as above. *[Code quality]*
