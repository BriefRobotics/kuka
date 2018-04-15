// configuration values

config "relayRocUri" "https://kuka.savioke.com/api/v2/tasks"
config "relayApiToken" "DTzDXcoLEycTgihyh"

// message queues on hich to receive commands
config "amazonSqsUri" "https://sqs.us-west-2.amazonaws.com/613660770529/robot0"
config "ashleyf-dell.amazonSqsUri" "https://sqs.us-west-2.amazonaws.com/613660770529/robot0"
config "amazonSqsRegion" "us-west-2"
config "amazonSqsKey" "AKIAIEBVVLIN6RTQXCRQ"
config "amazonSqsSecret" "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"

// face reco - cognitive services
config "azureCognitiveKey" "05e23a6b8a494ac3ba2a3d49053ccf48"
config "azureCognitiveUri" "https://westcentralus.api.cognitive.microsoft.com"

config "faceDirectory" "../../../faces"
config "faceConfidenceThreshold" 0.5
config "faceRepeatSeconds" 30

config "faceGreeting.default" [say "Hi {0}"]
config "faceGreeting.ashley" [say "Hello Ashley, you freaking wacko!"]
config "faceGreeting.greg" [say "Hey Greg! You sir, are a nut!"]
