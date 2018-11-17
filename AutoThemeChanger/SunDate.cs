using System;

namespace AutoThemeChanger
{
    // source: https://github.com/DrKLO/Telegram/blob/master/TMessagesProj/src/main/java/org/telegram/messenger/time/SunDate.java
    // ported to c#
    public class SunDate
    {
        private const double DEGRAD = Math.PI / 180.0;
        private const double RADEG = 180.0 / Math.PI;
        private const double INV360 = 1.0 / 360.0;

        private static long Days_since_2000_Jan_0(int y, int m, int d)
        {
            return 367L * y - ((7 * (y + ((m + 9) / 12))) / 4) + ((275 * m) / 9) + d - 730530L;
        }

        private static double Revolution(double x)
        {
            return x - 360.0 * Math.Floor(x * INV360);
        }

        private static double Rev180(double x)
        {
            return x - 360.0 * Math.Floor(x * INV360 + 0.5);
        }

        private static double GMST0(double d)
        {
            return Revolution((180.0 + 356.0470 + 282.9404) + (0.9856002585 + 4.70935E-5) * d);
        }

        private static double Sind(double x)
        {
            return Math.Sin(x * DEGRAD);
        }

        private static double Cosd(double x)
        {
            return Math.Cos(x * DEGRAD);
        }

        private static double Tand(double x)
        {
            return Math.Tan(x * DEGRAD);
        }

        private static double Acosd(double x)
        {
            return RADEG * Math.Acos(x);
        }

        private static double Atan2d(double y, double x)
        {
            return RADEG * Math.Atan2(y, x);
        }

        private static void SunposAtDay(double p, double[] ot, double[] d)
        {
            double S, a, V, l, k, i;

            S = Revolution(356.0470 + 0.9856002585 * p);
            l = 282.9404 + 4.70935E-5 * p;
            a = 0.016709 - 1.151E-9 * p;

            V = a * RADEG * Sind(S) * (1.0 + a * Cosd(S)) + S;
            k = Cosd(V) - a;

            i = Math.Sqrt(1.0 - a * a) * Sind(V);
            d[0] = Math.Sqrt(k * k + i * i);
            i = Atan2d(i, k);
            ot[0] = i + l;
            if (ot[0] >= 360.0)
            {
                ot[0] -= 360.0;
            }
        }

        private static void Sun_RA_decAtDay(double d, double[] RA, double[] dec, double[] r)
        {
            double[] lon = new double[1];
            double obl_ecl;
            double xs, ys;
            double xe, ye, ze;

            SunposAtDay(d, lon, r);

            xs = r[0] * Cosd(lon[0]);
            ys = r[0] * Sind(lon[0]);

            obl_ecl = 23.4393 - 3.563E-7 * d;

            xe = xs;
            ye = ys * Cosd(obl_ecl);
            ze = ys * Sind(obl_ecl);

            RA[0] = Atan2d(ye, xe);
            dec[0] = Atan2d(ze, Math.Sqrt(xe * xe + ye * ye));
        }

        private static int SunRiseSetHelperForYear(int year, int month, int day, double lon, double lat, double altit, int upper_limb, double[] sun)
        {
            double[] sRA = new double[1];
            double[] sdec = new double[1];
            double[] sr = new double[1];

            double d, sradius, t, tsouth, sidtime;
            int rc = 0;
            d = Days_since_2000_Jan_0(year, month, day) + 0.5 - lon / 360.0;
            sidtime = Revolution(GMST0(d) + 180.0 + lon);
            Sun_RA_decAtDay(d, sRA, sdec, sr);
            tsouth = 12.0 - Rev180(sidtime - sRA[0]) / 15.0;
            sradius = 0.2666 / sr[0];
            if (upper_limb != 0)
            {
                altit -= sradius;
            }

            double cost;
            cost = (Sind(altit) - Sind(lat) * Sind(sdec[0])) / (Cosd(lat) * Cosd(sdec[0]));
            if (cost >= 1.0)
            {
                rc = -1;
                t = 0.0;
            }
            else if (cost <= -1.0)
            {
                rc = +1;
                t = 12.0;
            }
            else
            {
                t = Acosd(cost) / 15.0;
            }
            sun[0] = tsouth - t;
            sun[1] = tsouth + t;

            return rc;
        }

        private static int SunRiseSetForYear(int year, int month, int day, double lon, double lat, double[] sun)
        {
            return SunRiseSetHelperForYear(year, month, day, lon, lat, (-35.0 / 60.0), 1, sun);
        }

        public static int[] CalculateSunriseSunset(double lat, double lon)
        {
            //Calendar calendar = Calendar.getInstance();
            //calendar.setTimeInMillis(System.currentTimeMillis());
            DateTime calendar = DateTime.Now;
            double[] sun = new double[2];
            SunRiseSetForYear(calendar.Year, calendar.Month, calendar.Day, lon, lat, sun); // calendar.get(Calendar.YEAR), calendar.get(Calendar.MONTH) + 1, calendar.get(Calendar.DAY_OF_MONTH), lon, lat, sun);
            int timeZoneOffset = (int)TimeZoneInfo.Local.GetUtcOffset(calendar).TotalMinutes; // TimeZone.getDefault().getOffset(System.currentTimeMillis()) / 1000 / 60;
            int sunrise = (int)(sun[0] * 60) + timeZoneOffset;
            int sunset = (int)(sun[1] * 60) + timeZoneOffset;
            if (sunrise < 0)
            {
                sunrise += 60 * 24;
            }
            else if (sunrise > 60 * 24)
            {
                sunrise -= 60 * 24;
            }
            if (sunset < 0)
            {
                sunset += 60 * 24;
            }
            else if (sunset > 60 * 24)
            {
                sunset += 60 * 24;
            }
            return new int[] { sunrise, sunset };
        }
    }
}
