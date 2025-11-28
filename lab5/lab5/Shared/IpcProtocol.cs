using System.Runtime.InteropServices;

namespace Shared
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct HistogramMessage
    {
        public bool IsEndOfStream;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ImageName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
        public float[] Histogram;
    }

    public class Constants
    {
        public const string MemoryMapName = "Lab5_SharedMemory";
        public const string MutexName = "Lab5_Mutex";
        public const string EventDataReady = "Lab5_DataReady"; 
        public const string EventSpaceAvailable = "Lab5_SpaceAvailable"; 

        public const int BufferCapacity = 5;
    }
}