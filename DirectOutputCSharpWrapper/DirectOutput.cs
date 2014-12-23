using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32;

using HResult = System.Int32;

namespace DirectOutputCSharpWrapper {

	public class RegistryKeyNotFound : Exception {
		public RegistryKeyNotFound() : base(@"HKEY_LOCAL_MACHINE\SOFTWARE\Saitek\DirectOutput key not found.") {}
	}

	public class RegistryValueNotFound : Exception {
		public RegistryValueNotFound() : base(@"DirectOutput value in key HKEY_LOCAL_MACHINE\SOFTWARE\Saitek\DirectOutput not found.") {}
	}

	public class HResultException : Exception {

		public const HResult S_OK = 0x00000000;
		public const HResult E_OUTOFMEMORY = unchecked((HResult)0x8007000E);
		public const HResult E_INVALIDARG = unchecked((HResult)0x80070057);
		public const HResult E_HANDLE = unchecked((HResult)0x80070006);

		public HResultException(HResult hResult, Dictionary<HResult, String> errorsMap)
			: base(errorsMap[hResult]) {
			HResult = hResult;
		}
	}

	public class DirectOutput {

		struct SRequestStatus {
			Int32 dwHeaderError;
			Int32 dwHeaderInfo;
			Int32 dwRequestError;
			Int32 dwRequestInfo;
		};

		// Callbacks
		public delegate void EnumerateCallback(IntPtr hDevice, IntPtr pvContext);
		public delegate void DeviceCallback(IntPtr hDevice, bool bAdded, IntPtr pvContext);
		public delegate void SoftButtonCallback(IntPtr hDevice, UInt32 buttons, IntPtr pvContext);
		public delegate void PageCallback(IntPtr hDevice, bool bActivated, IntPtr pvContext);

		// Library functions
		private delegate HResult DirectOutput_Initialize([MarshalAsAttribute(UnmanagedType.LPWStr)] String appName);
		private delegate HResult DirectOutput_Deinitialize();
		private delegate HResult RegisterDeviceCallback([MarshalAs(UnmanagedType.FunctionPtr)]DeviceCallback callback, IntPtr target);
		private delegate HResult Enumerate([MarshalAs(UnmanagedType.FunctionPtr)]EnumerateCallback callback, IntPtr target);
		private delegate HResult GetDeviceType(IntPtr hDevice, out Guid pGuidType);
		private delegate HResult GetDeviceInstance(IntPtr hDevice, out Guid pGuidInstance);
		private delegate HResult SetProfile(IntPtr hDevice, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] String filename);
		private delegate HResult RegisterSoftButtonCallback(IntPtr hDevice, [MarshalAs(UnmanagedType.FunctionPtr)]SoftButtonCallback callback, IntPtr target);
		private delegate HResult RegisterPageCallback(IntPtr hDevice, [MarshalAs(UnmanagedType.FunctionPtr)]PageCallback callback, IntPtr target);
		private delegate HResult AddPage(IntPtr hDevice, Int32 dwPage, [MarshalAsAttribute(UnmanagedType.LPWStr)] String wszName, Int32 dwFlags);
		private delegate HResult RemovePage(IntPtr hDevice, Int32 dwPage);
		private delegate HResult SetLed(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 dwValue);
		private delegate HResult SetString(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 cchValue, [MarshalAsAttribute(UnmanagedType.LPWStr)] String wszValue);
		private delegate HResult SetImage(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 cbValue, byte[] pbValue);
		private delegate HResult SetImageFromFile(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] String filename);
		private delegate HResult StartServer(IntPtr hDevice, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] String filename, out IntPtr pdwServerId, out SRequestStatus psStatus);
		private delegate HResult CloseServer(IntPtr hDevice, Int32 dwServerId, out SRequestStatus psStatus);
		private delegate HResult SendServerMsg(IntPtr hDevice, Int32 dwServerId, Int32 dwRequest, Int32 dwPage, Int32 cbIn, IntPtr pvIn, Int32 cbOut, out IntPtr pvOut, out SRequestStatus psStatus);
		private delegate HResult SendServerFile(IntPtr hDevice, Int32 dwServerId, Int32 dwRequest, Int32 dwPage, Int32 cbInHdr, IntPtr pvInHdr, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] String filename, Int32 cbOut, out IntPtr pvOut, out SRequestStatus psStatus);
		private delegate HResult SaveFile(IntPtr hDevice, Int32 dwPage, Int32 dwFile, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] String filename, out SRequestStatus psStatus);
		private delegate HResult DisplayFile(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 dwFile, out SRequestStatus psStatus);
		private delegate HResult DeleteFile(IntPtr hDevice, Int32 dwPage, Int32 dwFile, out SRequestStatus psStatus);
		
		// Functions placeholders
		private DirectOutput_Initialize initialize;
		private DirectOutput_Deinitialize deinitialize;
		private RegisterDeviceCallback registerDeviceCallback;
		private Enumerate enumerate;
		private GetDeviceType getDeviceType;
		private GetDeviceInstance getDeviceInstance;
		private SetProfile setProfile;
		private RegisterSoftButtonCallback registerSoftButtonCallback;
		private RegisterPageCallback registerPageCallback;
		private AddPage addPage;
		private RemovePage removePage;
		private SetLed setLed;
		private SetString setString;
		private SetImage setImage;
		private SetImageFromFile setImageFromFile;
		private StartServer startServer;
		private CloseServer closeServer;
		private SendServerMsg sendServerMsg;
		private SendServerFile sendServerFile;
		private SaveFile saveFile;
		private DisplayFile displayFile;
		private DeleteFile deleteFile;

		private const String directOutputKey = "SOFTWARE\\Saitek\\DirectOutput";

		private GCHandle thisHandle;
		private UInt32 hModule;

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
		public DirectOutput(String libPath = null) {
			thisHandle = GCHandle.Alloc(this);

			if (libPath == null) {
				RegistryKey key = Registry.LocalMachine.OpenSubKey(directOutputKey);
				if (key == null) {
					throw new RegistryKeyNotFound();
				}

				object value = key.GetValue("DirectOutput");
				if ((value == null) || !(value is String)) {
					throw new RegistryValueNotFound();
				}

				libPath = (String)value;
			}

			hModule = DllHelper.LoadLibrary(libPath);

			InitializeLibraryFunctions();
		}

		~DirectOutput() {
			DllHelper.FreeLibrary(hModule);
			thisHandle.Free();
		}

		private void InitializeLibraryFunctions() {
			initialize = DllHelper.GetFunction<DirectOutput_Initialize>(hModule, "DirectOutput_Initialize");
			deinitialize = DllHelper.GetFunction<DirectOutput_Deinitialize>(hModule, "DirectOutput_Deinitialize");
			registerDeviceCallback = DllHelper.GetFunction<RegisterDeviceCallback>(hModule, "DirectOutput_RegisterDeviceCallback");
			enumerate = DllHelper.GetFunction<Enumerate>(hModule, "DirectOutput_Enumerate");
			getDeviceType = DllHelper.GetFunction<GetDeviceType>(hModule, "DirectOutput_GetDeviceType");
			getDeviceInstance = DllHelper.GetFunction<GetDeviceInstance>(hModule, "DirectOutput_GetDeviceInstance");
			setProfile = DllHelper.GetFunction<SetProfile>(hModule, "DirectOutput_SetProfile");
			registerSoftButtonCallback = DllHelper.GetFunction<RegisterSoftButtonCallback>(hModule, "DirectOutput_RegisterSoftButtonCallback");
			registerPageCallback = DllHelper.GetFunction<RegisterPageCallback>(hModule, "DirectOutput_RegisterPageCallback");
			addPage = DllHelper.GetFunction<AddPage>(hModule, "DirectOutput_AddPage");
			removePage = DllHelper.GetFunction<RemovePage>(hModule, "DirectOutput_RemovePage");
			setLed = DllHelper.GetFunction<SetLed>(hModule, "DirectOutput_SetLed");
			setString = DllHelper.GetFunction<SetString>(hModule, "DirectOutput_SetString");
			setImage = DllHelper.GetFunction<SetImage>(hModule, "DirectOutput_SetImage");
			setImageFromFile = DllHelper.GetFunction<SetImageFromFile>(hModule, "DirectOutput_SetImageFromFile");
			startServer = DllHelper.GetFunction<StartServer>(hModule, "DirectOutput_StartServer");
			closeServer = DllHelper.GetFunction<CloseServer>(hModule, "DirectOutput_CloseServer");
			sendServerMsg = DllHelper.GetFunction<SendServerMsg>(hModule, "DirectOutput_SendServerMsg");
			sendServerFile = DllHelper.GetFunction<SendServerFile>(hModule, "DirectOutput_SendServerFile");
			saveFile = DllHelper.GetFunction<SaveFile>(hModule, "DirectOutput_SaveFile");
			displayFile = DllHelper.GetFunction<DisplayFile>(hModule, "DirectOutput_DisplayFile");
			deleteFile = DllHelper.GetFunction<DeleteFile>(hModule, "DirectOutput_DeleteFile");		
		}

		/// <summary>
		/// Initialize the DirectOutput library.
		/// </summary>
		/// <param name="appName">String that specifies the name of the application. Optional</param>
		/// <remarks>
		/// This function must be called before calling any others. Call this function when you want to initialize the DirectOutput library.
		/// </remarks>
		/// <exception cref="HResultException"></exception>
		public void Initialize(String appName = "DirectOutputCSharpWrapper") {
			HResult retVal = initialize(appName);
			if (retVal != HResultException.S_OK) {
				Dictionary<HResult, String> errorsMap = new Dictionary<HResult, String>() {
					{HResultException.E_OUTOFMEMORY, "There was insufficient memory to complete this call."},
					{HResultException.E_INVALIDARG, "The argument is invalid."},
					{HResultException.E_HANDLE, "The DirectOutputManager prcess could not be found."}
				};
				throw new HResultException(retVal, errorsMap);
			}
		}

		/// <summary>
		/// Clean up the DirectOutput library
		/// </summary>
		/// <remarks>
		/// This function must be called before termination. Call this function to clean up any resources allocated by <see cref="Initialize"/> .
		/// </remarks>
		/// <exception cref="HResultException"></exception>
		public void Deinitialize() {
			HResult retVal = deinitialize();
			if (retVal != HResultException.S_OK) {
				Dictionary<HResult, String> errorsMap = new Dictionary<HResult, String>() {
					{HResultException.E_HANDLE, "DirectOutput was not initialized or was already deinitialized."}
				};
				throw new HResultException(retVal, errorsMap);
			}
		}

	}

}
