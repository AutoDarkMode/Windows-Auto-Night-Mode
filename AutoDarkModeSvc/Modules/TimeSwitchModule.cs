using System;
using System.Collections.Generic;
using System.Text;
using AutoDarkModeSvc.Handler;
using AutoDarkModeApp.Config;
using AutoDarkModeApp;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : IAutoDarkModeModule
    {
        AutoDarkModeConfigBuilder Builder { get; }
        public TimeSwitchModule(string name)
        {
            Builder = AutoDarkModeConfigBuilder.GetInstance();
            Name = name;
        }

        public string Name { get; }

        public void Poll()
        {
            Task.Run(() =>
            {
                RegistryHandler rh = new RegistryHandler();
                int hour = DateTime.Now.Hour;
                int minute = DateTime.Now.Minute;
                int[] sunrise = new int[2];
                int[] sunset = new int[2];
                try
                {
                    DateTime sunriseDT = Convert.ToDateTime(Builder.Config.SunRise);
                    DateTime sunsetDT = Convert.ToDateTime(Builder.Config.SunSet);
                    sunrise[0] = sunriseDT.Hour;
                    sunrise[1] = sunriseDT.Minute;
                    sunset[0] = sunsetDT.Hour;
                    sunset[1] = sunsetDT.Minute;
                }
                catch
                {
                    //todo: LOGGER!
                    Console.WriteLine("Malformed JSON config for sunrise or sunset times");

                }

                if (!Builder.Config.Location.Disabled)
                {
                    DateTime[] suntimes = CalculateSunTimes();
                    DateTime sunriseDT = suntimes[0];
                    DateTime sunsetDT = suntimes[1];
                    sunrise[0] = sunriseDT.Hour;
                    sunrise[1] = sunriseDT.Minute;
                    sunset[0] = sunsetDT.Hour;
                    sunset[1] = sunsetDT.Minute;
                }

                if (hour < sunrise[0] || hour >= sunset[0])
                {
                    if (hour == sunset[0])
                    {
                        if (minute < sunset[1])
                        {
                            rh.ThemeToLight();
                        }
                        if (minute >= sunset[1])
                        {
                            rh.ThemeToDark();
                        }
                    }
                    else
                    {
                        rh.ThemeToDark();
                    }
                }
                else if (hour >= sunrise[0] || hour < sunset[0])
                {
                    if (hour == sunrise[0])
                    {
                        if (minute < sunrise[1])
                        {
                            rh.ThemeToDark();
                        }
                        if (minute >= sunrise[1])
                        {
                            rh.ThemeToLight();
                        }
                    }
                    else
                    {
                        rh.ThemeToLight();
                    }
                }
            });
            
        }

        private DateTime[] CalculateSunTimes()
        {
            int[] sundate = new int[4];
            int[] sun = SunDate.CalculateSunriseSunset(Builder.Config.Location.Lat, Builder.Config.Location.Lon);

            //Add offset to sunrise and sunset hours using Settings
            DateTime sunrise = new DateTime(1, 1, 1, sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0);
            sunrise = sunrise.AddMinutes(Builder.Config.Location.SunRiseOffsetMin);

            DateTime sunset = new DateTime(1, 1, 1, sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0);
            sunset = sunset.AddMinutes(Builder.Config.Location.SunSetOffsetMin);

            sundate[0] = sunrise.Hour; //sunrise hour
            sundate[1] = sunrise.Minute; //sunrise minute
            sundate[2] = sunset.Hour; //sunset hour
            sundate[3] = sunset.Minute; //sunset minute
            return new [] {sunrise, sunset};
        }

    }
}
