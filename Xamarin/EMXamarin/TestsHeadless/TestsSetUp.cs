using EMXamarin;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading;
namespace TestsHeadless
{
	[TestFixture ()]
	public class EMClientTests
	{
		[SetUp ()]
		public void BuildDBAndUsers ()
		{
			Monitor.Enter (TestsManager.TestLock);
			ProcessStartInfo dBStartInfo = new ProcessStartInfo (){ FileName = "../../blastDB.sh"};
			Process blastDBProcess = new Process () {StartInfo = dBStartInfo};
			blastDBProcess.Start ();
			blastDBProcess.WaitForExit (30 * 1000);
			blastDBProcess.Close (); 
			ConnectionInfo.connectionType = ConnectionType.Test;
			TestUserDB.LoadUsers ();
		}

		[TearDown ()]
		public void TestHasFinished ()
		{
			Monitor.Exit (TestsManager.TestLock);
		}
	}
}