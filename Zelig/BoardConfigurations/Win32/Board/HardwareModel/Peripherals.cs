//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using Zelig.Runtime.TargetPlatform.ARMv7;
    using Chipset = DeviceModels.Chipset.CortexM3;

    public sealed class Peripherals : Microsoft.Zelig.Runtime.Peripherals
    {
        public override void Activate( )
        {
            Chipset.Drivers.ContextSwitchTimer.Instance.Initialize();
            Chipset.SystemServices.SystemTimer.Instance.Initialize();
        }

        public override void CauseInterrupt( )
        {
            ProcessorARMv7M.CompleteContextSwitch( ); 
        }

        public override void ContinueUnderNormalInterrupt( Continuation dlg )
        {
        }

        public override void DisableInterrupt( uint index )
        {
        }

        public override void EnableInterrupt( uint index )
        {
        }

        public override ulong GetPerformanceCounterFrequency( )
        {
            return 0;
        }

        public override void Initialize( )
        {
            Chipset.NVIC.SetPriority( ProcessorARMv7M.IRQn_Type.SVCall_IRQn , ProcessorARMv7M.c_Priority__SVCCall ); 
            Chipset.NVIC.SetPriority( ProcessorARMv7M.IRQn_Type.SysTick_IRQn, ProcessorARMv7M.c_Priority__SysTick ); 
            Chipset.NVIC.SetPriority( ProcessorARMv7M.IRQn_Type.PendSV_IRQn , ProcessorARMv7M.c_Priority__PendSV  ); 
        }

        public override void ProcessFastInterrupt( )
        {
        }

        public override void ProcessInterrupt( )
        {
        }

        public override uint ReadPerformanceCounter( )
        {
            return 0;
        }

        public override void WaitForInterrupt( )
        {
            ProcessorARMv7M.WaitForEvent( );
        }
    }
}
