var LINE_COUNT = 20
var COLS_COUNT = 35
var opponentCount = parseInt(readline());
var opponents
var rawMap

function Brain() {

    var x;
    var y;
    var rawMap
    var opponents
    var backInTimesMe = false
    var currentSquare
    var roundCheckPoint = 0
    var squaresByRound = []
    var roundCache = {}
    this.resetRoundCache = function() {roundCache = {}}

    var freeZoneSizeByOpponents = [0,12,6,5]

    this.setPosition = function(px, py){ x = px ; y = py }
    this.setRawMap = function(p) { rawMap = p }
    this.setOpponents = function(p) { opponents = p }

    var getFreeMap = function () {
        if (!('rawMapToFreeMap' in roundCache))
            roundCache.rawMapToFreeMap = rawMapToFreeMap(rawMap)
        return roundCache.rawMapToFreeMap
    }

    var lookForSquare = function() {
        var hotest = hotPoints(getFreeMap(), x, y, opponents, 3) //TODO: deal with distance factor to find best hotpoint
        var closestHot = hotest.sort(function(cellA, cellB) {
            return manhattan(cellA.x,cellA.y,x,y) - manhattan(cellB.x,cellB.y,x,y)
        }).shift()
        if (!closestHot) { currentSquare = false; return}
        var possessionMap = rawMapToPossessionMap(rawMap)
        var bestFreeZone = freeZone(closestHot.x, closestHot.y, possessionMap, freeZoneSizeByOpponents[opponents.length])
        var squareBox = bestSquareInZoneMap(freeZoneToMap(bestFreeZone), closestHot.x, closestHot.y)
        currentSquare = new SmartSquare(rawMapToWonMap(rawMap), squareBox)
    }

    var tryToSaveSquare = function () {
        var bestCell = completeSquare(x,y,rawMapSaveSquareMap(rawMap))
        if (!bestCell) return
        if (bestCell.score < 2.5) return
        currentSquare = new SmartSquare(rawMapToWonMap(rawMap), bestCell.box)
    }

    var backInTime = {
        saveCheckPoint: function(currentRound) {
            if (backInTimesMe) return
            roundCheckPoint = currentRound
        },
        handleEnemyInSquare: function (currentRound) {
            currentSquare = false
            if (backInTimesMe) return
            var backward = currentRound - roundCheckPoint
            if (backward > 25 || backward < 15) return
            backInTimesMe = true
            return 'BACK ' + (currentRound - roundCheckPoint)
        }
    }

    var onBackInTimeEvent = function (gameRound) {
        currentSquare = squaresByRound[gameRound]
        squaresByRound = squaresByRound.slice(0, gameRound+1)
    }

    var lookForFreeUntilPoint = function(xTo, yTo) {
        if (xTo == x || y == yTo) return {x:xTo, y:yTo}
        var closestFreeCell = false
        getFreeMap().forEach(function (line, yCell) {
            if (yCell < Math.min(yTo, y) || yCell > Math.max(y, yTo)) return
            line.forEach(function(cell, xCell) {
                if (!cell) return
                if (xCell < Math.min(xTo, x) || xCell > Math.max(x, xTo)) return
                var dist = manhattan(xCell, yCell, x, y)
                if (false === closestFreeCell || closestFreeCell.dist > dist)
                    closestFreeCell = {x:xCell, y:yCell, dist:dist}
            })
        })
        if (false === closestFreeCell) closestFreeCell = {x:xTo, y:yTo}
        return {x:closestFreeCell.x, y:closestFreeCell.y}
    }

    this.whereToGo = function(currentRound) {
        if (gameRound < squaresByRound.length ) onBackInTimeEvent(gameRound)
        if (currentSquare && isEnemyInSquare(currentSquare.square, rawMapToPossessionMap(rawMap))) {
            var enemyHandling = backInTime.handleEnemyInSquare(currentRound)
            if (enemyHandling) return enemyHandling
            tryToSaveSquare()
        }

        currentSquare || lookForSquare()
        if (!currentSquare) return x + ' ' + y

        printErr(JSON.stringify(currentSquare))
        var nextPoint = currentSquare.getClosest(x, y, rawMapToWonMap(rawMap))
        var retry = 5
        while(!nextPoint && retry--) {
            backInTime.saveCheckPoint(currentRound)
            lookForSquare()
            nextPoint = currentSquare ? currentSquare.getClosest(x, y, rawMapToWonMap(rawMap)) : null
        }

        if (!nextPoint) return (x + ' ' + y)
        var nextDestination = lookForFreeUntilPoint(nextPoint.x, nextPoint.y)
        squaresByRound[gameRound] = currentSquare
        return (nextDestination.x + ' ' + nextDestination.y)
    }
}

function SmartSquare(map, box) {
    this.square = box
    var cells = []

    this.initCells = function (map) {
        cells = []
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
    }

    this.initCells(map)
    this.remainingCells = function() {return cells}
    this.getClosest = function (x,y, map) {
        this.initCells(map)
        var closest = false
        var nbCells = cells.length
        for (var i = 0 ; i < nbCells ; i++) {
            var cell = cells.shift()
            cell.dist = manhattan(cell.x, cell.y, x, y)
            if (0 === cell.dist) continue
            if (false === closest || closest.dist > cell.dist) closest = cell
            cells.push(cell)
        }
        return closest
    }
}

var brain = new Brain()

while (true) {
    brain.resetRoundCache()
    var gameRound = parseInt(readline());
    var inputs = readline().split(' ');
    var x = parseInt(inputs[0])
    var y = parseInt(inputs[1])
    var backInTimeLeft = parseInt(inputs[2])

    opponents = []
    for (var i = 0; i < opponentCount; i++) {
        var inputs = readline().split(' ');
        var opponentX = parseInt(inputs[0]); // X position of the opponent
        var opponentY = parseInt(inputs[1]); // Y position of the opponent
        var opponentBackInTimeLeft = parseInt(inputs[2]); // Remaining back in time of the opponent
        opponents.push({
            x:opponentX, y:opponentY, backInTimeLeft:opponentBackInTimeLeft
        })
    }

    rawMap = []
    for (var i = 0; i < 20; i++) rawMap.push(readline().split(''))

    brain.setPosition(x,y)
    brain.setOpponents(opponents)
    brain.setRawMap(rawMap)

    print(brain.whereToGo(gameRound))
}
function rawMapToPossessionMap(rawMap) {
    return rawMap.map(function(line){return line.map(function(cell){
        if (cell == '.' || cell == '0') return 1
        return 0
    })})
}

function rawMapSaveSquareMap(rawMap) {
    return rawMap.map(function(line){return line.map(function(cell){
        if (cell === '.') return 1
        if (cell === '0') return 0
        return false
    })})
}

function rawMapToWonMap(rawMap) {
    return rawMap.map(function(line){return line.map(function(cell){
        if (cell == '0') return 1
        return 0
    })})
}

function rawMapToFreeMap(rawMap) {
    return rawMap.map(function(line){return line.map(function(cell){
        return cell == '.' ? 1 : 0
    })})
}

function isEnemyInSquare(square, map) {
    for (var y = square.top ; y <= square.bottom ; y++) {
        for (var x = square.left; x <= square.right; x++) {
            if (!map[y][x]) return true
        }
    }
    return false
}

function freeZoneToMap(zone) {
    var map = []
    for (var i = 0 ; i < LINE_COUNT ; i++) {
        var line = []
        for (var j = 0 ; j < COLS_COUNT ; j++) {
            var key = j +';' + i
            line.push((key in zone) ? 1 : 0)
        }
        map.push(line)
    }
    return map
}

function isLineForMe(ax,ay,bx,by,map) {
    for (var y = Math.min(ay,by) ; y <= Math.max(ay,by) ; y++) {
        for (var x = Math.min(ax,bx) ; x <= Math.max(ax,bx) ; x++) {
            if (!map[y][x]) return false
        }
    }

    return true
}

function freeZone(x,y,map, maxHotDist) {
      var key = x+';'+y
      var cell = {x:x,y:y,dist:0}
      var validCells = {}
      var checkingCells = [cell]
      validCells[key] = cell

      while(checkingCells.length) {
            var toCheck = checkingCells.shift()
            adjactentes(toCheck.x,toCheck.y, COLS_COUNT-1,LINE_COUNT-1).forEach(function(cell) {
                var key = cell.x + ';' + cell.y
                if ((key in validCells) || !map[cell.y][cell.x]) return
                if (Math.abs(x-cell.x) > maxHotDist || Math.abs(y-cell.y) > maxHotDist) return
                if (manhattan(x,y,cell.x,cell.y) > (2*maxHotDist)) return
                checkingCells.push(cell)
                validCells[key] = cell
            })
      }

      return validCells
}

function manhattan(xa,ya,xb,yb) {
    if ( typeof manhattan.cache == 'undefined' ) manhattan.cache = []
    var cacheKey = [xa,ya,xb,yb].join(';')
    if (cacheKey in manhattan.cache) return manhattan.cache[cacheKey]
    return manhattan.cache[cacheKey] = Math.abs(xa-xb) + Math.abs(yb-ya)
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

function hotPoints(map, x, y, opponents, distanceFactor) {
    var model = {}
    map.forEach(function(line, iline) {
        line.forEach(function(col, icol) {
            var cell = {x:icol, y:iline, total: col, round:col}
            model[cell.x+';'+cell.y] = cell
        })
    })

    for (var iteration = 0 ; iteration < 4 ; iteration++) {
        for (var key in model) {
            var cell = model[key]
            if (!cell.total) continue
            adjactentes(cell.x,cell.y, COLS_COUNT-1,LINE_COUNT-1).forEach(function(adjCell) {
                cell.round += model[adjCell.x+';'+adjCell.y].total
            })
        }
        
        for (var key in model) {
            var cell = model[key]
            if (cell.total) cell.total = cell.round
        }
    }

    for (var key in model) { // decrease the score if long distance
        var cell = model[key]
        if (!cell.total) {
            delete model[key]
            continue
        }
        cell.total -= manhattan(cell.x, cell.y, x, y) * distanceFactor
        if (opponents.length == 1) continue
        cell.total += opponents
            .map(function (opp) { return manhattan(opp.x, opp.y, cell.x, cell.y)})
            .reduce(function(prev, curr) {return prev + curr}, 0) * distanceFactor / opponents.length
    }

    var maxCells = []
    var maxScore = Infinity * -1
    for (var key in model) {
        var cell = model[key]
        if (cell.total < maxScore) continue
        if (cell.total > maxScore) {
            maxCells = [cell]
            maxScore = cell.total
        }
        else maxCells.push(cell)
    }
    
    return maxCells
}

function bestSquareInZoneMap(zone, xStart, yStart) {
    var zoneHeight = zone.length
    var zoneWidth = zone[0].length

    var canGoTop = yStart > 0
    var canGoDown = yStart < zoneHeight-1
    var canGoLeft = xStart > 0
    var canGoRight = xStart < zoneWidth-1

    var left = xStart
    var right = left
    var top = yStart
    var bottom = top

    while (canGoRight || canGoTop || canGoLeft || canGoRight) {
        //try right
        for (var i = top ; canGoRight && i <= bottom ; i++) {
            if (!zone[i][right+1]) canGoRight = false
        }
        canGoRight && right++
        if (right + 1 == zoneWidth) canGoRight = false

        // try left
        for (var i = top ; canGoLeft && i <= bottom ; i++) {
            if (!zone[i][left-1]) canGoLeft = false
        }
        canGoLeft && left--
        if (left == 0) canGoLeft = false

        // try top
        for (var i = left ; canGoTop && i <= right ; i++) {
            if (!zone[top-1][i]) canGoTop = false
        }
        canGoTop && top--
        if (0 == top) canGoTop = false

        // try bottom
        for (var i = left ; canGoDown && i <= right ; i++) {
            if (!zone[bottom+1][i]) canGoDown = false
        }
        canGoDown && bottom++
        if (bottom + 1 == zoneHeight) canGoDown = false
    }

    return {left:left, right:right, top:top, bottom:bottom}
}

function completeSquare(x,y,map) {
    /**
     * if (enemy) return false
     if (possessed) return 0
     if (free) return 1
     */
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

    var countFreeCellInBox = function (box) {
        var c = 0
        for( var x = box.left ; x <= box.right ; x++) {
            for (var y = box.top ; y <= box.bottom; y++) {
                if (map[y][x] === 1) c++
            }
        }
        return c
    }

    var limit = 100 //avoid timeouts
    while (cellsToCheck.length && limit--) {
        var cell = cellsToCheck.shift()
        cellKeysChecked.push(cell.x+';'+cell.y)
        if (false === bestCell || bestCell.score < cell.score) bestCell = cell
        var adj = adjactentes(cell.x, cell.y,COLS_COUNT-1,LINE_COUNT-1)
        adj.filter(function (adjCell) {
            if (cellKeysChecked.indexOf(adjCell.x+';'+adjCell.y) !== -1) return 0 //already analyzed
            if (map[adjCell.y][adjCell.x] !== 0) return 0 //not reached
            return 1
        }).forEach(function (adjCell) {
            adjCell.dist = manhattan(x,y,adjCell.x,adjCell.y)
            var box = {
                top:Math.min(cell.box.top, adjCell.y),
                bottom:Math.max(cell.box.bottom, adjCell.y),
                left:Math.min(cell.box.left, adjCell.x),
                right:Math.max(cell.box.right, adjCell.x)
            }
            if (isBoxAtEnemy(box)) return
            var freeCellCount = countFreeCellInBox(box)
            if (adjCell.x != x && adjCell.y != y && !freeCellCount) return
            adjCell.score = freeCellCount / adjCell.dist
            adjCell.box = box
            cellsToCheck.push(adjCell)
        })
    }

    return bestCell
}
