//
//  AFProtocols.h
//  AFLibrary
//
//  Created by James Nguyen on 3/16/15.
//  Copyright (c) 2015 myrete. All rights reserved.
//

#ifndef AFLibrary_AFProtocols_h
#define AFLibrary_AFProtocols_h

@protocol ProgressHelper <NSObject>
-(void)progress:(NSNumber*)progress;
-(void)begin;
-(NSString*)mediaAddress;
@end

@protocol MediaDownloadHelper <ProgressHelper>
-(void)end:(NSError*)error withResponse:(NSURLResponse*)response atFilePath:(NSURL*)filePath;
@end

@protocol MediaUploadHelper <ProgressHelper>
-(void)end:(NSError*)error withResponse:(NSURLResponse*)response responseObject:(id)responesObject;
-(void)endWithError:(NSError*)error;
-(NSMutableArray*)names;
-(NSMutableArray*)filenames;
-(NSMutableArray*)filepaths;
-(NSMutableArray*)mimeTypes;
@end
#endif
