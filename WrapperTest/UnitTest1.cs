using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DirectInputCSharpWrapper;

namespace WrapperTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            DirectInput directInput = new DirectInput();
            Assert.AreEqual("ololo", directInput.Test());
        }
    }
}
