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
    console.log(msg)
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

var stage = new Konva.Stage({
    container: 'container',
    width: WIDTH,
    height: HEIGHT
});
requestAnimationFrame(function animate(time) {
        items.forEach(item=>item.animate(time));

    requestAnimationFrame(animate);
    stage.batchDraw()
  });

var layer = new Konva.Layer();
var laverText = new Konva.Layer();
var autoLayer = new Konva.Layer();
const points = getPoints(WIDTH, HEIGHT);

setLines(layer);
setPoints(layer);

const texture = new Image();
texture.src = 'image/texture.jpg'
texture.onload = () => {
  var tex = new Konva.Image({
    x: 0,
    y: 0,
    image: texture,
    width: 900,
    height: 700
  });

  laverText.add(tex);
  laverText.batchDraw();

}


stage.add(laverText);

stage.add(layer);

stage.add(autoLayer);



  class Item {
      constructor(id, startPoint, type){
          this.id = id;
          this.startPoint = startPoint;
          this.type = type;
          const image = Item.getImage(type);
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
          } else{
            layer.add(this.image);
          }
          stage.batchDraw()
      }
      setAnimation(startPoint,endPoint, speed){
          this.image.rotation(getAngle(startPoint, endPoint))
          this.startPoint = startPoint;
          this.endPoint = endPoint;
          this.startAnimateTime = performance.now();
          this.speed = speed;
      }
      animate(thisTime){
          if (this.endPoint && this.speed && this.startAnimateTime){
              if (this.startPoint === this.endPoint){
                this.deleteAnimation();
                return;
              }
            const angle = Math.atan2(this.startPoint.y - this.endPoint.y, this.startPoint.x - this.endPoint.x)
            

            let newX = Math.round(this.startPoint.x - this.speed*factor * (thisTime - this.startAnimateTime)* Math.cos(angle));
            let newY = Math.round(this.startPoint.y - this.speed*factor * (thisTime - this.startAnimateTime)* Math.sin(angle));
            if (this.endPoint.x > this.startPoint.x && newX >= this.endPoint.x || this.endPoint.x < this.startPoint.x && newX <= this.endPoint.x){
                newX = this.endPoint.x;
            }
            if (this.endPoint.y > this.startPoint.y && newY >= this.endPoint.y || this.endPoint.y < this.startPoint.y && newY <= this.endPoint.y){
                newY = this.endPoint.y;
            }
            if (newX == this.endPoint.x && newY == this.endPoint.y){
                this.deleteAnimation();
            } else {
                this.image.x(newX)
                this.image.y(newY)
            }
          }
      }
      deleteAnimation(){
        this.startPoint = this.endPoint;
        this.endPoint = undefined;
        this.speed = undefined;
        this.startAnimateTime = undefined;
      }
      static getImage(type){
        return Images[type];
      }
  }


  function setLines (thisLayer){
    lines.forEach((line) => {
        const pointsCords = line.map((id) => {
            return points.find(el => el.id === id);
        }).reduce((accum, el) => {
            accum.push(el.x, el.y)
            return accum;
        }, []);
    
        var road = new Konva.Line({
            points: pointsCords,
            stroke: 'lightgray',
            strokeWidth: 10,
            lineCap: 'round',
            lineJoin: 'round'
        });
        thisLayer.add(road);
    })
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
  function setTexture(thisLayer){
    const texture = new Image();
    texture.src = 'image/texture.jpg'

    texture.onload = ()=>{
      var tex = new Konva.Image({
        x: 0,
        y: 0,
        image: texture,
        width: 900,
        height: 700
      });
      thisLayer.add(tex);
      thisLayer.batchDraw();
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