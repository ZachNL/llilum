//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using Microsoft.Zelig.Runtime;

    public sealed class I2cProviderUwp : Microsoft.Zelig.Runtime.I2cProviderUwp
    {
        private static string[] m_i2cChannels = { };

        public override I2cChannelInfo GetI2cChannelInfo(string busId)
        {
            switch (busId)
            {
                default:
                    return null;
            }
        }

        public override string[] GetI2cChannels()
        {
            return m_i2cChannels;
        }
    }
}