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
		private static extern IntPtr _LoadLibrary(string dllPath);

		[DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
		private static extern IntPtr _GetProcAddress(IntPtr hModule, string procedureName);

		[DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
		private static extern bool _FreeLibrary(IntPtr hModule);

		[DllImport("kernel32.dll", EntryPoint = "GetLastError")]
		private static extern int _GetLastError();

		public static IntPtr LoadLibrary(string dllPath) {
			IntPtr moduleHandle = _LoadLibrary(dllPath);
			if (moduleHandle == new IntPtr(0)) {
				throw new OutOfMemoryException();
			} else if (moduleHandle == new IntPtr(2)) {
				throw new FileNotFountException();
			} else if (moduleHandle == new IntPtr(3)) {
				throw new PathNotFountException();
			} else if (moduleHandle == new IntPtr(11)) {
				throw new BadFormatException();
			}

			return moduleHandle;
		}

		public static T GetFunction<T>(IntPtr hModule, string procedureName) where T : class {
			IntPtr addressOfFunctionToCall = _GetProcAddress(hModule, procedureName);
			if (addressOfFunctionToCall == IntPtr.Zero) {
				throw new Win32Exception(_GetLastError());
			}

            Delegate functionDelegate = Marshal.GetDelegateForFunctionPointer(addressOfFunctionToCall, typeof(T));
			return functionDelegate as T;
		}

		public static void FreeLibrary(IntPtr hModule) {
			bool isLibraryUnloadedSuccessfully = _FreeLibrary(hModule);
			if (!isLibraryUnloadedSuccessfully) {
				throw new Win32Exception(_GetLastError());
			}
		}

    }

}
