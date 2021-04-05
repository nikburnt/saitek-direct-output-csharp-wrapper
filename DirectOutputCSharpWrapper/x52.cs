using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DirectOutputCSharpWrapper
{
    public class X52
    {
        /// <summary>
        /// Contains pages loaded into MFD. Index in list should correspond to the MFD one
        /// Bug: Potential bug if they do not correspond, consider creating a sync function
        /// Bug: Errors could occur during page removal if their page numbers are not updated! TODO create a page adding and removing functions
        /// </summary>
        public List<Page> pages = new List<Page>();

        public int currPage = -1;

        public readonly DirectOutputExtended _context;
        public IntPtr _thisDevice { get; private set; }

        /// <summary>
        /// High level device implementation. Allows for parameter-less constructor,
        /// but will show a warning if parameter-less constructor is used when multiple devices are plugged in!
        /// </summary>
        /// <param name="context">DirectOutputExtended object which represents SDK instance. Will create new if not supplied</param>
        /// <param name="deviceId">Id created device should assume</param>
        public X52(DirectOutputExtended context = null, IntPtr? deviceId = null)
        {
            _context = context ?? new DirectOutputExtended();

            if (_context.IsInitialized == false)
            {
                _context.Initialize();
            }

            if (deviceId == null)
            {
                bool singleAssert = false;
                bool warningIssued = false;
                //Try to get first device
                _context.Enumerate((deviceIntPtr, target) =>
                {
                    if (singleAssert == false)
                    {
                        singleAssert = true;
                    }
                    else if (warningIssued == false)
                    {
                        Console.Error.WriteLine("WARNING: More than one device detected! Last found device will be used!");
                        warningIssued = true;
                    }

                    return _thisDevice = deviceIntPtr;
                });
            }
            else
            {
                _thisDevice = (IntPtr) deviceId;
            }

            _context.RegisterPageCallback(_thisDevice, (device, page, activated, target) =>
            {
                if (activated)
                {
                    pages[page].Refresh();
                    currPage = page;
                    Console.WriteLine("### Callback values ###");
                    Console.WriteLine("Device: " + device + " page: " + page + " activated: " + activated + " target: " + target);
                }
            });
        }

        /// <summary>
        /// Creates a new page and returns it's index todo Create page callback and only update values when it becomes active!
        /// </summary>
        /// <returns></returns>
        public int AddPage()
        {
            var pageToAdd = new Page(_thisDevice, pages.Count, _context);
            pages.Add(pageToAdd);
            var index = pages.FindIndex(page => page == pageToAdd);
            _context.AddPage(_thisDevice, index, 0);
            return index;
        }

        /// <summary>
        /// TODO needs further testing
        /// </summary>
        /// <param name="page"></param>
        public void RemovePage(Page page)
        {
            _context.RemovePage(_thisDevice, pages.FindIndex(page1 => page1 == page));
            pages.Remove(page);
        }
    }
}