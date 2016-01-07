//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using System;
    using Devices.I2c;
    using RT = Microsoft.Zelig.Runtime;

    public sealed class I2cProvider : RT.I2cProvider
    {
        public override I2cChannel CreateI2cChannel( II2cChannelInfo channelInfo )
        {
            throw new NotImplementedException( );
        }

        public override RT.I2cChannelInfo GetI2cChannelInfo(int id)
        {
            switch (id)
            {
                default:
                    return null;
            }
        }
    }
}
