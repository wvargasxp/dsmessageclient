//
//  AFProgressListener.m
//  AFLibrary
//
//  Created by James Nguyen on 3/19/15.
//  Copyright (c) 2015 myrete. All rights reserved.
//

#import "AFProgressListener.h"

@implementation AFProgressListener
@synthesize kvoContext;
@synthesize helper;
@synthesize progress;
@synthesize progressFractionCompleted;

-(NSString*)progressFractionCompleted {
    if (!progressFractionCompleted) {
        progressFractionCompleted = NSStringFromSelector(@selector(fractionCompleted));
    }
    
    return progressFractionCompleted;
}

-(id)initWithContext:(void*)_context withHelper:(id<ProgressHelper>)_helper {
    self = [super init];
    if (self) {
        self.kvoContext = _context;
        self.helper = _helper;
    }
    
    return self;
}

-(void)observeValueForKeyPath:(NSString *)keyPath
                     ofObject:(id)object
                       change:(NSDictionary *)change
                      context:(void *)context {
    if (context == self.kvoContext) {
        NSProgress* theProgress = (NSProgress*)object;
        double fractionCompleted= theProgress.fractionCompleted;
        [self.helper progress:[NSNumber numberWithDouble:fractionCompleted]];
    } else {
        [super observeValueForKeyPath:keyPath ofObject:object change:change context:context];
    }
}

-(void)beginListeningForProgress:(NSProgress*)_progress {
    self.progress = _progress;
    [self.progress addObserver:self forKeyPath:self.progressFractionCompleted options:NSKeyValueObservingOptionNew context:self.kvoContext];
}

-(void)endListeningForProgress {
    [self.progress removeObserver:self forKeyPath:self.progressFractionCompleted context:self.kvoContext];
    self.progress = nil;
}

/*
-(void)dealloc {
    NSLog(@"AFProgressListener: Dealloc Called");
}
*/

@end
