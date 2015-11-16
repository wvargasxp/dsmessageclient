using System;
using NUnit.Framework;
using NUnit;
using EMXamarin;
using WebSocket4Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using SuperSocket.ClientEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Threading;
using em;

namespace Tests
{

	//[SetUp]
	public class TestSetUp
	{
		public void SetUp() {
			TestUserDB.LoadUsers ();
		}

		[TearDown]
		public void TearDown() {
		}
	}
}

