//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.K26
{
    using System;
    using System.Runtime.InteropServices;
    using RT = Microsoft.Zelig.Runtime;

    public sealed class Device : RT.Device
    {
        [RT.MemoryUsage(RT.MemoryUsage.Stack, ContentsUninitialized = true, AllocateFromHighAddress = true)]
        private static readonly uint[] s_bootstrapStackK26 = new uint[ 1024 / sizeof( uint ) ];

        private const uint c_K26HeapSize = 512*1024;

        //
        // Access Methods
        //

        public override uint[] BootstrapStack
        {
            get
            {
                return s_bootstrapStackK26;
            }
        }

        public override void PreInitializeProcessorAndMemory( )
        {
        }

        public override void MoveCodeToProperLocation( )
        {
        }
    }
}
