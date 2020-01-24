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
        var queueTime = 'timeservicevisualizer';

        channel.assertQueue(queue, {
            durable: false
        });

        console.log(" [*] Waiting for messages in %s. To exit press CTRL+C", queue);
        // let type = 5;
        // let factor = 0;
        // setInterval(()=>{
        //     const testMessage = {
        //         type: TYPES[type % 8 +1], id: type % 24 + 1 , start: type % 24 + 1, end: type % 24 + 2, speed: 0.001 * (type % 8 + 1)
        //         };
        //         io.emit('visualizer', testMessage);
        //     type++
        // }, 10000)

        // setInterval(()=>{
        //     const testMessage = {
        //         factor: factor % 10 + 1
        //         };
        //         io.emit('timeservicevisualizer', testMessage);
        //         factor++
        // }, 1000)


        channel.consume(queue, function(msg) {
            const message = JSON.parse(msg.content.toString());
            console.log(message.speed)
            message.speed = message.speed / 1000
            io.emit('visualizer', message);
        }, {
            noAck: true
        });

        channel.consume(queueTime, function(msg) {
            io.emit('timeservicevisualizer', JSON.parse(msg.content.toString()));
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