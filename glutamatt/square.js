var initMap = [
    '00011000000000100000001100000000010000',
    '00001111000000000100001100000000010000',
    '00111111111110000000001100000000010000',
    '01110110001111110001111111110000010000',
    '00110011111100011111111111110000010000',
    '01110110001111111111101111111100010000',
    '00111111111110000000001111110000010000',
    '01110111111111110000001100000000010000',
    '00110011111100000000001100000000010000',
    '01110111111111110000001100000000010000',
    '00110011111100000000001100000000010000',
    '00111000010010000000001100000000010000',
    '00000000001110010000001100000000010000'
].map(function(e) {return e.split('').map(function(i){return parseInt(i)})})

var printErr = function(e) { console.log(e) }

var square = new SmartSquare(initMap, {top:4,left:4,right:30,bottom:10})
square.remainingCells().forEach(function (cell) {
    initMap[cell.y][cell.x] = 'X'
})
initMap.forEach(function (line) {
    printErr(line.join(''))
})

printErr(square.getClosest(7,5))
printErr(square.getClosest(7,6))
printErr(square.getClosest(7,7))
printErr(square.getClosest(8,5))
printErr(square.getClosest(9,5))

function SmartSquare(map, box) {
    this.square = box
    var cells = []

    for (var i = box.left ; i <= box.right ; i++) { // top line
        if (map[box.top][i]) continue
        cells.push({x:i,y:box.top})
    }

    if (box.top != box.bottom) {
        for (var i = box.left ; i <= box.right ; i++) { //bottom line
            if (map[box.bottom][i]) continue
            cells.push({x:i,y:box.bottom})
        }
    }

    for (var i = box.top+1 ; i <= box.bottom-1 ; i++) { //left line
        if (map[i][box.left]) continue
        cells.push({x:box.left,y:i})
    }

    if (box.left != box.right) {
        for (var i = box.top+1 ; i <= box.bottom-1 ; i++) { //right line
            if (map[i][box.right]) continue
            cells.push({x:box.right,y:i})
        }
    }

    this.remainingCells = function() {return cells}
    this.getClosest = function (x,y) {
        var closest = false
        for (var i = 0 ; i < cells.length ; i++) {
            var cell = cells.shift()
            cell.dist = manhattan(cell.x, cell.y, x, y)
            if (0 === cell.dist) continue
            if (false === closest || closest.dist > cell.dist) closest = cell
            cells.push(cell)

        }
        return closest

        /*
        var closest = false

        cells.forEach(function (cell, i) {
            if (cell.x == x && cell.y == y) return cells.splice(i, 1)
            cell.dist = manhattan(cell.x, cell.y, x, y)
            if (false === closest || closest.dist > cell.dist) closest = cell
        })
        return closest
         */
    }
}

function manhattan(xa,ya,xb,yb) {
    if ( typeof manhattan.cache == 'undefined' ) manhattan.cache = []
    var cacheKey = [xa,ya,xb,yb].join(';')
    if (cacheKey in manhattan.cache) return manhattan.cache[cacheKey]
    return manhattan.cache[cacheKey] = Math.abs(xa-xb) + Math.abs(yb-ya)
}