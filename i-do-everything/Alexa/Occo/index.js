var APP_ID = "amzn1.ask.skill.e86a376c-b602-4840-84b8-06634beb30cb";

var AlexaSkill = require('./AlexaSkill');

var Kuka = function () { AlexaSkill.call(this, APP_ID); };

Kuka.prototype = Object.create(AlexaSkill.prototype);
Kuka.prototype.constructor = Kuka;

Kuka.prototype.intentHandlers = {
    "goto": function (event, context, response) {
        var place = event.request.intent.slots.place.value;
        if (place)
        {
            response. send('occo0', 'goto "' + place +'"', "Going to " + place, response);
        }
        else
        {
            response.tell("I can't find that on my map.", "Unknown Location");
        }
    },
    "summon": function (event, context, response) {
        response.send('occo0', 'occo', "Bringing oak-oh to take photos!", response);
    },
    "AMAZON.HelpIntent": function (event, content, response) {
        response.tell("You can say things such as, 'Come take my photo'.", "Kuka", "Help");
    },
};

exports.handler = function (event, context) {
    var kuka = new Kuka();
    kuka.execute(event, context);
};