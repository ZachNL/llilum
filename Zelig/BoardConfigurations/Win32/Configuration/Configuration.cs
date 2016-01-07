//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace Microsoft.Llilum.BoardConfigurations
{
    using Microsoft.Zelig.Configuration.Environment;

    [DisplayName("Win32Product")]
    public sealed class Win32Product : ProductCategory
    {
        [Defaults("CoreClockFrequency", 1000000UL)]
        [Defaults("RealTimeClockFrequency", 1000000UL)]
        public ProcessorCategory Processor;
    }

    [DisplayName("Memory Map for Win32")]
    public sealed class Win32CoreMemoryMap : MemoryMapCategory
    {
    }

    [DisplayName("LLVM Compilation for K26")]
    [Defaults("Platform", typeof(Microsoft.Zelig.Configuration.Environment.Abstractions.Architectures.ArmV7M_VFP))]
    [Defaults("CallingConvention", typeof(Microsoft.Zelig.Configuration.Environment.Abstractions.Architectures.ArmV7MCallingConvention))]
    [Defaults("Product", typeof(Win32Product))]
    [Defaults("MemoryMap", typeof(Win32CoreMemoryMap))]
    public sealed class Win32CompilationSetup : CompilationSetupCategory
    {
    }
}
