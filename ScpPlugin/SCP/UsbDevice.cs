using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace SCP
{

    public unsafe abstract class UsbDevice
    {
        private Guid m_Class;
        private string m_Path;
        private SafeFileHandle m_FileHandle;
        private bool m_IsActive = false;

        protected UsbDevice(string cls)
        {
            m_Class = new Guid(cls);
            Open();
        }

        protected bool IOControl(void* data, uint dataSize, uint controlCode, void* returnData = null, uint returnSize = 0)
        {
            uint bytesReturned = 0;
            bool result = DeviceIoControl(m_FileHandle, controlCode, data, dataSize, returnData, returnSize, &bytesReturned, null);
            return result && returnSize == bytesReturned;
        }

        protected void Open(int instance = 0)
        {
            string devicePath;
            if (Find(m_Class, instance, out devicePath))
                Open(devicePath);
            else
                throw new Exception("Unable to find Xbox device!");
        }

        protected virtual bool Open(string DevicePath)
        {
            m_Path = DevicePath.ToUpper();
            Console.WriteLine("Opening USB device at {0}", m_Path);

            if (GetDeviceHandle(m_Path))
                m_IsActive = true;
            else
                throw new Exception();
            return m_IsActive;
        }

        private bool GetDeviceHandle(string Path)
        {
            m_FileHandle = CreateFile(Path, (GENERIC_WRITE | GENERIC_READ), FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, 0);
            return !m_FileHandle.IsInvalid;
        }

        protected void Close()
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

        private static bool Find(Guid target, int instance, out string devicePath)
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
                            IntPtr pDevicePathName = detailDataBuffer + 4;

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
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
            devicePath = string.Empty;
            return false;
        }

        #region Constant and Structure Definitions
        private const int DIGCF_PRESENT = 0x0002,
            DIGCF_DEVICEINTERFACE = 0x0010;

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
        private static extern int SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, ref SP_DEVICE_INTERFACE_DATA DeviceInfoData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, uint hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern unsafe bool DeviceIoControl(SafeFileHandle DeviceHandle, uint IoControlCode, void* InBuffer, uint InBufferSize, void* OutBuffer, uint OutBufferSize, uint* BytesReturned, void* Overlapped);
        #endregion
    }
}