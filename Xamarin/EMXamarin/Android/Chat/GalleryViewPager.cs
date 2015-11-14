using System;
using Android.Support.V4.View;
using Android.Content;
using Android.Util;
using Com.Ortiz.Touch;
using Android.Views;

namespace Emdroid {
	public class GalleryViewPager : ViewPager {
		public GalleryViewPager (Context context) : base (context) {}

		public GalleryViewPager (Context context, IAttributeSet attributeSet) : base (context, attributeSet) {}

		protected override bool CanScroll (Android.Views.View v, bool checkV, int dx, int x, int y) {
			// slow
			TouchImageView imageView = v.FindViewById<TouchImageView> (Resource.Id.galleryItemImageView);
			if (imageView != null && imageView.IsZoomed)
				return imageView.CanScrollHorizontally (-dx);
			return base.CanScroll (v, checkV, dx, x, y);
		}
	}
}

