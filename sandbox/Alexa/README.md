# Alexa Skill

This is a [custom skill](https://developer.amazon.com/docs/custom-skills/steps-to-build-a-custom-skill.html) for the Amazon Echo.

Driven by [these intents](https://developer.amazon.com/alexa/console/ask/build/custom/amzn1.ask.skill.7153141f-eb07-4394-9548-d9c6fdf7d2d8/development/en_US/json-editor) ([checked in here](./intents/intents.json)) and by [this lambda](https://console.aws.amazon.com/lambda/home?region=us-east-1#/functions/KukaBot?tab=graphA) with code in [/lambda](./lambda). Results are pushed to a [queue here](https://console.aws.amazon.com/sqs/home?region=us-east-1#queue-browser:selected=https://sqs.us-east-1.amazonaws.com/660181231855/KukaBot;prefix=) from where they're read by [the app](Program.cs).
