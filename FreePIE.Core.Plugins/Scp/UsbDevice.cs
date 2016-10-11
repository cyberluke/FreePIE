using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace SCP
{
    public class UsbDevice
    {
        private Guid m_Class;
        private string m_Path;
        private SafeFileHandle m_FileHandle;
        private bool m_IsActive = false;

        public UsbDevice(string cls)
        {
            m_Class = new Guid(cls);
            Open();
        }

        public bool IOControl<T>(T data, int controlCode, int returnSize = 0)
            where T : struct
        {
            int bytesReturned = 0;
            int size = Marshal.SizeOf(typeof(T));
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            GCHandle outputHandle = new GCHandle();
            IntPtr outAddr = IntPtr.Zero;
            if (returnSize > 0)
            {
                byte[] output = new byte[returnSize];
                outputHandle = GCHandle.Alloc(output, GCHandleType.Pinned);
                outAddr = outputHandle.AddrOfPinnedObject();
            }
            bool result = DeviceIoControl(m_FileHandle, controlCode, handle.AddrOfPinnedObject(), size, outAddr, returnSize, ref bytesReturned, IntPtr.Zero);

            handle.Free();
            if (outAddr != IntPtr.Zero)
                outputHandle.Free();
            return result && returnSize == bytesReturned;
        }

        public void Open(int instance = 0)
        {
            string devicePath;
            if (Find(m_Class, instance, out devicePath))
                Open(devicePath);
            else
                throw new Exception("Unable to find Xbox device! (is the SCP driver installed?)");
        }

        public virtual bool Open(string DevicePath)
        {
            m_Path = DevicePath.ToUpper();
            if (GetDeviceHandle(m_Path))
                m_IsActive = true;
            else
                throw new Exception("Unable to open the device!");
            return m_IsActive;
        }

        private bool GetDeviceHandle(string Path)
        {
            m_FileHandle = CreateFile(Path, (GENERIC_WRITE | GENERIC_READ), FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, 0);
            return !m_FileHandle.IsInvalid;
        }

        public void Close()
        {
            m_IsActive = false;
            if (m_FileHandle != null && !m_FileHandle.IsInvalid && !m_FileHandle.IsClosed)
            {
                m_FileHandle.Close();
                m_FileHandle = null;
            }
        }

        ~UsbDevice()
        {
            Close();
        }

        public static bool Find(Guid target, int instance, out string devicePath)
        {
            IntPtr detailDataBuffer = IntPtr.Zero;
            IntPtr deviceInfoSet = IntPtr.Zero;

            try
            {
                SP_DEVICE_INTERFACE_DATA DeviceInterfaceData = new SP_DEVICE_INTERFACE_DATA(), da = new SP_DEVICE_INTERFACE_DATA();
                int bufferSize = 0, memberIndex = 0;

                deviceInfoSet = SetupDiGetClassDevs(ref target, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                DeviceInterfaceData.cbSize = da.cbSize = Marshal.SizeOf(DeviceInterfaceData);

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref target, memberIndex, ref DeviceInterfaceData))
                {
                    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, ref da);
                    {
                        detailDataBuffer = Marshal.AllocHGlobal(bufferSize);

                        Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                        if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, detailDataBuffer, bufferSize, ref bufferSize, ref da))
                        {
                            IntPtr pDevicePathName = new IntPtr(IntPtr.Size == 4 ? detailDataBuffer.ToInt32() + 4 : detailDataBuffer.ToInt64() + 4);

                            devicePath = Marshal.PtrToStringAuto(pDevicePathName).ToUpper();
                            Marshal.FreeHGlobal(detailDataBuffer);

                            if (memberIndex == instance) return true;
                        } else Marshal.FreeHGlobal(detailDataBuffer);
                    }

                    memberIndex++;
                }
            } catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            } finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            devicePath = string.Empty;
            return false;
        }

        #region Constant and Structure Definitions
        public const int DIGCF_PRESENT = 0x0002;
        public const int DIGCF_DEVICEINTERFACE = 0x0010;

        public delegate int ServiceControlHandlerEx(int Control, int Type, IntPtr Data, IntPtr Context);



        [StructLayout(LayoutKind.Sequential)]
        protected struct SP_DEVICE_INTERFACE_DATA
        {
            internal int cbSize;
            internal Guid InterfaceClassGuid;
            internal int Flags;
            internal IntPtr Reserved;
        }

        protected const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        protected const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        protected const uint FILE_SHARE_READ = 1;
        protected const uint FILE_SHARE_WRITE = 2;
        protected const uint GENERIC_READ = 0x80000000;
        protected const uint GENERIC_WRITE = 0x40000000;
        protected const uint OPEN_EXISTING = 3;

        #endregion

        #region Interop Definitions

        [DllImport("setupapi.dll", SetLastError = true)]
        protected static extern Int32 SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        protected static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid, Int32 MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, Int32 Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize, ref SP_DEVICE_INTERFACE_DATA DeviceInfoData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern SafeFileHandle CreateFile(String lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode, IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes, UInt32 hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern unsafe bool DeviceIoControl(SafeFileHandle DeviceHandle, Int32 IoControlCode, IntPtr InBuffer, Int32 InBufferSize, IntPtr OutBuffer, Int32 OutBufferSize, ref Int32 BytesReturned, IntPtr Overlapped);
        #endregion
    }
}