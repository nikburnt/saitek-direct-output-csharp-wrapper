using System;
using Microsoft.Win32;

using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace DirectOutputCSharpWrapper {

	public class RegistryKeyNotFound : Exception {
		public RegistryKeyNotFound() : base(@"HKEY_LOCAL_MACHINE\SOFTWARE\Saitek\DirectOutput key not found.") {}
	}

	public class RegistryValueNotFound : Exception {
		public RegistryValueNotFound() : base(@"DirectOutput value in key HKEY_LOCAL_MACHINE\SOFTWARE\Saitek\DirectOutput not found.") {}
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
		private delegate Int32 Initialize([MarshalAsAttribute(UnmanagedType.LPWStr)] string appName);
		private delegate Int32 Deinitialize();
		private delegate Int32 RegisterDeviceCallback([MarshalAs(UnmanagedType.FunctionPtr)]DeviceCallback callback, IntPtr target);
		private delegate Int32 Enumerate([MarshalAs(UnmanagedType.FunctionPtr)]EnumerateCallback callback, IntPtr target);
		private delegate Int32 GetDeviceType(IntPtr hDevice, out Guid pGuidType);
		private delegate Int32 GetDeviceInstance(IntPtr hDevice, out Guid pGuidInstance);
		private delegate Int32 SetProfile(IntPtr hDevice, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] string filename);
		private delegate Int32 RegisterSoftButtonCallback(IntPtr hDevice, [MarshalAs(UnmanagedType.FunctionPtr)]SoftButtonCallback callback, IntPtr target);
		private delegate Int32 RegisterPageCallback(IntPtr hDevice, [MarshalAs(UnmanagedType.FunctionPtr)]PageCallback callback, IntPtr target);
		private delegate Int32 AddPage(IntPtr hDevice, Int32 dwPage, [MarshalAsAttribute(UnmanagedType.LPWStr)] string wszName, Int32 dwFlags);
		private delegate Int32 RemovePage(IntPtr hDevice, Int32 dwPage);
		private delegate Int32 SetLed(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 dwValue);
		private delegate Int32 SetString(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 cchValue, [MarshalAsAttribute(UnmanagedType.LPWStr)] string wszValue);
		private delegate Int32 SetImage(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 cbValue, byte[] pbValue);
		private delegate Int32 SetImageFromFile(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] string filename);
		private delegate Int32 StartServer(IntPtr hDevice, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] string filename, out IntPtr pdwServerId, out SRequestStatus psStatus);
		private delegate Int32 CloseServer(IntPtr hDevice, Int32 dwServerId, out SRequestStatus psStatus);
		private delegate Int32 SendServerMsg(IntPtr hDevice, Int32 dwServerId, Int32 dwRequest, Int32 dwPage, Int32 cbIn, IntPtr pvIn, Int32 cbOut, out IntPtr pvOut, out SRequestStatus psStatus);
		private delegate Int32 SendServerFile(IntPtr hDevice, Int32 dwServerId, Int32 dwRequest, Int32 dwPage, Int32 cbInHdr, IntPtr pvInHdr, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] string filename, Int32 cbOut, out IntPtr pvOut, out SRequestStatus psStatus);
		private delegate Int32 SaveFile(IntPtr hDevice, Int32 dwPage, Int32 dwFile, Int32 fileNameLength, [MarshalAsAttribute(UnmanagedType.LPWStr)] string filename, out SRequestStatus psStatus);
		private delegate Int32 DisplayFile(IntPtr hDevice, Int32 dwPage, Int32 dwIndex, Int32 dwFile, out SRequestStatus psStatus);
		private delegate Int32 DeleteFile(IntPtr hDevice, Int32 dwPage, Int32 dwFile, out SRequestStatus psStatus);
		
		// Functions placeholders
		private Initialize initialize;
		private Deinitialize deinitialize;
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

		public DirectOutput() {
			thisHandle = GCHandle.Alloc(this);

			RegistryKey key = Registry.LocalMachine.OpenSubKey(directOutputKey);
			if (key == null) {
				throw new RegistryKeyNotFound();
			}

			object value = key.GetValue("DirectOutput");
			if ((value == null) || !(value is String)) {
				throw new RegistryValueNotFound();
			}

			hModule = DllHelper.LoadLibrary((String)value);

			// Initialize functions
			initialize = DllHelper.GetFunction<Initialize>(hModule, "DirectOutput_Initialize");
			deinitialize = DllHelper.GetFunction<Deinitialize>(hModule, "DirectOutput_Deinitialize");
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

		~DirectOutput() {
			DllHelper.FreeLibrary(hModule);
			thisHandle.Free();
		}

	}

}
