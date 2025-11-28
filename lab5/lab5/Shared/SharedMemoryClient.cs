using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Shared
{
    public class SharedMemoryClient : IDisposable
    {
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _view;
        private readonly Mutex _mutex;
        private readonly EventWaitHandle _dataReadyEvent;
        private readonly EventWaitHandle _spaceAvailableEvent;

        private const int OffsetReadIndex = 0;
        private const int OffsetWriteIndex = 4;
        private const int OffsetDataStart = 8;

        private readonly int _itemSize;

        public SharedMemoryClient(bool isProducer)
        {
            _itemSize = Marshal.SizeOf<HistogramMessage>();
            long totalSize = OffsetDataStart + (_itemSize * Constants.BufferCapacity);

            _mmf = MemoryMappedFile.CreateOrOpen(Constants.MemoryMapName, totalSize);
            _view = _mmf.CreateViewAccessor();

            _mutex = new Mutex(false, Constants.MutexName);

            _dataReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Constants.EventDataReady);
            _spaceAvailableEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Constants.EventSpaceAvailable);
        }

        public void Produce(HistogramMessage message)
        {
            while (true)
            {
                _mutex.WaitOne();

                int readIndex = _view.ReadInt32(OffsetReadIndex);
                int writeIndex = _view.ReadInt32(OffsetWriteIndex);

                int nextWriteIndex = (writeIndex + 1) % Constants.BufferCapacity;

                if (nextWriteIndex == readIndex)
                {
                    _mutex.ReleaseMutex();

                    _spaceAvailableEvent.WaitOne();
                    continue;
                }


                long offset = OffsetDataStart + (writeIndex * _itemSize);
                WriteStructure(offset, message);

                _view.Write(OffsetWriteIndex, nextWriteIndex);

                _mutex.ReleaseMutex();

                _dataReadyEvent.Set();
                return;
            }
        }

        public HistogramMessage Consume()
        {
            while (true)
            {
                _mutex.WaitOne();
                int readIndex = _view.ReadInt32(OffsetReadIndex);
                int writeIndex = _view.ReadInt32(OffsetWriteIndex);

                if (readIndex == writeIndex)
                { 
                    _mutex.ReleaseMutex();
                    _dataReadyEvent.WaitOne();
                    continue;
                }

                long offset = OffsetDataStart + (readIndex * _itemSize);
                HistogramMessage msg = ReadStructure(offset);

                int nextReadIndex = (readIndex + 1) % Constants.BufferCapacity;
                _view.Write(OffsetReadIndex, nextReadIndex);

                _mutex.ReleaseMutex();

                _spaceAvailableEvent.Set();

                return msg;
            }
        }

        private void WriteStructure(long offset, HistogramMessage data)
        {
            int size = Marshal.SizeOf(data);
            byte[] buffer = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(data, ptr, false);
                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            _view.WriteArray(offset, buffer, 0, size);
        }

        private HistogramMessage ReadStructure(long offset)
        {
            int size = Marshal.SizeOf<HistogramMessage>();
            byte[] buffer = new byte[size];
            _view.ReadArray(offset, buffer, 0, size);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(buffer, 0, ptr, size);
                return Marshal.PtrToStructure<HistogramMessage>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public void Dispose()
        {
            _mutex?.Dispose();
            _view?.Dispose();
            _mmf?.Dispose();
            _dataReadyEvent?.Dispose();
            _spaceAvailableEvent?.Dispose();
        }
    }
}