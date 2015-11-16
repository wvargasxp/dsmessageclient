//
//  AFProgressListener.h
//  AFLibrary
//
//  Created by James Nguyen on 3/19/15.
//  Copyright (c) 2015 myrete. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AFProtocols.h"

@interface AFProgressListener : NSObject
@property (nonatomic) void* kvoContext;
@property (strong, nonatomic) id<ProgressHelper> helper;
@property (strong, nonatomic) NSProgress* progress;
@property (strong, nonatomic) NSString* progressFractionCompleted;

-(id)initWithContext:(void*)_context withHelper:(id<ProgressHelper>)_helper;
-(void)endListeningForProgress;
-(void)beginListeningForProgress:(NSProgress*)_progress;
@end
