﻿using CANDefinitions;
using CANHandler;
using OpenSkipperApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANMessages
{
    public class N2kMessages
    {
        public enum KnownPGNs
        {
            PGN128275 = 128275,
            PGN130310 = 130310
        }

        /// <summary>
        /// Creates 130310: Environmental Parameters frame
        /// </summary>
        /// <param name="SID"></param>
        /// <param name="waterTemp">Water Temperature in Kelvin (K)</param>
        /// <param name="airTemp">Outside Ambient Air Temperature in Kelvin (K)</param>
        /// <param name="airPressure">Atmospheric Pressure in Hectopascal (hPa)</param>
        /// <returns></returns>
        public static N2kFrame CreatePGN130310(int sid, double waterTemp, double airTemp, int airPressure)
        {
            #region Example
            /*
            130310: Environmental Parameters
            Source          = 3
            Destination     = 255
            Priority        = 5
            Expected Length = 7 bytes
            Actual Length   = 8 bytes
                0: 20 -- -- -- -- -- -- -- 00100000  SID = 32
                0: -- CD 71 -- -- -- -- -- ........  Water Temperature = 18.18 °C
                0: -- -- -- FF FF -- -- -- ........  Outside Ambient Air Temperature =  NotAvailable
                0: -- -- -- -- -- FF FF -- ........  Atmospheric Pressure =  NotAvailable
                0: -- -- -- -- -- -- -- FF 11111111  Reserved =  NotAvailable
            */
            #endregion

            // TODO: Priority and default Destination should be read from PGNDefns.N2kDfn.xml?
            int priority = 5; //Default Priority for 130310
            int source = 1;
            int destination = 255;

            N2kMessage msg = new N2kMessage(130310, priority, source, destination);

            // SID (UIntField, 1 byte) : Sequence ID (0 - 252)
            msg.SetUIntField("SID", sid);

            // Water Temperature (UDblField, 2 bytes)
            msg.SetUDblField("Water Temperature", waterTemp);

            // Outside Ambient Air Temperature (UDblField, 2 bytes)
            msg.SetUDblField("Outside Ambient Air Temperature", airTemp);

            // Atmospheric Pressure (UIntField, 2 bytes)
            msg.SetUIntField("Atmospheric Pressure", airPressure);

            // Reserved (UIntField, 1 bytes)
            msg.SetUIntField("Reserved", N2kField.IntNA);

            return msg.Frame;
        }

        /// <summary>
        /// Parses 130310: Environmental Parameters frame
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="SID"></param>
        /// <param name="WaterTemperature"></param>
        /// <param name="OutsideAmbientAirTemperature"></param>
        /// <param name="AtmosphericPressure"></param>
        /// <returns></returns>
        public static bool ParsePGN130310(N2kFrame frame, out int SID, out double WaterTemperature, out double OutsideAmbientAirTemperature, out double AtmosphericPressure)
        {
            SID = new byte();
            WaterTemperature = 0;
            OutsideAmbientAirTemperature = 0;
            AtmosphericPressure = 0;

            // Check if frame is the correct PGN
            if (frame.Defn.PGN != 130310) return false;

            // Parse the frame
            N2kMessage msg = new N2kMessage(frame);
            SID = (int)msg.GetDblField("SID");
            WaterTemperature = msg.GetUDblField("Water Temperature");
            OutsideAmbientAirTemperature = msg.GetUDblField("Outside Ambient Air Temperature");
            AtmosphericPressure = msg.GetUIntField("Atmospheric Pressure");

            return true;
        }

        /// <summary>
        /// 128275: Distance Log
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="waterTemp"></param>
        /// <param name="airTemp"></param>
        /// <param name="airPressure"></param>
        /// <returns></returns>
        public static N2kFrame CreatePGN128275(DateTime date, int log, int tripLog)
        {
            #region Example
            /*
            128275: Distance Log
            Source          = 35
            Destination     = 255
            Priority        = 6
            Expected Length = 14 bytes
            Actual Length   = 14 bytes
                0: FF FF -- -- -- -- -- -- ........  Date =  NotAvailable
                0: -- -- FF FF FF FF -- -- ........  Time =  NotAvailable
                0: -- -- -- -- -- -- 3A 04 
                8: 00 00 -- -- -- --       ........  Log = 1082
                8: -- -- 3A 04 00 00       ........  Trip Log = 1082
            */
            #endregion

            int priority = 5; // Default Priority for 130310
            int source = 1;
            int destination = 255;

            N2kMessage msg = new N2kMessage(128275, priority, source, destination);

            int numDays = N2kField.IntNA;
            int numSec = N2kField.IntNA;

            if (date > DateTime.MinValue && date < DateTime.MaxValue)
            {
                numDays = UnitConverter.ConvertToNumDays(date);
                numSec = UnitConverter.ConvertToNumSeconds(date);
            }

            // Date (UIntField, 2 bytes) : Days since January 1, 1970
            msg.SetUIntField("Date", numDays);

            // Time (UIntField, 2 bytes) : Seconds since midnight
            msg.SetUDblField("Time", numSec);

            // Log (UIntField, 4 bytes) : Total cumulative distance
            msg.SetUIntField("Log", log);

            // Trip Log (UIntField, 4 bytes) : Distance since last reset
            msg.SetUIntField("Trip Log", tripLog);

            return msg.Frame;
        }

        public static bool ParsePGN128275(N2kFrame frame, out DateTime date, out int log, out int tripLog)
        {
            date = DateTime.MinValue;
            log = 0;
            tripLog = 0;

            // Check if frame is the correct PGN
            if (frame.Defn.PGN != 128275) return false;

            // Parse the frame
            N2kMessage msg = new N2kMessage(frame);

            int numDays = (int)msg.GetUIntField("Date");
            int numSec = (int)msg.GetUDblField("Time");
            date= UnitConverter.ConvertFromNumDays(numDays).AddSeconds(numSec);
            log = (int)msg.GetUIntField("Log");
            tripLog = (int)msg.GetUIntField("Trip Log");

            return true;
        }

    } // class

} // namespace
