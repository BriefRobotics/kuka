var APP_ID = "amzn1.ask.skill.b4a11c2a-4bb1-4d86-b105-f715ed8dac79";

var AlexaSkill = require('./AlexaSkill');

var Kuka = function () { AlexaSkill.call(this, APP_ID); };

Kuka.prototype = Object.create(AlexaSkill.prototype);
Kuka.prototype.constructor = Kuka;

Kuka.prototype.intentHandlers = {
    "coffee": function (event, context, response) {
        response.send('occo1', 'coffee', "Coming with coffee!", response);
    },
    "occo": function (event, context, response) {
        response.send('occo1', 'occo', "Bringing oak-oh to take photos!", response);
    },
    "purifier": function (event, context, response) {
        response.send('occo1', 'purifier', "Coming with the air purifier!", response);
    },
    "wellness": function (event, context, response) {
        response.send('occo1', 'wellness', "Bringing the wellness module!", response);
    },
    "AMAZON.HelpIntent": function (event, content, response) {
        response.tell("You can say things such as, 'Bring me coffee', 'Bring the oak-oh to take photos', 'Bring the air purifier' and 'Bring the wellness module'.", "Kuka", "Help");
    },
};

exports.handler = function (event, context) {
    var kuka = new Kuka();
    kuka.execute(event, context);
};