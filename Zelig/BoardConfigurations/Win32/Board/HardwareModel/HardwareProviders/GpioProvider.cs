//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26.HardwareModel.HardwareProviders
{
    using System;
    using RT = Microsoft.Zelig.Runtime;

    public sealed class GpioProvider : RT.GpioProvider
    {
        public override Llilum.Devices.Gpio.GpioPin CreateGpioPin( int pinNumber )
        {
            throw new NotImplementedException( );
        }

        public override int GetGpioPinIRQNumber(int pinNumber)
        {
            throw new NotSupportedException();
        }
    }
}
