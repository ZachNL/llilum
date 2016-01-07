//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using System;
    using RT = Microsoft.Zelig.Runtime;

    public sealed class SpiProvider : RT.SpiProvider
    {
        public override bool SpiBusySupported
        {
            get
            {
                return false;
            }
        }

        public override Llilum.Devices.Spi.SpiChannel CreateSpiChannel( Llilum.Devices.Spi.ISpiChannelInfo channelInfo )
        {
            throw new NotImplementedException( );
        }

        public override RT.SpiChannelInfo GetSpiChannelInfo(int id)
        {
            switch (id)
            {
                default:
                    return null;
            }
        }
    }
}
