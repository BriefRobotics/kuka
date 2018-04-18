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
config "faceGreeting.ashley" [say "Hello Ashley, you are a genius!"]
config "faceGreeting.greg" [say "Hey Greg! Nice seeing you at the show!"]
config "faceGreeting.dominik" [say "Hey Dominik! Hope you are having a good day!"]
config "faceGreeting.bernd" [say "Good morning Bernd! Nice to see you!"]
config "faceGreeting.andy" [say "KneeHow, Andy, great to see you!"]
config "faceGreeting.peter" [say "Hello mr. Mohen, nice to see you!"]
config "faceGreeting.enrique" [say "Hello mr. President. Its an honor to see you!"]
config "faceGreeting.till" [say "Hi boss!"]
config "faceGreeting.angela" [say "Hello mrs. Merkel, hope you like the cooka booth!"]
