# Windows 10 Auto-Night-Mode
Automatically switches between the dark and light theme of Windows 10
![Image](https://github.com/Armin2208/Windows-Auto-Night-Mode/blob/master/screenshot.png)

## Description
Microsoft provides a dark theme in Windows 10. You can switch manually between the implemented dark and white theme in the settings app. On the other side some programs or operating systems are allowing a automatic switch at a specific time. While it is bright outside, you have a bright OS. At afternoon it begins to get darker, so your operating system also switches to a darker look to take care of your eyes.

I wanted to have this kind of solution in Windows, so I wrote a little program.

With enabling the automatic theme switcher in the app, it creates a task in the task sheduler of Windows. This task will start the app with the right arguments. No background task, no interruption of a cmd-window. You can set your own preferred start-times in the user interface.

## Planned features
- Force grey searchbox in taskbar.
- Set time at sunset/sunrise with location service.
- Appx-File & Microsoft Store release.
- Better user interface with more feedback and information.
- Respect 1903 changes.
