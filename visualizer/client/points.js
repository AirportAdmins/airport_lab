const topOffset = 120;
const offset = 60;

var getPoints = (width, height) => {
    const points = [];
    const yGap = Math.floor((height - topOffset - offset)/3);
    const xGap = Math.floor(( width - offset * 2 )/5);

    for (let i = 1; i <= 3; i++){
        const point = {
            id: i ,
            x: offset + (i+1)*xGap,
            y: - topOffset,
        };
        points.push(point)
    }

    for (let i = 4; i <= 9; i++){
         const point = {
            id: i ,
            x: offset + xGap * (i-4),
            y: topOffset,
        };

        points.push(point)
    }
    points.push(
        {
        id: 10 ,
        x: offset,
        y: topOffset+yGap/2,
    },
    {
        id: 11 ,
        x: offset + xGap*4,
        y: topOffset+yGap/2,
    },
    {
        id: 12 ,
        x: offset + xGap*5,
        y: topOffset+yGap/2,
    }
    )

    for (let i = 13; i <= 15; i++){
        const point = {
           id: i ,
           x: offset + xGap * (i-12),
           y: topOffset+yGap,
       };

       points.push(point)
   }

   points.push(
    {
    id: 16 ,
    x: offset,
    y: topOffset+yGap*1.5,
},
{
    id: 17 ,
    x: offset + xGap*4,
    y: topOffset+yGap*1.5,
},
{
    id: 18 ,
    x: offset + xGap*5,
    y: topOffset+yGap*1.5,
}
)

for (let i = 19; i <= 24; i++){
    const point = {
       id: i ,
       x: offset + xGap * (i-19),
       y: topOffset+ yGap*2,
   };

   points.push(point)
}

points.push({
    id: 25 ,
    x: offset + xGap*2,
    y: topOffset+yGap*3,
})
    return points;
}



var lines = [
    [1,5],
    [2,6],
    [3,7],
    [4,5],
    [4,10],
    [5,6],
    [5,13],
    [6,7],
    [6,14],
    [7,8],
    [7,15],
    [8,9],
    [8, 11],
    [9, 12],
    [10,13],
    [10,16],
    [13,14],
    [13,16],
    [13,20],
    [14,15],
    [14,21],
    [15,11],
    [11,12],
    [11,17],
    [12,18],
    [16,19],
    [15,17],
    [15,22],
    [17,18],
    [18,24],
    [19,25],
    [20,25],
    [21,25],
    [22,25],
    [23,25],
    [24,25],
    [23,24],
    [22,23],
    [21,22],
    [20,21],
    [19,20],
    [17,23],
    
];
