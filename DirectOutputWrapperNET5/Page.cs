using System;
using DirectOutputWrapperNET5;

namespace DirectOutputCSharpWrapper
{
    public class Page
    {
        private string _firstLine = "";
        private string _secondLine = "";
        private string _thirdLine = "";
        private DirectOutputExtended _context;
        private int _pageNum { get; set; }
        private readonly IntPtr _parentDevice;

        public Page(IntPtr parentDevice, int pageNum, DirectOutputExtended context)
        {
            _context = context;
            _pageNum = pageNum;
            _parentDevice = parentDevice;
        }

        public string FirstLine
        {
            get => _firstLine;
            set => _firstLine = value;
        }

        public string SecondLine
        {
            get => _secondLine;
            set => _secondLine = value;
        }

        public string ThirdLine
        {
            get => _thirdLine;
            set => _thirdLine = value;
        }

        /// <summary>
        /// Writes all string onto MFD, use only on an active page!
        /// </summary>
        public void Refresh()
        {
            _context.SetString(_parentDevice, _pageNum, MFDLines.FirstLine, _firstLine);
            _context.SetString(_parentDevice, _pageNum, MFDLines.SecondLine, _secondLine);
            _context.SetString(_parentDevice, _pageNum, MFDLines.ThirdLine, _thirdLine);
        }
    }
}