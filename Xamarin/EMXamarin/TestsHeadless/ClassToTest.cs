using System;
using System.Diagnostics;

namespace TestsHeadless
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Debug.WriteLine ("Hello World!");
			Adder adder = new Adder();
			Int64 c = adder.add(1, 2);
			Debug.WriteLine ("C = " + c);
		}
	}

	class Adder
	{
		public Int64 add(Int64 a, Int64 b)
		{
			return a + b;
		}
	}
}