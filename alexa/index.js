var APP_ID = "amzn1.ask.skill.40a8153d-fb60-41d4-9f71-0c82a7636524";

var AlexaSkill = require('./AlexaSkill');

var Kuka = function () { AlexaSkill.call(this, APP_ID); };

Kuka.prototype = Object.create(AlexaSkill.prototype);
Kuka.prototype.constructor = Kuka;

Kuka.prototype.eventHandlers.onSessionStarted = function (sessionStartedRequest, session) {
    console.log("Kuka onSessionStarted requestId: " + sessionStartedRequest.requestId + ", sessionId: " + session.sessionId);
};

Kuka.prototype.eventHandlers.onLaunch = function (launchRequest, session, response) {
    console.log("Kuka onLaunch requestId: " + launchRequest.requestId + ", sessionId: " + session.sessionId);
    response.ask("Hello, I'm KukaBot.", "What should I do?");
};

Kuka.prototype.eventHandlers.onSessionEnded = function (sessionEndedRequest, session) {
    console.log("Kuka onSessionEnded requestId: " + sessionEndedRequest.requestId + ", sessionId: " + session.sessionId);
};

var AWS = require('aws-sdk');
var QUEUE_URL = 'https://sqs.us-west-2.amazonaws.com/613660770529/dev1';
var sqs = new AWS.SQS({ region: 'us-west-2' });

function send(message, say, response) {
    try {
        var params = { MessageBody: message, QueueUrl: QUEUE_URL };
        sqs.sendMessage(params, function (err, data) {
            if (err) {
                response.tellWithCard("Exception: " + JSON.stringify(err), "Kuka", "Error");
            } else {
                response.tellWithCard(say, "Kuka", say);
            }
        });
    } catch (ex) {
        response.tellWithCard("Exception: " + JSON.stringify(ex), "Kuka", "Error");
    }
}

Kuka.prototype.intentHandlers = {
    "goto": function (event, context, response) {
        //var device = event.context.System.device.deviceId;
        var place = event.request.intent.slots.place.value;
        if (place) {
            send('goto "' + place + '"', "Going to " + place, response);
        }
        else {
            response.tellWithCard("I don't know where that is.", "Unknown Location");
        }
    },
    "coffee": function (event, context, response) {
        send('coffee', "Coming with coffee!", response);
    },
    "purifier": function (event, context, response) {
        send('purifier', "Coming with the air purifier!", response);
    },
    "occo": function (event, context, response) {
        send('occo', "Bringing OCCO to take photos!", response);
    },
};

exports.handler = function (event, context) {
    var kuka = new Kuka();
    kuka.execute(event, context);
};