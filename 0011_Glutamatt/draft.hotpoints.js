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

var LINE_COUNT = initMap.length
var COLS_COUNT = initMap[0].length

function hotPoint(map) {
    var model = {}
    map.forEach(function(line, iline) {
        line.forEach(function(col, icol) {
            var cell = {x:icol, y:iline, total: col, round:col}
            model[cell.x+';'+cell.y] = cell
        })
    })

    for (var iteration = 0 ; iteration < 3 ; iteration++) {
        for (var key in model) {
            var cell = model[key]
            if (!cell.total) continue
            adjactentes(cell.x,cell.y, COLS_COUNT-1,LINE_COUNT-1).forEach(function(adjCell) {
                cell.round += model[adjCell.x+';'+adjCell.y].total
            })
        }
        
        for (var key in model) {
            var cell = model[key]
            if (!cell.total) continue
            cell.total = cell.round
        }
    }

    var maxCell = false
    for (var key in model) {
        var cell = model[key]
        if (!cell.total) continue
        if (!maxCell || cell.total > maxCell.total) maxCell = cell
    }

    //print
    printErr(maxCell)
    for (var i = 0 ; i < LINE_COUNT ; i++) {
        var out = []
        for (var j = 0 ; j < COLS_COUNT ; j++) {
            out.push(model[j+';'+i].total)
        }
        printErr(out.join(";\t"))
    }
}

function printErr(message) {
    console.log(message)
}

hotPoint(initMap)

function adjactentes(x,y,xmax,ymax) {
    if ( typeof adjactentes.cache == 'undefined' ) adjactentes.cache = []
    var cacheKey = [x,y,xmax,ymax].join(';')
    if (cacheKey in adjactentes.cache) return adjactentes.cache[cacheKey]
    var adj = []
    if(x > 0) {
        adj.push({x:x-1,y:y})
        y < ymax && adj.push({x:x-1,y:y+1})
        y > 0  && adj.push({x:x-1,y:y-1})
    }
    y < ymax && adj.push({x:x,y:y+1})
    y > 0  && adj.push({x:x,y:y-1})
    if (x < xmax) {
        adj.push({x:x+1,y:y})
        y < ymax && adj.push({x:x+1,y:y+1})
        y > 0  && adj.push({x:x+1,y:y-1})
    }

    return adjactentes.cache[cacheKey] = adj
}