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
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers
{
    public sealed class Sunriset
    {
#pragma warning disable IDE1006, IDE0051, IDE0018, IDE0054
        // Copyright 2017 Aurélien Dussauge
        //
        // Licensed under the Apache License, Version 2.0 (the "License");
        // you may not use this file except in compliance with the License.
        // You may obtain a copy of the License at
        //
        //     http://www.apache.org/licenses/LICENSE-2.0
        //
        // Unless required by applicable law or agreed to in writing, software
        // distributed under the License is distributed on an "AS IS" BASIS,
        // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        // See the License for the specific language governing permissions and
        // limitations under the License.
        // https://github.com/Mursaat/SunriseSunset
        private Sunriset()
        {

        }

        private const double SunriseSunsetAltitude = -35d / 60d;
        private const double CivilTwilightAltitude = -6d;
        private const double NauticalTwilightAltitude = -12d;
        private const double AstronomicalTwilightAltitude = -18d;

        /// <summary>
        /// Compute sunrise/sunset times UTC
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month of year</param>
        /// <param name="day">The day of month</param>
        /// <param name="lat">The latitude</param>
        /// <param name="lng">The longitude</param>
        /// <param name="tsunrise">The computed sunrise time (in seconds)</param>
        /// <param name="tsunset">The computed sunset time (in seconds)</param>
        public static void SunriseSunset(int year, int month, int day, double lat, double lng, out double tsunrise, out double tsunset)
        {
            SunriseSunset(year, month, day, lng, lat, SunriseSunsetAltitude, true, out tsunrise, out tsunset);
        }

        /// <summary>
        /// Compute civil twilight times UTC
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month of year</param>
        /// <param name="day">The day of month</param>
        /// <param name="lat">The latitude</param>
        /// <param name="lng">The longitude</param>
        /// <param name="tsunrise">The computed civil twilight time at sunrise (in seconds)</param>
        /// <param name="tsunset">The computed civil twilight time at sunset (in seconds)</param>
        public static void CivilTwilight(int year, int month, int day, double lat, double lng, out double tsunrise, out double tsunset)
        {
            SunriseSunset(year, month, day, lng, lat, CivilTwilightAltitude, false, out tsunrise, out tsunset);
        }

        /// <summary>
        /// Compute nautical twilight times UTC
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month of year</param>
        /// <param name="day">The day of month</param>
        /// <param name="lat">The latitude</param>
        /// <param name="lng">The longitude</param>
        /// <param name="tsunrise">The computed nautical twilight time at sunrise (in seconds)</param>
        /// <param name="tsunset">The computed nautical twilight time at sunset (in seconds)</param>
        public static void NauticalTwilight(int year, int month, int day, double lat, double lng, out double tsunrise, out double tsunset)
        {
            SunriseSunset(year, month, day, lng, lat, NauticalTwilightAltitude, false, out tsunrise, out tsunset);
        }

        /// <summary>
        /// Compute astronomical twilight times UTC
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month of year</param>
        /// <param name="day">The day of month</param>
        /// <param name="lat">The latitude</param>
        /// <param name="lng">The longitude</param>
        /// <param name="tsunrise">The computed astronomical twilight time at sunrise (in seconds)</param>
        /// <param name="tsunset">The computed astronomical twilight time at sunset (in seconds)</param>
        public static void AstronomicalTwilight(int year, int month, int day, double lat, double lng, out double tsunrise, out double tsunset)
        {
            SunriseSunset(year, month, day, lng, lat, AstronomicalTwilightAltitude, false, out tsunrise, out tsunset);
        }

        /* +++Date last modified: 05-Jul-1997 */
        /* Updated comments, 05-Aug-2013 */

        /*
			SUNRISET.C - computes Sun rise/set times, start/end of twilight, and
			the length of the day at any date and latitude
			Written as DAYLEN.C, 1989-08-16
			Modified to SUNRISET.C, 1992-12-01
			(c) Paul Schlyter, 1989, 1992
			Released to the public domain by Paul Schlyter, December 1992
		*/

        /* Converted to C# by Mursaat 05-Feb-2017 */

        /// <summary>
        /// A function to compute the number of days elapsed since 2000 Jan 0.0
        /// (which is equal to 1999 Dec 31, 0h UT)
        /// </summary>
        /// <param name="y"></param>
        /// <param name="m"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private static long daysSince2000Jan0(int y, int m, int d)
        {
            return (367L * y - ((7 * (y + ((m + 9) / 12))) / 4) + ((275 * m) / 9) + d - 730530L);
        }

        /* Some conversion factors between radians and degrees */
        private const double RadDeg = 180.0 / Math.PI;
        private const double DegRad = Math.PI / 180.0;

        /* The trigonometric functions in degrees */
        private static double sind(double x)
        {
            return Math.Sin(x * DegRad);
        }

        private static double cosd(double x)
        {
            return Math.Cos(x * DegRad);
        }

        private static double tand(double x)
        {
            return Math.Tan(x * DegRad);
        }

        private static double atand(double x)
        {
            return RadDeg * Math.Atan(x);
        }

        private static double asind(double x)
        {
            return RadDeg * Math.Asin(x);
        }

        private static double acosd(double x)
        {
            return RadDeg * Math.Acos(x);
        }

        private static double atan2d(double y, double x)
        {
            return RadDeg * Math.Atan2(y, x);
        }

        /// <summary>
        /// The "workhorse" function for sun rise/set times
        /// Note: year,month,date = calendar date, 1801-2099 only.
        /// Eastern longitude positive, Western longitude negative
        /// Northern latitude positive, Southern latitude negative
        /// The longitude value IS critical in this function!
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="lon"></param>
        /// <param name="lat"></param>
        /// <param name="altit">
        /// the altitude which the Sun should cross
        /// Set to -35/60 degrees for rise/set, -6 degrees
        /// for civil, -12 degrees for nautical and -18
        /// degrees for astronomical twilight.
        /// </param>
        /// <param name="upper_limb">
        /// true -> upper limb, false -> center
        /// Set to true (e.g. 1) when computing rise/set
        /// times, and to false when computing start/end of twilight.
        /// </param>
        /// <param name="trise">where to store the rise time</param>
        /// <param name="tset">where to store the set time</param>
        /// <returns>
        ///  0	=	sun rises/sets this day, times stored at trise and tset
        /// +1	=	sun above the specified "horizon" 24 hours.
        ///			trise set to time when the sun is at south,
        ///			minus 12 hours while *tset is set to the south
        ///			time plus 12 hours. "Day" length = 24 hours
        /// -1	=	sun is below the specified "horizon" 24 hours
        ///			"Day" length = 0 hours, *trise and *tset are
        ///			both set to the time when the sun is at south.
        /// </returns>
        private static int SunriseSunset(int year, int month, int day, double lon, double lat,
                         double altit, bool upper_limb, out double trise, out double tset)
        {
            double d;          /* Days since 2000 Jan 0.0 (negative before) */
            double sr;         /* Solar distance, astronomical units */
            double sRA;        /* Sun's Right Ascension */
            double sdec;       /* Sun's declination */
            double sradius;    /* Sun's apparent radius */
            double t;          /* Diurnal arc */
            double tsouth;     /* Time when Sun is at south */
            double sidtime;    /* Local sidereal time */

            int rc = 0; /* Return cde from function - usually 0 */

            /* Compute d of 12h local mean solar time */
            d = daysSince2000Jan0(year, month, day) + 0.5 - lon / 360.0;

            /* Compute the local sidereal time of this moment */
            sidtime = revolution(GMST0(d) + 180.0 + lon);

            /* Compute Sun's RA, Decl and distance at this moment */
            sun_RA_dec(d, out sRA, out sdec, out sr);

            /* Compute time when Sun is at south - in hours UT */
            tsouth = 12.0 - rev180(sidtime - sRA) / 15.0;

            /* Compute the Sun's apparent radius in degrees */
            sradius = 0.2666 / sr;

            /* Do correction to upper limb, if necessary */
            if (upper_limb)
                altit -= sradius;

            /* Compute the diurnal arc that the Sun traverses to reach */
            /* the specified altitude altit: */
            {
                double cost;
                cost = (sind(altit) - sind(lat) * sind(sdec)) /
                (cosd(lat) * cosd(sdec));
                if (cost >= 1.0) /* Sun always below altit */
                {
                    rc = -1;
                    t = 0.0;
                }
                else if (cost <= -1.0) /* Sun always above altit */
                {
                    rc = +1;
                    t = 12.0;
                }
                else
                {
                    t = acosd(cost) / 15.0;   /* The diurnal arc, hours */
                }
            }

            /* Store rise and set times - in hours UT */
            trise = tsouth - t;
            tset = tsouth + t;

            return rc;
        }

        /// <summary>
        /// Note: year,month,date = calendar date, 1801-2099 only.
        /// Eastern longitude positive, Western longitude negative
        /// Northern latitude positive, Southern latitude negative
        /// The longitude value is not critical. Set it to the correct
        /// The latitude however IS critical - be sure to get it correct
        /// </summary>
        /// <param name="year">
        /// altit = the altitude which the Sun should cross
        /// Set to -35/60 degrees for rise/set, -6 degrees
        /// for civil, -12 degrees for nautical and -18
        /// degrees for astronomical twilight.
        /// </param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="lon"></param>
        /// <param name="lat"></param>
        /// <param name="altit"></param>
        /// <param name="upper_limb">
        /// true -> upper limb, true -> center
        /// Set to true (e.g. 1) when computing day length
        /// and to false when computing day+twilight length.
        /// </param>
        /// <returns></returns>
        public static double DayLen(int year, int month, int day, double lon, double lat,
                          double altit, bool upper_limb)
        {
            double d;          /* Days since 2000 Jan 0.0 (negative before) */
            double obl_ecl;    /* Obliquity (inclination) of Earth's axis */
            double sr;         /* Solar distance, astronomical units */
            double slon;       /* True solar longitude */
            double sin_sdecl;  /* Sine of Sun's declination */
            double cos_sdecl;  /* Cosine of Sun's declination */
            double sradius;    /* Sun's apparent radius */
            double t;          /* Diurnal arc */

            /* Compute d of 12h local mean solar time */
            d = daysSince2000Jan0(year, month, day) + 0.5 - lon / 360.0;

            /* Compute obliquity of ecliptic (inclination of Earth's axis) */
            obl_ecl = 23.4393 - 3.563E-7 * d;

            /* Compute Sun's ecliptic longitude and distance */
            sunpos(d, out slon, out sr);

            /* Compute sine and cosine of Sun's declination */
            sin_sdecl = sind(obl_ecl) * sind(slon);
            cos_sdecl = Math.Sqrt(1.0 - sin_sdecl * sin_sdecl);

            /* Compute the Sun's apparent radius, degrees */
            sradius = 0.2666 / sr;

            /* Do correction to upper limb, if necessary */
            if (upper_limb)
            {
                altit -= sradius;
            }

            /* Compute the diurnal arc that the Sun traverses to reach */
            /* the specified altitude altit: */
            double cost = (sind(altit) - sind(lat) * sin_sdecl) / (cosd(lat) * cos_sdecl);

            /* Sun always below altit */
            if (cost >= 1.0)
            {
                t = 0.0;
            }
            /* Sun always above altit */
            else if (cost <= -1.0)
            {
                t = 24.0;
            }
            /* The diurnal arc, hours */
            else
            {
                t = (2.0 / 15.0) * acosd(cost);
            }

            return t;
        }

        /// <summary>
        /// Computes the Sun's ecliptic longitude and distance
        /// at an instant given in d, number of days since
        /// 2000 Jan 0.0.  The Sun's ecliptic latitude is not
        /// computed, since it's always very near 0.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="lon"></param>
        /// <param name="r"></param>
        private static void sunpos(double d, out double lon, out double r)
        {
            double M;         /* Mean anomaly of the Sun */
            double w;         /* Mean longitude of perihelion */
            /* Note: Sun's mean longitude = M + w */
            double e;         /* Eccentricity of Earth's orbit */
            double E;         /* Eccentric anomaly */
            double x, y;      /* x, y coordinates in orbit */
            double v;         /* True anomaly */

            /* Compute mean elements */
            M = revolution(356.0470 + 0.9856002585 * d);
            w = 282.9404 + 4.70935E-5 * d;
            e = 0.016709 - 1.151E-9 * d;

            /* Compute true longitude and radius vector */
            E = M + e * RadDeg * sind(M) * (1.0 + e * cosd(M));
            x = cosd(E) - e;
            y = Math.Sqrt(1.0 - e * e) * sind(E);
            r = Math.Sqrt(x * x + y * y);       /* Solar distance */
            v = atan2d(y, x);                   /* True anomaly */
            lon = v + w;                        /* True solar longitude */
            if (lon >= 360.0)
            {
                lon -= 360.0;                   /* Make it 0..360 degrees */
            }
        }

        /// <summary>
        /// Computes the Sun's equatorial coordinates RA, Decl
        /// and also its distance, at an instant given in d,
        /// the number of days since 2000 Jan 0.0.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="RA"></param>
        /// <param name="dec"></param>
        /// <param name="r"></param>
        private static void sun_RA_dec(double d, out double RA, out double dec, out double r)
        {
            double lon, obl_ecl, x, y, z;

            /* Compute Sun's ecliptical coordinates */
            sunpos(d, out lon, out r);

            /* Compute ecliptic rectangular coordinates (z=0) */
            x = r * cosd(lon);
            y = r * sind(lon);

            /* Compute obliquity of ecliptic (inclination of Earth's axis) */
            obl_ecl = 23.4393 - 3.563E-7 * d;

            /* Convert to equatorial rectangular coordinates - x is unchanged */
            z = y * sind(obl_ecl);
            y = y * cosd(obl_ecl);

            /* Convert to spherical coordinates */
            RA = atan2d(y, x);
            dec = atan2d(z, Math.Sqrt(x * x + y * y));
        }

        private const double INV360 = 1.0d / 360.0d;

        /// <summary>
        /// This function reduces any angle to within the first revolution
        /// by subtracting or adding even multiples of 360.0 until the
        /// result is >= 0.0 and < 360.0
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static double revolution(double x)
        {
            return (x - 360.0 * Math.Floor(x * INV360));
        }

        /// <summary>
        /// Reduce angle to within +180..+180 degrees
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static double rev180(double x)
        {
            return (x - 360.0 * Math.Floor(x * INV360 + 0.5));
        }

        /// <summary>
        /// This function computes GMST0, the Greenwich Mean Sidereal Time
        /// at 0h UT (i.e. the sidereal time at the Greenwhich meridian at
        /// 0h UT).  GMST is then the sidereal time at Greenwich at any
        /// time of the day.  I've generalized GMST0 as well, and define it
        /// as:  GMST0 = GMST - UT  --  this allows GMST0 to be computed at
        /// other times than 0h UT as well.
        ///
        /// While this sounds somewhat contradictory, it is very practical:
        /// instead of computing  GMST like:
        /// GMST = (GMST0) + UT * (366.2422/365.2422)
        /// where (GMST0) is the GMST last time UT was 0 hours, one simply
        /// computes: GMST = GMST0 + UT
        /// where GMST0 is the GMST "at 0h UT" but at the current moment!
        ///
        /// Defined in this way, GMST0 will increase with about 4 min a
        /// day.  It also happens that GMST0 (in degrees, 1 hr = 15 degr)
        /// is equal to the Sun's mean longitude plus/minus 180 degrees!
        /// (if we neglect aberration, which amounts to 20 seconds of arc
        /// or 1.33 seconds of time)
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private static double GMST0(double d)
        {
            double sidtim0;
            /* Sidtime at 0h UT = L (Sun's mean longitude) + 180.0 degr  */
            /* L = M + w, as defined in sunpos().  Since I'm too lazy to */
            /* add these numbers, I'll let the C compiler do it for me.  */
            /* Any decent C compiler will add the constants at compile   */
            /* time, imposing no runtime or code overhead.               */
            sidtim0 = revolution((180.0 + 356.0470 + 282.9404) + (0.9856002585 + 4.70935E-5) * d);
            return sidtim0;
        }
        #pragma warning restore IDE1006, IDE0051, IDE0018, IDE0054
    }
}
