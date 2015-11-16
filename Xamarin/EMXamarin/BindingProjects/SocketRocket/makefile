XBUILD=/Applications/Xcode.app/Contents/Developer/usr/bin/xcodebuild
PROJECT_ROOT=.
PROJECT=$(PROJECT_ROOT)/SocketRocket.xcodeproj
TARGET=SocketRocket
BTOUCH=btouch-native
# BTOUCH=/Developer/MonoTouch/usr/bin/btouch-native

all: libSocketRocket.a

SocketRocket-i386.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphonesimulator -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphonesimulator/lib$(TARGET).a $@

SocketRocket-armv7.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch armv7 -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphoneos/lib$(TARGET).a $@

SocketRocket-arm64.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch arm64 -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphoneos/lib$(TARGET).a $@

libSocketRocket.a: SocketRocket-i386.a SocketRocket-armv7.a SocketRocket-arm64.a
	lipo -create -output $@ $^

clean:
	-rm -f libSocketRocket.a
