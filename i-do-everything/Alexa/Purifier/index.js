var APP_ID = "amzn1.ask.skill.0e21a07f-3104-4c71-8b1a-0948ee649004";

var AlexaSkill = require('./AlexaSkill');

var Kuka = function () { AlexaSkill.call(this, APP_ID); };

Kuka.prototype = Object.create(AlexaSkill.prototype);
Kuka.prototype.constructor = Kuka;

Kuka.prototype.intentHandlers = {
    "goto": function (event, context, response) {
        var place = event.request.intent.slots.place.value;
        if (place)
        {
            response. send('purifier0', 'goto "' + place +'"', "Going to " + place, response);
        }
        else
        {
            response.tell("I can't find that on my map.", "Unknown Location");
        }
    },
    "summon": function (event, context, response) {
        response.send('purifier0', 'purifier', "Coming with an air purifier!", response);
    },
    "AMAZON.HelpIntent": function (event, content, response) {
        response.tell("You can say things such as, 'Bring me the air purifier' or 'Come clean my air'.");
    },
};

exports.handler = function (event, context) {
    var kuka = new Kuka();
    kuka.execute(event, context);
};