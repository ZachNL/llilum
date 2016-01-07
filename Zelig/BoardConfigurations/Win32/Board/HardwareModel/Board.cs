//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//


namespace Microsoft.Llilum.K26
{
    using System;

    using Chipset = Microsoft.DeviceModels.Chipset.CortexM4;

    public sealed class Board : Chipset.Board
    {
        //
        // Serial Ports
        //
        private static readonly string[] m_serialPorts = { };

        //
        // Gpio discovery
        //

        public override int PinCount
        {
            get
            {
                return 0;
            }
        }

        public override int NCPin
        {
            get
            {
                return -1;
            }
        }
        
        public override int PinToIndex( int pin )
        {
            return pin;
        }

        //
        // Serial Port
        //
        public override string[] GetSerialPorts()
        {
            return m_serialPorts;
        }

        public override Board.SerialPortInfo GetSerialPortInfo(string portName)
        {
            switch (portName)
            {
                default:
                    return null;
            }
        }

        //
        // System timer
        //
        public override int GetSystemTimerIRQNumber( )
        {
            throw new NotSupportedException();
        }

        public override int GetSerialPortIRQNumber(string portName)
        {
            switch (portName)
            {
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
