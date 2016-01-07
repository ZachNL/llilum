//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using RT = Microsoft.Zelig.Runtime;

    public sealed class PwmProviderUwp : RT.PwmProviderUwp
    {
        public override RT.PwmChannelInfoUwp GetPwmChannelInfo()
        {
            return null;
        }
    }
}