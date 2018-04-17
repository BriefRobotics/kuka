var APP_ID = "amzn1.ask.skill.6a2a2255-8690-4aef-a1f6-8671a2ed8e77";

var AlexaSkill = require('./AlexaSkill');

var Kuka = function () { AlexaSkill.call(this, APP_ID); };

Kuka.prototype = Object.create(AlexaSkill.prototype);
Kuka.prototype.constructor = Kuka;

Kuka.prototype.intentHandlers = {
    "goto": function (event, context, response) {
        var place = event.request.intent.slots.place.value;
        if (place)
        {
            response. send('occo1', 'goto "' + place +'"', "Going to " + place, response);
        }
        else
        {
            response.tell("I don't know where " + place + " is.", "Unknown Location");
        }
    },
    "summon": function (event, context, response) {
        response.send('occo1', 'occo', "Bringing oak-oh to take photos!", response);
    },
    "AMAZON.HelpIntent": function (event, content, response) {
        response.tell("You can say things such as, 'Come take my photo'.", "Kuka", "Help");
    },
};

exports.handler = function (event, context) {
    var kuka = new Kuka();
    kuka.execute(event, context);
};