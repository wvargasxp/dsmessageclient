//
//  AFResponse.h
//  AFLibrary
//
//  Created by James Nguyen on 3/16/15.
//  Copyright (c) 2015 myrete. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface AFResponse : NSObject
@property (strong, nonatomic) NSDictionary* headers;
@property (strong, nonatomic) NSString* responseString;
@property (strong, nonatomic) NSError* error;
@property (strong, nonatomic) NSData* data;
@property (nonatomic) long statusCode;
@property (nonatomic) BOOL success;

+(AFResponse*)fromResponseString:(NSString*)response;
+(AFResponse*)fromError:(NSError*)error;
+(AFResponse*)fromData:(NSData*)data;
@end
