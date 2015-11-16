//
//  AFResponse.m
//  AFLibrary
//
//  Created by James Nguyen on 3/16/15.
//  Copyright (c) 2015 myrete. All rights reserved.
//

#import "AFResponse.h"

@implementation AFResponse
@synthesize error;
@synthesize headers;
@synthesize responseString;
@synthesize statusCode;
@synthesize success;
@synthesize data;

-(BOOL)success {
    return !error; // if error is nil, we have a response string
}

+(AFResponse*)fromResponseString:(NSString*)response {
    AFResponse* afResponse = [[AFResponse alloc] init];
    afResponse.headers =  [[NSMutableDictionary alloc] init];
    afResponse.responseString = response;
    return afResponse;
}

+(AFResponse*)fromError:(NSError*)error {
    AFResponse* afResponse = [[AFResponse alloc] init];
    afResponse.headers =  [[NSMutableDictionary alloc] init];
    afResponse.error = error;
    return afResponse;
}

+(AFResponse*)fromData:(NSData *)data {
    AFResponse* afResponse = [[AFResponse alloc] init];
    afResponse.headers =  [[NSMutableDictionary alloc] init];
    afResponse.data = data;
    return afResponse;
}

@end
