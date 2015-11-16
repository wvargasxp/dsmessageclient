XBUILD=/Applications/Xcode.app/Contents/Developer/usr/bin/xcodebuild
PROJECT_ROOT=.
PROJECT=$(PROJECT_ROOT)/AFLibrary.xcodeproj
TARGET=AFLibrary
BTOUCH=btouch-native

all: AFLibrary.dll

libAFLibrary-i386.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphonesimulator -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphonesimulator/lib$(TARGET).a $@

libAFLibrary-armv7.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch armv7 -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphoneos/lib$(TARGET).a $@

libAFLibrary-arm64.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch arm64 -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphoneos/lib$(TARGET).a $@

libAFLibrary.a: libAFLibrary-i386.a libAFLibrary-armv7.a libAFLibrary-arm64.a
	lipo -create -output $@ $^

AFLibrary.dll: AssemblyInfo.cs ApiDefinition.cs extras.cs libAFLibrary.a
	$(BTOUCH) -unsafe -out:$@ ApiDefinition.cs -x=AssemblyInfo.cs -x=extras.cs --link-with=libAFLibrary.a,libAFLibrary.a

clean:
	-rm -f *.a *.dll
