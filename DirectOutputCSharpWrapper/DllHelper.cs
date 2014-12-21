using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DirectOutputCSharpWrapper {

	public class OutOfMemoryException : Exception {
		public OutOfMemoryException() : base("The system is out of memory or resources.") {}
	}

	public class BadFormatException : Exception {
		public BadFormatException() : base("The target file is invalid.") {}
	}

	public class FileNotFountException : Exception {
		public FileNotFountException() : base("The specified file was not found.") {}
	}

	public class PathNotFountException : Exception {
		public PathNotFountException() : base("The specified path was not found.") {}
	}

	public class UnknownException : Exception {
		public UnknownException() : base("Unknown exception.") {}
	}

	class DllHelper {

		[DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
		private static extern UInt32 _LoadLibrary(string dllPath);

		[DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
		private static extern IntPtr _GetProcAddress(UInt32 hModule, string procedureName);

		[DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
		private static extern bool _FreeLibrary(UInt32 hModule);

		[DllImport("kernel32.dll", EntryPoint = "GetLastError")]
		private static extern int _GetLastError();

		public static UInt32 LoadLibrary(string dllPath) {
			UInt32 moduleHandle = _LoadLibrary(dllPath);

			switch (moduleHandle) {
				case 0:
					throw new OutOfMemoryException();

				case 2:
					throw new FileNotFountException();

				case 3:
					throw new PathNotFountException();

				case 11:
					throw new BadFormatException();
			}

			if (moduleHandle <= 31) {
				throw new UnknownException();
			}

			return moduleHandle;
		}

		public static T GetFunction<T>(UInt32 hModule, string procedureName) where T : class {
			IntPtr addressOfFunctionToCall = _GetProcAddress(hModule, procedureName);
			if (addressOfFunctionToCall.ToInt32() == 0) {
				throw new Win32Exception(_GetLastError());
			}

            Delegate functionDelegate = Marshal.GetDelegateForFunctionPointer(addressOfFunctionToCall, typeof(T));
			return functionDelegate as T;
		}

		public static void FreeLibrary(UInt32 hModule) {
			bool isLibraryUnloadedSuccessfully = _FreeLibrary(hModule);
			if (!isLibraryUnloadedSuccessfully) {
				throw new Win32Exception(_GetLastError());
			}
		}

    }

}
