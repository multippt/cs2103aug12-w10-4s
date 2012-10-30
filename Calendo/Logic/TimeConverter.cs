﻿//@author Nicholas
using System;
using System.Collections.Generic;
using System.Text;
using Calendo.Data;
using Calendo.Diagnostics;

namespace Calendo.Logic
{
    public class TaskTime
    {
        public TaskTime()
        {
            this.Format = TimeFormat.NONE;
            this.Time = DateTime.Today;
            this.HasError = true;
        }
        public TaskTime(DateTime Time, TimeFormat Format)
        {
            this.Time = Time;
            this.Format = Format;
            this.HasError = false;
        }
        public TimeFormat Format
        {
            get;
            set;
        }
        public DateTime Time
        {
            get;
            set;
        }
        public bool HasError
        {
            get;
            set;
        }
    }

    public class TimeConverter
    {
        private const string ERROR_INVALIDDATETIME = "Specified Date or Time is invalid";
        private const int INVALID_VALUE = -1;
        private const int IS_24HOUR = 0;
        private const int IS_AM = 1;
        private const int IS_PM = 2;

        public TimeConverter()
        {

        }

        /// <summary>
        /// Converts a string to an integer
        /// </summary>
        /// <param name="str">Integer in string format</param>
        /// <returns>Return the converted numeric value. or -1 if conversion failed</returns>
        private int ConvertInt(string str)
        {
            try
            {
                return int.Parse(str);
            }
            catch
            {
                return INVALID_VALUE;
            }
        }

        /// <summary>
        /// Gets the TimeFormat associated with the date and time
        /// </summary>
        /// <param name="hasDate">Format has a date</param>
        /// <param name="hasTime">Format has a time</param>
        /// <returns>Returns TimeFormat value</returns>
        private TimeFormat GetFormat(bool hasDate, bool hasTime)
        {
            TimeFormat newTimeFormat = TimeFormat.NONE;
            if (hasDate)
            {
                newTimeFormat = TimeFormat.DATE;
            }
            if (hasTime)
            {
                newTimeFormat = TimeFormat.TIME;
            }
            if (hasDate && hasTime)
            {
                newTimeFormat = TimeFormat.DATETIME;
            }
            return newTimeFormat;
        }

        /// <summary>
        /// Is it a leap year?
        /// </summary>
        /// <param name="year">Year</param>
        /// <returns>Returns true if it is a leap year, false otherwise</returns>
        private bool IsLeapYear(int year)
        {
            bool isLeap = false;
            if (year % 4 == 0)
            {
                isLeap = true;
            }
            if (year % 100 == 0)
            {
                isLeap = false;
            }
            if (year % 400 == 0)
            {
                isLeap = true;
            }
            return isLeap;
        }

        /// <summary>
        /// Get the maximum number of days in the specified month
        /// </summary>
        /// <param name="month">Month</param>
        /// <param name="year">Year</param>
        /// <returns></returns>
        private int MaxDays(int month, int year)
        {
            if (month >= 1 && month <= 12 && year >= 0)
            {
                // Max days of each month
                int[] maxDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                if (IsLeapYear(year))
                {
                    // February has an extra day
                    maxDays[1] = 29;
                }
                return maxDays[month - 1];
            }
            else
            {
                return INVALID_VALUE;
            }
        }

        /// <summary>
        /// Attempts to convert a value to an integer
        /// </summary>
        /// <param name="value">Value to be converted</param>
        /// <param name="lowerBound">Converted value should be at least this amount</param>
        /// <param name="upperBound">Converted value should be at most this amount</param>
        /// <returns>Returns converted value, otherwise -1 on failure</returns>
        private int ConvertValue(string value, int lowerBound, int upperBound)
        {
            int convertedValue = ConvertInt(value);
            if (convertedValue < lowerBound)
            {
                return INVALID_VALUE;
            }
            if (convertedValue > upperBound)
            {
                return INVALID_VALUE;
            }
            return convertedValue;
        }

        /// <summary>
        /// Converts a string date and time to DateTime object
        /// </summary>
        /// <param name="date">Date in Day/Month/Year</param>
        /// <param name="time">Time in Hour/Minutes (24 hour)</param>
        /// <returns>Returns TaskTime object</returns>
        public TaskTime Convert(string date, string time)
        {
            date = SanitizeString(date);
            time = SanitizeString(time);

            bool isValidDate = true;
            bool isValidTime = true;
            bool hasError = false;

            // Convert date
            DateTime convertedTime = DateTime.Today;
            ConvertDate(date, ref convertedTime, ref hasError, ref isValidDate);

            // Convert time
            ConvertTime(time, ref convertedTime, ref hasError, ref isValidTime);

            if (hasError)
            {
                DebugTool.Alert(ERROR_INVALIDDATETIME);
            }

            TaskTime taskDuration = new TaskTime();
            taskDuration.Format = GetFormat(isValidDate, isValidTime);

            if (convertedTime < DateTime.Now)
            {
                if (taskDuration.Format == TimeFormat.TIME)
                {
                    // Date is actually the next day
                    convertedTime = convertedTime.AddDays(1);
                    taskDuration.Format = TimeFormat.DATETIME;
                }
            }
            taskDuration.Time = convertedTime;
            taskDuration.HasError = hasError;
            return taskDuration;
        }

        /// <summary>
        /// Replace null strings with a default
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Returns string if not null, empty string if null</returns>
        private string SanitizeString(string input)
        {
            if (input == null)
            {
                return "";
            }
            else
            {
                return input;
            }
        }

        private string GetSubstring(string input, int start)
        {
            if (input.Length >= start && start >= 0)
            {
                return input.Substring(start);
            }
            else
            {
                return "";
            }
        }

        private int ConvertAMPM(int hour, int flagAMPM)
        {
            bool isAM = (flagAMPM == IS_AM);
            bool isPM = (flagAMPM == IS_PM);

            if (hour >= 0)
            {
                if (hour == 12 && isAM)
                {
                    // 12 Midnight case
                    hour = 0;
                }
                else if (hour == 12 && isPM)
                {
                    // 12 Noon case
                    hour = 12;
                }
                else if (hour > 0 && isPM)
                {
                    // 1 to 11PM case
                    hour += 12;
                }
                else if (hour == 0 && isPM)
                {
                    // 0 PM case (0 AM is valid)
                    hour = INVALID_VALUE;
                }
            }
            return hour;
        }

        private int GetFlagAMPM(string timeString)
        {
            // Get last 2 letters
            string timeMeta = GetSubstring(timeString, timeString.Length - 2); 
            timeMeta = timeMeta.ToUpper();

            // Handle AM or PM
            int flagAMPM = IS_24HOUR;
            if (timeMeta == "AM")
            {
                flagAMPM = IS_AM;
            }
            if (timeMeta == "PM")
            {
                flagAMPM = IS_PM;
            }
            return flagAMPM;
        }

        /// <summary>
        /// Converts time to DateTime
        /// </summary>
        /// <param name="timeString">Input time</param>
        /// <param name="newTime">DateTime object to modify</param>
        /// <param name="hasError">Has Error</param>
        /// <param name="isValidTime">Is Valid Time</param>
        private void ConvertTime(string timeString, ref DateTime newTime, ref bool hasError, ref bool isValidTime)
        {
            // Hour must be specified, Minute is optional
            int hour = INVALID_VALUE;
            int minute = 0;
            
            // Handle AM or PM
            int flagAMPM = GetFlagAMPM(timeString);
            if (flagAMPM != IS_24HOUR)
            {
                // Get the remainder excluding AM and PM
                timeString = timeString.Substring(0, timeString.Length - 2);
                timeString = timeString.Trim();
            }

            string[] timeFragment = timeString.Split(new char[] { ':', '.' }, 2);

            // Hour
            if (timeFragment.Length > 0)
            {
                int minHour = 0;
                int maxHour = 24;
                if (flagAMPM != IS_24HOUR)
                {
                    maxHour = 12;
                }
                hour = this.ConvertValue(timeFragment[0], minHour, maxHour);
                hour = this.ConvertAMPM(hour, flagAMPM);
            }

            // Minute
            if (timeFragment.Length > 1)
            {
                minute = this.ConvertValue(timeFragment[1], 0, 59);
            }

            if (timeString == "")
            {
                // No time supplied
                isValidTime = false;
            }
            else if (hour == INVALID_VALUE || minute == INVALID_VALUE)
            {
                hasError = true;
                isValidTime = false;
            }

            if (isValidTime)
            {
                newTime = newTime.AddHours(hour);
                newTime = newTime.AddMinutes(minute);
            }
        }

        /// <summary>
        /// Converts date to DateTime
        /// </summary>
        /// <param name="time">Input date</param>
        /// <param name="newTime">DateTime object to modify</param>
        /// <param name="hasError">Has Error</param>
        /// <param name="isValidTime">Is Valid Time</param>
        private void ConvertDate(string date, ref DateTime newDate, ref bool hasError, ref bool isValidDate)
        {
            bool isYearProvided = false;
            // Day and Month must be specified, Year is optional
            int year = DateTime.Today.Year;
            int day = INVALID_VALUE;
            int month = INVALID_VALUE;

            // Date: Day/Month[/Year]
            string[] dateFragment = date.Split(new char[] { '/', '.' }, 3);

            // Year (optional)
            if (dateFragment.Length > 2)
            {
                // Year must be 4 digits
                year = this.ConvertValue(dateFragment[2], 1000, 9999);
                isYearProvided = true;
            }

            // Month
            if (dateFragment.Length > 1)
            {
                month = this.ConvertValue(dateFragment[1], 1, 12);
            }

            // Day
            if (dateFragment.Length > 0)
            {
                day = this.ConvertValue(dateFragment[0], 1, MaxDays(month, year));
            }

            if (date == "")
            {
                // No date provided
                isValidDate = false;
            }
            else if (year == INVALID_VALUE || month == INVALID_VALUE || day == INVALID_VALUE)
            {
                // Date provided, but there are errors
                hasError = true;
                isValidDate = false;
            }

            if (isValidDate)
            {
                newDate = new DateTime(year, month, day);
                if (newDate < DateTime.Today)
                {
                    if (!isYearProvided)
                    {
                        // Event occurs next year
                        newDate = newDate.AddYears(1);
                    }
                    else
                    {
                        // Due to requests, checking for dates in the past is disabled
                        //hasError = true;
                        //isValidDate = false;
                    }
                }
            }
        }
    }
}
