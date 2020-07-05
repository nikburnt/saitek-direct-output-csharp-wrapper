using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HResult = System.Int32;

namespace DirectOutputCSharpWrapper
{
    public struct SRequestStatus
    {
        public Int32 headerError;
        public Int32 headerInfo;
        public Int32 requestError;
        public Int32 requestInfo;
    };

    public class RegistryKeyNotFound : Exception
    {
        public RegistryKeyNotFound() : base(@"HKEY_LOCAL_MACHINE\SOFTWARE\Saitek\DirectOutput key not found.")
        {
        }
    }

    public class RegistryValueNotFound : Exception
    {
        public RegistryValueNotFound() : base(
            @"DirectOutput value in key HKEY_LOCAL_MACHINE\SOFTWARE\Saitek\DirectOutput not found.")
        {
        }
    }

    public class SRequestStatusException : Exception
    {
        public SRequestStatus requestStatus;

        public SRequestStatusException(SRequestStatus requestStatus)
        {
            this.requestStatus = requestStatus;
        }
    }

    public class HResultException : Exception
    {
        public const HResult S_OK = 0x00000000;
        public const HResult E_OUTOFMEMORY = unchecked((HResult) 0x8007000E);
        public const HResult E_NOTIMPL = unchecked((HResult) 0x80004001);
        public const HResult E_INVALIDARG = unchecked((HResult) 0x80070057);
        public const HResult E_PAGENOTACTIVE = unchecked((HResult) 0xFF040001);
        public const HResult E_HANDLE = unchecked((HResult) 0x80070006);
        public const HResult E_UNKNOWN_1 = unchecked(0x51B87CE3);

        public HResultException(HResult result, Dictionary<HResult, string> errorsMap)
            : base(errorsMap[result])
        {
            HResult = result;
        }
    }

    public class DirectOutput
    {
        public bool IsInitialized { get; private set; }

        public const Int32 IsActive = 0x00000001;

        // Callbacks
        public delegate IntPtr EnumerateCallback(IntPtr device, IntPtr target);

        public delegate void DeviceCallback(IntPtr device, bool added, IntPtr target);

        public delegate void SoftButtonCallback(IntPtr device, uint buttons, IntPtr target);

        public delegate void PageCallback(IntPtr device, Int32 page, bool activated, IntPtr target);

        // Library functions
        private delegate HResult DirectOutput_Initialize([MarshalAsAttribute(UnmanagedType.LPWStr)]
            string appName);

        private delegate HResult DirectOutput_Deinitialize();

        private delegate HResult DirectOutput_RegisterDeviceCallback(
            [MarshalAs(UnmanagedType.FunctionPtr)] DeviceCallback callback, IntPtr target);

        private delegate HResult DirectOutput_Enumerate(
            [MarshalAs(UnmanagedType.FunctionPtr)] EnumerateCallback callback, IntPtr target);

        private delegate HResult DirectOutput_GetDeviceType(IntPtr device, out Guid guidType);

        private delegate HResult DirectOutput_GetDeviceInstance(IntPtr device, out Guid guidInstance);

        private delegate HResult DirectOutput_SetProfile(IntPtr device, Int32 fileNameLength,
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            string filename);

        private delegate HResult DirectOutput_RegisterSoftButtonCallback(IntPtr device,
            [MarshalAs(UnmanagedType.FunctionPtr)] SoftButtonCallback callback, IntPtr target);

        private delegate HResult DirectOutput_RegisterPageCallback(IntPtr device,
            [MarshalAs(UnmanagedType.FunctionPtr)] PageCallback callback, IntPtr target);

        private delegate HResult DirectOutput_AddPage(IntPtr device, Int32 page, Int32 flags);

        private delegate HResult DirectOutput_RemovePage(IntPtr device, Int32 page);

        private delegate HResult DirectOutput_SetLed(IntPtr device, Int32 page, Int32 index, Int32 value);

        private delegate HResult DirectOutput_SetString(IntPtr device, Int32 page, Int32 index, Int32 valueLength,
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            string value);

        private delegate HResult DirectOutput_SetImage(IntPtr device, Int32 page, Int32 index, Int32 bufferlength,
            byte[] buffer);

        private delegate HResult DirectOutput_SetImageFromFile(IntPtr device, Int32 page, Int32 index,
            Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)]
            string filename);

        private delegate HResult DirectOutput_StartServer(IntPtr device, Int32 fileNameLength,
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            string filename, out IntPtr serverId, out SRequestStatus status);

        private delegate HResult DirectOutput_CloseServer(IntPtr device, Int32 serverId, out SRequestStatus status);

        private delegate HResult DirectOutput_SendServerMsg(IntPtr device, Int32 serverId, Int32 request, Int32 page,
            Int32 inBufferSize, byte[] inBuffer, Int32 outBufferSize, out byte[] outBuffer, out SRequestStatus status);

        private delegate HResult DirectOutput_SendServerFile(IntPtr device, Int32 serverId, Int32 request, Int32 page,
            Int32 cbInHdr, IntPtr pvInHdr, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)]
            string filename, Int32 cbOut, out IntPtr pvOut, out SRequestStatus status);

        private delegate HResult DirectOutput_SaveFile(IntPtr device, Int32 page, Int32 dwFile, Int32 fileNameLength,
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            string filename, out SRequestStatus status);

        private delegate HResult DirectOutput_DisplayFile(IntPtr device, Int32 page, Int32 dwIndex, Int32 dwFile,
            out SRequestStatus status);

        private delegate HResult DirectOutput_DeleteFile(IntPtr device, Int32 page, Int32 dwFile,
            out SRequestStatus status);

        // Functions placeholders
        private DirectOutput_Initialize initialize;
        private DirectOutput_Deinitialize deinitialize;
        private DirectOutput_RegisterDeviceCallback registerDeviceCallback;
        private DirectOutput_Enumerate enumerate;
        private DirectOutput_GetDeviceType getDeviceType;
        private DirectOutput_GetDeviceInstance getDeviceInstance;
        private DirectOutput_SetProfile setProfile;
        private DirectOutput_RegisterSoftButtonCallback registerSoftButtonCallback;
        private DirectOutput_RegisterPageCallback registerPageCallback;
        private DirectOutput_AddPage addPage;
        private DirectOutput_RemovePage removePage;
        private DirectOutput_SetLed setLed;
        private DirectOutput_SetString setString;
        private DirectOutput_SetImage setImage;
        private DirectOutput_SetImageFromFile setImageFromFile;
        private DirectOutput_StartServer startServer;
        private DirectOutput_CloseServer closeServer;
        private DirectOutput_SendServerMsg sendServerMsg;
        private DirectOutput_SendServerFile sendServerFile;
        private DirectOutput_SaveFile saveFile;
        private DirectOutput_DisplayFile displayFile;
        private DirectOutput_DeleteFile deleteFile;

        private const string directOutputKey = "SOFTWARE\\Saitek\\DirectOutput";

        private readonly IntPtr hModule;

        /// <summary>
        /// Creates DirectOutput wrapper
        /// </summary>
        /// <param name="libPath">Path to DirectOutput.dll</param>
        /// <exception cref="RegistryKeyNotFound">HKEY_LOCAL_MACHINE\SOFTWARE\Saitek\DirectOutput key not found. Usualy, this mean what Saitek Drivers not installed properly.</exception>
        /// <exception cref="RegistryValueNotFound">DirectOutput value in key HKEY_LOCAL_MACHINE\SOFTWARE\Saitek\DirectOutput key not found. Usualy, this mean what Saitek Drivers not installed properly.</exception>
        /// <exception cref="DllHelper.OutOfMemoryException">The system is out of memory or resources.</exception>
        /// <exception cref="DllHelper.BadFormatException">The target file is invalid.</exception>
        /// <exception cref="DllHelper.FileNotFountException">The specified file was not found.</exception>
        /// <exception cref="DllHelper.PathNotFountException">The specified path was not found.</exception>
        /// <exception cref="DllHelper.UnknownException">Unknown exception during library loading.</exception>
        public DirectOutput(string libPath = null)
        {
            if (libPath == null)
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(directOutputKey);
                if (key == null)
                {
                    throw new RegistryKeyNotFound();
                }

                object value = key.GetValue("DirectOutput");
                if ((value == null) || !(value is string))
                {
                    throw new RegistryValueNotFound();
                }

                libPath = (string) value;
            }

            hModule = DllHelper.LoadLibrary(libPath);

            InitializeLibraryFunctions();
        }

        ~DirectOutput()
        {
            DllHelper.FreeLibrary(hModule);
        }

        private void InitializeLibraryFunctions()
        {
            initialize = DllHelper.GetFunction<DirectOutput_Initialize>(hModule, "DirectOutput_Initialize");
            deinitialize = DllHelper.GetFunction<DirectOutput_Deinitialize>(hModule, "DirectOutput_Deinitialize");
            registerDeviceCallback =
                DllHelper.GetFunction<DirectOutput_RegisterDeviceCallback>(hModule,
                    "DirectOutput_RegisterDeviceCallback");
            enumerate = DllHelper.GetFunction<DirectOutput_Enumerate>(hModule, "DirectOutput_Enumerate");
            getDeviceType = DllHelper.GetFunction<DirectOutput_GetDeviceType>(hModule, "DirectOutput_GetDeviceType");
            getDeviceInstance =
                DllHelper.GetFunction<DirectOutput_GetDeviceInstance>(hModule, "DirectOutput_GetDeviceInstance");
            setProfile = DllHelper.GetFunction<DirectOutput_SetProfile>(hModule, "DirectOutput_SetProfile");
            registerSoftButtonCallback =
                DllHelper.GetFunction<DirectOutput_RegisterSoftButtonCallback>(hModule,
                    "DirectOutput_RegisterSoftButtonCallback");
            registerPageCallback =
                DllHelper.GetFunction<DirectOutput_RegisterPageCallback>(hModule, "DirectOutput_RegisterPageCallback");
            addPage = DllHelper.GetFunction<DirectOutput_AddPage>(hModule, "DirectOutput_AddPage");
            removePage = DllHelper.GetFunction<DirectOutput_RemovePage>(hModule, "DirectOutput_RemovePage");
            setLed = DllHelper.GetFunction<DirectOutput_SetLed>(hModule, "DirectOutput_SetLed");
            setString = DllHelper.GetFunction<DirectOutput_SetString>(hModule, "DirectOutput_SetString");
            setImage = DllHelper.GetFunction<DirectOutput_SetImage>(hModule, "DirectOutput_SetImage");
            setImageFromFile =
                DllHelper.GetFunction<DirectOutput_SetImageFromFile>(hModule, "DirectOutput_SetImageFromFile");
            startServer = DllHelper.GetFunction<DirectOutput_StartServer>(hModule, "DirectOutput_StartServer");
            closeServer = DllHelper.GetFunction<DirectOutput_CloseServer>(hModule, "DirectOutput_CloseServer");
            sendServerMsg = DllHelper.GetFunction<DirectOutput_SendServerMsg>(hModule, "DirectOutput_SendServerMsg");
            sendServerFile = DllHelper.GetFunction<DirectOutput_SendServerFile>(hModule, "DirectOutput_SendServerFile");
            saveFile = DllHelper.GetFunction<DirectOutput_SaveFile>(hModule, "DirectOutput_SaveFile");
            displayFile = DllHelper.GetFunction<DirectOutput_DisplayFile>(hModule, "DirectOutput_DisplayFile");
            deleteFile = DllHelper.GetFunction<DirectOutput_DeleteFile>(hModule, "DirectOutput_DeleteFile");
        }

        /// <summary>
        /// Initialize the DirectOutput library.
        /// </summary>
        /// <param name="appName">String that specifies the name of the application. Optional</param>
        /// <remarks>
        /// This function must be called before calling any others. Call this function when you want to initialize the DirectOutput library.
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void Initialize(string appName = "DirectOutputCSharpWrapper")
        {
            HResult retVal = initialize(appName);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_OUTOFMEMORY, "There was insufficient memory to complete this call."},
                    {HResultException.E_INVALIDARG, "The argument is invalid."},
                    {HResultException.E_HANDLE, "The DirectOutputManager process could not be found."}
                };
                throw new HResultException(retVal, errorsMap);
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Clean up the DirectOutput library.
        /// </summary>
        /// <remarks>
        /// This function must be called before termination. Call this function to clean up any resources allocated by <see cref="Initialize"/> .
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void Deinitialize()
        {
            HResult retVal = deinitialize();
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_HANDLE, "DirectOutput was not initialized or was already de-initialized."}
                };
                throw new HResultException(retVal, errorsMap);
            }

            IsInitialized = false;
        }

        /// <summary>
        /// Register a callback function to be called when a device is added or removed.
        /// </summary>
        /// <param name="callback">Callback delegate to be called whenever a device is added or removed</param>
        /// <remarks>
        /// Passing a NULL function pointer will disable the callback.
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void RegisterDeviceCallback(DeviceCallback callback)
        {
            HResult retVal = registerDeviceCallback(callback, new IntPtr());
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_HANDLE, "DirectOutput was not initialized."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Enumerate all currently attached DirectOutput devices.
        /// </summary>
        /// <param name="callback">Callback delegate to be called for each detected device.</param>
        /// <remarks>
        /// This function has changed from previous releases. 
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void Enumerate(EnumerateCallback callback)
        {
            HResult retVal = enumerate(callback, new IntPtr());
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_HANDLE, "DirectOutput was not initialized."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Gets an identifier that identifies the device.
        /// </summary>
        /// <param name="device">Handle that was supplied in the device change callback.</param>
        /// <returns>Guid value that will recieve the type identifier of this device.</returns>
        /// <remarks>
        /// Refer to the list of type GUIDs to find out about what features are available on each device.
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public Guid GetDeviceType(IntPtr device)
        {
            HResult retVal = getDeviceType(device, out Guid guidType);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_INVALIDARG, "An argument is invalid."},
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }

            return guidType;
        }

        /// <summary>
        /// Gets an instance identifier to used with Microsoft DirectInput.
        /// </summary>
        /// <param name="device">Handle that was supplied in the device change callback.</param>
        /// <returns>Guid value that will recieve the instance identifier of this device.</returns>
        /// <remarks>
        /// Use guid in IDirectInput::CreateDevice to create the IDirectInputDevice that corrresponds to this DirectOutput device.
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public Guid GetDeviceInstance(IntPtr device)
        {
            HResult retVal = getDeviceInstance(device, out Guid guidType);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_NOTIMPL, "This device does not support DirectInput."},
                    {HResultException.E_INVALIDARG, "An argument is invalid."},
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }

            return guidType;
        }

        /// <summary>
        /// Sets the profile on this device.
        /// </summary>
        /// <param name="device">A handle to a device.</param>
        /// <param name="fileName">Full path and filename of the profile to activate.</param>
        /// <remarks>
        /// Passing in a null to fileName will clear the current profile. 
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void SetProfile(IntPtr device, string fileName)
        {
            Int32 fileNameLength = fileName != null ? fileName.Length : 0;
            HResult retVal = setProfile(device, fileNameLength, fileName);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_NOTIMPL, "The device does not support profiling."},
                    {HResultException.E_INVALIDARG, "An argument is invalid."},
                    {HResultException.E_OUTOFMEMORY, "Insufficient memory to complete the request."},
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Registers a callback with a device, that gets called whenever a "Soft Button" is pressed or released.
        /// </summary>
        /// <param name="device">A handle to a device.</param>
        /// <param name="callback">Callback delegate to be called whenever a "Soft Button" is pressed or released.</param>
        /// <remarks>
        /// Passing in a null to callback will disable the callback. 
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void RegisterSoftButtonCallback(IntPtr device, SoftButtonCallback callback)
        {
            HResult retVal = registerSoftButtonCallback(device, callback, new IntPtr());
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Registers a callback with a device, that gets called whenever the active page is changed.
        /// </summary>
        /// <param name="device">A handle to a device.</param>
        /// <param name="callback">Callback delegate to be called whenever the active page is changed.</param>
        /// <remarks>
        /// Adding a page with an existing page id is not allowed. The page id only has to be unique on a per application basis. The callback
        /// will not be called when a page is added as the active page with a call to AddPage(device, page, name, DirectOutput.IsActive);
        /// Passing a null to callback will disable the callback.
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void RegisterPageCallback(IntPtr device, PageCallback callback)
        {
            HResult retVal = registerPageCallback(device, callback, new IntPtr());
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Adds a page to the specified device.
        /// </summary>
        /// <param name="device"> A handle to a device.</param>
        /// <param name="page"> A numeric identifier of a page. Usually this is the 0 based number of the page.</param>
        /// <param name="flags">If this contains DirectOutput.IsActive, then this page will become the active page. If zero,
        /// this page will not change the active page.</param>
        /// <remarks>
        /// Only one page per-application per-device should have flags contain DirectOutput.IsActive. The plugin is not informed about
        /// the active page change if the DirectOutput.IsActive is set. 
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void AddPage(IntPtr device, Int32 page, Int32 flags)
        {
            HResult retVal = addPage(device, page, flags);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_OUTOFMEMORY, "Insufficient memory to complete the request."},
                    {HResultException.E_INVALIDARG, "The page parameter already exists."},
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Removes a page.
        /// </summary>
        /// <param name="device">A handle to a device.</param>
        /// <param name="page">A numeric identifier of a page. Usually this is the 0 based number of the page.</param>
        /// <exception cref="HResultException"></exception>
        public void RemovePage(IntPtr device, Int32 page)
        {
            HResult retVal = removePage(device, page);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {HResultException.E_INVALIDARG, "The page parameter argument does not reference a valid page id."},
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Sets the state of a given LED indicator.
        /// </summary>
        /// <param name="device">A handle to a device.</param>
        /// <param name="page">A numeric identifier of a page. Usually this is the 0 based number of the page.</param>
        /// <param name="index">A numeric identifier of the LED. Refer to the data sheet for each device to determine what LEDs are present.</param>
        /// <param name="value">The numeric value of a given state of a LED. Refer to the data sheet for each device to determine what are legal values.</param>
        /// <remarks>
        /// value is usually 0 (off) or 1 (on).
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void SetLed(IntPtr device, Int32 page, Int32 index, Int32 value)
        {
            HResult retVal = setLed(device, page, index, value);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {
                        HResultException.E_PAGENOTACTIVE,
                        "The specified page is not active. Displaying information is not permitted when the page is not active."
                    },
                    {
                        HResultException.E_INVALIDARG,
                        "The dwPage argument does not reference a valid page id, or the dwIndex argument does not specifiy a valid LED id."
                    },
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Sets a string value of a given string.
        /// </summary>
        /// <param name="device">A handle to a device.</param>
        /// <param name="page">A numeric identifier of a page. Usually this is the 0 based number of the page.</param>
        /// <param name="index">A numeric identifier of the string. Refer to the data sheet for each device to determine what strings are present.</param>
        /// <param name="text">String that specifies the value to display. Providing a null pointer will clear the string.</param>
        /// <exception cref="HResultException"></exception>
        public void SetString(IntPtr device, Int32 page, Int32 index, [MarshalAsAttribute(UnmanagedType.LPWStr)]
            string text)
        {
            HResult retVal = setString(device, page, index, text.Length, text);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {
                        HResultException.E_PAGENOTACTIVE,
                        "The specified page is not active. Displaying information is not permitted when the page is not active."
                    },
                    {
                        HResultException.E_INVALIDARG,
                        "The dwPage argument does not reference a valid page id, or the dwIndex argument does not reference a valid string id."
                    },
                    {HResultException.E_OUTOFMEMORY, "Insufficient memory to complete the request."},
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Sets the image data of a given image.
        /// </summary>
        /// <param name="device"> A handle to a device.</param>
        /// <param name="page">A numeric identifier of a page. Usually this is the 0 based number of the page.</param>
        /// <param name="index">A numeric identifier of the image. Refer to the data sheet for each device to determine what images are present.</param>
        /// <param name="buffer">An array of bytes that represents the raw bitmap to display on the screen.</param>
        /// <remarks>
        /// The buffer passed must be the correct size for the specified image. Devices support JPEG or BITMAP image data, and the buffer
        /// should contain all necessary headers and footers.
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void SetImage(IntPtr device, Int32 page, Int32 index, byte[] buffer)
        {
            HResult retVal = setImage(device, page, index, buffer.Length, buffer);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {
                        HResultException.E_PAGENOTACTIVE,
                        "The specified page is not active. Displaying information is not permitted when the page is not active."
                    },
                    {
                        HResultException.E_INVALIDARG,
                        "The page argument does not reference a valid page id, or the index argument does not reference a valid image id."
                    },
                    {HResultException.E_OUTOFMEMORY, "Insufficient memory to complete the request."},
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }

        /// <summary>
        /// Sets the image from an image file.
        /// </summary>
        /// <param name="device">A handle to a device.</param>
        /// <param name="page">A numeric identifier of a page. Usually this is the 0 based number of the page.</param>
        /// <param name="index">A numeric identifier of the image. Refer to the data sheet for each device to determine what images are present.</param>
        /// <param name="filename">Full path to the image to load</param>
        /// <remarks>
        /// The file must be a JPEG or BMP file of the correct size.
        /// </remarks>
        /// <exception cref="HResultException"></exception>
        public void SetImageFromFile(IntPtr device, Int32 page, Int32 index, string filename)
        {
            HResult retVal = setImageFromFile(device, page, index, filename.Length, filename);
            if (retVal != HResultException.S_OK)
            {
                Dictionary<HResult, string> errorsMap = new Dictionary<HResult, string>()
                {
                    {
                        HResultException.E_PAGENOTACTIVE,
                        "The specified page is not active. Displaying information is not permitted when the page is not active."
                    },
                    {
                        HResultException.E_INVALIDARG,
                        "The page argument does not reference a valid page id, or the index argument does not reference a valid image id."
                    },
                    {HResultException.E_OUTOFMEMORY, "Insufficient memory to complete the request."},
                    {HResultException.E_HANDLE, "The device handle specified is invalid."}
                };
                throw new HResultException(retVal, errorsMap);
            }
        }
    }
}