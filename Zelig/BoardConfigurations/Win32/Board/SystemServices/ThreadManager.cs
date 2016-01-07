//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//


namespace Microsoft.Llilum.K26
{
    using System;
    using Zelig.Runtime;
    using Chipset = Microsoft.DeviceModels.Chipset.CortexM3;
    using LLOS = Zelig.LlilumOSAbstraction.HAL;

    public sealed class ThreadManager : Chipset.Runtime.ThreadManager
    {
        private const int DefaultStackSizeK26 = 4 * 1024;

        //--//

        Chipset.SystemServices.SystemTimer.Timer m_timerForWaits;

        //
        // Helper Methods
        //

        public override int DefaultStackSize
        {
            get
            {
                return DefaultStackSizeK26;
            }
        }

        public override void Activate()
        {
            base.Activate( ); 

            m_timerForWaits = Chipset.SystemServices.SystemTimer.Instance.CreateTimer( WaitExpired );
        }


        public override void SetNextWaitTimer( SchedulerTime nextTimeout )
        {
            if(nextTimeout != SchedulerTime.MaxValue)
            {
                m_timerForWaits.Timeout = nextTimeout.Units;
            }
            else
            {
                m_timerForWaits.Cancel( );
            }
        }

        private void WaitExpired( Drivers.SystemTimer.Timer sysTickTimer, ulong currentTime )
        {
            WaitExpired( SchedulerTime.FromUnits( currentTime ) );
        }
    }
}

