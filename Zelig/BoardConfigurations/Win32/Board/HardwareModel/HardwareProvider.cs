namespace Microsoft.Llilum.K26.HardwareModel
{
    using System;
    using RT = Microsoft.Zelig.Runtime;

    public sealed class HardwareProvider : RT.HardwareProvider
    {
        public override int InvalidPin
        {
            get
            {
                throw new NotImplementedException( );
            }
        }

        public override int PinCount
        {
            get
            {
                throw new NotImplementedException( );
            }
        }

        public override bool GetSerialPinsFromPortName( string portName, out int txPin, out int rxPin, out int rtsPin, out int ctsPin )
        {
            throw new NotImplementedException( );
        }

        public override string[] GetSerialPorts( )
        {
            throw new NotImplementedException( );
        }

        public override int PinToIndex( int pin )
        {
            throw new NotImplementedException( );
        }
    }
}
