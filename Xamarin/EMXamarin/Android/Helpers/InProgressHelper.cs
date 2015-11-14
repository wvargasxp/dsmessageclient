using System;
using Android.App;
using AndroidHUD;
using em;
using Android.Content;

namespace Emdroid {
	public static class InProgressHelper {
		public static void Show (Fragment f) {
			EMTask.DispatchMain (() => {
				if (f == null || !f.IsAdded) return;
				Context ctx = f.Activity;
				AndHUD.Shared.Show (ctx, null, -1, MaskType.Clear, default(TimeSpan?), null, true, null);
			});
		}

		public static void Hide (Fragment f) {
			EMTask.DispatchMain (() => {
				if (f == null || !f.IsAdded) return;
				Context ctx = f.Activity;
				AndHUD.Shared.Dismiss (ctx);
			});
		}

		public static void Show (Activity ctx) {
			EMTask.DispatchMain (() => {
				if (ctx.IsFinishing) return;
				AndHUD.Shared.Show (ctx, null, -1, MaskType.Clear, default(TimeSpan?), null, true, null);
			});
		}

		public static void Hide (Activity ctx) {
			EMTask.DispatchMain (() => {
				if (ctx.IsFinishing) return;
				AndHUD.Shared.Dismiss (ctx);
			});
		}
	}
}

