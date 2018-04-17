var APP_ID = "amzn1.ask.skill.4b598ee3-29fb-40f6-9052-d637ab033f7f";

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
        response.send('occo1', 'purifier', "Coming with an air purifier!", response);
    },
    "AMAZON.HelpIntent": function (event, content, response) {
        response.tell("You can say things such as, 'Bring me the air purifier' or 'Come clean my air'.");
    },
};

exports.handler = function (event, context) {
    var kuka = new Kuka();
    kuka.execute(event, context);
};