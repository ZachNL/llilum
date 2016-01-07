//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26.SystemServices
{
    using System;

    using RT = Microsoft.Zelig.Runtime;
    using LLOS = Zelig.LlilumOSAbstraction.HAL;
    using DeviceModels.Chipset.CortexM3.SystemServices;


    //
    // We use the 1MHz timer for the internal time.
    //
    [RT.ExtendClass(typeof(RT.SchedulerTime), NoConstructors=true)]
    public struct SchedulerTimeImpl
    {
        //
        // Helper Methods
        //

        private static ulong ConvertFromMillisecondsToUnits( int milliSeconds )
        {
            if(milliSeconds < 0)
            {
                return ulong.MaxValue;
            }
            else
            {
                ulong res = (ulong)milliSeconds * (ulong)HardwareClockFrequency / 1000;
                
                return GetCurrentTime() + res;
            }
        }

        private static ulong ConvertFromTimeSpanTicksToUnits( long ticks )
        {
            if(ticks < 0)
            {
                return ulong.MaxValue;
            }
            else
            {
                ulong res = ConvertFromDateTimeTicksToUnits( ticks );

                return GetCurrentTime() + res;
            }
        }

        private static ulong ConvertFromDateTimeTicksToUnits( long ticks )
        {
            return (ulong)((double)ticks * RatioFromDateTimeTicksToUnits);
        }

        private static long ConvertFromUnitsToDateTimeTicks( ulong units )
        {
            return (long)((double)units * RatioFromUnitsToDateTimeTicks);
        }

        private static ulong GetCurrentTime()
        {
            return SystemTimer.Instance.Ticks;
        }

        //
        // Access Methods
        //

        static int HardwareClockFrequency
        {
            [RT.Inline]
            get
            {
                return (int)SystemTimer.Instance.Frequency;
            }
        }

        static double RatioFromMillisecondsToUnits
        {
            [RT.Inline]
            get
            {
                return (double)HardwareClockFrequency / (double)1000;
            }
        }

        static double RatioFromDateTimeTicksToUnits
        {
            [RT.Inline]
            get
            {
                return (double)HardwareClockFrequency / (double)TimeSpan.TicksPerSecond;
            }
        }

        static double RatioFromUnitsToMilliseconds
        {
            [RT.Inline]
            get
            {
                return 1.0 / RatioFromMillisecondsToUnits;
            }
        }

        static double RatioFromUnitsToDateTimeTicks
        {
            [RT.Inline]
            get
            {
                return 1.0 / RatioFromDateTimeTicksToUnits;
            }
        }
    }
}
