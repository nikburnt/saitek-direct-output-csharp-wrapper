using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DirectOutputCSharpWrapper;

namespace WrapperTest {
	[TestClass]
	public class DirectOutputTest {
		private DirectOutput directOutput;

		[TestInitialize()]
		public void Prepare() {
			directOutput = new DirectOutput(@"c:\Users\daspisch\Downloads\DirectOutput\DirectOutput_x86.dll");
		}

		[TestCleanup()]
		public void Cleanup() {
		}

		[TestMethod]
		public void Initialize() {
			directOutput.Initialize();
			Assert.IsTrue(true);
		}

		[TestMethod]
		public void Deinitialize() {
			directOutput.Deinitialize();
			Assert.IsTrue(true);
		}

	}
}
