var zone = [
// 0123456789A123456789B12
  '   00000000000000      ', //0
  '   11111111111111111111', //1
  '   100000000001000     ', //2
  '   100000000001000     ', //3
  '   10000 0000 1000     ', //4
  '   100000000001110     ', //5
  '   10000000000000      ', //6
  '   10000000000000      ', //7
  '                       '  //8
];

var LINE_COUNT = zone.length
var COLS_COUNT = zone[0].length

var printErr = console.log
var computedMap = zone.map(function (line) {
    return line.split('').map(function(cell) {
        if (cell === ' ') return false
        if (cell === '1') return 0
        if (cell === '0') return 1
    })
})
printErr(zone)
printErr(completeSquare(15,5, computedMap))

function jsprint  (m) { return JSON.stringify(m)}

function completeSquare(x,y,map) {
//    printErr('x: ' + x + '; y: ' + y)
//    printErr(JSON.stringify(map))
    var cellKeysChecked = []
    var cellsToCheck = [{x:x,y:y,score:0,dist:0,box:{top:y,bottom:y,left:x,right:x}}]
    var bestCell = false

    var isBoxAtEnemy = function (box) {
        for( var x = box.left ; x <= box.right ; x++) {
            for (var y = box.top ; y <= box.bottom; y++) {
                if (map[y][x] === false) return true
            }
        }
        return false
    }

    while (cellsToCheck.length) {
        var cell = cellsToCheck.shift()
        //printErr('=== analyse ' + jsprint(cell))
        cellKeysChecked.push(cell.x+';'+cell.y)
        if (false === bestCell || bestCell.score < cell.score) bestCell = cell
        var adj = adjactentes(cell.x, cell.y,COLS_COUNT-1,LINE_COUNT-1)
        adj.filter(function (adjCell) {
            if (cellKeysChecked.indexOf(adjCell.x+';'+adjCell.y) !== -1) return 0 //already analyzed
            if (map[adjCell.y][adjCell.x] !== 0) return 0 //not reached
            return 1
        }).forEach(function (adjCell) {
            adjCell.dist = manhattan(x,y,adjCell.x,adjCell.y)
            adjCell.box = {
                top:Math.min(cell.box.top, adjCell.y),
                bottom:Math.max(cell.box.bottom, adjCell.y),
                left:Math.min(cell.box.left, adjCell.x),
                right:Math.max(cell.box.right, adjCell.x)
            }
            if (isBoxAtEnemy(adjCell.box)) {
                //printErr('Enemy already on the box :(')
                return
            }
            adjCell.area = Math.abs(adjCell.box.left - adjCell.box.right + 1) * Math.abs(adjCell.box.top - adjCell.box.bottom + 1)
            adjCell.score = adjCell.area / adjCell.dist
            //printErr('-- analyse adj cell : ' + jsprint(adjCell))
            cellsToCheck.push(adjCell)
        })
    }

    return bestCell
}

function adjactentes(x,y,xmax,ymax) {
    if ( typeof adjactentes.cache == 'undefined' ) adjactentes.cache = []
    var cacheKey = [x,y,xmax,ymax].join(';')
    if (cacheKey in adjactentes.cache) return adjactentes.cache[cacheKey]
    var adj = []
    x > 0    && adj.push({x:x-1,y:y})
    y < ymax && adj.push({x:x,y:y+1})
    y > 0    && adj.push({x:x,y:y-1})
    x < xmax && adj.push({x:x+1,y:y})
    return adjactentes.cache[cacheKey] = adj
}

function manhattan(xa,ya,xb,yb) {
    if ( typeof manhattan.cache == 'undefined' ) manhattan.cache = []
    var cacheKey = [xa,ya,xb,yb].join(';')
    if (cacheKey in manhattan.cache) return manhattan.cache[cacheKey]
    return manhattan.cache[cacheKey] = Math.abs(xa-xb) + Math.abs(yb-ya)
}