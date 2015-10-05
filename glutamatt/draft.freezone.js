var initMap = [
       '000000000000000000',
       '000000000100000000',
       '000000001110000000',
       '000000011000000000',
       '000000000000000000'
    ].map(function(e) {return e.split('').map(function(i){return parseInt(i)})})

var LINE_COUNT = initMap.length
var COLS_COUNT = initMap[0].length

var printErr = console.log

printErr(JSON.stringify(freeZone(8,2, initMap),null,' '))

function freeZone(x,y,map) {
      var key = x+';'+y
      var cell = {x:x,y:y,dist:0}
      var validCells = {}
      var checkingCells = [cell]
      validCells[key] = cell

      while(checkingCells.length) {
            var toCheck = checkingCells.shift()
            adjactentes(toCheck.x,toCheck.y, COLS_COUNT-1,LINE_COUNT-1).forEach(function(cell) {
                  var key = cell.x+';'+cell.y
                  if ((key in validCells) || !map[cell.y][cell.x]) return
                  cell.dist = manhattan(x,y,cell.x,cell.y)
                  checkingCells.push(cell)
                  validCells[key] = cell
            })
      }

      return validCells
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