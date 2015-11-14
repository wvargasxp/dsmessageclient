using System;
using Com.EM.Android;
using Com.EM.Util;

namespace Emdroid {
	public class DrawableResources : Java.Lang.Object, IDrawableResources {

		public int VideoResource { get; set; }
		public int AudioResource { get; set; }

		private static DrawableResources defaultResource;
		public static DrawableResources Default {
			get {
				if (defaultResource == null) {
					defaultResource =  new DrawableResources (Resource.Drawable.videoicon, Resource.Drawable.soundrecordinginlinewaveformgray);
				}

				return defaultResource;
			}
		}

		public DrawableResources (int videoResource, int audioResource) {
			this.VideoResource = videoResource;
			this.AudioResource = audioResource;
		}
	}
}

