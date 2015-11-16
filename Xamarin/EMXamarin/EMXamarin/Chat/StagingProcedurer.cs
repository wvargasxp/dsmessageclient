using System;

namespace em {
	public class StagingProcedurer {
		
		private bool busyStaging = false;

		/**
		 * Whether or not a staging procedure is in progress
		 */
		public bool IsBusyStaging { 
			get {
				return this.busyStaging;
			}
		}

		/**
		 * @returns true if this invokcation successfully started the procedure
		 * note: invoke on main thread only
		 */
		public bool BeginStagingItemProcedure () {
			if (this.IsBusyStaging) {
				return false;
			}

			this.busyStaging = true;
			return true;
		}

		/**
		 * note: invoke on main thread only
		 */
		public void EndStagingItemProcedure () {
			this.busyStaging = false;
		}
	}
}

