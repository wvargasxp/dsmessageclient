//
//  AFLibrary.m
//  AFLibrary
//
//  Created by James Nguyen on 3/16/15.
//  Copyright (c) 2015 myrete. All rights reserved.
//

#import "AFLibrary.h"
#import "AFHTTPRequestOperationManager.h"

@implementation AFLibrary
@synthesize downloadSessionManager;
@synthesize uploadSessionManager;
@synthesize imageSearchSessionManager;

#define REQUEST_TIMEOUT_LONG 300.0
#define REQUEST_TIMEOUT_DEFAULT 60.0
static void * const DownloadMediaContext = (void*)&DownloadMediaContext;
static void * const UploadMediaContext = (void*)&UploadMediaContext;

-(AFURLSessionManager*)downloadSessionManager {
    if (!downloadSessionManager) {
        NSURLSessionConfiguration *configuration = [NSURLSessionConfiguration defaultSessionConfiguration];
        [configuration setTimeoutIntervalForRequest:REQUEST_TIMEOUT_LONG];
        downloadSessionManager = [[AFURLSessionManager alloc] initWithSessionConfiguration:configuration];
    }
    
    return downloadSessionManager;
}

-(AFURLSessionManager*)uploadSessionManager {
    if (!uploadSessionManager) {
        NSURLSessionConfiguration *configuration = [NSURLSessionConfiguration defaultSessionConfiguration];
        [configuration setTimeoutIntervalForRequest:REQUEST_TIMEOUT_LONG];
        uploadSessionManager = [[AFURLSessionManager alloc] initWithSessionConfiguration:configuration];
    }
    
    return uploadSessionManager;
}

-(AFURLSessionManager*)imageSearchSessionManager {
    if (!imageSearchSessionManager) {
        NSURLSessionConfiguration *configuration = [NSURLSessionConfiguration defaultSessionConfiguration];
        imageSearchSessionManager = [[AFURLSessionManager alloc] initWithSessionConfiguration:configuration];
    }
    
    return imageSearchSessionManager;
}


-(void)sendRequestWithAddress:(NSString*)address
                         json:(NSString*)json
                   httpMethod:(NSString*)httpMethod
                  contentType:(NSString*)contentType
                   completion:(void (^)(AFResponse*))completion {
    [self sendRequestWithAddress:address
                            json:json
                      httpMethod:httpMethod
                     contentType:contentType
                         timeout:REQUEST_TIMEOUT_DEFAULT
                      completion:completion];
}

-(void)sendRequestWithAddress:(NSString*)address
                         json:(NSString*)json
                   httpMethod:(NSString*)httpMethod
                  contentType:(NSString*)contentType
                      timeout:(float)timeout
                   completion:(void (^)(AFResponse*))completion {
    AFHTTPRequestOperationManager* manager = [AFHTTPRequestOperationManager manager];
    
    
    NSURL* url = [[NSURL alloc] initWithString:address];
    NSMutableURLRequest* request = [[NSMutableURLRequest alloc] initWithURL:url];
    [request setTimeoutInterval:timeout];
    request.HTTPMethod = httpMethod;
    if (contentType) {
        [request addValue:contentType forHTTPHeaderField:@"Content-Type"];
    }
    
    if (json) {
        NSData* data = [json dataUsingEncoding:NSUTF8StringEncoding];
        [request setHTTPBody:data];
    }
    
    [[manager HTTPRequestOperationWithRequest:request
                                      success:^(AFHTTPRequestOperation* operation, id responseObject) {
                                          AFResponse* response = [AFResponse fromResponseString:operation.responseString];
                                          response.headers = operation.response.allHeaderFields;
                                          response.statusCode = operation.response.statusCode;
                                          
                                          completion (response);
                                      }
                                      failure:^(AFHTTPRequestOperation* operation, NSError* error) {
                                          AFResponse* response = [AFResponse fromError:error];
                                          response.statusCode = operation.response.statusCode;
                                          
                                          completion (response);
                                      }] start];
    
}

-(void)sendMediaRequestForMedia:(id<MediaDownloadHelper>)mediaHelper {
    NSString* address = [mediaHelper mediaAddress];
    if (!address) {
        return;
    }
    
    NSURL *url = [NSURL URLWithString:address];
    NSMutableURLRequest* request = [[NSMutableURLRequest alloc] initWithURL:url];
    
    NSProgress* progress = nil;
    AFProgressListener* listener = [[AFProgressListener alloc] initWithContext:DownloadMediaContext withHelper:mediaHelper];
    
    NSURLSessionDownloadTask *downloadTask = [self.downloadSessionManager
                                              downloadTaskWithRequest:request
                                              progress:&progress
                                              destination:^NSURL *(NSURL *targetPath, NSURLResponse *response) {
                                                  
        NSURL *documentsDirectoryURL = [[NSFileManager defaultManager] URLForDirectory:NSDocumentDirectory inDomain:NSUserDomainMask appropriateForURL:nil create:NO error:nil];
        return [documentsDirectoryURL URLByAppendingPathComponent:[response suggestedFilename]];
                                                  
    } completionHandler:^(NSURLResponse *response, NSURL *filePath, NSError *error) {
        if (error != nil) {
            if (filePath != nil) {
                // workaround: need to remove the temp file, else the retry case fails to write properly to the temp file.
                [[NSFileManager defaultManager] removeItemAtURL:filePath error:nil];
            }
        }
        
        [mediaHelper end:error withResponse:response atFilePath:filePath];
        [listener endListeningForProgress];
    }];
    
    [mediaHelper begin];
    [listener beginListeningForProgress:progress];
    [downloadTask resume];
}

-(void)sendUploadMediaRequest:(id<MediaUploadHelper>)mediaHelper {
    
    
    NSString* destinationAddress = mediaHelper.mediaAddress;
    
    NSArray* names = mediaHelper.names;
    NSArray* filenames = mediaHelper.filenames;
    NSArray* filepaths = mediaHelper.filepaths;
    NSArray* mimeTypes = mediaHelper.mimeTypes;
    
    if (!names || names.count == 0) {
        return;
    }
    
    // Prepare a temporary file to store the multipart request prior to sending it to the server due to an alleged
    // bug in NSURLSessionTask.
    NSString* tmpFilename = [NSString stringWithFormat:@"%f", [NSDate timeIntervalSinceReferenceDate]];
    
    NSString* tmpFileUrlPath = [NSTemporaryDirectory() stringByAppendingPathComponent:tmpFilename];
    NSURL* tmpFileUrl = [NSURL fileURLWithPath:tmpFileUrlPath];
    
    NSUInteger count = names.count; // the count should be the same for all of the NSArrays
    
    NSError* errorFormingRequest;
    NSMutableURLRequest *request = [[AFHTTPRequestSerializer serializer] multipartFormRequestWithMethod:@"POST"
                                                                                              URLString:destinationAddress
                                                                                             parameters:nil
                                                                              constructingBodyWithBlock:^(id<AFMultipartFormData> formData) {
        
        for (NSUInteger i = 0; i < count; i++) {
            NSString* name = [names objectAtIndex:i];
            NSString* filePath = [filepaths objectAtIndex:i];
            NSString* fileName = [filenames objectAtIndex:i];
            NSString* mimeType = [mimeTypes objectAtIndex:i];
            
            [formData appendPartWithFileURL:[NSURL fileURLWithPath:filePath]
                                       name:name
                                   fileName:fileName
                                   mimeType:mimeType
                                      error:nil];
        }

    } error:&errorFormingRequest];
    
    if (errorFormingRequest) {
        [mediaHelper endWithError:errorFormingRequest];
        return;
    }
    
    
    
    NSError* fileRemovingError;
    BOOL isDir = NO;
    
    if ([[NSFileManager defaultManager] fileExistsAtPath:tmpFileUrlPath isDirectory:&isDir]) {
        [[NSFileManager defaultManager] removeItemAtPath:tmpFileUrlPath error:&fileRemovingError];
        
        // Not doing anything with error.
        if (fileRemovingError) {}
    }
    
    // Dump multipart request into the temporary file.
    [[AFHTTPRequestSerializer serializer] requestWithMultipartFormRequest:request
                                              writingStreamContentsToFile:tmpFileUrl
                                                        completionHandler:^(NSError *error) {
                                                            if (error) {
                                                                [mediaHelper endWithError:error];
                                                                return;
                                                            }
                                                            
                                                            // Once the multipart form is serialized into a temporary file, we can initialize
                                                            // the actual HTTP request using session manager.

                                                            NSProgress *progress = nil;
                                                            AFProgressListener* listener = [[AFProgressListener alloc] initWithContext:UploadMediaContext withHelper:mediaHelper];
                                                            
                                                            // Here note that we are submitting the initial multipart request. We are, however,
                                                            // forcing the body stream to be read from the temporary file.
                                                            NSURLSessionUploadTask *uploadTask = [self.uploadSessionManager uploadTaskWithRequest:request
                                                                                                                       fromFile:tmpFileUrl
                                                                                                                       progress:&progress
                                                                                                              completionHandler:^(NSURLResponse *response, id responseObject, NSError *error)
                                                                                                  {
                                                                                                      // Cleanup: remove temporary file.
                                                                                                      NSError* removeTmpError;
                                                                                                      [[NSFileManager defaultManager] removeItemAtURL:tmpFileUrl error:&removeTmpError];
                                                                                                      if (removeTmpError) {
                                                                                                          NSLog(@"error removing temp file %@", removeTmpError);
                                                                                                      }
                                                                                                      
                                                                                                      [mediaHelper end:error withResponse:response responseObject:responseObject];
                                                                                                      [listener endListeningForProgress];
                                                                                                  }];
                                                            
                                                            [mediaHelper begin];
                                                            [listener beginListeningForProgress:progress];
                                                            [uploadTask resume];

                                                        }];
}


-(void)sendImageSearchRequest:(NSString*)address
                       apiKey:(NSString*)key
                   completion:(void (^)(AFResponse*))completion {
    AFHTTPRequestOperationManager* manager = [AFHTTPRequestOperationManager manager];
    
    NSURL* url = [[NSURL alloc] initWithString:address];
    NSMutableURLRequest* request = [[NSMutableURLRequest alloc] initWithURL:url];
    request.HTTPMethod = @"GET";
    
    if (key) {
        // This is the Bing API call to get a json list of images.
        [request addValue:key forHTTPHeaderField:@"Authorization"];
        [[manager HTTPRequestOperationWithRequest:request
                                          success:^(AFHTTPRequestOperation* operation, id responseObject) {
                                              AFResponse* response = [AFResponse fromResponseString:operation.responseString];
                                              response.statusCode = operation.response.statusCode;
                                              
                                              completion (response);
                                          }
                                          failure:^(AFHTTPRequestOperation* operation, NSError* error) {
                                              AFResponse* response = [AFResponse fromError:error];
                                              response.statusCode = operation.response.statusCode;
                                              
                                              completion (response);
                                          }] start];
    } else {
        // This is a call to get an image from a request.
        // We need to set the serializer to the AFHTTPResponseSerializer serializer to get back NSData.
        AFHTTPRequestOperation *operation = [[AFHTTPRequestOperation alloc] initWithRequest:request];
        operation.responseSerializer = [AFHTTPResponseSerializer serializer];
        
        [operation setCompletionBlockWithSuccess:^(AFHTTPRequestOperation *operation, NSData *responseObject) {
            AFResponse* response = [AFResponse fromData:responseObject];
            response.statusCode = operation.response.statusCode;
            
            completion (response);
        } failure:^(AFHTTPRequestOperation* operation, NSError* error) {
            AFResponse* response = [AFResponse fromError:error];
            response.statusCode = operation.response.statusCode;
            
            completion (response);
        }];
        
        [operation start];
    }
}







































@end
