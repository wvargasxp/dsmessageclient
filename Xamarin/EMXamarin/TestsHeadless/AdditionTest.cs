using NUnit.Framework;
using System;
using em;
using EMXamarin;

namespace TestsHeadless
{
	[TestFixture ()]
	public class AdditionTest
	{
		[Test ()]
		public void AdditionTestCase ()
		{
			Adder adder = new Adder ();
			Int64 sum = adder.add (1, 2);
			Assert.AreEqual (sum, 3);
		}

		[Test ()]
		public void AdditionBigNumTestCase ()
		{
			Adder adder = new Adder ();
			Int64 sum = adder.add (1000000000000, 2000000000000);
			Assert.AreEqual (sum, 3000000000000);
		}

		[Test ()]
		public void AdditionNegativeTestCase ()
		{
			Adder adder = new Adder ();
			Int64 sum = adder.add (-1, -2);
			Assert.AreEqual (sum, -3);
		}

		[Test ()]
		public void FailedAdditionCase ()
		{
			Adder adder = new Adder ();
			Int64 sum = adder.add (-1, -2);
			Assert.AreNotEqual (sum, -4);
		}
	}
}