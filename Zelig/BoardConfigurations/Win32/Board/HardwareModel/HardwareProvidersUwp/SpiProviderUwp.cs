//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using RT = Microsoft.Zelig.Runtime;

    public sealed class SpiProviderUwp : RT.SpiProviderUwp
    {
        private static readonly string[] m_spiDevices = { };

        public override RT.SpiChannelInfoUwp GetSpiChannelInfo(string busId)
        {
            switch (busId)
            {
                default:
                    return null;
            }
        }

        public override string[] GetSpiChannels()
        {
            return m_spiDevices;
        }
    }
}
