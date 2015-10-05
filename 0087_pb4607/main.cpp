#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
#include <sys/time.h>

using namespace std;


#define HORAIRE 0
#define ANTIHOR 1

#define DISTANCE_OUT 40

#define _width 35
#define _height 20
#define _maxRadius 11
#define _nPlayersMaximum 4

#define NMETRIQUES 17


int _myId = 0;

int _nPlayers;

int swapPlayers[_nPlayersMaximum][9] =
{
  {0,1,2,3,4,5,6,7,8},
  {1,0,2,3,4,5,6,7,8},
  {2,1,0,3,4,5,6,7,8},
  {3,1,2,0,4,5,6,7,8}
};

int oppositeDirection[8] = {1,0,3,2,0,0,0};

int parametresFonctionTriMetriques[NMETRIQUES][3] =
{
  {0,0,0},
  {1,0,0},
  {4,0,0},
  {0,1,0},
  {1,1,0},
  {4,1,0},
  {0,0,1},
  {0,1,1},
  {0,0,0},
  {0,0,0},
  {0,0,0},
  {0,0,0},
  {0,0,0},
  {0,0,0},
  {0,0,0}
};//dernier crochet : [0]:quel seuil pour margeZoneSure  [1]:zoneNormale ou zoneGrande  [2] quelle margeZoneSure prendre comme référence

float _alpha = 0.5;//global
float _alphaThreshold = 7.0;
float _roundWhereAlphaOne;

/*
inline int distance(int x1, int y1, int x2, int y2)
{
  return abs(x1-x2)+abs(y1-y2);
}*/

int _dx[4] = {0,0,-1,1};
int _dy[4] = {-1, 1, 0,0};






class EvaluationDirection
{
public:
  int direction;
  float metrique[NMETRIQUES];


  /*float ponderer() {
      float myFloat=0;
      for(int bbb = 8; bbb < 6+2*_nPlayers; bbb++) myFloat += metrique[bbb];

      return _alpha*_alpha*metrique[0]+(1.0-_alpha*_alpha-(1.0-_alpha)*(1.0-_alpha))*metrique[1]+(1.0-_alpha)*(1.0-_alpha)*metrique[2]+
              _alpha*_alpha*metrique[3]+(1.0-_alpha*_alpha-(1.0-_alpha)*(1.0-_alpha))*metrique[4]+(1.0-_alpha)*(1.0-_alpha)*metrique[5]
              -0.10*myFloat;
  }
  */
  float ponderer()
  {
    float myFloat =0;
    for(int bbb = 8; bbb < 6+2*_nPlayers; bbb++) myFloat += ((bbb%2==0)?1.0:0.1)*metrique[bbb];

    return 1.0*metrique[0]*metrique[1]
           + 1.0*metrique[2]*metrique[3]
           + 1.0*metrique[4]*metrique[5]
           -(0.2-0.04*_alpha)*myFloat*0.33*(metrique[1]+metrique[3]+metrique[5])
           +0.33*metrique[14]*(metrique[1]+metrique[3]+metrique[5])*(1.0-0.1*_alpha);
  }
};

class Node
{
public:
  int x,y,id;//
  int radius;//
  int explored;//
};


class Zone
{
public:
  int radius, L, N;//
  Node* centerNode;//
  int airDistance[_nPlayersMaximum];//air distance to be inside my zone
  int minAirDistanceAnyOpponentToInside;
  int minAirDistanceToPlayerZero;//air distance to be somewhere on the current border

  float evalZone[2];  //0:    (1+N+NgainedLastMove)/(1+L+minDistanceToBorderPlayerZero)
  //1:    (N-2+NgainedLastMove)/(1+L+minDistanceToBorderPlayerZero)

  int margeZoneSure[2];// 0 : =  L + minDistanceToBorderPlayerZero - min(zone1AD[i])
  // 1:  L + minDistanceToBorderPlayerZero - floor(pow(...min(zone1AD[i])
  float coefficient;




  void display()
  {

    if(margeZoneSure[0] > 999)
    {
      cerr << "No node left !" << endl;
      return;
    }
    cerr << "   Zone : xy : " << centerNode->x << " " << centerNode->y << " _ RNL : " << radius << " "<< N << " " << L << endl;
    //cerr << "Ratio : "<<((float)N/(float)L) << endl;
    cerr << "   EvalNormale : " << evalZone[0]<<endl;
    cerr << "   EvalGrande  : " << evalZone[1]<<endl;
    cerr << "   Air :  ";
    for(int i = 0; i < _nPlayers; i++)
    {
      cerr << " " << airDistance[i];
    }
    cerr << endl;
    cerr << "   minAirDistanceToP0 : " << minAirDistanceToPlayerZero << endl;
    cerr << "   margeZoneSure[0] : " << margeZoneSure[0] << endl;
  }

};


class Player
{
public:

  int playerId;//
  int x,y;//
  bool backInTimeLeft;//
  int score;
  Node* node;//
};



int _ParametreZoneCompareMarge=0; //global  0 : sûre, 1 : presque sûre
int _ParametreZoneCompareNormaleOuGrande=0; //global   0 : zoneNormale, 1 : grandeZone
int _ParametreZoneCompareMargeRacineOuPas=0;

bool zoneCompare(const Zone * z1, const Zone * z2)
{
  //Point de vue J0, la meilleure zone totalement sûre
  if (z1 -> margeZoneSure[_ParametreZoneCompareMargeRacineOuPas] < _ParametreZoneCompareMarge && z2-> margeZoneSure[_ParametreZoneCompareMargeRacineOuPas] < _ParametreZoneCompareMarge)
  {
    return z1->evalZone[_ParametreZoneCompareNormaleOuGrande] > z2->evalZone[_ParametreZoneCompareNormaleOuGrande];
  }
  else if (z1 -> margeZoneSure[_ParametreZoneCompareMargeRacineOuPas] == z2 -> margeZoneSure[_ParametreZoneCompareMargeRacineOuPas])
  {
    return z1->evalZone[_ParametreZoneCompareNormaleOuGrande] > z2->evalZone[_ParametreZoneCompareNormaleOuGrande];
  }
  else
  {
    return z1 -> margeZoneSure[_ParametreZoneCompareMargeRacineOuPas] < z2 -> margeZoneSure[_ParametreZoneCompareMargeRacineOuPas];
  }
}


bool zoneCompareNewVersion(const Zone * z1, const Zone * z2)
{
  if(z1->margeZoneSure[0] < 900 && z2->margeZoneSure[0] < 900) {
    return (z1->evalZone[0]+z1->evalZone[1])*z1->coefficient > (z2->evalZone[0]+z2->evalZone[1])*z2->coefficient;
  } else {
    return z1->margeZoneSure[0] < z2->margeZoneSure[0];
  }
}


bool meilleureEvaluationGarantieEvaluationDirection(const EvaluationDirection e1, const EvaluationDirection e2)
{
  return e1.metrique[0]+e1.metrique[3]>e2.metrique[0]+e2.metrique[3] ;
}



bool maximiseMetriqueEvaluationDirection(EvaluationDirection e1, EvaluationDirection e2)
{
  /*return (10*_alpha*e1.metrique[0] +  10*(1-_alpha)*e1.metrique[1]
  								+  10*_alpha*e1.metrique[2]
  								+  10*(1-_alpha)*e1.metrique[3]) < (10*_alpha*e2.metrique[0] +  10*(1-_alpha)*e2.metrique[1]
  								+  10*_alpha*e2.metrique[2]
  								+  10*(1-_alpha)*e2.metrique[3])*/



  //return e1.metrique[0] > e2.metrique[0];

  return e1.ponderer() > e2.ponderer();
}


inline int distance(Node *node, Player *player)
{
  return abs(node->x - player->x)+abs(node->y-player->y);
}



int main()
{
  struct timeval start, end;

  int savePrevDirection = 0;
  int savePrevGameRound = 0;

  int seuils[5][2][3]= {   {{0,0,0},{0,0,0}},
    {{0,0,0},{0,0,0}},
    {{580,630,699},{620,660,0}},
    {{450,520,610},{550,620,0}},
    {{400,480,600},{500,610,0}}
  };

  int NPointsJ0 = 0;
  int NPointsJ1 = 0;
  int NPointsJ2 = 0;
  int NPointsJ3 = 0;

  //x+y*_width
  const int offsetVoisin[8] = {-_width, -1, 1,  _width, _width + 1,_width-1, -_width-1,1-_width,};
  int currentIteration = 1000;
  int compteurTimeOut = 0;
  Player players[_nPlayersMaximum];
  Player savePlayers[_nPlayersMaximum];

  int grid[_width*_height];
  int gridSave[_width*_height];
  bool tabTouchUneTraceAdv[_width*_height];

  Node nodePool[_width*_height];

  /////////////////////////////////////
  /////   INITIAL INPUT
  /////////////////////////////////////
  cin >> _nPlayers;
  _nPlayers++;

  _roundWhereAlphaOne = 400.0/_nPlayers;

  cin.ignore();

  vector<EvaluationDirection> vectorEvalDirection;


  int neutralNodesSize;

  int neutralNodesSizeP0;
  int nFreeCells;
  int sumX, sumY;
  while (1)
  {
    /////////////////////////////////////
    /////   ROUND INPUT
    /////////////////////////////////////
    int gameRound;
    cin >> gameRound;
    cin.ignore();
    cin >> savePlayers[0].x >> savePlayers[0].y >> savePlayers[0].backInTimeLeft;
    savePlayers[0].playerId = 0;
    savePlayers[0].node = &nodePool[savePlayers[0].x+savePlayers[0].y*_width];
    cerr << "  myPosition xy : " << savePlayers[0].x << " "<< savePlayers[0].y << endl;
    cin.ignore();
    for (int i = 1; i < _nPlayers; i++)
    {
      cin >> savePlayers[i].x >> savePlayers[i].y >> savePlayers[i].backInTimeLeft;
      savePlayers[i].playerId = i;
      cerr << " hisPosition xy : " << savePlayers[i].x << " "<< savePlayers[i].y << endl;
      savePlayers[i].node = &nodePool[savePlayers[i].x+savePlayers[i].y*_width];
      cin.ignore();
    }

    nFreeCells = 0;
    sumX =0;
    sumY=0;

    NPointsJ0 = 0;
    NPointsJ1 = 0;
    NPointsJ2 = 0;
    NPointsJ3 = 0;


    for (int i = 0; i < 20; i++)
    {
      string line; // One line of the map ('.' = free, '0' = you, otherwise the id of the opponent)
      cin >> line;


      for(int j = 0 ; j< 35; j++)
      {
        if(line[j] == '.')
        {
          gridSave[j+i*_width] = 8;
          nFreeCells++;
          sumX+=j;
          sumY+=i;
        }
        else if(line[j] == '0')
        {
          gridSave[j+i*_width] = 0;
          NPointsJ0++;
        }
        else if(line[j] == '1')
        {
          gridSave[j+i*_width] = 1;
          NPointsJ1++;
        }
        else if(line[j] == '2')
        {
          gridSave[j+i*_width] = 2;
          NPointsJ2++;
        }
        else if(line[j] == '3')
        {
          gridSave[j+i*_width] = 3;
          NPointsJ3++;
        }
      }
      cin.ignore();
    }

    //cerr << "DEBUG2 " << NPointsJ0 <<" "<<NPointsJ1<<" "<<NPointsJ2<<" "<<NPointsJ3<<endl;
    sumX = (int)((float)sumX/(nFreeCells+0.1));
    sumY = (int)((float)sumY/(nFreeCells+0.1));

    gettimeofday(&start, NULL);

    vectorEvalDirection.clear();

    bool flagCompteurTimeOut = true;

    /////////////////////////////////////
    /////   DEBUT TESTER LES 4 DIRECTIONS
    /////////////////////////////////////


    int saveP0x = savePlayers[0].x;
    int saveP0y = savePlayers[0].y;

    float NGainedLastMove;
    bool flagNGainedLastMove;


    for(int chaqueDirection = 0; chaqueDirection < 4; chaqueDirection++)
    {
      NGainedLastMove = 0.0;
      flagNGainedLastMove = true;


      Zone* bestZone;
      EvaluationDirection myEvalDirection;
      myEvalDirection.direction = chaqueDirection;


      gettimeofday(&end, NULL);
      if((((end.tv_sec * 1000000 + end.tv_usec)  -  (start.tv_sec * 1000000 + start.tv_usec))>82000) && flagCompteurTimeOut)  //(((float)100000)*((float)_nPlayers+1.0)/((float)_nPlayers+2.0))
      {
        compteurTimeOut++;
        flagCompteurTimeOut = false;
      }

      for(int chaqueJoueur = 0 ; chaqueJoueur < _nPlayers ; chaqueJoueur++)
      {
        players[0].x = saveP0x;
        players[0].y = saveP0y;



        //cerr << "---"<<endl;

        for(int aaa = 0; aaa < _nPlayers; aaa++)
        {
          players[aaa].x = savePlayers[swapPlayers[chaqueJoueur][aaa]].x;
          players[aaa].y = savePlayers[swapPlayers[chaqueJoueur][aaa]].y;
        }


        if(savePlayers[0].x+_dx[chaqueDirection] >= 0
            && savePlayers[0].x+_dx[chaqueDirection] < _width
            && savePlayers[0].y+_dy[chaqueDirection] >= 0
            && savePlayers[0].y+_dy[chaqueDirection] < _height
            && flagCompteurTimeOut)
        {



          //Le déplacement est acceptable sur le terrain.

          for(int x=0; x < _width; x++)
          {
            for(int y=0; y< _height; y++)
            {
              grid[x+y*_width] = swapPlayers[chaqueJoueur][gridSave[x+y*_width]];

            }

          }

          players[swapPlayers[chaqueJoueur][0]].x += _dx[chaqueDirection];
          players[swapPlayers[chaqueJoueur][0]].y += _dy[chaqueDirection];

          if(grid[players[swapPlayers[chaqueJoueur][0]].x+players[swapPlayers[chaqueJoueur][0]].y*_width] == 8)
          {
            grid[players[swapPlayers[chaqueJoueur][0]].x+players[swapPlayers[chaqueJoueur][0]].y*_width] = swapPlayers[chaqueJoueur][0];
            NGainedLastMove = 1.2;
          }

          /*   for(int y=0; y < _height; y++)
             {
               for(int x=0; x< _width; x++)
               {
                 cerr << grid[x+y*_width];
               }
               cerr << endl;
             }*/


          ///////////
          // Dérouler l'évaluation de la position
          ///////////











          /////////////////////////////////////
          /////   RECHERCHE DES ZONES
          /////////////////////////////////////




          Zone zonePool[_width*_height*_maxRadius+1];

          if(chaqueJoueur == 0)
          {
            zonePool[0].evalZone[0]=100;
            zonePool[0].evalZone[1]=100;
          }
          else
          {
            zonePool[0].evalZone[0]=0;
            zonePool[0].evalZone[1]=0;
          }
          zonePool[0].margeZoneSure[0]=1000;
          zonePool[0].margeZoneSure[1]=1000;
          zonePool[0].minAirDistanceAnyOpponentToInside=1000;
          zonePool[0].coefficient = 1.0;


          vector<Zone*> zoneList;
          zoneList.reserve(_width*_height*_maxRadius+1);
          zoneList.clear();
          zoneList.push_back(&zonePool[0]);

          Node* neutralNodes[_width*_height];
          neutralNodesSize = 0;

          bool flagFill;

          ///////////////////////////////////////////
          /////  DEBUT DU PRECALCUL
          ///////////////////////////////////////////

          for(int x = 0; x < _width ; x++)
          {
            for (int y = 0; y < _height ; y++)
            {
              bool toucheUneTraceAdversaire = false;
              for(int k = 0; k < 8 ; k++)
              {
                if(x+y*_width+offsetVoisin[k] >= 0
                    && x+y*_width+offsetVoisin[k] < _width*_height
                    && grid[x+y*_width+offsetVoisin[k]] != 0
                    && grid[x+y*_width+offsetVoisin[k]] != 8)
                {
                  toucheUneTraceAdversaire = true;
                }
              }
              tabTouchUneTraceAdv[x+y*_width] = toucheUneTraceAdversaire;


              nodePool[x+y*_width].x = x;
              nodePool[x+y*_width].y = y;
              nodePool[x+y*_width].id = x+y*_width;
              nodePool[x+y*_width].radius = 0;
              nodePool[x+y*_width].explored = 0;


              if(grid[x+y*_width] ==  8 && x != 0 && x!= 34 && y !=0 && y!= 19)
              {
                flagFill = true;
                for(int k = 0; k < 8 ; k++)
                {
                  if(x+y*_width+offsetVoisin[k]  >= 0 && x+y*_width+offsetVoisin[k] < _width*_height)
                  {
                    if(grid[x+y*_width+offsetVoisin[k]] !=  8 && grid[x+y*_width+offsetVoisin[k]] !=  0)
                    {
                      flagFill = false;
                      k=8;
                    }
                  }
                }
                //if(flagFill && (gameRound > 45 || (x+y)%3 == 1))
                /*if(flagFill && (
                                (chaqueJoueur > 0 && (gameRound > 60
                                || (gameRound > 27 && gameRound <= 60 && (x+y)%2 == 1)
                                || (gameRound > 10 && gameRound <= 27 && (x+y)%3 == 1)
                                || (gameRound <= 10 && (x+y)%4 == 1)))
                                ||
                                (chaqueJoueur == 0 && (gameRound > 45
                                || (gameRound > 15 && gameRound <= 45 && (x+y)%2 == 1)
                                || (gameRound <= 15 && (x+y)%3 == 1)))))
                {
                  neutralNodes[neutralNodesSize] = &nodePool[x+y*_width];
                  neutralNodesSize++;
                }*/
                /*if(flagFill && (
                                (chaqueJoueur > 0 && (nFreeCells < 350
                                || (nFreeCells >= 350 && nFreeCells < 550 && (x+y)%2 == 1)
                                || (nFreeCells >= 550 && nFreeCells < 620 && (x+y)%3 == 1)
                                || (nFreeCells >= 620 && (x+y)%4 == 1)))
                                ||
                                (chaqueJoueur == 0 && (nFreeCells < 500
                                || (nFreeCells >= 500 && nFreeCells < 640 && (x+y)%2 == 1)
                                || (nFreeCells >= 640 && (x+y)%3 == 1)))))
                {
                  neutralNodes[neutralNodesSize] = &nodePool[x+y*_width];
                  neutralNodesSize++;
                }*/

                if(flagFill && (
                      (chaqueJoueur > 0 && (nFreeCells < seuils[_nPlayers][0][0]
                                            || (nFreeCells >= seuils[_nPlayers][0][0] && nFreeCells < seuils[_nPlayers][0][1] && (x+y)%2 == 1)
                                            || (nFreeCells >= seuils[_nPlayers][0][1] && nFreeCells < seuils[_nPlayers][0][2] && (x+y)%3 == 1)
                                            || (nFreeCells >= seuils[_nPlayers][0][2] && (x+y)%5 == 1)))
                      ||
                      (chaqueJoueur == 0 && (nFreeCells < seuils[_nPlayers][1][0]
                                             || (nFreeCells >= seuils[_nPlayers][1][0] && nFreeCells < seuils[_nPlayers][1][1] && (x+y)%2 == 1)
                                             || (nFreeCells >= seuils[_nPlayers][1][1] && (x+y)%3 == 1)))))
                {
                  neutralNodes[neutralNodesSize] = &nodePool[x+y*_width];
                  neutralNodesSize++;
                }
              }
            }
          }
          //cerr << seuils[_nPlayers][1][1] << "  " << seuils[_nPlayers][1][2] << endl;
          //cerr << neutralNodesSize << "   " << nFreeCells << endl;
          if(chaqueJoueur == 0) neutralNodesSizeP0 = neutralNodesSize;

          if(NGainedLastMove >= 0.6 && tabTouchUneTraceAdv[players[0].x+players[0].y*_width] && players[0].x != 0 && players[0].x!= 34 && players[0].y !=0 && players[0].y!= 19 && chaqueJoueur == 0)
            NGainedLastMove +=0.5;

          ///////////////////////////////////////////
          /////  FIN DU PRECALCUL
          ///////////////////////////////////////////



          //cerr << "Neutral Nodes Size : " << neutralNodesSize<<endl;

          Node* BFSNodePile[_width*_height];

          int BFSCurrent;
          int BFSSize;
          int BFSDebutZone;

          int nSavedBorderNodes;
          int nSavedBorderNodesThisZone;

          int minXSavedBorderNodes;
          int minYSavedBorderNodes;
          int maxXSavedBorderNodes;
          int maxYSavedBorderNodes;

          int minXCurrentBorderNodes;
          int minYCurrentBorderNodes;
          int maxXCurrentBorderNodes;
          int maxYCurrentBorderNodes;

          int savedBorderNodesMinAirDistanceToPlayerZero;
          int currentBorderNodesMinAirDistanceToPlayerZero;

          int insideNodesMinAirDistanceToPlayer[_nPlayersMaximum];

          Node* voisin;



          for(int i = 0; i< neutralNodesSize; i++)
          {



            ///////////////////////////////////////////
            /////  DEBUT BFS A PARTIR D'UN NOEUD CENTRAL
            ///////////////////////////////////////////

            currentIteration++;

            neutralNodes[i]->explored = currentIteration;

            BFSCurrent = 0;
            BFSNodePile[BFSCurrent]=neutralNodes[i];

            neutralNodes[i]->radius = 0;
            BFSSize = 1;

            BFSDebutZone = 0;
            nSavedBorderNodes = 0;
            nSavedBorderNodesThisZone = 0;

            minXSavedBorderNodes = 66;
            minYSavedBorderNodes = 66;
            maxXSavedBorderNodes = -66;
            maxYSavedBorderNodes = -66;

            minXCurrentBorderNodes = 66;
            minYCurrentBorderNodes = 66;
            maxXCurrentBorderNodes = -66;
            maxYCurrentBorderNodes = -66;

            savedBorderNodesMinAirDistanceToPlayerZero = 66;

            for (int playerId = 0; playerId < _nPlayers ; playerId++)
            {
              insideNodesMinAirDistanceToPlayer[playerId] = 66;
              currentBorderNodesMinAirDistanceToPlayerZero = 66;
            }

            for(int playerId = 0; playerId < _nPlayers; playerId++)
              zonePool[neutralNodes[i]->id*_maxRadius].airDistance[playerId] = distance(BFSNodePile[BFSCurrent], &players[playerId]);

            //Fin d'initialisation


            while (BFSCurrent < BFSSize && BFSNodePile[BFSCurrent]->radius < _maxRadius)
            {

              //cerr << BFSCurrent << " " << BFSSize << endl;

              /////////////////////////////
              /////  BOUCLE DU BFS
              /////////////////////////////

              bool flag = true; //Le noeud actuel ne fait pas encore partie de la limite de zone immuable

              for(int k = 0; k < 8 ; k++)
              {
                //if(BFSNodePile[BFSCurrent]->id+offsetVoisin[k]  >= 0 && BFSNodePile[BFSCurrent]->id+offsetVoisin[k] < _width*_height
                if((k == 0 && (BFSNodePile[BFSCurrent]->y > 0))
                    ||(k == 1 && BFSNodePile[BFSCurrent]->x > 0)
                    ||(k == 2 && BFSNodePile[BFSCurrent]->x < 34)
                    ||(k == 3 && BFSNodePile[BFSCurrent]->y < 19)
                    ||(k == 4 && BFSNodePile[BFSCurrent]->x < 34 && BFSNodePile[BFSCurrent]->y < 19)
                    ||(k == 5 && BFSNodePile[BFSCurrent]->x > 0 && BFSNodePile[BFSCurrent]->y < 19)
                    ||(k == 6 && BFSNodePile[BFSCurrent]->x > 0 && BFSNodePile[BFSCurrent]->y > 0)
                    ||(k == 7 && BFSNodePile[BFSCurrent]->x < 34 && BFSNodePile[BFSCurrent]->y > 0)
                  )

                {
                  ///////////////////////////////////////////
                  //  AJOUT VOISINS DANS LA PILE
                  ///////////////////////////////////////////

                  voisin = &nodePool[BFSNodePile[BFSCurrent]->id+offsetVoisin[k]];

                  if(voisin->explored != currentIteration && !tabTouchUneTraceAdv[BFSNodePile[BFSCurrent]->id])
                  {
                    voisin->explored = currentIteration;

                    if(grid[voisin->id] == 8)
                    {
                      //Case neutre
                      BFSNodePile[BFSSize] = voisin;
                      BFSNodePile[BFSSize]->radius = BFSNodePile[BFSCurrent]->radius + 1;
                      BFSSize++;
                    }

                  }

                  if((grid[voisin->id] != 0 && (grid[voisin->id] != 8)&& flag))
                  {
                    // La case n'est pas neutre et ne m'appartient pas, il faut enregistrer que la frontière doit rester immuable sur la case actuelle.
                    nSavedBorderNodes++;
                    nSavedBorderNodesThisZone++;

                    minXSavedBorderNodes = min(minXSavedBorderNodes, BFSNodePile[BFSCurrent]->x);
                    minYSavedBorderNodes = min(minYSavedBorderNodes, BFSNodePile[BFSCurrent]->y);
                    maxXSavedBorderNodes = max(maxXSavedBorderNodes, BFSNodePile[BFSCurrent]->x);
                    maxYSavedBorderNodes = max(maxYSavedBorderNodes, BFSNodePile[BFSCurrent]->y);


                    savedBorderNodesMinAirDistanceToPlayerZero = min(savedBorderNodesMinAirDistanceToPlayerZero, distance(BFSNodePile[BFSCurrent], &players[0]));


                    flag = false; // on n'ajoute le noeud actuel qu'une seule fois
                  }

                }
                else if (flag)
                {
                  //au moins un des voisins est hors du jeu donc on est au bord

                  nSavedBorderNodes++;
                  nSavedBorderNodesThisZone++;

                  minXSavedBorderNodes = min(minXSavedBorderNodes, BFSNodePile[BFSCurrent]->x);
                  minYSavedBorderNodes = min(minYSavedBorderNodes, BFSNodePile[BFSCurrent]->y);
                  maxXSavedBorderNodes = max(maxXSavedBorderNodes, BFSNodePile[BFSCurrent]->x);
                  maxYSavedBorderNodes = max(maxYSavedBorderNodes, BFSNodePile[BFSCurrent]->y);

                  savedBorderNodesMinAirDistanceToPlayerZero = min(savedBorderNodesMinAirDistanceToPlayerZero, distance(BFSNodePile[BFSCurrent], &players[0]));
                  flag = false; // on n'ajoute le noeud actuel qu'une seule fois
                }



              }



              ///////////////////////////////////////////
              //  TRAITER LE NOEUD ACTUEL
              ///////////////////////////////////////////
              if(flag)
              {
                for (int playerId = 0; playerId < _nPlayers ; playerId++)
                {
                  insideNodesMinAirDistanceToPlayer[playerId] = min(insideNodesMinAirDistanceToPlayer[playerId], distance(BFSNodePile[BFSCurrent], &players[playerId]));
                }
              }
              currentBorderNodesMinAirDistanceToPlayerZero = min(currentBorderNodesMinAirDistanceToPlayerZero, distance(BFSNodePile[BFSCurrent], &players[0]));

              minXCurrentBorderNodes = min(minXCurrentBorderNodes, BFSNodePile[BFSCurrent]->x);
              minYCurrentBorderNodes = min(minYCurrentBorderNodes, BFSNodePile[BFSCurrent]->y);
              maxXCurrentBorderNodes = max(maxXCurrentBorderNodes, BFSNodePile[BFSCurrent]->x);
              maxYCurrentBorderNodes = max(maxYCurrentBorderNodes, BFSNodePile[BFSCurrent]->y);

              ///////////////////////////////////////////
              //  DETECTER LE PASSAGE A UNE ZONE DE TAILLE SUPERIEURE
              ///////////////////////////////////////////

              if(BFSCurrent == BFSSize - 1 || BFSNodePile[BFSCurrent]->radius < BFSNodePile[BFSCurrent+1]->radius)
              {
                //Traiter la zone
                zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius].radius = BFSNodePile[BFSCurrent]->radius;
                zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius].L = max(BFSCurrent-BFSDebutZone+nSavedBorderNodes+1-nSavedBorderNodesThisZone, (max(maxXSavedBorderNodes, maxXCurrentBorderNodes)-min(minXSavedBorderNodes, minXCurrentBorderNodes))+(max(maxYSavedBorderNodes, maxYCurrentBorderNodes)-min(minYSavedBorderNodes, minYCurrentBorderNodes)));
                //zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius].L = BFSCurrent-BFSDebutZone+nSavedBorderNodes+1-nSavedBorderNodesThisZone;

                if(BFSCurrent == BFSSize-1 && BFSNodePile[BFSCurrent]->radius < _maxRadius -1)
                {
                  zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius].L = max(nSavedBorderNodes, (maxXSavedBorderNodes)-minXSavedBorderNodes+maxYSavedBorderNodes-minYSavedBorderNodes);
                  currentBorderNodesMinAirDistanceToPlayerZero = savedBorderNodesMinAirDistanceToPlayerZero;
                }

                zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius].N = 1+ BFSCurrent;
                zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius].centerNode = neutralNodes[i];

                for(int playerId = 0; playerId < _nPlayers; playerId++)
                  zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius + 1].airDistance[playerId] = insideNodesMinAirDistanceToPlayer[playerId];

                zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius].minAirDistanceToPlayerZero = min(currentBorderNodesMinAirDistanceToPlayerZero, savedBorderNodesMinAirDistanceToPlayerZero);

                zoneList.push_back(&zonePool[neutralNodes[i]->id*_maxRadius+BFSNodePile[BFSCurrent]->radius]);

                if(BFSCurrent == BFSSize-1 && nSavedBorderNodes == 0)
                {
                  //On a terminé de remplir une zone sans trouver de cases vides à explorer ou de cases de l'adversaire. La zone nous appartient complètement.
                  /* cerr << "#############" << endl;
                   cerr << "#############" << endl;
                   cerr << zoneList.size() << endl;*/
                  //zoneList.erase(zoneList.begin() + zoneList.size()-BFSNodePile[BFSCurrent]->radius-2,zoneList.end());
                  for(int compteur = 0; compteur <= BFSNodePile[BFSCurrent]->radius; compteur++)
                  {
                    zoneList.pop_back();
                  }

                  //cerr << zoneList.size() << endl;
                  //(*(zoneList.end()))->display();
                  if(flagNGainedLastMove)
                  {
                    NGainedLastMove += 1+BFSCurrent;
                    flagNGainedLastMove = false;
                  }
                }

                //Réinitialiser la zone
                BFSDebutZone = BFSCurrent + 1;
                nSavedBorderNodesThisZone = 0;
                currentBorderNodesMinAirDistanceToPlayerZero = 66;
                minXCurrentBorderNodes = 66;
                minYCurrentBorderNodes = 66;
                maxXCurrentBorderNodes = -66;
                maxYCurrentBorderNodes = -66;
              }
              BFSCurrent++;
            }
            /*
                      if(neutralNodes[i]->id == 9+15*_width) {
                        cerr << "      FOUNDIT"<<endl;
                        (*(zoneList.end()-1))->display();
                      }*/
          }

          //////////////////////////////////////////////
          /////  TOUTES LES ZONES ONT ETE CONSTRUITES
          //////////////////////////////////////////////

          /*
          TODO : equivalent de groundDistance
              for(int playerId = 0; playerId < _nPlayers; playerId++) {
                int i = zoneList.size()-1;
                while(i >= 0) {
                  //On commence par la zone la plus éloignée
                  bool aAugmenteUneFois = false;
                  int saveAD = zoneList[i]->airDistance[playerId];


                }
              }
          */


          for(int i = 1; i < zoneList.size(); i++)
          {

            if(chaqueJoueur == 0)
            {
              if(NGainedLastMove > 1.9)
              {

                //if(gameRound>40  && i<20) cerr << "blaaaa " << NGainedLastMove << " " << ((float)(1+zoneList[i]->N*0.05+NGainedLastMove))<< " " <<((float)(2+zoneList[i]->L*0.05+zoneList[i]->minAirDistanceToPlayerZero*0.05)) <<endl;

                //zoneList[i]->evalZone[0] = ((float)(1+zoneList[i]->N*0.05+NGainedLastMove))/((float)(2+2.5+zoneList[i]->L*0.05+zoneList[i]->minAirDistanceToPlayerZero*0.05));
                //zoneList[i]->evalZone[1] = 1.2*((float)(1+zoneList[i]->N*0.05+NGainedLastMove))/((float)(2+2.5+4+zoneList[i]->L*0.05+zoneList[i]->minAirDistanceToPlayerZero*0.05));
                zoneList[i]->evalZone[0] = ((float)(1+zoneList[i]->N*0.05+NGainedLastMove))/((float)(2+2.0+zoneList[i]->L*0.05+zoneList[i]->minAirDistanceToPlayerZero*0.05));
                zoneList[i]->evalZone[1] = 1.2*((float)(1+zoneList[i]->N*0.05+NGainedLastMove))/((float)(2+2.0+4+zoneList[i]->L*0.05+zoneList[i]->minAirDistanceToPlayerZero*0.05));

              }
              else
              {
                zoneList[i]->evalZone[0] = ((float)(1+zoneList[i]->N+NGainedLastMove))/((float)(3+1+zoneList[i]->L+zoneList[i]->minAirDistanceToPlayerZero));
                zoneList[i]->evalZone[1] = 1.2*((float)(1+zoneList[i]->N+NGainedLastMove))/((float)(3+4+1+zoneList[i]->L+zoneList[i]->minAirDistanceToPlayerZero));
              }
            }
            else
            {
              zoneList[i]->evalZone[0] = ((float)(1+zoneList[i]->N))/((float)(3+1+zoneList[i]->L+zoneList[i]->minAirDistanceToPlayerZero));
              zoneList[i]->evalZone[1] = 1.2*((float)(1+zoneList[i]->N))/((float)(3+4+1+zoneList[i]->L+zoneList[i]->minAirDistanceToPlayerZero));
            }

            zoneList[i]->evalZone[0] -=  0.04* (float) ((abs(zoneList[i]->centerNode->x - sumX) + abs(zoneList[i]->centerNode->y-sumY))-8);
            zoneList[i]->evalZone[1] -=  0.04* (float) ((abs(zoneList[i]->centerNode->x - sumX) + abs(zoneList[i]->centerNode->y-sumY))-8);

            int temp = zoneList[i]->airDistance[1];
            for(int j = 2; j<_nPlayers ; j++) temp = min(temp, zoneList[i]->airDistance[j]);

            //cerr << temp << "  " << (int)pow((float)temp+0.99,1.0-0.1*min(1.0,(float)nFreeCells/600.0)) << endl;

            zoneList[i]->margeZoneSure[0] = zoneList[i]->L+zoneList[i]->minAirDistanceToPlayerZero-temp;// =  L + minDistanceToBorderPlayerZero - min(zone1AD[i])
            //zoneList[i]->margeZoneSure[1] = (int)pow((float)(zoneList[i]->L+zoneList[i]->minAirDistanceToPlayerZero)+0.99,1.0-0.4*min(1.0,(float)nFreeCells/600.0))-temp;// =  L + minDistanceToBorderPlayerZero - min(zone1AD[i])
            //zoneList[i]->minAirDistanceAnyOpponentToInside = temp;
            zoneList[i]->coefficient = min(pow(((float)temp) / pow(((float)(zoneList[i]->L+zoneList[i]->minAirDistanceToPlayerZero)), 1.0-0.19*min(1.0,(float)nFreeCells/600.0)), 8),1.0);
            if(zoneList[i]->margeZoneSure[0] < 0) zoneList[i]->coefficient *= ((100.0-zoneList[i]->margeZoneSure[0])/100.0);
            if(chaqueJoueur == 0 && NGainedLastMove > 1.9) zoneList[i]->coefficient = 1.0;
          }


          /*  bestZone = *(std::min_element(zoneList.begin(), zoneList.end(), [&](const Zone* c1, const Zone* c2)
            {
              return ((float)c1->N/(float)c1->L) > ((float)c2->N/(float)c2->L);
            }));*/

          /*std::min_element(zoneList.begin(), zoneList.end(), [&](const Zone* c1, const Zone* c2)
          {
            return ((float)c1->N/pow((float)c1->L, 1.4)) > ((float)c2->N/pow((float)c2->L, 1.4));
          });*/


          /*std::sort(zoneList.begin(), zoneList.end(), zonePointerCompare);*/








          //cerr << "________BEGIN_________" << endl;
          if(chaqueJoueur == 0)
          {

//cerr << "_________BEGIN__________" << endl;

            _ParametreZoneCompareMarge=0; //global  0 : sûre, 1 : presque sûre
            _ParametreZoneCompareNormaleOuGrande=0; //global   0 : zoneNormale, 1 : grandeZone
            _ParametreZoneCompareMargeRacineOuPas=0; //global   0 : normale, 1 : racine

            bestZone = *(std::min_element(zoneList.begin(), zoneList.end(), &zoneCompare));
            myEvalDirection.metrique[15]=bestZone->evalZone[0];
            //bestZone->display();

            _ParametreZoneCompareMarge=0; //global  0 : sûre, 1 : presque sûre
            _ParametreZoneCompareNormaleOuGrande=1; //global   0 : zoneNormale, 1 : grandeZone
            _ParametreZoneCompareMargeRacineOuPas=0; //global   0 : normale, 1 : racine

            bestZone = *(std::min_element(zoneList.begin(), zoneList.end(), &zoneCompare));
            myEvalDirection.metrique[16]=bestZone->evalZone[1];
           //bestZone->display();
//cerr << "_________END__________" << endl;


            for(int imetrique = 0; imetrique<3 ; imetrique++ )
            {
              bestZone = *(std::min_element(zoneList.begin(), zoneList.end(), &zoneCompareNewVersion));
              myEvalDirection.metrique[2*imetrique]=(bestZone->evalZone[0]+bestZone->evalZone[1])*0.5;
              myEvalDirection.metrique[2*imetrique+1]=bestZone->coefficient;


              if(zoneList.size() > 1) { bestZone->evalZone[0] = 0; bestZone->evalZone[1] = 0; bestZone->margeZoneSure[0] = 1100; }
            }
          }
          else
          {
            _ParametreZoneCompareMarge=2; //global  0 : sûre, 1 : presque sûre
            _ParametreZoneCompareNormaleOuGrande=0; //global   0 : zoneNormale, 1 : grandeZone
            _ParametreZoneCompareMargeRacineOuPas=0;
            bestZone = *(std::min_element(zoneList.begin(), zoneList.end(), &zoneCompare));
            myEvalDirection.metrique[6+2*chaqueJoueur]=bestZone->evalZone[0];
            //bestZone->display();

            _ParametreZoneCompareMarge=3; //global  0 : sûre, 1 : presque sûre
            _ParametreZoneCompareNormaleOuGrande=1; //global   0 : zoneNormale, 1 : grandeZone
            _ParametreZoneCompareMargeRacineOuPas=0;
            bestZone = *(std::min_element(zoneList.begin(), zoneList.end(), &zoneCompare));
            myEvalDirection.metrique[6+2*chaqueJoueur+1]=bestZone->evalZone[1];
            // bestZone->display();
          }
          //cerr << "_________END__________" << endl;

        }
      }

      if(savePlayers[0].x+_dx[chaqueDirection] >= 0 && savePlayers[0].x+_dx[chaqueDirection] < _width && savePlayers[0].y+_dy[chaqueDirection] >= 0 && savePlayers[0].y+_dy[chaqueDirection] < _height && flagCompteurTimeOut )
      {
        if(gameRound == savePrevGameRound + 1 && chaqueDirection==savePrevDirection)
          myEvalDirection.metrique[14] = 0.15;
        else if(gameRound == savePrevGameRound + 1 && chaqueDirection==oppositeDirection[savePrevDirection])
          myEvalDirection.metrique[14] = -0.0;
        else
          myEvalDirection.metrique[14] = 0.0;
        cerr << "Metrique Dir: "<<myEvalDirection.ponderer()<<endl;
        vectorEvalDirection.push_back(myEvalDirection);
      }
    }



   ///////////////////////
      ///////////////////////
         ///////////////////////
            ///////////////////////
               ///////////////////////

    EvaluationDirection evalDirectionLaPlusSure = *(std::min_element(vectorEvalDirection.begin(), vectorEvalDirection.end(), &meilleureEvaluationGarantieEvaluationDirection));

    _alpha = 1.0 +  max(-1.0, min(0.0, ((evalDirectionLaPlusSure.metrique[15]+evalDirectionLaPlusSure.metrique[16])/(2.0*_alphaThreshold))-1.0))*(0.05+min(0.95,((float)nFreeCells/600.0)));//(1.0-min((float)gameRound, _roundWhereAlphaOne)/_roundWhereAlphaOne);

    cerr << "Valeur sure : " <<(evalDirectionLaPlusSure.metrique[0]+evalDirectionLaPlusSure.metrique[3])/(2.0) << endl;
    cerr << "ALPHA       : " << _alpha << endl;


    EvaluationDirection bestEvalDirection = *(std::min_element(vectorEvalDirection.begin(), vectorEvalDirection.end(), &maximiseMetriqueEvaluationDirection));






    gettimeofday(&end, NULL);

    if(gridSave[saveP0x + saveP0y*_width]==8 && gameRound%5 == 1)
    {
      cout << saveP0x << " " << saveP0y<<endl;// << " UNSTUCK" << endl;
      savePrevDirection = 5;
    }
    else if(flagCompteurTimeOut
            && gameRound == savePrevGameRound + 1
            && gameRound > 30
            && NPointsJ0<(int)((float)(NPointsJ1+NPointsJ2+NPointsJ3)/((float)(_nPlayers-1.0)))
            && savePlayers[0].backInTimeLeft == 1
            && bestEvalDirection.ponderer() < 0.4)
    {
      cout << "BACK 25" << endl;
      _alphaThreshold = 5.0;
    }
    else if(nFreeCells > 0 && (neutralNodesSizeP0 < 6 || bestEvalDirection.ponderer() < 0.2 || !flagCompteurTimeOut) )
    {
      //aller sur la case vide la plus proche
      int bestX = 1000;
      int bestY = 1000;
      for(int x=0; x<_width; x++)
      {
        for(int y =0; y<_height; y++)
        {
          if(gridSave[x+y*_width]==8)
          {
            if(((abs(saveP0x - x)+abs(saveP0y - y)) <= (abs(saveP0x - bestX)+abs(saveP0y - bestY))) && (abs(saveP0x - x)+abs(saveP0y - y)) != 0)
            {
              bestX = x;
              bestY = y;
            }
          }
        }
      }
      if(bestX == 1000)
      {
        bestX=15;
        bestY = 15;
      }
      cout << bestX << " " << bestY<<endl;// <<" "<< compteurTimeOut<<"  //  "<< " FINISH IT" << endl;
      savePrevDirection = 5;
    }

    else
    {
      cout << saveP0x + _dx[bestEvalDirection.direction] << " " << saveP0y + _dy[bestEvalDirection.direction]<<endl;// <<" "<< compteurTimeOut<<"  //  "<< _alpha << "  //  " << ((end.tv_sec * 1000000 + end.tv_usec)  -  (start.tv_sec * 1000000 + start.tv_usec)) <<endl;
      savePrevDirection = bestEvalDirection.direction;
    }

    savePrevGameRound = gameRound;

    cerr << "Time : " << ((end.tv_sec * 1000000 + end.tv_usec)  -  (start.tv_sec * 1000000 + start.tv_usec)) << endl;
    cerr << "Compteur Time Out : "<<compteurTimeOut <<" "<< _alpha <<endl;
    cerr << "GameRound : " << gameRound << endl;
  }
}
