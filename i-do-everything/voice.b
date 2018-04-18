// voice commands to be used by presentor with a microphone
// type: load 'voice.b

phrase "Stop" [say "stopping" stop]

phrase "Go to echo"    [say "going to echo"    goto "echo"]
phrase "Go to beanbag" [say "going to beanbag" goto "beanbag"]
phrase "Go to corner"  [say "going to corner"  goto "corner"]
phrase "Go to charger" [say "going to charger" goto "charger"]

phrase "Find Till"    [find "Looking for Till"]
phrase "Find Greg"    [find "Looking for Greg"]
phrase "Find Dominik" [find "Looking for Dominik"]

phrase "Wander around" [say "wandering" wander ["beanbag" "corner" "charger"]]

speechreco