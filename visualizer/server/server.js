const express = require('express')
const app = express()
const port = 3000

const TYPES = [
        'bus',
        'baggage',
        'followme',
        'catering',
        'deicing',
        'fueltruck',
        'Boeing 737',
        'Airbus A320',
        'MRJ 70'
];

var http = require('http').createServer(app);
var io = require('socket.io')(http);

app.use(express.static(`../client`))


io.on('connection', function(socket){
  console.log('a user connected');
});

http.listen(port, function(){
  console.log('listening on *:3000');
});

var amqp = require('amqplib/callback_api');

amqp.connect({
    hostname: 'v174153.hosted-by-vdsina.ru',
    username: 'slava',
    password: '228',
    vhost: '/'
}, function(error0, connection) {
    if (error0) {
        throw error0;
    }
    connection.createChannel(function(error1, channel) {
        if (error1) {
            throw error1;
        }

        var queue = 'visualizer';
  

        channel.assertQueue(queue, {
            durable: false
        });

        console.log(" [*] Waiting for messages in %s. To exit press CTRL+C", queue);
        let type = 0;
        setInterval(()=>{
            const testMessage = JSON.stringify({
                type: TYPES[type++ % 8], id: randomInteger(1,25), start: randomInteger(1,25), end: randomInteger(1,25), speed: 0.1
                });
            channel.sendToQueue(queue, Buffer.from(testMessage))
        }, 1000)

        channel.consume(queue, function(msg) {
            io.emit('visualizer', JSON.parse(msg.content.toString()));
        }, {
            noAck: true
        });
    });
});

function randomInteger(min, max) {
    // случайное число от min до (max+1)
    let rand = min + Math.random() * (max + 1 - min);
    return Math.floor(rand);
  }