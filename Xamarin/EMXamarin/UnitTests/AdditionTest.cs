using System;
using NUnit.Framework;

namespace em {

	[TestFixture]
	public class AdditionTest {
		[Test]
		public void Addition () {
			Assert.AreEqual (3, 3);
		}

		[Test]
		public void AdditionBigNum () {
			Assert.AreEqual (3000000000000, 3000000000000);
		}

		[Test]
		public void AdditionNegative () {
		
			Assert.AreEqual (-3, -3);
		}

		[Test]
		public void FailedAddition () {
		
			Assert.AreNotEqual (-3, -4);
		}
	}
}