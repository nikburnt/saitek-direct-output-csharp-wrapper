using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DirectOutputCSharpWrapper;

namespace WrapperTest
{
    [TestClass]
    public class DirectOutputTest
    {
        [TestMethod]
        public void IsLibraryFounded()
        {
            DirectOutput directInput = new DirectOutput();
            Assert.AreEqual(true, directInput.isActive);
        }
    }
}
