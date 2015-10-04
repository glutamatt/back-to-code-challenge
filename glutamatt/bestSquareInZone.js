var zone = [
  ' 00000000000           ',
  ' 00000000000           ',
  ' 00000000000           ',
  ' 00000000000           ',
  ' 00000000000           ',
  '00000000000000000     ',
  '000000000000000000     ',
  '0000000000000 000000   ',
  '00000000000000000000   ',
  '00000000000000000000   ',
  '00000000000000000000   ',
  '0000000 000000000000   ',
  '00000000000000000000   ',
  '00000000000000000000   '
];

var printErr = console.log

var bests = bestSquareInZone(zone.map(function(line) {
    return line.split('').map(function(cell) { return cell == '0' ? 1 : 0 })
}))

printErr(JSON.stringify(bests))
printErr(zone)
zone.forEach(function(line, i){
    if (i < bests.top || i > bests.bottom) printErr(line)
    else {
        out = []
        line.split('').forEach(function(letter, i) {
            if (i > bests.right || i < bests.left) out.push(letter)
            else out.push('X')
        })
        printErr(out.join(''))
    }
})

function bestSquareInZone(zone) {
    var zoneHeight = zone.length
    var zoneWidth = zone[0].length
    var xCenter = Math.floor(zoneWidth/2)
    var yCenter = Math.floor(zoneHeight/2)
    
    var canGoTop = 1
    var canGoDown = 1
    var canGoLeft = 1
    var canGoRight = 1

    var left = xCenter
    var right = xCenter
    var top = yCenter
    var bottom = yCenter

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
