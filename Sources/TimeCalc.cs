/*
    Copyright (c) 2005-2012 Fedir Nepyivoda <fednep@gmail.com>
  
    This file is part of UV Outliner project.
    http://uvoutliner.com

    UV Outliner is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    UV Outliner is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with UV Outliner.  If not, see <http://www.gnu.org/licenses/>
 
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace UVOutliner
{
    class TimeIntervalException : Exception 
    {
        public TimeIntervalException() : base() { }
        public TimeIntervalException(string s) : base(s) { }
    }

    class TimeInterval
    {
        long interval;     // seconds

        long secondsInMinute = 60;
        long minutesInHour = 60;        
        long hoursInDay = 4;
        long daysInWeek = 5;
        long weeksInMonth = 4;

        public TimeInterval()
        {
            interval = 0;
        }

        public TimeInterval(long interval)
        {
            this.interval = interval;
        }

        public TimeInterval(string initialLength)
        {
            interval = TryParse(initialLength);
        }

        private enum ParsePhase {pfNone, pfDigit}

        public long TryParse(string str)
        {
            long total = 0;
            long currentNum = 0;
            ParsePhase currentPhase = ParsePhase.pfNone;
            str += " ";
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ' ')
                {
                    if (currentPhase != ParsePhase.pfNone)
                        throw new TimeIntervalException("Time interval parse error");
                }
                else if (str[i] >= '0' && str[i] <= '9')
                {
                    currentNum *= 10;
                    currentNum += Convert.ToInt16(str[i]) - Convert.ToInt16('0');
                    currentPhase = ParsePhase.pfDigit;
                }
                else
                {
                    if (currentPhase != ParsePhase.pfDigit)
                        throw new TimeIntervalException("Interval modifier before number");

                    switch (str[i])
                    {
                        case 's': total += currentNum; break;
                        case 'm': total += currentNum * secondsInMinute; break;
                        case 'h': total += currentNum * secondsInMinute * minutesInHour; break;

                        case 'd': total += currentNum * hoursInDay * secondsInMinute * minutesInHour; break;
                        case 'w': total += currentNum * daysInWeek * hoursInDay * secondsInMinute * minutesInHour; break;
                        case 'M': total += currentNum * weeksInMonth * daysInWeek * hoursInDay * secondsInMinute * minutesInHour; break;

                        default: throw new TimeIntervalException("Unknown time interval modifier");
                    }
                    currentNum = 0;
                    currentPhase = ParsePhase.pfNone;
                }
            }
            return total;
        }

        public override string ToString()
        {
            long ts = interval;
            long seconds = ts % secondsInMinute;
            ts /= secondsInMinute;
            long minutes = ts % minutesInHour;
            ts /= minutesInHour;
            long hours = ts % hoursInDay;
            ts /= hoursInDay;
            long days = ts % daysInWeek;
            ts /= daysInWeek;
            long weeks = ts % weeksInMonth;
            ts /= weeksInMonth;
            long months = ts;

            string res = "";
            if (months > 0)
                res += string.Format("{0}M ", months);
            if (weeks > 0)
                res += string.Format("{0}w ", weeks);
            if (days > 0)
                res += string.Format("{0}d ", days);
            if (hours > 0)
                res += string.Format("{0}h ", hours);
            if (minutes > 0)
                res += string.Format("{0}m ", minutes);
            if (seconds > 0)
                res += string.Format("{0}s ", seconds);

            return res.Trim();
        }

        public static TimeInterval operator +(TimeInterval i1, TimeInterval i2)
        {
            return new TimeInterval(i1.Interval + i2.Interval);            
        }

        public long Interval
        {
            get { return interval; }
            set { interval = value; }
        }
    }
}
