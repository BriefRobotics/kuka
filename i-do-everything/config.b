// configuration values

config "relayRocUri" "https://kuka.savioke.com"
config "relayApiToken" "DTzDXcoLEycTgihyh"

// message queues on which to receive commands
config "amazonSqsBaseUri" "https://sqs.us-west-2.amazonaws.com/613660770529"
config "amazonSqsRegion" "us-west-2"
config "amazonSqsKey" "AKIAIEBVVLIN6RTQXCRQ"
config "amazonSqsSecret" "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"

// face reco - cognitive services
config "azureCognitiveKey" "9fe288f421a94b2dbe48181a62b6c216"
config "azureCognitiveUri" "https://westcentralus.api.cognitive.microsoft.com"

config "faceDirectory" "../../../faces"
config "faceConfidenceThreshold" 0.6
config "faceRepeatSeconds" 30

config "faceGreeting.default" [say "Hi {0}"]
config "faceGreeting.ashley" [say "Hello Ashley, you freaking wacko!"]
config "faceGreeting.greg" [say "Hey Greg! You sir, are a nut!"]
config "faceGreeting.dominik" [say "Hey Dominik! You're fired!"]
config "faceGreeting.bernd" [say "Good morning Bernd! Nice to see you!"]
config "faceGreeting.tracey" [say "Hi there Tracey, you cutie!"]
config "faceGreeting.nathan" [say "Hey there Mr. Nathan! Glad you finally made it here. Long trip from Redmond to Iceland to Germany, eh?"]
