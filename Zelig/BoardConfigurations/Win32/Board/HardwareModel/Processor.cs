//
// Copyright ((c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using RT = Microsoft.Zelig.Runtime;
    using ChipsetModel =  Microsoft.DeviceModels.Chipset.CortexM4;


    [RT.ProductFilter("Microsoft.Llilum.BoardConfigurations.K26Envoy")]
    public sealed class Processor : ChipsetModel.Processor
    {
        public new class Context : ChipsetModel.Processor.Context
        {
            public override unsafe void SwitchTo( )
            {
                //
                // BUGBUG: return to thread using VFP state as well 
                //
                base.SwitchTo( ); 
            }
        }

        //
        // Helper methods
        //

        [RT.Inline]
        public override void InitializeProcessor()
        {
            //base.InitializeProcessor( );
        }
        
        [RT.Inline]
        public override Microsoft.Zelig.Runtime.Processor.Context AllocateProcessorContext()
        {
            return new Context();
        }
    }
}
