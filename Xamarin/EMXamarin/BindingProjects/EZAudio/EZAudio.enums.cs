namespace EZAudio {

	[Native]
	public enum EZRecorderFileType : long /* nint */ {
		AIFF,
		M4A,
		WAV
	}

	[Native]
	public enum EZPlotType : long /* nint */ {
		Buffer,
		Rolling
	}

	[Native]
	public enum EZAudioPlotGLDrawType : ulong /* nuint */ {
		LineStrip = 3,
		TriangleStrip = 5
	}
}
