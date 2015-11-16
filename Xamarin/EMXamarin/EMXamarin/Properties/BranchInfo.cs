using System;

namespace EMXamarin
{
	public static class BranchInfo
	{
		// The idea is that we can rename this constant on
		// branches so that we know the branch name.  It's
		// obviously not automatic, and needs to be updated
		// each time a branch is created (on that branch).
		public readonly static string BRANCH_NAME = "dev";
	}
}

