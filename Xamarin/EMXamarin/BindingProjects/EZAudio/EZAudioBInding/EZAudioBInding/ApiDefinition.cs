using System;

using UIKit;
using Foundation;
using ObjCRuntime;
using CoreGraphics;
using AVFoundation;
using AudioToolbox;
using GLKit;
using OpenGLES;

namespace EZAudio {

	delegate void GetStaticWaveFormDataAction(ref float waveformData, UInt32 bufferLength);

	// @interface AEFloatConverter : NSObject
	[BaseType (typeof (NSObject))]
	interface AEFloatConverter {

		// -(id)initWithSourceFormat:(AudioStreamBasicDescription)sourceFormat;
		[Export ("initWithSourceFormat:")]
		IntPtr Constructor (AudioStreamBasicDescription sourceFormat);

		// @property (readonly, nonatomic) AudioStreamBasicDescription floatingPointAudioDescription;
		[Export ("floatingPointAudioDescription")]
		AudioStreamBasicDescription FloatingPointAudioDescription { get; }

		// @property (readonly, nonatomic) AudioStreamBasicDescription sourceFormat;
		[Export ("sourceFormat")]
		AudioStreamBasicDescription SourceFormat { get; }
	}

	// @protocol EZAudioFileDelegate <NSObject>
	[Protocol, Model]
	[BaseType (typeof (NSObject))]
	interface EZAudioFileDelegate {

		// @optional -(void)audioFile:(EZAudioFile *)audioFile readAudio:(float **)buffer withBufferSize:(UInt32)bufferSize withNumberOfChannels:(UInt32)numberOfChannels;
		[Export ("audioFile:readAudio:withBufferSize:withNumberOfChannels:")]
		void ReadAudio (EZAudioFile audioFile, ref IntPtr buffer, UInt32 bufferSize, UInt32 numberOfChannels);

		// @optional -(void)audioFile:(EZAudioFile *)audioFile updatedPosition:(SInt64)framePosition;
		[Export ("audioFile:updatedPosition:")]
		void UpdatedPosition (EZAudioFile audioFile, Int64 framePosition);
	}

	// @interface EZAudioFile : NSObject
	[BaseType (typeof (NSObject))]
	interface EZAudioFile {

		// -(EZAudioFile *)initWithURL:(NSURL *)url;
		[Export ("initWithURL:")]
		IntPtr Constructor (NSUrl url);

		// -(EZAudioFile *)initWithURL:(NSURL *)url andDelegate:(id<EZAudioFileDelegate>)delegate;
		[Export ("initWithURL:andDelegate:")]
		IntPtr Constructor (NSUrl url, EZAudioFileDelegate audioFileDelegate);

		// @property (assign, nonatomic) id<EZAudioFileDelegate> audioFileDelegate;
		[Export ("audioFileDelegate", ArgumentSemantic.UnsafeUnretained)]
		[NullAllowed]
		NSObject WeakAudioFileDelegate { get; set; }

		// @property (assign, nonatomic) id<EZAudioFileDelegate> audioFileDelegate;
		[Wrap ("WeakAudioFileDelegate")]
		EZAudioFileDelegate AudioFileDelegate { get; set; }

		// @property (assign, nonatomic) UInt32 waveformResolution;
		[Export ("waveformResolution", ArgumentSemantic.UnsafeUnretained)]
		nuint WaveformResolution { get; set; }

		// +(EZAudioFile *)audioFileWithURL:(NSURL *)url;
		[MarshalNativeExceptions]
		[Static, Export ("audioFileWithURL:")]
		EZAudioFile AudioFileWithURL (NSUrl url);

		// +(EZAudioFile *)audioFileWithURL:(NSURL *)url andDelegate:(id<EZAudioFileDelegate>)delegate;
		[Static, Export ("audioFileWithURL:andDelegate:")]
		EZAudioFile AudioFileWithURL (NSUrl url, EZAudioFileDelegate audioFileDelegate);

		// +(NSArray *)supportedAudioFileTypes;
		[Static, Export ("supportedAudioFileTypes")]
		NSObject [] SupportedAudioFileTypes ();

		// -(void)readFrames:(UInt32)frames audioBufferList:(AudioBufferList *)audioBufferList bufferSize:(UInt32 *)bufferSize eof:(BOOL *)eof;
		[Export ("readFrames:audioBufferList:bufferSize:eof:")]
		void ReadFrames (UInt32 frames, IntPtr audioBufferList, UInt32 bufferSize, ref bool eof);

		// -(void)seekToFrame:(SInt64)frame;
		[Export ("seekToFrame:")]
		void SeekToFrame (Int64 frame);

		// -(AudioStreamBasicDescription)clientFormat;
		[Export ("clientFormat")]
		AudioStreamBasicDescription ClientFormat ();

		// -(AudioStreamBasicDescription)fileFormat;
		[Export ("fileFormat")]
		AudioStreamBasicDescription FileFormat ();

		// -(SInt64)frameIndex;
		[Export ("frameIndex")]
		long FrameIndex ();

		// -(NSDictionary *)metadata;
		[Export ("metadata")]
		NSDictionary Metadata ();

		// -(Float32)totalDuration;
		[Export ("totalDuration")]
		float TotalDuration ();

		// -(SInt64)totalFrames;
		[Export ("totalFrames")]
		long TotalFrames ();

		// -(NSURL *)url;
		[Export ("url")]
		NSUrl Url ();

		// -(BOOL)hasLoadedAudioData;
		[Export ("hasLoadedAudioData")]
		bool HasLoadedAudioData ();

		// -(void)getWaveformDataWithCompletionBlock:(WaveformDataCompletionBlock)waveformDataCompletionBlock;
		[Export ("getWaveformDataWithCompletionBlock:")]
		void GetWaveformDataWithCompletionBlock (GetStaticWaveFormDataAction waveformDataCompletionBlock);

		// -(UInt32)minBuffersWithFrameRate:(UInt32)frameRate;
		[Export ("minBuffersWithFrameRate:")]
		nuint MinBuffersWithFrameRate (UInt32 frameRate);

		// -(UInt32)recommendedDrawingFrameRate;
		[Export ("recommendedDrawingFrameRate")]
		nuint RecommendedDrawingFrameRate ();
	}

	// @protocol EZMicrophoneDelegate <NSObject>
	[Protocol, Model]
	[BaseType (typeof (NSObject))]
	interface EZMicrophoneDelegate {

		// @optional -(void)microphone:(EZMicrophone *)microphone hasAudioStreamBasicDescription:(AudioStreamBasicDescription)audioStreamBasicDescription;
		[Export ("microphone:hasAudioStreamBasicDescription:")]
		void HasAudioStreamBasicDescription (EZMicrophone microphone, AudioStreamBasicDescription audioStreamBasicDescription);

		// @optional -(void)microphone:(EZMicrophone *)microphone hasAudioReceived:(float **)buffer withBufferSize:(UInt32)bufferSize withNumberOfChannels:(UInt32)numberOfChannels;
		[Export ("microphone:hasAudioReceived:withBufferSize:withNumberOfChannels:")]
		void HasAudioReceived (EZMicrophone microphone, ref IntPtr buffer, UInt32 bufferSize, UInt32 numberOfChannels);

		// @optional -(void)microphone:(EZMicrophone *)microphone hasBufferList:(AudioBufferList *)bufferList withBufferSize:(UInt32)bufferSize withNumberOfChannels:(UInt32)numberOfChannels;
		[Export ("microphone:hasBufferList:withBufferSize:withNumberOfChannels:")]
		void HasBufferList (EZMicrophone microphone, IntPtr bufferList, UInt32 bufferSize, UInt32 numberOfChannels);
	}

	// @interface EZMicrophone : NSObject
	[BaseType (typeof (NSObject))]
	interface EZMicrophone {

		// -(EZMicrophone *)initWithMicrophoneDelegate:(id<EZMicrophoneDelegate>)microphoneDelegate;
		[Export ("initWithMicrophoneDelegate:")]
		IntPtr Constructor (EZMicrophoneDelegate microphoneDelegate);

		// -(EZMicrophone *)initWithMicrophoneDelegate:(id<EZMicrophoneDelegate>)microphoneDelegate withAudioStreamBasicDescription:(AudioStreamBasicDescription)audioStreamBasicDescription;
		[Export ("initWithMicrophoneDelegate:withAudioStreamBasicDescription:")]
		IntPtr Constructor (EZMicrophoneDelegate microphoneDelegate, AudioStreamBasicDescription audioStreamBasicDescription);

		// -(EZMicrophone *)initWithMicrophoneDelegate:(id<EZMicrophoneDelegate>)microphoneDelegate startsImmediately:(BOOL)startsImmediately;
		[Export ("initWithMicrophoneDelegate:startsImmediately:")]
		IntPtr Constructor (EZMicrophoneDelegate microphoneDelegate, bool startsImmediately);

		// -(EZMicrophone *)initWithMicrophoneDelegate:(id<EZMicrophoneDelegate>)microphoneDelegate withAudioStreamBasicDescription:(AudioStreamBasicDescription)audioStreamBasicDescription startsImmediately:(BOOL)startsImmediately;
		[Export ("initWithMicrophoneDelegate:withAudioStreamBasicDescription:startsImmediately:")]
		IntPtr Constructor (EZMicrophoneDelegate microphoneDelegate, AudioStreamBasicDescription audioStreamBasicDescription, bool startsImmediately);

		// @property (assign, nonatomic) id<EZMicrophoneDelegate> microphoneDelegate;
		[Export ("microphoneDelegate", ArgumentSemantic.UnsafeUnretained)]
		[NullAllowed]
		NSObject WeakMicrophoneDelegate { get; set; }

		// @property (assign, nonatomic) id<EZMicrophoneDelegate> microphoneDelegate;
		[Wrap ("WeakMicrophoneDelegate")]
		EZMicrophoneDelegate MicrophoneDelegate { get; set; }

		// @property (assign, nonatomic) BOOL microphoneOn;
		[Export ("microphoneOn", ArgumentSemantic.UnsafeUnretained)]
		bool MicrophoneOn { get; set; }

		// +(EZMicrophone *)microphoneWithDelegate:(id<EZMicrophoneDelegate>)microphoneDelegate;
		[Static, Export ("microphoneWithDelegate:")]
		EZMicrophone MicrophoneWithDelegate (EZMicrophoneDelegate microphoneDelegate);

		// +(EZMicrophone *)microphoneWithDelegate:(id<EZMicrophoneDelegate>)microphoneDelegate withAudioStreamBasicDescription:(AudioStreamBasicDescription)audioStreamBasicDescription;
		[Static, Export ("microphoneWithDelegate:withAudioStreamBasicDescription:")]
		EZMicrophone MicrophoneWithDelegate (EZMicrophoneDelegate microphoneDelegate, AudioStreamBasicDescription audioStreamBasicDescription);

		// +(EZMicrophone *)microphoneWithDelegate:(id<EZMicrophoneDelegate>)microphoneDelegate startsImmediately:(BOOL)startsImmediately;
		[Static, Export ("microphoneWithDelegate:startsImmediately:")]
		EZMicrophone MicrophoneWithDelegate (EZMicrophoneDelegate microphoneDelegate, bool startsImmediately);

		// +(EZMicrophone *)microphoneWithDelegate:(id<EZMicrophoneDelegate>)microphoneDelegate withAudioStreamBasicDescription:(AudioStreamBasicDescription)audioStreamBasicDescription startsImmediately:(BOOL)startsImmediately;
		[Static, Export ("microphoneWithDelegate:withAudioStreamBasicDescription:startsImmediately:")]
		EZMicrophone MicrophoneWithDelegate (EZMicrophoneDelegate microphoneDelegate, AudioStreamBasicDescription audioStreamBasicDescription, bool startsImmediately);

		// +(EZMicrophone *)sharedMicrophone;
		[Static, Export ("sharedMicrophone")]
		EZMicrophone SharedMicrophone ();

		// -(NSError *)startFetchingAudio;
		[Export ("startFetchingAudio")]
		NSError StartFetchingAudio ();

		// -(void)stopFetchingAudio;
		[Export ("stopFetchingAudio")]
		void StopFetchingAudio ();

		// -(AudioStreamBasicDescription)audioStreamBasicDescription;
		[Export ("audioStreamBasicDescription")]
		AudioStreamBasicDescription AudioStreamBasicDescription ();

		// -(void)setAudioStreamBasicDescription:(AudioStreamBasicDescription)asbd;
		[Export ("setAudioStreamBasicDescription:")]
		void SetAudioStreamBasicDescription (AudioStreamBasicDescription asbd);
	}

	// @protocol EZOutputDataSource <NSObject>
	[Protocol, Model]
	[BaseType (typeof (NSObject))]
	interface EZOutputDataSource {

		// @optional -(void)output:(EZOutput *)output callbackWithActionFlags:(AudioUnitRenderActionFlags *)ioActionFlags inTimeStamp:(const AudioTimeStamp *)inTimeStamp inBusNumber:(UInt32)inBusNumber inNumberFrames:(UInt32)inNumberFrames ioData:(AudioBufferList *)ioData;
		[Export ("output:callbackWithActionFlags:inTimeStamp:inBusNumber:inNumberFrames:ioData:")]
		void CallbackWithActionFlags (EZOutput output, nuint ioActionFlags, AudioTimeStamp inTimeStamp, UInt32 inBusNumber, UInt32 inNumberFrames, IntPtr ioData);

		// @optional -(TPCircularBuffer *)outputShouldUseCircularBuffer:(EZOutput *)output;
		[Export ("outputShouldUseCircularBuffer:")]
		TPCircularBuffer OutputShouldUseCircularBuffer (EZOutput output);

		// @optional -(void)output:(EZOutput *)output shouldFillAudioBufferList:(AudioBufferList *)audioBufferList withNumberOfFrames:(UInt32)frames;
		[Export ("output:shouldFillAudioBufferList:withNumberOfFrames:")]
		void ShouldFillAudioBufferList (EZOutput output, IntPtr audioBufferList, UInt32 frames);
	}

	// @interface EZOutput : NSObject
	[BaseType (typeof (NSObject))]
	interface EZOutput {

		// -(id)initWithDataSource:(id<EZOutputDataSource>)dataSource;
		[Export ("initWithDataSource:")]
		IntPtr Constructor (EZOutputDataSource dataSource);

		// -(id)initWithDataSource:(id<EZOutputDataSource>)dataSource withAudioStreamBasicDescription:(AudioStreamBasicDescription)audioStreamBasicDescription;
		[Export ("initWithDataSource:withAudioStreamBasicDescription:")]
		IntPtr Constructor (EZOutputDataSource dataSource, AudioStreamBasicDescription audioStreamBasicDescription);

		// @property (assign, nonatomic) id<EZOutputDataSource> outputDataSource;
		[Export ("outputDataSource", ArgumentSemantic.UnsafeUnretained)]
		EZOutputDataSource OutputDataSource { get; set; }

		// +(EZOutput *)outputWithDataSource:(id<EZOutputDataSource>)dataSource;
		[Static, Export ("outputWithDataSource:")]
		EZOutput OutputWithDataSource (EZOutputDataSource dataSource);

		// +(EZOutput *)outputWithDataSource:(id<EZOutputDataSource>)dataSource withAudioStreamBasicDescription:(AudioStreamBasicDescription)audioStreamBasicDescription;
		[Static, Export ("outputWithDataSource:withAudioStreamBasicDescription:")]
		EZOutput OutputWithDataSource (EZOutputDataSource dataSource, AudioStreamBasicDescription audioStreamBasicDescription);

		// +(EZOutput *)sharedOutput;
		[Static, Export ("sharedOutput")]
		EZOutput SharedOutput ();

		// -(void)startPlayback;
		[Export ("startPlayback")]
		void StartPlayback ();

		// -(void)stopPlayback;
		[Export ("stopPlayback")]
		void StopPlayback ();

		// -(AudioStreamBasicDescription)audioStreamBasicDescription;
		[Export ("audioStreamBasicDescription")]
		AudioStreamBasicDescription AudioStreamBasicDescription ();

		// -(BOOL)isPlaying;
		[Export ("isPlaying")]
		bool IsPlaying ();

		// -(void)setAudioStreamBasicDescription:(AudioStreamBasicDescription)asbd;
		[Export ("setAudioStreamBasicDescription:")]
		void SetAudioStreamBasicDescription (AudioStreamBasicDescription asbd);
	}

	// @interface EZRecorder : NSObject
	[BaseType (typeof (NSObject))]
	interface EZRecorder {

		// -(EZRecorder *)initWithDestinationURL:(NSURL *)url sourceFormat:(AudioStreamBasicDescription)sourceFormat destinationFileType:(EZRecorderFileType)destinationFileType;
		[Export ("initWithDestinationURL:sourceFormat:destinationFileType:")]
		IntPtr Constructor (NSUrl url, AudioStreamBasicDescription sourceFormat, EZRecorderFileType destinationFileType);

		// +(EZRecorder *)recorderWithDestinationURL:(NSURL *)url sourceFormat:(AudioStreamBasicDescription)sourceFormat destinationFileType:(EZRecorderFileType)destinationFileType;
		[Static, Export ("recorderWithDestinationURL:sourceFormat:destinationFileType:")]
		EZRecorder RecorderWithDestinationURL (NSUrl url, AudioStreamBasicDescription sourceFormat, EZRecorderFileType destinationFileType);

		// -(NSURL *)url;
		[Export ("url")]
		NSUrl Url ();

		// -(void)appendDataFromBufferList:(AudioBufferList *)bufferList withBufferSize:(UInt32)bufferSize;
		[Export ("appendDataFromBufferList:withBufferSize:")]
		void AppendDataFromBufferList (IntPtr bufferList, UInt32 bufferSize);

		// -(void)closeAudioFile;
		[Export ("closeAudioFile")]
		void CloseAudioFile ();
	}

	// @protocol EZAudioPlayerDelegate <NSObject>
	[Protocol, Model]
	[BaseType (typeof (NSObject))]
	interface EZAudioPlayerDelegate {

		// @optional -(void)audioPlayer:(EZAudioPlayer *)audioPlayer didResumePlaybackOnAudioFile:(EZAudioFile *)audioFile;
		[Export ("audioPlayer:didResumePlaybackOnAudioFile:")]
		void DidResumePlaybackOnAudioFile (EZAudioPlayer audioPlayer, EZAudioFile audioFile);

		// @optional -(void)audioPlayer:(EZAudioPlayer *)audioPlayer didPausePlaybackOnAudioFile:(EZAudioFile *)audioFile;
		[Export ("audioPlayer:didPausePlaybackOnAudioFile:")]
		void DidPausePlaybackOnAudioFile (EZAudioPlayer audioPlayer, EZAudioFile audioFile);

		// @optional -(void)audioPlayer:(EZAudioPlayer *)audioPlayer reachedEndOfAudioFile:(EZAudioFile *)audioFile;
		[Export ("audioPlayer:reachedEndOfAudioFile:")]
		void ReachedEndOfAudioFile (EZAudioPlayer audioPlayer, EZAudioFile audioFile);

		// @optional -(void)audioPlayer:(EZAudioPlayer *)audioPlayer readAudio:(float **)buffer withBufferSize:(UInt32)bufferSize withNumberOfChannels:(UInt32)numberOfChannels inAudioFile:(EZAudioFile *)audioFile;
		[Export ("audioPlayer:readAudio:withBufferSize:withNumberOfChannels:inAudioFile:")]
		void ReadAudio (EZAudioPlayer audioPlayer, ref IntPtr buffer, UInt32 bufferSize, UInt32 numberOfChannels, EZAudioFile audioFile);

		// @optional -(void)audioPlayer:(EZAudioPlayer *)audioPlayer updatedPosition:(SInt64)framePosition inAudioFile:(EZAudioFile *)audioFile;
		[Export ("audioPlayer:updatedPosition:inAudioFile:")]
		void UpdatedPosition (EZAudioPlayer audioPlayer, Int64 framePosition, EZAudioFile audioFile);
	}

	// @interface EZAudioPlayer : NSObject
	[BaseType (typeof (NSObject))]
	interface EZAudioPlayer {

		// -(EZAudioPlayer *)initWithEZAudioFile:(EZAudioFile *)audioFile;
		[Export ("initWithEZAudioFile:")]
		IntPtr Constructor (EZAudioFile audioFile);

		// -(EZAudioPlayer *)initWithEZAudioFile:(EZAudioFile *)audioFile withDelegate:(id<EZAudioPlayerDelegate>)audioPlayerDelegate;
		[Export ("initWithEZAudioFile:withDelegate:")]
		IntPtr Constructor (EZAudioFile audioFile, EZAudioPlayerDelegate audioPlayerDelegate);

		// -(EZAudioPlayer *)initWithURL:(NSURL *)url;
		[Export ("initWithURL:")]
		IntPtr Constructor (NSUrl url);

		// -(EZAudioPlayer *)initWithURL:(NSURL *)url withDelegate:(id<EZAudioPlayerDelegate>)audioPlayerDelegate;
		[Export ("initWithURL:withDelegate:")]
		IntPtr Constructor (NSUrl url, EZAudioPlayerDelegate audioPlayerDelegate);

		// @property (assign, nonatomic) id<EZAudioPlayerDelegate> audioPlayerDelegate;
		[Export ("audioPlayerDelegate", ArgumentSemantic.UnsafeUnretained)]
		[NullAllowed]
		NSObject WeakAudioPlayerDelegate { get; set; }

		// @property (assign, nonatomic) id<EZAudioPlayerDelegate> audioPlayerDelegate;
		[Wrap ("WeakAudioPlayerDelegate")]
		EZAudioPlayerDelegate AudioPlayerDelegate { get; set; }

		// @property (assign, nonatomic) BOOL shouldLoop;
		[Export ("shouldLoop", ArgumentSemantic.UnsafeUnretained)]
		bool ShouldLoop { get; set; }

		// +(EZAudioPlayer *)audioPlayerWithEZAudioFile:(EZAudioFile *)audioFile;
		[Static, Export ("audioPlayerWithEZAudioFile:")]
		EZAudioPlayer AudioPlayerWithEZAudioFile (EZAudioFile audioFile);

		// +(EZAudioPlayer *)audioPlayerWithEZAudioFile:(EZAudioFile *)audioFile withDelegate:(id<EZAudioPlayerDelegate>)audioPlayerDelegate;
		[Static, Export ("audioPlayerWithEZAudioFile:withDelegate:")]
		EZAudioPlayer AudioPlayerWithEZAudioFile (EZAudioFile audioFile, EZAudioPlayerDelegate audioPlayerDelegate);

		// +(EZAudioPlayer *)audioPlayerWithURL:(NSURL *)url;
		[Static, Export ("audioPlayerWithURL:")]
		EZAudioPlayer AudioPlayerWithURL (NSUrl url);

		// +(EZAudioPlayer *)audioPlayerWithURL:(NSURL *)url withDelegate:(id<EZAudioPlayerDelegate>)audioPlayerDelegate;
		[Static, Export ("audioPlayerWithURL:withDelegate:")]
		EZAudioPlayer AudioPlayerWithURL (NSUrl url, EZAudioPlayerDelegate audioPlayerDelegate);

		// +(EZAudioPlayer *)sharedAudioPlayer;
		[Static, Export ("sharedAudioPlayer")]
		EZAudioPlayer SharedAudioPlayer ();

		// -(EZAudioFile *)audioFile;
		[Export ("audioFile")]
		EZAudioFile AudioFile ();

		// -(float)currentTime;
		[Export ("currentTime")]
		float CurrentTime ();

		// -(BOOL)endOfFile;
		[Export ("endOfFile")]
		bool EndOfFile ();

		// -(SInt64)frameIndex;
		[Export ("frameIndex")]
		long FrameIndex ();

		// -(BOOL)isPlaying;
		[Export ("isPlaying")]
		bool IsPlaying ();

		// -(EZOutput *)output;
		[Export ("output")]
		EZOutput Output ();

		// -(float)totalDuration;
		[Export ("totalDuration")]
		float TotalDuration ();

		// -(SInt64)totalFrames;
		[Export ("totalFrames")]
		long TotalFrames ();

		// -(NSURL *)url;
		[Export ("url")]
		NSUrl Url ();

		// -(void)setAudioFile:(EZAudioFile *)audioFile;
		[Export ("setAudioFile:")]
		void SetAudioFile (EZAudioFile audioFile);

		// -(void)setOutput:(EZOutput *)output;
		[Export ("setOutput:")]
		void SetOutput (EZOutput output);

		// -(void)play;
		[Export ("play")]
		void Play ();

		// -(void)pause;
		[Export ("pause")]
		void Pause ();

		// -(void)stop;
		[Export ("stop")]
		void Stop ();

		// -(void)seekToFrame:(SInt64)frame;
		[Export ("seekToFrame:")]
		void SeekToFrame (Int64 frame);
	}

	// @interface EZPlot : UIView
	[BaseType (typeof (UIView))]
	interface EZPlot {

		// @property (nonatomic, strong) id backgroundColor;
		[Export ("backgroundColor", ArgumentSemantic.Retain)]
		NSObject BackgroundColor { get; set; }

		// @property (nonatomic, strong) id color;
		[Export ("color", ArgumentSemantic.Retain)]
		NSObject Color { get; set; }

		// @property (assign, nonatomic, setter = setGain:) float gain;
		[Export ("gain", ArgumentSemantic.UnsafeUnretained)]
		float Gain { get; set; }

		// @property (assign, nonatomic, setter = setPlotType:) EZPlotType plotType;
		[Export ("plotType", ArgumentSemantic.UnsafeUnretained)]
		EZPlotType PlotType { get; set; }

		// @property (assign, nonatomic, setter = setShouldFill:) BOOL shouldFill;
		[Export ("shouldFill", ArgumentSemantic.UnsafeUnretained)]
		bool ShouldFill { get; set; }

		// @property (assign, nonatomic, setter = setShouldMirror:) BOOL shouldMirror;
		[Export ("shouldMirror", ArgumentSemantic.UnsafeUnretained)]
		bool ShouldMirror { get; set; }

		// -(void)clear;
		[Export ("clear")]
		void Clear ();

		// -(void)updateBuffer:(float *)buffer withBufferSize:(UInt32)bufferSize;
		[Export ("updateBuffer:withBufferSize:")]
		void UpdateBuffer (ref float buffer, UInt32 bufferSize);
	}

	// @interface EZAudioPlot : EZPlot
	[BaseType (typeof (EZPlot))]
	interface EZAudioPlot {

		// -(int)setRollingHistoryLength:(int)historyLength;
		[Export ("setRollingHistoryLength:")]
		int SetRollingHistoryLength (int historyLength);

		// -(int)rollingHistoryLength;
		[Export ("rollingHistoryLength")]
		int RollingHistoryLength ();

		// -(void)setSampleData:(float *)data length:(int)length;
		[Export ("setSampleData:length:")]
		void SetSampleData (ref float data, int length);

		// -(id)initWithFrame:(CGRect)frame
		[Export ("initWithFrame:")]
		IntPtr Constructor (CGRect frame);
	}

	// @interface EZAudioPlotGL : EZPlot
	[BaseType (typeof (EZPlot))]
	interface EZAudioPlotGL {

		// -(int)setRollingHistoryLength:(int)historyLength;
		[Export ("setRollingHistoryLength:")]
		int SetRollingHistoryLength (int historyLength);

		// -(int)rollingHistoryLength;
		[Export ("rollingHistoryLength")]
		int RollingHistoryLength ();

		// -(void)clear;
		[Export ("clear")]
		void Clear ();

		// +(void)fillGraph:(EZAudioPlotGLPoint *)graph withGraphSize:(UInt32)graphSize forDrawingType:(EZAudioPlotGLDrawType)drawingType withBuffer:(float *)buffer withBufferSize:(UInt32)bufferSize withGain:(float)gain;
		[Static, Export ("fillGraph:withGraphSize:forDrawingType:withBuffer:withBufferSize:withGain:")]
		void FillGraph (EZAudioPlotGLPoint graph, UInt32 graphSize, EZAudioPlotGLDrawType drawingType, ref float buffer, UInt32 bufferSize, float gain);

		// +(UInt32)graphSizeForDrawingType:(EZAudioPlotGLDrawType)drawingType withBufferSize:(UInt32)bufferSize;
		[Static, Export ("graphSizeForDrawingType:withBufferSize:")]
		nuint GraphSizeForDrawingType (EZAudioPlotGLDrawType drawingType, UInt32 bufferSize);

		// -(id)initWithFrame:(CGRect)frame
		[Export ("initWithFrame:")]
		IntPtr Constructor (CGRect frame);
	}

	// @interface EZAudioPlotGLKViewController : GLKViewController
	[BaseType (typeof (GLKViewController))]
	interface EZAudioPlotGLKViewController {

		// @property (nonatomic, strong) UIColor * backgroundColor;
		[Export ("backgroundColor", ArgumentSemantic.Retain)]
		UIColor BackgroundColor { get; set; }

		// @property (nonatomic, strong) GLKBaseEffect * baseEffect;
		[Export ("baseEffect", ArgumentSemantic.Retain)]
		GLKBaseEffect BaseEffect { get; set; }

		// @property (nonatomic, strong) UIColor * color;
		[Export ("color", ArgumentSemantic.Retain)]
		UIColor Color { get; set; }

		// @property (nonatomic, strong) EAGLContext * context;
		[Export ("context", ArgumentSemantic.Retain)]
		EAGLContext Context { get; set; }

		// @property (assign, nonatomic) EZAudioPlotGLDrawType drawingType;
		[Export ("drawingType", ArgumentSemantic.UnsafeUnretained)]
		EZAudioPlotGLDrawType DrawingType { get; set; }

		// @property (assign, nonatomic, setter = setGain:) float gain;
		[Export ("gain", ArgumentSemantic.UnsafeUnretained)]
		float Gain { get; set; }

		// @property (assign, nonatomic, setter = setPlotType:) EZPlotType plotType;
		[Export ("plotType", ArgumentSemantic.UnsafeUnretained)]
		EZPlotType PlotType { get; set; }

		// @property (assign, nonatomic, setter = setShouldMirror:) BOOL shouldMirror;
		[Export ("shouldMirror", ArgumentSemantic.UnsafeUnretained)]
		bool ShouldMirror { get; set; }

		// -(int)setRollingHistoryLength:(int)historyLength;
		[Export ("setRollingHistoryLength:")]
		int SetRollingHistoryLength (int historyLength);

		// -(int)rollingHistoryLength;
		[Export ("rollingHistoryLength")]
		int RollingHistoryLength ();

		// -(void)clear;
		[Export ("clear")]
		void Clear ();

		// -(void)updateBuffer:(float *)buffer withBufferSize:(UInt32)bufferSize;
		[Export ("updateBuffer:withBufferSize:")]
		void UpdateBuffer (ref float buffer, UInt32 bufferSize);
	}

	// @interface EZAudio : NSObject
	[BaseType (typeof (NSObject))]
	interface EZAudio {

		// +(AudioBufferList *)audioBufferListWithNumberOfFrames:(UInt32)frames numberOfChannels:(UInt32)channels interleaved:(BOOL)interleaved;
		[Static, Export ("audioBufferListWithNumberOfFrames:numberOfChannels:interleaved:")]
		IntPtr AudioBufferListWithNumberOfFrames (UInt32 frames, UInt32 channels, bool interleaved);

		// +(void)freeBufferList:(AudioBufferList *)bufferList;
		[Static, Export ("freeBufferList:")]
		void FreeBufferList (IntPtr bufferList);

		// +(AudioStreamBasicDescription)AIFFFormatWithNumberOfChannels:(UInt32)channels sampleRate:(float)sampleRate;
		[Static, Export ("AIFFFormatWithNumberOfChannels:sampleRate:")]
		AudioStreamBasicDescription AIFFFormatWithNumberOfChannels (UInt32 channels, float sampleRate);

		// +(AudioStreamBasicDescription)iLBCFormatWithSampleRate:(float)sampleRate;
		[Static, Export ("iLBCFormatWithSampleRate:")]
		AudioStreamBasicDescription ILBCFormatWithSampleRate (float sampleRate);

		// +(AudioStreamBasicDescription)M4AFormatWithNumberOfChannels:(UInt32)channels sampleRate:(float)sampleRate;
		[Static, Export ("M4AFormatWithNumberOfChannels:sampleRate:")]
		AudioStreamBasicDescription M4AFormatWithNumberOfChannels (UInt32 channels, float sampleRate);

		// +(AudioStreamBasicDescription)monoFloatFormatWithSampleRate:(float)sampleRate;
		[Static, Export ("monoFloatFormatWithSampleRate:")]
		AudioStreamBasicDescription MonoFloatFormatWithSampleRate (float sampleRate);

		// +(AudioStreamBasicDescription)monoCanonicalFormatWithSampleRate:(float)sampleRate;
		[Static, Export ("monoCanonicalFormatWithSampleRate:")]
		AudioStreamBasicDescription MonoCanonicalFormatWithSampleRate (float sampleRate);

		// +(AudioStreamBasicDescription)stereoCanonicalNonInterleavedFormatWithSampleRate:(float)sampleRate;
		[Static, Export ("stereoCanonicalNonInterleavedFormatWithSampleRate:")]
		AudioStreamBasicDescription StereoCanonicalNonInterleavedFormatWithSampleRate (float sampleRate);

		// +(AudioStreamBasicDescription)stereoFloatInterleavedFormatWithSampleRate:(float)sampleRate;
		[Static, Export ("stereoFloatInterleavedFormatWithSampleRate:")]
		AudioStreamBasicDescription StereoFloatInterleavedFormatWithSampleRate (float sampleRate);

		// +(AudioStreamBasicDescription)stereoFloatNonInterleavedFormatWithSampleRate:(float)sameRate;
		[Static, Export ("stereoFloatNonInterleavedFormatWithSampleRate:")]
		AudioStreamBasicDescription StereoFloatNonInterleavedFormatWithSampleRate (float sameRate);

		// +(void)printASBD:(AudioStreamBasicDescription)asbd;
		[Static, Export ("printASBD:")]
		void PrintASBD (AudioStreamBasicDescription asbd);

		// +(void)setCanonicalAudioStreamBasicDescription:(AudioStreamBasicDescription *)asbd numberOfChannels:(UInt32)nChannels interleaved:(BOOL)interleaved;
		[Static, Export ("setCanonicalAudioStreamBasicDescription:numberOfChannels:interleaved:")]
		void SetCanonicalAudioStreamBasicDescription (AudioStreamBasicDescription asbd, UInt32 nChannels, bool interleaved);

		// +(void)appendBufferAndShift:(float *)buffer withBufferSize:(int)bufferLength toScrollHistory:(float *)scrollHistory withScrollHistorySize:(int)scrollHistoryLength;
		[Static, Export ("appendBufferAndShift:withBufferSize:toScrollHistory:withScrollHistorySize:")]
		void AppendBufferAndShift (ref float buffer, int bufferLength, ref float scrollHistory, int scrollHistoryLength);

		// +(void)appendValue:(float)value toScrollHistory:(float *)scrollHistory withScrollHistorySize:(int)scrollHistoryLength;
		[Static, Export ("appendValue:toScrollHistory:withScrollHistorySize:")]
		void AppendValue (float value, ref float scrollHistory, int scrollHistoryLength);

		// +(float)MAP:(float)value leftMin:(float)leftMin leftMax:(float)leftMax rightMin:(float)rightMin rightMax:(float)rightMax;
		[Static, Export ("MAP:leftMin:leftMax:rightMin:rightMax:")]
		float MAP (float value, float leftMin, float leftMax, float rightMin, float rightMax);

		// +(float)RMS:(float *)buffer length:(int)bufferSize;
		[Static, Export ("RMS:length:")]
		float RMS (ref float buffer, int bufferSize);

		// +(float)SGN:(float)value;
		[Static, Export ("SGN:")]
		float SGN (float value);

		// +(void)checkResult:(OSStatus)result operation:(const char *)operation;
		[Static, Export ("checkResult:operation:")]
		void CheckResult (nint result, sbyte operation);

		// +(void)updateScrollHistory:(float **)scrollHistory withLength:(int)scrollHistoryLength atIndex:(int *)index withBuffer:(float *)buffer withBufferSize:(int)bufferSize isResolutionChanging:(BOOL *)isChanging;
		[Static, Export ("updateScrollHistory:withLength:atIndex:withBuffer:withBufferSize:isResolutionChanging:")]
		void UpdateScrollHistory (ref IntPtr scrollHistory, int scrollHistoryLength, ref int index, ref float buffer, int bufferSize, ref bool isChanging);

		// +(void)appendDataToCircularBuffer:(TPCircularBuffer *)circularBuffer fromAudioBufferList:(AudioBufferList *)audioBufferList;
		[Static, Export ("appendDataToCircularBuffer:fromAudioBufferList:")]
		void AppendDataToCircularBuffer (TPCircularBuffer circularBuffer, IntPtr audioBufferList);

		// +(void)circularBuffer:(TPCircularBuffer *)circularBuffer withSize:(int)size;
		[Static, Export ("circularBuffer:withSize:")]
		void CircularBuffer (TPCircularBuffer circularBuffer, int size);

		// +(void)freeCircularBuffer:(TPCircularBuffer *)circularBuffer;
		[Static, Export ("freeCircularBuffer:")]
		void FreeCircularBuffer (TPCircularBuffer circularBuffer);
	}
}
