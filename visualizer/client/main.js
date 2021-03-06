const WIDTH = 900;
const HEIGHT = 700;
const SUPER_ANGLE = 180 + 90;
const AIRPLAN_WIDTH=30;
const AIRPLAN_HEIGHT=30;

const items = [];

var factor = 1;

const TYPES_AUTO = {
  bus:'bus',
  baggage:'baggage',
  followme:'followme',
  catering:'catering',
  deicing:'deicing',
  fueltruck:'fueltruck'
}
const TYPES_AIR = {
  Boeing737:'Boeing 737',
  AirbusA320:'Airbus A320',
  MRJ70:"MRJ 70"
}

const Images = {
  [TYPES_AIR.Boeing737]: new Image(),
  [TYPES_AIR.AirbusA320]: new Image(),
  [TYPES_AIR.MRJ70]: new Image(),
  [TYPES_AUTO.bus]: new Image(),
  [TYPES_AUTO.baggage]: new Image(),
  [TYPES_AUTO.followme]: new Image(),
  [TYPES_AUTO.catering]: new Image(),
  [TYPES_AUTO.deicing]: new Image(),
  [TYPES_AUTO.fueltruck]: new Image(),
}

Images[TYPES_AIR.Boeing737].src = 'image/1.png';
Images[TYPES_AIR.AirbusA320].src = 'image/2.png';
Images[TYPES_AIR.MRJ70].src = 'image/3.png';
Images[TYPES_AUTO.bus].src = 'image/avtobus.png';
Images[TYPES_AUTO.baggage].src = 'image/bagazh.png';
Images[TYPES_AUTO.followme].src = 'image/folloumi.png';
Images[TYPES_AUTO.catering].src = 'image/eda.png';
Images[TYPES_AUTO.deicing].src = 'image/mashinka.png';
Images[TYPES_AUTO.fueltruck].src = 'image/zheltaya.png';

var socket = io();

socket.on('visualizer', function(msg){
    console.log('visualizer', msg)
    if (msg.id){
        const airplane = items.find(el=>el.id === msg.id);
        if (airplane){
            airplane.setAnimation(getPointById(msg.start), getPointById(msg.end), msg.speed);
        } else {
            const newItem = new Item(msg.id, getPointById(msg.start), msg.type);
            newItem.setAnimation(getPointById(msg.start), getPointById(msg.end), msg.speed);
            items.push(newItem);
        }
    } 
  });

  socket.on('timeservicevisualizer', function(msg){
    console.log('timeservicevisualizer', msg)
    factor = msg.factor
  });

var stage = new Konva.Stage({
    container: 'container',
    width: WIDTH,
    height: HEIGHT
});

requestAnimationFrame(function animate(time) {
      items.forEach(item => item.animate(time));
      requestAnimationFrame(animate);
  });

var layer = new Konva.Layer();
var laverText = new Konva.Layer();
var autoLayer = new Konva.Layer();
var garageLayer = new Konva.Layer();
const points = getPoints(WIDTH, HEIGHT);

setPoints(autoLayer);
setTexture();

stage.add(laverText);

stage.add(autoLayer);

stage.add(layer);

stage.add(garageLayer);


  class Item {
      constructor(id, startPoint, type){
          this.id = id;
          this.startPoint = startPoint;
          this.curentPoint = {
            x: startPoint.x,
            y: startPoint.y
          }
          this.type = type;
          const image = Item.getImage(type);
          this.text = new Konva.Text({
            x: startPoint.x,
            y: startPoint.y - 20,
            text: id,
            fontSize: 12,
            fill: 'black'
          });

          this.image = new Konva.Image({
            x: startPoint.x,
            y: startPoint.y,
            image,
            offsetY: AIRPLAN_HEIGHT/2,
            offsetX: AIRPLAN_WIDTH/2,
            width: AIRPLAN_WIDTH,
            height: AIRPLAN_HEIGHT,
          });
          if (Object.values(TYPES_AUTO).includes(type)){
            autoLayer.add(this.image);
            autoLayer.add(this.text);
            autoLayer.batchDraw();
          } else{
            layer.add(this.image);
            layer.add(this.text);
            layer.batchDraw();
          }
      }
      setAnimation(startPoint,endPoint, speed){
          this.image.rotation(getAngle(startPoint, endPoint))
          this.startPoint = startPoint;
          this.curentPoint = {
            x: startPoint.x,
            y: startPoint.y
          }
          this.endPoint = endPoint;
          this.currentTime =  performance.now();
          this.speed = speed;
      }
      animate(thisTime){
          if (this.endPoint && this.speed !== undefined && this.currentTime){
              if (this.speed === 0){
                this.image.x(this.endPoint.x)
                this.image.y(this.endPoint.y)
                this.text.x(this.endPoint.x)
                this.text.y(this.endPoint.y - 20)
                layer.batchDraw();
                autoLayer.batchDraw();
                this.deleteAnimation();
                return;
              }
              if (this.startPoint === this.endPoint){
                this.deleteAnimation();
                return;
              }
            const angle = Math.atan2(this.startPoint.y - this.endPoint.y, this.startPoint.x - this.endPoint.x)
            this.curentPoint.x = this.curentPoint.x - this.speed*factor * (thisTime - this.currentTime)* Math.cos(angle);
            this.curentPoint.y = this.curentPoint.y - this.speed*factor * (thisTime - this.currentTime)* Math.sin(angle);
            this.currentTime = thisTime;

            if (this.endPoint.x > this.startPoint.x && this.curentPoint.x >= this.endPoint.x || this.endPoint.x < this.startPoint.x && this.curentPoint.x <= this.endPoint.x){
              this.curentPoint.x = this.endPoint.x;
            }
            if (this.endPoint.y > this.startPoint.y && this.curentPoint.y >= this.endPoint.y || this.endPoint.y < this.startPoint.y && this.curentPoint.y <= this.endPoint.y){
                this.curentPoint.y = this.endPoint.y;
            }
            if (this.image.x() !== this.curentPoint.x){
              this.image.x(this.curentPoint.x)
              this.text.x(this.curentPoint.x)
              layer.batchDraw();
              autoLayer.batchDraw();

            } 
            if (this.image.y() !== this.curentPoint.y){
              this.image.y(this.curentPoint.y)
              this.text.y(this.curentPoint.y - 20)
              layer.batchDraw();
              autoLayer.batchDraw();
            }

            if (this.curentPoint.x === this.endPoint.x && this.curentPoint.y === this.endPoint.y){
                this.deleteAnimation();
            } 
          }
      }
      deleteAnimation(){
        this.startPoint = this.endPoint;
        this.curentPoint = this.endPoint;
        this.endPoint = undefined;
        this.speed = undefined;
        this.currentTime = undefined;
      }
      static getImage(type){
        return Images[type];
      }
  }



  function setPoints(thisLayer) {
    points.forEach(point => {
        const { x, y } = point;
        const circle = new Konva.Circle({
            x,
            y,
            radius: 5,
            stroke: 'black',
            strokeWidth:2,
            fill: 'white',
        });
        thisLayer.add(circle);
    })
  }

  function setTexture(){
    const terminal = new Image();
    terminal.src = 'image/terminal.png'
    terminal.onload = () => {
      const tex = new Konva.Image({
        x: 300,
        y: 580,
        width: 200,
        height: 130,
        image: terminal,
      });
      garageLayer.add(tex);
      garageLayer.batchDraw();
    }

    const garage =  new Image();
    garage.src = 'image/garazh.png';

    garage.onload = () => {
      const tex = new Konva.Image({
        x: 10,
        y: 80,
        width: 100,
        height: 420,
        image: garage,
      });
      garageLayer.add(tex);
      garageLayer.batchDraw();
    }

    const texture = new Image();
    texture.src = 'image/texture.jpg'
    texture.onload = ()=>{
      const tex = new Konva.Image({
        x: 0,
        y: 0,
        image: texture,
        width: 900,
        height: 700
      });
      laverText.add(tex);
      laverText.batchDraw();
    }

  }

function getPointById(id){
    return points.find((point) => point.id === id)
  }


  const getAngle = (point1, point2) => {
    const angle = Math.atan2(point1.y - point2.y, point1.x - point2.x) * 180 / Math.PI;
    return angle + SUPER_ANGLE;
}



function randomInteger(min, max) {
    // случайное число от min до (max+1)
    let rand = min + Math.random() * (max + 1 - min);
    return Math.floor(rand);
  }
