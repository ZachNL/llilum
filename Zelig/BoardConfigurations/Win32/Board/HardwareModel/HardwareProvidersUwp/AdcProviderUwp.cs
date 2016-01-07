//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using RT = Microsoft.Zelig.Runtime;

    public sealed class AdcProviderUwp : RT.AdcProviderUwp
    {
        public override RT.AdcChannelInfoUwp GetAdcChannelInfo()
        {
            return null;
        }
    }
}