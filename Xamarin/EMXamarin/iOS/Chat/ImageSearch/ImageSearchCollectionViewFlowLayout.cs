using System;
using UIKit;
using CoreGraphics;

namespace iOS {
	public class ImageSearchCollectionViewFlowLayout : UICollectionViewFlowLayout {
		public ImageSearchCollectionViewFlowLayout () {}

		public static UICollectionViewFlowLayout NewInstance () {
			UICollectionViewFlowLayout layout = new ImageSearchCollectionViewFlowLayout ();
			layout.ItemSize = new CGSize (120, 120);
			layout.MinimumInteritemSpacing = 4;
			layout.ScrollDirection = UICollectionViewScrollDirection.Vertical;
			return layout;
		}
	}
}

