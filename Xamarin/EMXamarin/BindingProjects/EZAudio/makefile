XBUILD=/Applications/Xcode.app/Contents/Developer/usr/bin/xcodebuild
PROJECT_ROOT=.
SRC_PROJECT=$(PROJECT_ROOT)/EZAudioXCode/EZAudio
PROJECT=$(SRC_PROJECT)/EZAudio.xcodeproj
TARGET=EZAudio
XCODE=$(SRC_PROJECT)/EZAudio

all: libEZAudio.a

libEZAudio-i386.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphonesimulator -configuration Release clean build
	-mv $(SRC_PROJECT)/build/Release-iphonesimulator/lib$(TARGET).a $@

libEZAudio-armv7.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch armv7 -configuration Release clean build
	-mv $(SRC_PROJECT)/build/Release-iphoneos/lib$(TARGET).a $@

libEZAudio-arm64.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch arm64 -configuration Release clean build
	-mv $(SRC_PROJECT)/build/Release-iphoneos/lib$(TARGET).a $@

libEZAudio.a: libEZAudio-i386.a libEZAudio-armv7.a libEZAudio-arm64.a
	lipo -create -output $@ $^

bind:
	sharpie bind --output=EZAudio --namespace=EZAudio --sdk=iphoneos8.2 \
		${XCODE}/EZAudio.h \
		${XCODE}/AEFloatConverter.h \
		${XCODE}/EZAudioFile.h \
		${XCODE}/EZAudioPlayer.h \
		${XCODE}/EZAudioFile.h \
		${XCODE}/EZAudioPlot.h \
		${XCODE}/EZAudioPlotGL.h \
		${XCODE}/EZAudioPlotGLKViewController.h \
		${XCODE}/EZMicrophone.h \
		${XCODE}/EZOutput.h \
		${XCODE}/EZPlot.h \
		${XCODE}/EZRecorder.h \
		${XCODE}/TPCircularBuffer.h \
		--unified

clean:
	-rm -f *.a *.dll
