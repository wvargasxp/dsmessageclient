//
//  AFLibrary.h
//  AFLibrary
//
//  Created by James Nguyen on 3/16/15.
//  Copyright (c) 2015 myrete. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AFResponse.h"
#import "AFURLSessionManager.h"
#import "AFProtocols.h"
#import "AFProgressListener.h"
@interface AFLibrary : NSObject

@property (strong, nonatomic) AFURLSessionManager* downloadSessionManager;
@property (strong, nonatomic) AFURLSessionManager* uploadSessionManager;
@property (strong, nonatomic) AFURLSessionManager* imageSearchSessionManager;

-(void)sendRequestWithAddress:(NSString*)address
                         json:(NSString*)json
                   httpMethod:(NSString*)httpMethod
                  contentType:(NSString*)contentType
                   completion:(void (^)(AFResponse*))completion;

-(void)sendRequestWithAddress:(NSString*)address
                         json:(NSString*)json
                   httpMethod:(NSString*)httpMethod
                  contentType:(NSString*)contentType
                      timeout:(float)timeout
                   completion:(void (^)(AFResponse*))completion;

-(void)sendMediaRequestForMedia:(id<MediaDownloadHelper>)mediaHelper;
-(void)sendUploadMediaRequest:(id<MediaUploadHelper>)mediaHelper;
-(void)sendImageSearchRequest:(NSString*)address
                       apiKey:(NSString*)key
                   completion:(void (^)(AFResponse*))completion;
@end
