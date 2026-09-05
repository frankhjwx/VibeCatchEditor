#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>

// Each retained player is owned by one managed transport and accessed under its lock.
void *fa_audio_open(const char *path, char *message, int capacity) {
    @autoreleasepool {
        NSError *error = nil;
        AVAudioPlayer *player = [[AVAudioPlayer alloc] initWithContentsOfURL:
            [NSURL fileURLWithPath:[NSString stringWithUTF8String:path]] error:&error];
        if (!player || ![player prepareToPlay]) {
            snprintf(message, capacity, "%s", (error.localizedDescription ?: @"Audio preparation failed").UTF8String);
            return NULL;
        }
        return (__bridge_retained void *)player;
    }
}
void fa_audio_close(void *handle) {
    @autoreleasepool { AVAudioPlayer *player = (__bridge_transfer AVAudioPlayer *)handle; [player stop]; }
}
int fa_audio_play(void *handle) { return [(__bridge AVAudioPlayer *)handle play]; }
void fa_audio_pause(void *handle) { [(__bridge AVAudioPlayer *)handle pause]; }
void fa_audio_seek(void *handle, double seconds) { [(__bridge AVAudioPlayer *)handle setCurrentTime:seconds]; }
double fa_audio_position(void *handle) { return [(__bridge AVAudioPlayer *)handle currentTime]; }
double fa_audio_duration(void *handle) { return [(__bridge AVAudioPlayer *)handle duration]; }
int fa_audio_playing(void *handle) { return [(__bridge AVAudioPlayer *)handle isPlaying]; }
void fa_audio_volume(void *handle, float volume) { [(__bridge AVAudioPlayer *)handle setVolume:volume]; }
