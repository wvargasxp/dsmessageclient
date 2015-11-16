using System;
using CoreGraphics;
using GLKit;
using OpenGLES;

namespace EZAudio {

	public enum EZRecorderFileType : long /* nint */ {
		AIFF,
		M4A,
		WAV
	}

	public enum EZPlotType : long /* nint */ {
		Buffer,
		Rolling
	}


	public enum EZAudioPlotGLDrawType : ulong /* nuint */ {
		LineStrip = 3,
		TriangleStrip = 5
	}


	public struct EZAudioPlotGLPoint {
		public float x;
		public float y;
	}
		
	public struct TPCircularBuffer {
		public IntPtr          buffer;
		public Int32           length;
		public Int32           tail;
		public Int32           head;
		public volatile Int32  fillCount;
	}
}