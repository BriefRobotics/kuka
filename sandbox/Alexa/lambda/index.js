var APP_ID = "amzn1.ask.skill.7153141f-eb07-4394-9548-d9c6fdf7d2d8";

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
var QUEUE_URL = 'https://sqs.us-east-1.amazonaws.com/660181231855/KukaBot';
var sqs = new AWS.SQS({region : 'us-east-1'});

Kuka.prototype.intentHandlers = {
    "come_here": function (event, context, response) {
        try {
            var params = { MessageBody: JSON.stringify({ event: event, context: context, response: response }), QueueUrl: QUEUE_URL };
            sqs.sendMessage(params, function(err, data){
                if(err) {
                    response.tellWithCard("Exception: " + JSON.stringify(err), "Greeter", "Nothing");
                } else {
                    response.tellWithCard("On my way...", "Greeter", "Nothing");
                }
            });
        } catch(ex) {
            response.tellWithCard("Exception: " + JSON.stringify(ex), "Greeter", "Nothing");
        }
    },
    "goto": function (event, context, response) {
        try {
            var params = { MessageBody: JSON.stringify({ event: event, context: context, response: response }), QueueUrl: QUEUE_URL };
            sqs.sendMessage(params, function(err, data){
                if(err) {
                    response.tellWithCard("Exception: " + JSON.stringify(err), "Greeter", "Nothing");
                } else {
                    response.tellWithCard("Going to...", "Greeter", "Nothing");
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