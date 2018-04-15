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
    response.ask("Hello, I'm KukaBot.",  "What should I do?");
};

Kuka.prototype.eventHandlers.onSessionEnded = function (sessionEndedRequest, session) {
    console.log("Kuka onSessionEnded requestId: " + sessionEndedRequest.requestId + ", sessionId: " + session.sessionId);
};

var AWS = require('aws-sdk');
var QUEUE_URL = 'https://sqs.us-west-2.amazonaws.com/613660770529/robot1';
var sqs = new AWS.SQS({region : 'us-west-2'});

function send(message, say) {
    sqs.sendMessage(params, function(err, data) {
        if(err) {
            response.tellWithCard("Exception: " + JSON.stringify(err), "Kuka", "Nothing");
        } else {
            response.tellWithCard(say, "Kuka", "Nothing");
        }
    });
}

Kuka.prototype.intentHandlers = {
    "goto": function (event, context, response) {
        try {
            //var device = event.context.System.device.deviceId;
            var place = event.request.intent.slots.place.value;
            var params = { MessageBody: 'goto "' + place +'" "Arriving at ' + place + '"', QueueUrl: QUEUE_URL };
            sqs.sendMessage(params, function(err, data){
                if(err) {
                    response.tellWithCard("Exception: " + JSON.stringify(err), "Greeter", "Nothing");
                } else {
                    response.tellWithCard("Going to " + place, "Greeter", "Nothing");
                }
            });
        } catch(ex) {
            response.tellWithCard("Exception: " + JSON.stringify(ex), "Greeter", "Nothing");
        }
    },
};

exports.handler = function (event, context) {
    var kuka = new Kuka();
    kuka.execute(event, context);
};