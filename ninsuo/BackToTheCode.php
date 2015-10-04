<?php

class Game
{
    protected $debug;

    protected $gameRound;
    protected $backInTimeLeft;

    protected $contexts = [];
    protected $backs = null;

    protected $map = [];
    protected $x;
    protected $y;

    protected $opponentCount;
    protected $opponents = [];

    protected $way       = [];
    protected $rectangle = null;

    protected $scores;
    protected $needed = 0;
    protected $missing = 0;

    public function err($err)
    {
        return file_put_contents("php://stderr", $err, FILE_APPEND);
    }

    public function dist($x1, $y1, $x2, $y2)
    {
        return sqrt(pow($x2 - $x1, 2) + pow($y2 - $y1, 2));
    }

    public function getRounds($x1, $y1, $x2, $y2)
    {
        return abs($x1 - $x2) + abs($y1 - $y2);
    }

    public function scores($map = null)
    {
        if (is_null($map)) {
            $map = $this->map;
        }

        $scores = array('0' => 0, '1' => 0, '2' => 0, '3' => 0, '.' => 0);
        for ($i = 0; ($i < 700); $i++) {
            $scores[$map[$i] == '5' ? $map['0'] : $map[$i]] ++;
        }
        unset($scores['.']);

        return $scores;
    }

    public function ratios($map)
    {
        $scores = $this->scores($map);
        $total  = array_sum($scores);
        foreach ($scores as $player => $score) {
            $ratios[$player] = ($score * 100) / $total;
        }

        return $ratios;
    }

    // ======================================================================
    // Rectangles utilities
    // ======================================================================

    public function rectangleValue($x, $y, $limX, $limY)
    {
        $value = 0;
        for ($i = $x; ($i <= $limX); $i++) {
            for ($j = $y; ($j <= $limY); $j++) {
                if ($this->map[$j * 35 + $i] == '.') {
                    $value++;
                }
            }
        }

        return $value;
    }

    public function getOptimalRectangles($x, $y)
    {
        for ($limX = $x; ($limX < 35); $limX++) {
            if ($this->map[$y * 35 + $limX] != '.' && $this->map[$y * 35 + $limX] != '0') {
                break;
            }
        }
        for ($limY = $y; ($limY < 20); $limY++) {
            if ($this->map[$limY * 35 + $x] != '.' && $this->map[$limY * 35 + $x] != '0') {
                break;
            }
        }

        /*
         * calculating sums taking into account that we always go from top/left
         *
         * .  .  .  0  .  0  0  .  .  .  0  0  0  0  0  0  0  0  0  0  0  0  : 22
         * .  .  .  1  .  0  0  .  .  .  0  0  0  0  0  0  0  0  0  0  0  0  : 3
         * .  .  .  0  .  0  0  .  .  .  0  0  0  0  0  0  0  0  0  0  0  0  : 3
         * .  .  .  0  .  0  0  .  .  .  0  0  0  0  0  0  0  0  0  0  0  0  : 3
         * .  .  .  .  1  0  0  .  .  .  0  0  0  0  0  0  0  0  0  0  0  0  : 3
         * .  .  .  0  0  0  0  .  .  .  0  0  0  0  0  0  0  0  0  0  0  0  : 3
         * .  1  .  0  0  0  0  .  .  .  0  0  0  0  0  0  0  0  0  0  0  0  : 1
         * .  .  .  0  0  0  0  .  .  .  0  0  0  0  0  0  0  0  0  0  0  0  : 1
         */
        $sizeX = [];
        $maxX  = null;
        for ($j = $y; ($j < $limY); $j++) {
            $sizeX[$j] = 0;
            for ($i = $x; ($i < $limX); $i++) {
                if ($this->map[$j * 35 + $i] == '.' || $this->map[$j * 35 + $i] == '0') {
                    $sizeX[$j]++;
                } else {
                    break;
                }
            }
            if ((is_null($maxX)) || ($sizeX[$j] < $maxX)) {
                $maxX = $sizeX[$j];
            }
            if ($sizeX[$j] > $maxX) {
                $sizeX[$j] = $maxX;
            }
        }

        /*
         * now calculating all rectangles found using those data
         *
         * sizeX: 22, 3, 3, 3, 3, 3, 1, 1
         *
         * x:
         * - 22x1           = limX = x + 22 - 1, limY = y + 1 - 1, size 22
         * - 3x5 => 3x6     = limX = x + 3 - 1, limY = y + 6 - 1, size 18
         * - 1x2 => 1x8     = limX = x + 1 - 1, limY = y + 6 - 1, size 8
         */
        $matrix = [];
        foreach ($sizeX as $sx) {
            $matrix[$sx] = isset($matrix[$sx]) ? $matrix[$sx] + 1 : 1;
        }
        $count = 0;
        foreach ($matrix as $sx => $nb) {
            $matrix[$sx] = $nb + $count;
            $count       = $matrix[$sx];
        }
        $results = [];
        foreach ($matrix as $limX => $limY) {
            $results[] = [$x + $limX - 1, $y + $limY - 1];
        }

        if (empty($results)) {
            return null;
        }

        return $results;
    }

    public function getAllRectangles()
    {
        $max        = null;
        $rectangles = [];
        for ($y = 0; ($y < 20); $y++) {
            for ($x = 0; ($x < 35); $x++) {
                if ($this->map[$y * 35 + $x] != '.' && $this->map[$y * 35 + $x] != '0') {
                    continue;
                }
                $newRectangles = $this->getOptimalRectangles($x, $y);
                if (is_null($newRectangles)) {
                    continue;
                }
                foreach ($newRectangles as $newRectangle) {
                    list($limX, $limY) = $newRectangle;
                    $value = $this->rectangleValue($x, $y, $limX, $limY);
                    if ($value == 0) {
                        continue;
                    }

                    if (is_null($max) || $value > $max) {
                        $max = $value;
                    }

                    if ($value < $max * self::MAGIC) {
                        continue ;
                    }

                    $rectangles[] = ['pos' => [[$x, $y], [$limX, $limY]], 'value' => $value];
                }
            }
        }

        return $rectangles;
    }

    public function inRectanglePerimeter($x, $y, $topLeft, $bottomRight)
    {
        if (($x == $topLeft[0] || $x == $bottomRight[0]) && ($y >= $topLeft[1] && $y <= $bottomRight[1])) {
            return true;
        }

        if (($y == $topLeft[1] || $y == $bottomRight[1]) && ($x >= $topLeft[0] && $x <= $bottomRight[0])) {
            return true;
        }

        return false;
    }

    public function isEnemyInside($x, $y, $limX, $limY)
    {
        for ($i = $x; ($i <= $limX); $i++) {
            for ($j = $y; ($j <= $limY); $j++) {
                if ($this->map[$j * 35 + $i] !== '.' && $this->map[$j * 35 + $i] != '0') {
                    return true;
                }
            }
        }

        return false;
    }

    public function cleanExistingRectangles()
    {
        for ($x = 0; ($x < 35); $x++) {
            for ($y = 0; ($y < 20); $y++) {
                if (($this->map[$y * 35 + $x] == '0' || $this->map[$y * 35 + $x] == '5')
                   && ($y == 0 || ($this->map[($y - 1) * 35 + $x] == '0' || $this->map[($y - 1) * 35 + $x] == '5'))
                   && ($x == 34 || ($this->map[$y * 35 + $x + 1] == '0' || $this->map[$y * 35 + $x + 1] == '5'))
                   && ($y == 19 || ($this->map[($y + 1) * 35 + $x] == '0' || $this->map[($y + 1) * 35 + $x] == '5'))
                   && ($x == 0 || ($this->map[$y * 35 + $x - 1] == '0' || $this->map[$y * 35 + $x - 1] == '5'))) {
                    if ((!isset($this->map[($y + 1) * 35 + ($x + 1)]) || $this->map[($y + 1) * 35 + ($x + 1)] == '0' || $this->map[($y + 1) * 35 + ($x + 1)] == '5')
                       && (!isset($this->map[($y + 1) * 35 + ($x - 1)]) || $this->map[($y + 1) * 35 + ($x - 1)] == '0' || $this->map[($y + 1) * 35 + ($x - 1)] == '5')
                       && (!isset($this->map[($y - 1) * 35 + ($x + 1)]) || $this->map[($y - 1) * 35 + ($x + 1)] == '0' || $this->map[($y - 1) * 35 + ($x + 1)] == '5')
                       && (!isset($this->map[($y - 1) * 35 + ($x - 1)]) || $this->map[($y - 1) * 35 + ($x - 1)] == '0' || $this->map[($y - 1) * 35 + ($x - 1)] == '5')) {
                        $this->map[$y * 35 + $x] = '5';
                    }
                }
            }
        }
    }

    // ======================================================================
    // Way utilities
    // ======================================================================

    public function getWayRecur($x, $y, $limX, $limY, $factorX, $factorY)
    {
        $pt = $y * 35 + $x;

        if ($x == $limX && $y == $limY) {
            return ['score' => intval('.' == $this->map[$y * 35 + $x]), 'way' => [$pt]];
        }

        if ($x == $limX) {
            $r = $this->getWayRecur($x, $y + $factorY, $limX, $limY, $factorX, $factorY);
        } else if ($y == $limY) {
            $r = $this->getWayRecur($x + $factorX, $y, $limX, $limY, $factorX, $factorY);
        } else {
            $a = $this->getWayRecur($x + $factorX, $y, $limX, $limY, $factorX, $factorY);
            $b = $this->getWayRecur($x, $y + $factorY, $limX, $limY, $factorX, $factorY);
            if ($a['score'] == $b['score']) {
                $r = rand() % 2 ? $a : $b;
            } else if ($a['score'] > $b['score']) {
                $r = $a;
            } else {
                $r = $b;
            }
        }

        $s = intval('.' == $this->map[$y * 35 + $x]) + $r['score'];
        $w = array_merge([$pt], $r['way']);

        return ['score' => $s, 'way' => $w];
    }

    public function getWay($a, $b)
    {
        if ($a[0] == $b[0] && $a[1] == $b[1]) {
            return ['score' => 0, 'way' => []];
        }

        $factorX = 1;
        if ($a[0] > $b[0]) {
            $factorX = -1;
        }

        $factorY = 1;
        if ($a[1] > $b[1]) {
            $factorY = -1;
        }

        $r = $this->getWayRecur($a[0], $a[1], $b[0], $b[1], $factorX, $factorY);
        array_shift($r['way']);
        return $r;
    }

    // ======================================================================
    // Determinist strategy
    //
    // This algorithm looks for all available rectangles, and for each one:
    // - calculate the number of cells to earn inside ( value )
    // - calculate the risk it will be broken by enemies ( risk )
    // - calculate the distance to reach it and do the whole perimeter ( cost )
    //
    // Then a ratio is calculated from those values, and we keep the rectangle
    // with the highest ratio:
    //
    // ratio = risk/value * value/rounds
    //
    // Problem: too long
    // ======================================================================

    public function getMatrixes()
    {
        $enemyMap      = [];
        $myDistanceMap = [];
        for ($i = 0; ($i < 700); $i++) {
            $enemyMap[$i] = 220;
            for ($j = 1; ($j <= $this->opponentCount); $j++) {
                $enemyMap[$i] -= $this->getRounds($i % 35, intval($i / 35), $this->opponents[$j][0], $this->opponents[$j][1]);
            }
            $myDistanceMap[$i] = $this->getRounds($i % 35, intval($i / 35), $this->x, $this->y);
        }

        return array($enemyMap, $myDistanceMap);
    }

    public function getRectangleRisk(array $ennemyMap, $topLeft, $bottomRight)
    {
        $risk = 0;
        for ($x = $topLeft[0]; ($x <= $bottomRight[0]); $x++) {
            for ($y = $topLeft[1]; ($y <= $bottomRight[1]); $y++) {
                $risk += $ennemyMap[$y * 35 + $x];
            }
        }

        return $risk;
    }

    public function getRectanglePerimeter($topLeft, $bottomRight)
    {
        $perimeter = [];
        $needed    = 0;

        for ($x = $topLeft[0]; ($x <= $bottomRight[0]); $x++) {
            $perimeter[] = $topLeft[1] * 35 + $x;
            if ($this->map[$topLeft[1] * 35 + $x] == '.') {
                $needed++;
            }
        }
        for ($y = $topLeft[1] + 1; ($y <= $bottomRight[1]); $y++) {
            $perimeter[] = $y * 35 + $bottomRight[0];
            if ($this->map[$y * 35 + $bottomRight[0]] == '.') {
                $needed++;
            }
        }
        for ($x = $bottomRight[0] - 1; ($x >= $topLeft[0]); $x--) {
            $perimeter[] = $bottomRight[1] * 35 + $x;
            if ($this->map[$bottomRight[1] * 35 + $x] == '.') {
                $needed++;
            }
        }
        for ($y = $bottomRight[1] - 1; ($y > $topLeft[1]); $y--) {
            $perimeter[] = $y * 35 + $topLeft[0];
            if ($this->map[$y * 35 + $topLeft[0]] == '.') {
                $needed++;
            }
        }

        return [$perimeter, $needed];
    }

    public function getRectangleClosestCell(array $myDistanceMap, array $perimeter)
    {
        $distance = null;
        $cell     = null;

        foreach ($perimeter as $pt) {
            if (is_null($distance) || ($myDistanceMap[$pt] < $distance)) {
                $distance = $myDistanceMap[$pt];
                $cell     = $pt;
            }
        }

        return [$cell, $distance];
    }

    public function getPerimeterCost($perimeter, $needed, $pt, $clockwise = true)
    {
        $cost   = 0;
        $filled = 0;
        $way    = [];

        $count = count($perimeter);
        $index = array_search($pt, $perimeter);
        for ($i = $clockwise ? $index + 1 : $index - 1; $clockwise ? ($i < $count) : ($i != -1); $clockwise ? $i++ : $i--) {
            $way[] = $perimeter[$i];
            $cost++;
            if ($this->map[$perimeter[$i]] == '.') {
                $filled++;
            }
            if ($filled == $needed) {
                return ['cost' => $cost, 'way' => $way];
            }
        }

        for ($i = $clockwise ? 0 : $count - 1; $clockwise ? ($i < $index) : ($i != $index); $clockwise ? $i++ : $i--) {
            $way[] = $perimeter[$i];
            $cost++;
            if ($this->map[$perimeter[$i]] == '.') {
                $filled++;
            }
            if ($filled == $needed) {
                break;
            }
        }

        return ['cost' => $cost, 'way' => $way];
    }

    public function getBestWayToGoAroundRectangle($perimeter, $needed, $cell)
    {
        $a = $this->getPerimeterCost($perimeter, $needed, $cell, true);
        $b = $this->getPerimeterCost($perimeter, $needed, $cell, false);

        if ($a['cost'] < $b['cost']) {
            return [$a['cost'], $a['way']];
        }

        return [$b['cost'], $b['way']];
    }

    /*
     * avoids timeouts but breaks the main logic valuing value/rounds ratio
     */
    const MAGIC = 0.8;

    public function go()
    {
        $this->loop();
    }

    public function getMagicRatio($rectangle)
    {
        list($risk, $value, $rounds) = [
            $rectangle['risk'],
            $rectangle['value'],
            $rectangle['rounds'],
        ];

        $a = $risk / $value;
        $b = $value / $rounds;

        $shit = $a * $b;
        return $shit;
    }

    public function deterministStrategy()
    {
        list($enemyMap, $myDistanceMap) = $this->getMatrixes();
        $allRectangles = $this->getAllRectangles();

        foreach ($allRectangles as $key => $rectangle) {
            list($topLeft, $bottomRight) = $rectangle['pos'];
            $rectangle['risk'] = $this->getRectangleRisk($enemyMap, $topLeft, $bottomRight);

            list($perimeter, $needed) = $this->getRectanglePerimeter($topLeft, $bottomRight);
            list($cell, $distance) = $this->getRectangleClosestCell($myDistanceMap, $perimeter);
            list($cost, $way) = $this->getBestWayToGoAroundRectangle($perimeter, $needed, $cell);

            $rectangle['distance'] = $distance;
            $rectangle['rounds']   = ($cost + $distance);
            $rectangle['cell']     = $cell;
            $rectangle['way']      = $way;

            $allRectangles[$key] = $rectangle;
        }

        usort($allRectangles, function($a, $b) {
            $x = $this->getMagicRatio($a);
            $y = $this->getMagicRatio($b);
            return $x > $y ? -1 : 1;
        });

        $best = reset($allRectangles);

        $way = ['way' => []];
        if ($best['distance'] > 0) {
            $way = $this->getWay([$this->x, $this->y], [$best['cell'] % 35, intval($best['cell'] / 35)]);
        }

        // $this->showRectangles([$best]);

        return [$best, array_merge($way['way'], $best['way'])];
    }

    // ======================================================================
    // Back stuff
    // ======================================================================

    public function backupBack()
    {
        $context = [$this->map, $this->way, $this->rectangle];
        $this->contexts[] = $context;
    }

    public function detectBacks()
    {
        if (is_null($this->backs)) {
            $this->backs = $this->backInTimeLeft;
            for ($i = 1; ($i <= $this->opponentCount); $i++) {
                $this->backs .= $this->opponents[$i][2];
            }

            return false;
        }

        $currentBack = $this->backInTimeLeft;
        for ($i = 1; ($i <= $this->opponentCount); $i++) {
            $currentBack .= $this->opponents[$i][2];
        }

        if ($this->backs == $currentBack) {
            return false;
        }

        $this->backs = $currentBack;

        $sha1 = sha1(var_export($this->map, true));
        $count = count($this->contexts);
        for ($i = 0; ($i < $count - 1); $i++) {
            if (sha1(var_export($this->contexts[$i][0], true)) === $sha1) {
                $this->way = $this->contexts[$i][1];
                $this->rectangle = $this->contexts[$i][2];

                $nb         = $count - $i;
                $this->err("Detected BACK {$nb}\n");

                $this->contexts = array_slice($this->contexts, 0, $i - 1);
                break;
            }
        }
    }

    public function goBackStrategy()
    {
        if ($this->backInTimeLeft == 0) {

            // @TODO: if i'm loosing anyway, i should break the best dude's plan

            return false;
        }

        for ($i = 1; ($i <= $this->opponentCount); $i++) {
            if ($this->scores[$i] >= $this->needed) {
                $bestRatio = null;
                $bestKey   = null;
                $count     = count($this->contexts) - 1;
                for ($j = $count - 1; ($j >= ($count - 25)); $j--) {
                    $ratios = $this->ratios($this->contexts[$j][0]);
                    if ((is_null($bestRatio)) || ($ratios['0'] > $bestRatio)) {
                        $bestRatio = $ratios['0'];
                        $bestKey   = $j;
                    }
                }

                $this->closestStrategy = $bestKey;
                return ($count - $bestKey);
            }
        }

        return 0;
    }

    // ======================================================================
    // Core
    // ======================================================================

    public function round()
    {
        if (array_sum($this->scores) == 700) {
            return [34, 19];
        }

        $this->detectBacks();

        $this->cleanExistingRectangles();

        if (count($this->way) == 0) {
            list($this->rectangle, $this->way) = $this->deterministStrategy();
            goto way;
        }

        list($topLeft, $bottomRight) = $this->rectangle['pos'];
        if ($this->isEnemyInside($topLeft[0], $topLeft[1], $bottomRight[0], $bottomRight[1])) {
            list($this->rectangle, $this->way) = $this->deterministStrategy();
        }

        way:
        $pt = array_shift($this->way);
        $x  = $pt % 35;
        $y  = intval($pt / 35);

        return [$x, $y];
    }

    public function loop()
    {
        fscanf(STDIN, "%d", $this->opponentCount);
        $this->needed = ceil(35 * 20 / ($this->opponentCount + 1));

        $gameRound = null;
        while (true) {

            fscanf(STDIN, "%d", $gameRound);
            fscanf(STDIN, "%d %d %d", $this->x, $this->y, $this->backInTimeLeft);

            for ($i = 1; $i <= $this->opponentCount; $i++) {
                list($opponentX, $opponentY, $opponentBackInTimeLeft) = array_fill(0, 3, null);
                fscanf(STDIN, "%d %d %d", $opponentX, $opponentY, $opponentBackInTimeLeft);
                $this->opponents[$i] = array($opponentX, $opponentY, $opponentBackInTimeLeft);
            }

            $this->map = '';
            $line      = null;
            for ($i = 0; $i < 20; $i++) {
                fscanf(STDIN, "%s", $line);
                $this->map .= $line;
            }

            $this->scores  = $this->scores();
            $this->missing = $this->needed - $this->scores['0'];

            $back = $this->goBackStrategy();
            if ($back > 0) {
                echo "BACK {$back}\n";
                continue;
            }

            list($this->x, $this->y) = $this->round();
            echo "{$this->x} {$this->y}\n";

            $this->backupBack();
        }
    }

    // ======================================================================
    // Debug
    // ======================================================================

    public function loopDebug()
    {
        $this->debug = true;
        $this->map = join('', explode("\n", trim('
00000000000000000................00
0...............0.................0
0...............0.................0
0...............0.................0
0...............0..............0000
01111111111111111111111111111111111
01000000000000000000000000000000001
010....0............111111111110.01
010....0111111111111111111111111101
010....01.....................10101
050....01.....................10101
010.....1.....................10101
010.....1.....................10101
010.....1.....................10101
010.....000000000000000000000000101
010000000...............1.......101
01111111111111111111111111111111101
0...................000000000000001
0...................0...1.........0
000000000000000000000...11111111100
', "\n")));

        $this->opponentCount                 = 1;
        $this->opponents[1]                  = [33, 0, 1];
        $this->backInTimeLeft                = 1;
        $this->x                             = 20;
        $this->y                             = 7;
        $this->map[$this->y * 35 + $this->x] = '0';

        for ($i = 0; ($i <= 500); $i++) {
            $this->scores                        = $this->scores();
            list($this->x, $this->y) = $this->round();
            $this->map[$this->y * 35 + $this->x] = '0';
            echo "{$this->x} {$this->y}\n";
            $this->showMapDetails();
        }

        die();
    }

    public function showMap($map = null, $cellSeparator = '')
    {
        ob_start();
        if (is_null($map)) {
            $map = $this->map;
        }
        for ($y = 0; ($y < 20); $y++) {
            for ($x = 0; ($x < 35); $x++) {
                if ($x > 0) {
                    echo $cellSeparator;
                }
                echo $map[$y * 35 + $x];
            }
            echo PHP_EOL;
        }
        echo PHP_EOL;
        $err = ob_get_clean();
        $this->err($err);
    }

    public function showMapDetails()
    {
        echo "--- ";
        for ($i = 0; ($i < 35); $i++) {
            if ($i > 0) {
                echo " ";
            }
            echo $i >= 10 ? $i : "0{$i}";
        }
        echo "\n";

        $no = 0;
        for ($y = 0; ($y < 20); $y++) {
            echo $no >= 10 ? $no : "0".$no;
            echo ": ";
            for ($x = 0; ($x < 35); $x++) {
                echo $this->map[$y * 35 + $x].'  ';
            }
            $no++;
            echo "\n";
        }

        echo "\n";
    }

    public function showRectangles(array $allRectangles)
    {
        ob_start();
        foreach ($allRectangles as $key => $rectangle) {
            list($topLeft, $bottomRight) = $rectangle['pos'];
            echo "Rectangle {$key}: {$topLeft[0]} {$topLeft[1]} -> {$bottomRight[0]} {$bottomRight[1]} (me: {$this->x} {$this->y}):\n";

            echo "--- ";
            for ($i = 0; ($i < 35); $i++) {
                if ($i > 0) {
                    echo " ";
                }
                echo $i >= 10 ? $i : "0{$i}";
            }
            echo "\n";

            $no = 0;
            for ($y = 0; ($y < 20); $y++) {
                echo $no >= 10 ? $no : "0".$no;
                echo ": ";
                for ($x = 0; ($x < 35); $x++) {
                    if ($this->inRectanglePerimeter($x, $y, $topLeft, $bottomRight) && $this->map[$y * 35 + $x] == '.') {
                        echo 'X';
                    } else {
                        echo $this->map[$y * 35 + $x];
                    }
                    echo '  ';
                }
                $no++;
                echo "\n";
            }

            echo "Risk : {$rectangle['risk']}\n";
            echo "Value : {$rectangle['value']}\n";
            echo "Rounds : {$rectangle['rounds']}\n";

            echo "\n";
        }
        $this->err(ob_get_clean());
    }

}

$game = new Game();
$game->go();