var APP_ID = "amzn1.ask.skill.669ff46d-32ab-447a-a36f-dcd79d8fdb17";

var AlexaSkill = require('./AlexaSkill');

var Kuka = function () { AlexaSkill.call(this, APP_ID); };

Kuka.prototype = Object.create(AlexaSkill.prototype);
Kuka.prototype.constructor = Kuka;

Kuka.prototype.intentHandlers = {
    "goto": function (event, context, response) {
        var place = event.request.intent.slots.place.value;
        if (place)
        {
            response. send('wellness0', 'goto "' + place +'"', "Going to " + place, response);
        }
        else
        {
            response.tell("I can't find that on my map.", "Unknown Location");
        }
    },
    "summon": function (event, context, response) {
        response.send('wellness0', 'wellness', "Coming with the wellness module!", response);
    },
    "AMAZON.HelpIntent": function (event, content, response) {
        response.tell("You can say things such as, 'Bring the wellness unit'.");
    },
};

exports.handler = function (event, context) {
    var kuka = new Kuka();
    kuka.execute(event, context);
};