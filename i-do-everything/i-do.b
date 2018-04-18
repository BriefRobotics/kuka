faces-watch "C:/OCCO/LiveStreams" true

def 'goto [nav stop]
def 'wander [tour stop]

def 'occo [say "Bringing oak-oh to take photos" goto "echo"]
def 'coffee [say "Delivering coffee" goto "echo"]
def 'purifier [say "Delivering the air purifier" goto "echo"]
def 'wellness [say "Delivering the wellness module" goto "echo"]

def 'find [wander ["beanbag" "corner" "charger"] say]

// uncomment this for voice control
load 'voice.b