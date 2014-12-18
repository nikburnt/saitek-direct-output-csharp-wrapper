using System;
using Microsoft.Win32;

namespace DirectOutputCSharpWrapper
{
    public class DirectOutput
    {
        private const String directOutputKey = "SOFTWARE\\Saitek\\DirectOutput";

        public bool isActive
        {
            get
            {
                return libraryPath != null;
            }
        }

        String libraryPath = null;

        public DirectOutput()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(directOutputKey);
            if (key == null)
            {
                // No Key - No Game)
                return;
            }

            object value = key.GetValue("DirectOutput");
            if (value != null && value is String)
            {
                libraryPath = (String)value;
            }
        }
    }
}
