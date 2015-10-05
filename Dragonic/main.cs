using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    public struct Case
    {
        public Case(int x_, int y_)
        {
            x=x_;
            y=y_;
        }
        public int x;
        public int y;
    }
    public struct Historic
    {
        public Historic(int x_, int y_, int direction_, int sens_, bool roomMode_, bool firstRoom_, int timeRoom_, bool oneVSone_)
        {
            x=x_;
            y=y_;
            direction=direction_;
            sens=sens_;
            roomMode=roomMode_;
            firstRoom=firstRoom_;
            timeRoom=timeRoom_;
            oneVSone=oneVSone_;
        }
        public int x;
        public int y;
        public int direction;
        public int sens;
        public bool roomMode;
        public bool firstRoom;
        public int timeRoom;
        public bool oneVSone;
    }
    
    //structs V6
    struct CaseDatas
    {
        public int appartenance;
        public bool tagRoomCentre;//contient l'info si les 8 voisins sont vides (justifie la prÃ©sence d'une room)
        public int tagRoomStart;//contient l'info pour savoir si on doit Ã©viter ce point d'entrÃ©e (si >= 3 : correspond aux voisins, parmis les 8, dÃ©jÃ  en notre possession)
        public bool tagRoomEdge;//contient l'info pour savoir si on est sur un bord de room
        public int roomSize;
        public int roomOrigin;
    }
    struct GrilleDatas
    {
        public int nbrVides;
        public List<int> scores;
        public int maxScore;
    }
    struct HistoryV6
    {
        public int gameRound;
        public MODE mode;
        public GrilleDatas grilleDatas;
        public Case objectifCase;
        public Case roomStartCase;
        public List<Case> positions;
    }
    public struct Room
    {
        public List<Case> casesCentre;
        public List<Case> casesEdge;
        public int tailleBonus;//nombre de cases centre
    }
    enum MODE
    {
        room,//fermeture de zone
        search,//recherche de nouvelle zone
        next,//objectif Ã  atteindre dÃ©jÃ  renseignÃ©
    }
    static void Main(string[] args)
    {
        
        string[] inputs;
        int opponentCount = int.Parse(Console.ReadLine()); // Opponent count
        
        bool V6 = true;//trigger pour lancement de la V6 de l'IA
        
        if(opponentCount==1)//on va altener avec l'ancien algo pour le 1v1
        {
            int seed = (int) DateTime.Now.Ticks & 0x0000FFFF;
            Random r = new Random(seed);
            int a = r.Next(0, 2);
            
            if(a==0)
            {
                V6=false;
            }
        }
        
        if(V6)
        {
            Console.Error.WriteLine("Nouvel Algo");
        }
        else
        {
            Console.Error.WriteLine("1v1, ancien algo");
        }
        
        if(V6)//nouvel algo, recherche des zones d'intÃ©rÃªt et contournement de ces zones
        {
            int width = 35;
            int height = 20;
            int lastGameRound = 0;
            int isSymetric = -1;
            Case objectifCase = new Case(-1,-1);
            Case roomStartCase = new Case(-1,-1);
            MODE mode = MODE.search;
            List<HistoryV6> historique = new List<HistoryV6>();
            
            //on rajoute Ã  l'historique son Ã©tat initial (gameRound 0)
            HistoryV6 h = new HistoryV6();
            h.gameRound = 0;
            h.mode = mode;
            h.objectifCase.x = -1;
            h.objectifCase.y = -1;
            h.roomStartCase.x = -1;
            h.roomStartCase.y = -1;
            h.positions = new List<Case>();
            historique.Add(h);
            
            
            //info des backInTime effectuÃ©s
            List<bool> backInTimeUsed = new List<bool>();
            for (int i = -1; i < opponentCount; i++)
            {
                backInTimeUsed.Add(false);
            }
            
            
            int nbrManchesMax;
            if(opponentCount==1)
                nbrManchesMax=350;
            else if(opponentCount==2)
                nbrManchesMax=300;
            else 
                nbrManchesMax=250;
                
            //boucle principale
            while(true)
            {
                int gameRound = int.Parse(Console.ReadLine());
                
                //correction du gameRound Ã  cause du referee qui le fait sauter au max d'un coup si le jeu est fini avant le nombre max de rounds
                gameRound = Math.Min(lastGameRound+1,gameRound);
                
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]); // Your x position
                int y = int.Parse(inputs[1]); // Your y position
                int backInTimeLeft = int.Parse(inputs[2]); // Remaining back in time
                
                List<Case> positions = new List<Case>();
                positions.Add(new Case(x,y));
                
                int xAdvMoy=0;
                int yAdvMoy=0;
                
                for (int i = 0; i < opponentCount; i++)
                {
                    inputs = Console.ReadLine().Split(' ');
                    int opponentX = int.Parse(inputs[0]); // X position of the opponent
                    int opponentY = int.Parse(inputs[1]); // Y position of the opponent
                    int opponentBackInTimeLeft = int.Parse(inputs[2]); // Remaining back in time of the opponent
          
                    positions.Add(new Case(opponentX,opponentY));
                    
                
                    //gestion backInTime ennemi
                    
                    //backInTime utilisÃ© par un adversaire
                    if(!backInTimeUsed[i+1] && opponentBackInTimeLeft==0)
                    {
                        //backInTime appliquÃ© (peut Ãªtre un tour aprÃ¨s la dÃ©tection)
                        if(gameRound!=lastGameRound+1)
                        {
                            Console.Error.WriteLine("BackInTime dÃ©tectÃ©");
                    
                            backInTimeUsed[i+1] = true;
                            
                            //mise Ã  jour des infos par rapport Ã  ce retour en arriÃ¨re
                            mode = historique[gameRound-1].mode;
                            objectifCase.x = historique[gameRound-1].objectifCase.x;
                            objectifCase.y = historique[gameRound-1].objectifCase.y;
                            roomStartCase.x = historique[gameRound-1].roomStartCase.x;
                            roomStartCase.y = historique[gameRound-1].roomStartCase.y;
            
                            //purge de la suite de l'historique
                            while(historique.Count>gameRound)
                                historique.RemoveAt(historique.Count-1);
                        }
                    }
                }
                
                //positionnement moyen des adversaires
                for(int i=0; i<opponentCount; i++)
                {
                    xAdvMoy += positions[i+1].x;
                    yAdvMoy += positions[i+1].y;
                }
                
                xAdvMoy = xAdvMoy/opponentCount;
                yAdvMoy = yAdvMoy/opponentCount;
                
                //vÃ©rification si positionnement de dÃ©part symÃ©trique
                if(isSymetric==-1)
                {
                    //on considÃ¨re symÃ©trie si ennemi Ã  la position inverse de nous
                    for (int i = 1; i < opponentCount; i++)
                    {
                        if(positions[i].x == 34-x && positions[i].y == 19-y)
                        {
                            isSymetric = 1;
                        }
                    }
                    
                    if(isSymetric==-1)
                        isSymetric = 0;
                        
                    Console.Error.WriteLine("symetrie : "+(isSymetric==0?"false":"true"));
                }
                
                
                //infos globales de la grille, initialisation
                GrilleDatas grilleDatas = new GrilleDatas();
                grilleDatas.nbrVides = 0;
                grilleDatas.scores = new List<int>(4);
                for (int i = -1; i < opponentCount; i++)
                    grilleDatas.scores.Add(0);
                grilleDatas.maxScore = 0;
                
                //tableau des infos de chaque case (intÃ©rÃªt, appartenance, ...)
                CaseDatas[,] casesDatas = new CaseDatas[width,height];
                
                //intialisation des infos pour les cases
                for(int j=0;j<height;j++)
                {
                    for(int i=0;i<width;i++)
                    {
                        casesDatas[i,j].appartenance = -1;
                        
                        //par dÃ©faut on sait dÃ©jÃ  que les cases sur les cÃ´tÃ©s ne pourront pas avoir leurs 8 voisins vides
                        if(i==0 || i==width-1 || j==0 || j==height-1)
                            casesDatas[i,j].tagRoomCentre = false;
                        else
                            casesDatas[i,j].tagRoomCentre = true;
                            
                        casesDatas[i,j].tagRoomStart = 0;
                        casesDatas[i,j].tagRoomEdge = false;
                        casesDatas[i,j].roomSize = 0;
                        casesDatas[i,j].roomOrigin = -1;
                    }
                }
               
                //lecture des cases, renseignement des infos
                    
                for (int j = 0; j < height; j++)
                {
                    string line = Console.ReadLine(); // One line of the map ('.' = free, '0' = you, otherwise the id of the opponent)
                    
                    for(int i=0; i< width; i++)
                    {
                        if(line[i]=='.')//vide
                        {
                            //casesData[i,j] = -1;//dÃ©jÃ  renseignÃ© par dÃ©faut en vide
                            grilleDatas.nbrVides++;
                        }
                        else
                        {
                            int playerID = int.Parse(line[i].ToString());
                            casesDatas[i,j].appartenance = playerID;
                            grilleDatas.scores[playerID]++;
                            
                            //score max check et update
                            if(grilleDatas.scores[playerID]>grilleDatas.maxScore)
                                grilleDatas.maxScore = grilleDatas.scores[playerID];
                        }
                    }
                }
                
                //on entoure les case des adversairs libre d'un tag pour pas les parcourir par sÃ©curitÃ© en mode Room
                for(int i=0;i<positions.Count;i++)
                {
                    if(i==0)
                        continue;
                        
                    for(int jj=positions[i].y-1;jj<=positions[i].y+1;jj++)
                    {
                        for(int ii=positions[i].x-1;ii<=positions[i].x+1;ii++)
                        {
                            if(0<=ii && ii<width && 0<=jj && jj<height && casesDatas[ii,jj].appartenance==-1)
                            {
                                if(!(ii==x && jj==y))
                                    casesDatas[ii,jj].appartenance=10;
                            }
                        }
                    }
                }
                
                //cas particulier gameRound 1
                if(gameRound==1 && (x==0 || x==width-1 || y==0 || y==height-1))
                {
                    mode = MODE.room;
                    roomStartCase.x = x;
                    roomStartCase.y = y;
                }
                
                if(mode==MODE.room)// on va s'obliger Ã  fermer virtuellement certaines cases pour pas faire de zones trop grandes
                {
                    Console.Error.WriteLine("roomStartCase : "+roomStartCase.x+" "+roomStartCase.y);
                    int startX = -1;
                    int startY = -1;
                    int offset = 0;
                    
                    //TODO il faut amÃ©liorer cette partie
                    
                    //rÃ©glage de la taille des zones Ã  fermer
                    if(opponentCount==1)
                        offset = ((gameRound>nbrManchesMax/3)?8:10);//paramÃ©trage Ã  rÃ©gler
                    else if(opponentCount==2)
                        offset = ((gameRound>nbrManchesMax/3)?8:9);//paramÃ©trage Ã  rÃ©gler
                    else 
                        offset = ((gameRound>nbrManchesMax/3)?8:8);//paramÃ©trage Ã  rÃ©gler
                
                    if(roomStartCase.x>=offset)
                        startX = roomStartCase.x-offset;
                    if(roomStartCase.y>=offset)
                        startY = roomStartCase.y-offset;
                    
                    for(int j=startY;j<startY+2*offset;j++)
                    {
                        int xx = startX;
                        if(xx!=-1 && 0<=j && j<height)
                            casesDatas[xx,j].appartenance = 10;
                            
                        xx = startX+2*offset;
                        if(xx<width && 0<=j && j<height)
                            casesDatas[xx,j].appartenance = 10;
                    }
                    
                    for(int i=startX;i<startX+2*offset;i++)
                    {
                        int yy = startY;
                        if(yy!=-1 && 0<=i && i<width)
                            casesDatas[i,yy].appartenance = 10;
                            
                        yy = startY+2*offset;
                        if(yy<height && 0<=i && i<width)
                            casesDatas[i,yy].appartenance = 10;
                    }
                }
                    
                   
                //tag pour trouver les centres des rooms
                
                for (int j = 0; j < height; j++)
                {
                    for(int i=0; i< width; i++)
                    {
                        if(casesDatas[i,j].appartenance!=-1)
                        {
                            for(int jj=j-1;jj<=j+1;jj++)
                            {
                                for(int ii=i-1;ii<=i+1;ii++)
                                {
                                    if(0<=ii && ii<width && 0<=jj && jj<height)
                                    {
                                        //gestion du tag RoomStart
                                        if(casesDatas[i,j].appartenance==0)
                                        {
                                            casesDatas[ii,jj].tagRoomStart++;
                                            casesDatas[i,j].tagRoomStart+=5;
                                            
                                            //gestion de tag roomCentre
                                            casesDatas[i,j].tagRoomCentre=false;
                                        
                                        }
                                        else
                                        {
                                            //gestion de tag roomCentre
                                            casesDatas[ii,jj].tagRoomCentre=false;//les voisins d'une case prise ne peuvent Ãªtre un centre de room
                                        }
                                        
                                    }
                                }
                            }
                        }
                    }
                }
                
                //ici les centres de room sont bien renseignÃ©s
                
                //calcul des roomSize
                Dictionary<int,List<Case>> rooms = new Dictionary<int,List<Case>>();
               
                for (int j = 0; j < height; j++)
                {
                    for(int i=0; i< width; i++)
                    {
                        if(casesDatas[i,j].tagRoomCentre)
                        {
                            //check voisin haut si il a des origines
                            if(casesDatas[i,j-1].tagRoomCentre)
                            {
                                casesDatas[i,j].roomOrigin = casesDatas[i,j-1].roomOrigin;
                                rooms[casesDatas[i,j].roomOrigin].Add(new Case(i,j));
                                
                                //on doit checker le voisin de gauche voir si il a aussi une origine peut Ãªtre diffÃ©rente
                                if(casesDatas[i-1,j].tagRoomCentre && casesDatas[i-1,j].roomOrigin!=casesDatas[i,j].roomOrigin)
                                {
            	                    //dans ce cas on va convertir les cases de la room de gauche Ã  celle du haut
                                    List<Case> casesRoomGauche = rooms[casesDatas[i-1,j].roomOrigin];
                                    int roomGaucheOrigin = casesDatas[i-1,j].roomOrigin;
                                    
                                    foreach(Case c in casesRoomGauche)//maj des infos de la room de gauche et rajout dans celle du haut
                                    {
                                        casesDatas[c.x,c.y].roomOrigin = casesDatas[i,j].roomOrigin;
                                        rooms[casesDatas[i,j].roomOrigin].Add(new Case(c.x,c.y));
                                    }
                                    
                                    //destruction de la room de gauche
                                    rooms.Remove(roomGaucheOrigin);
                                }
                            }
                            else if(casesDatas[i-1,j].tagRoomCentre)//check voisin gauche sinon
                            {
                                casesDatas[i,j].roomOrigin = casesDatas[i-1,j].roomOrigin;
                                rooms[casesDatas[i,j].roomOrigin].Add(new Case(i,j));
                            }
                            else//on crÃ©e une nouvelle room
                            {
                                casesDatas[i,j].roomOrigin = i*100+j;
                                rooms.Add(casesDatas[i,j].roomOrigin,new List<Case>());
                                rooms[casesDatas[i,j].roomOrigin].Add(new Case(i,j));
                            }
                        }
                    }
                }
                
                //pour finir, on parcours les rooms du dictionary pour leur affecter leur taille
                foreach (KeyValuePair<int, List<Case>> pair in rooms)
            	{
            	    int nbr = pair.Value.Count;
            	    foreach(Case c in pair.Value)
            	        casesDatas[c.x,c.y].roomSize = nbr;
            	    
            	    Console.Error.WriteLine("roomSizeBonus : "+nbr);
            	}
                
                //lecture des centres et gestion du tag roomEdge
                
                //on mettra de cÃ´tÃ© les cases de bord de room pour faciliter la recherche du plus proche plus tard
                Dictionary<int,List<Case>> roomsEdges = new Dictionary<int,List<Case>>();
                List<Case> roomEdges = new List<Case>();
                List<int> roomEdges_int = new List<int>();//pour check Contains plus simple
                for (int j = 0; j < height; j++)
                {
                    for(int i=0; i< width; i++)
                    {
                        if(casesDatas[i,j].tagRoomCentre)
                        {
                            if(!roomsEdges.ContainsKey(casesDatas[i,j].roomOrigin))
                            {
                                roomsEdges.Add(casesDatas[i,j].roomOrigin,new List<Case>());
                            }
                            
                            //on check les voisins pour voir si il s'agit de bords
                            for(int jj=j-1;jj<=j+1;jj++)
                            {
                                for(int ii=i-1;ii<=i+1;ii++)
                                {
                                    if(!casesDatas[ii,jj].tagRoomCentre && ((ii==x && jj==y) || casesDatas[ii,jj].appartenance==-1))//bord trouvÃ©
                                    {
                                        casesDatas[ii,jj].tagRoomEdge = true;
                                        
                                        //rajout du roomSize et de l'origine de la room comme infos
                                        casesDatas[ii,jj].roomSize = casesDatas[i,j].roomSize;
                                        casesDatas[ii,jj].roomOrigin = casesDatas[i,j].roomOrigin;
                                        
                                        roomsEdges[casesDatas[ii,jj].roomOrigin].Add(new Case(ii,jj));
                                        
                                        //si pas dÃ©jÃ  rajoutÃ© dans la liste des bords de room
                                        //et que son tagStart est infÃ©rieur Ã  trois
                                        if(casesDatas[ii,jj].tagRoomStart<=3 && !roomEdges_int.Contains(ii*100+jj))
                                        {
                                            roomEdges_int.Add(ii*100+jj);
                                            roomEdges.Add(new Case(ii,jj));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            
                //a ce niveau roomEdges contient les possibles points d'entrÃ©es de toutes rooms confondues
                //et roomsEdges contient les bords pour chaque room (pas super bien choisis les noms ici, m'enfin pas grave Ã  ce niveau)
                
               
                //verification si case atteinte prise
                if(objectifCase.x!=-1 && casesDatas[objectifCase.x,objectifCase.y].appartenance!=-1)
                {
                    //TODO vÃ©rif si objectif atteint de type roomEdge
                    if(x==objectifCase.x && y==objectifCase.y && casesDatas[objectifCase.x,objectifCase.y].tagRoomEdge)
                    {
                        if(mode!=MODE.room)
                        {
                            //entrÃ©e dans la room
                            roomStartCase.x = x;
                            roomStartCase.y = y;
                        }
                        mode=MODE.room;
                    }
                    else
                    {
                        mode=MODE.search;
                    }
                    
                    objectifCase.x = -1;
                    objectifCase.y = -1;
                }
                
                //vÃ©rification si room finie
                if(mode==MODE.room && gameRound>1)
                {
                    //si on a fait un bond en score
                    if(grilleDatas.scores[0] > historique[gameRound-1].grilleDatas.scores[0]+1)
                        mode = MODE.search;
                        
                    //si on est entourÃ© de cases non vides, on a soit fini, soit on a Ã©tÃ© bloquÃ© d'une faÃ§on ou d'une autre
                    if((x==width-1 || casesDatas[x+1,y].appartenance!=-1)
                    && (x==0 || casesDatas[x-1,y].appartenance!=-1)
                    && (y==0 || casesDatas[x,y-1].appartenance!=-1)
                    && (y==height-1 || casesDatas[x,y+1].appartenance!=-1))
                        mode = MODE.search;
                        
                    //si le bonus de gain devient trop bas (ennemi qui parcours notre room)
                    if(casesDatas[x,y].roomOrigin != -1 && casesDatas[x,y].roomSize <4)
                        mode = MODE.search;
                        
                }
                
                //si room non finie Ã  priori
                if(mode==MODE.room)
                {
                    //on contourne les cases jusqu'Ã  espÃ©rer pouvoir fermer la room
                    
                    
                    int minStart = 50;
                    
                    //sens de recherche
                    List<int> order = new List<int>();
                    if(xAdvMoy > x)
                    {
                        if(yAdvMoy > y)
                        {
                            if(y-yAdvMoy > x-xAdvMoy)
                            {
                                order.Add(2);
                                order.Add(3);
                                order.Add(0);
                                order.Add(1);
                            }
                            else
                            {
                                order.Add(3);
                                order.Add(2);
                                order.Add(1);
                                order.Add(0);
                            }
                        }
                        else
                        {
                            if(yAdvMoy-y > x-xAdvMoy)
                            {
                                order.Add(2);
                                order.Add(1);
                                order.Add(0);
                                order.Add(3);
                            }
                            else
                            {
                                order.Add(1);
                                order.Add(2);
                                order.Add(3);
                                order.Add(0);
                            }
                        }
                    }
                    else
                    {
                        if(yAdvMoy > y)
                        {
                            if(y-yAdvMoy > xAdvMoy-x)
                            {
                                order.Add(0);
                                order.Add(3);
                                order.Add(2);
                                order.Add(1);
                            }
                            else
                            {
                                order.Add(3);
                                order.Add(0);
                                order.Add(1);
                                order.Add(2);
                            }
                        }
                        else
                        {
                            if(yAdvMoy-y > xAdvMoy-x)
                            {
                                order.Add(0);
                                order.Add(1);
                                order.Add(2);
                                order.Add(3);
                            }
                            else
                            {
                                order.Add(1);
                                order.Add(0);
                                order.Add(3);
                                order.Add(2);
                            }
                        }
                    }
                    
                    for(int i=0;i<4;i++)
                    {
                        //lecture des voisins pour trouver le tagRoomEdge avec le plus petit roomStart
                        if(order[i]==0 && x>0 && casesDatas[x-1,y].tagRoomEdge && casesDatas[x-1,y].tagRoomStart < minStart)
                        {
                            minStart = casesDatas[x-1,y].tagRoomStart;
                            objectifCase.x = x-1;
                            objectifCase.y = y;
                        }
                        if(order[i]==1 && y>0 && casesDatas[x,y-1].tagRoomEdge && casesDatas[x,y-1].tagRoomStart < minStart)
                        {
                            minStart = casesDatas[x,y-1].tagRoomStart;
                            objectifCase.x = x;
                            objectifCase.y = y-1;
                        }
                        if(order[i]==2 && x<width-1 && casesDatas[x+1,y].tagRoomEdge && casesDatas[x+1,y].tagRoomStart < minStart)
                        {
                            minStart = casesDatas[x+1,y].tagRoomStart;
                            objectifCase.x = x+1;
                            objectifCase.y = y;
                        }
                        if(order[i]==3 && y<height-1 && casesDatas[x,y+1].tagRoomEdge && casesDatas[x,y+1].tagRoomStart < minStart)
                        {
                            minStart = casesDatas[x,y+1].tagRoomStart;
                            objectifCase.x = x;
                            objectifCase.y = y+1;
                        }
                        
                        if(objectifCase.x!=-1)
                            break;
                    }
                    
                    if(minStart==50)
                        mode = MODE.search;
                        
                }
                
                //si mode recherche activÃ©
                if(mode==MODE.search)
                {
                    //on recherche la room la plus proche Ã  une distance et taille respectable
                    
                    if(roomEdges.Count>0)
                    {
                        double minDistance = 10000;
                        Case minCase = new Case(-1,-1);
                        foreach(Case c in roomEdges)
                        {
                            if(c.x!=x || c.y!=y)
                            {
                                bool alreadyStarted = false;
                                List<Case> edges = roomsEdges[casesDatas[c.x,c.y].roomOrigin];
                                foreach(Case c2 in edges)
                                {
                                    if((c2.x!=x || c2.y!=y) && casesDatas[c2.x,c2.y].tagRoomStart>=2)
                                    {
                                        alreadyStarted = true;   
                                    }
                                }
                            
                                double a = Math.Sqrt(Math.Pow(x-c.x,2)+Math.Pow(y-c.y,2));
                                
                                //premier check, distance
                                if(((a<8 && casesDatas[c.x,c.y].roomSize>=1) || (a<17 && casesDatas[c.x,c.y].roomSize>=4)))
                                {
                                    //bonus de recherche si room dÃ©jÃ  entamÃ©e et gain superieur Ã  3 attendu
                                    if(alreadyStarted && casesDatas[c.x,c.y].roomSize>3)
                                    {
                                        if(opponentCount==1)
                                            a-=30;
                                        else 
                                            a-=200;
                                    }
                                    
                                    //bonus en fonction de la taille de la room
                                    a-=casesDatas[c.x,c.y].roomSize;
                                    
                                    if(a < minDistance)//paramÃ¨tre Ã  adapter en fonction des tests
                                    {
                                        minDistance = a;
                                        minCase = new Case(c.x,c.y);
                                    }
                                }
                            }
                        }
                        
                        //si on a trouvÃ© quelquechose
                        if(minDistance!=10000)
                        {
                            //pour cette room Ã  distance min, on va vÃ©rifier si on a pas dÃ©jÃ  des cases Ã  nous pour changer l'objectif
                            double minDistance2 = 10000;
                            Case minCase2 = new Case(-1,-1);
                            List<Case> edges = roomsEdges[casesDatas[minCase.x,minCase.y].roomOrigin];
                            
                            foreach(Case c in edges)
                            {
                                if((c.x!=x || c.y!=y) && casesDatas[c.x,c.y].tagRoomStart>=2)
                                {
                                    double a = Math.Sqrt(Math.Pow(x-c.x,2)+Math.Pow(y-c.y,2));
                                    if(a < minDistance2)
                                    {
                                        minDistance2 = a;
                                        minCase2 = new Case(c.x,c.y);
                                    }
                                }
                            }
                            
                            //si la distance n'est pas bcp plus grande que minCase on prend cette nouvelle case
                            if(minDistance2 != 10000 && minDistance2-minDistance < 8)//paramÃ¨tre Ã  changer en fonction des tests
                            {
                                objectifCase.x = minCase2.x;
                                objectifCase.y = minCase2.y;
                            }
                            else
                            {
                                objectifCase.x = minCase.x;
                                objectifCase.y = minCase.y;
                            }
                        }
                    }
                    
                    if(objectifCase.x==-1) // on recherche la case vide la plus proche
                    {
                        double minDistance = 10000;
                        Case minCase = new Case(-1,-1);
                        for (int j = 0; j < height; j++)
                        {
                            for(int i=0; i< width; i++)
                            {  
                                if(casesDatas[i,j].appartenance == -1)
                                {
                                    double a = Math.Sqrt(Math.Pow(x-i,2)+Math.Pow(y-j,2));
                                    if(a < minDistance)
                                    {
                                        minDistance = a;
                                        minCase = new Case(i,j);
                                    }
                                }
                            }
                        }
                        
                        objectifCase.x = minCase.x;
                        objectifCase.y = minCase.y;
                    }
                }
                
                //cas particulier, si on est en conflit avec un ennemi, on revient en arriÃ¨re
                if(gameRound>1 && casesDatas[x,y].appartenance==-1)
                {
                    Console.Error.WriteLine("Conflit dÃ©tectÃ©");
                    objectifCase.x = historique[gameRound-1].positions[0].x;
                    objectifCase.y = historique[gameRound-1].positions[0].y;
                }
                
                Console.Error.WriteLine("Mode : "+(mode==MODE.room?"room":"search"));
                
                if(objectifCase.x==-1)//si aucun objectif tourvÃ©, mode random
                {
                    Console.Error.WriteLine("Random");
                    Random random = new Random();
                
                    //sortie random
                    Console.WriteLine(random.Next(0, width)+" "+random.Next(0, height)); // action: "x y" to move or "BACK rounds" to go back in time
                }
                else
                {
                    //sortie
                    Console.WriteLine(objectifCase.x+" "+objectifCase.y); // action: "x y" to move or "BACK rounds" to go back in time
                }
                
                
                //fin round, maj de l'historique 
                lastGameRound = gameRound;
                HistoryV6 hNew = new HistoryV6();
                hNew.gameRound = gameRound;
                hNew.grilleDatas = grilleDatas;
                hNew.mode = mode;
                hNew.objectifCase.x = objectifCase.x;
                hNew.objectifCase.y = objectifCase.y;
                hNew.roomStartCase.x = roomStartCase.x;
                hNew.roomStartCase.y = roomStartCase.y;
                hNew.positions = new List<Case>();
                foreach(Case c in positions)
                    hNew.positions.Add(new Case(c.x,c.y));
                
                historique.Add(hNew);
            }
        }
        else//ancien algo, pas mal de bordel ^^' ! Fais des rectangles et attaque en 1v1
        {
            int sizeX = 35;
            int sizeY = 20;
            int[][] statutCase = new int[sizeX][];
            for(int i=0; i<sizeX; i++)
            {
                statutCase[i] = new int[sizeY];
            }
            
            int nbrManchesMax;
            if(opponentCount==1)
                nbrManchesMax=350;
            else if(opponentCount==2)
                nbrManchesMax=300;
            else 
                nbrManchesMax=250;
                
            int isSymetric = -1;//0 non, 1 oui
                
            bool roomMode = true;//mode qui permet de dire Ã  notre IA de s'occuper de finaliser une room
            int sensTourne = 0;//0-> indÃ©fini, 1-> horaire, 2-> antihoraire
            int currentDirection = 0;//N,E,S,W
            
            //variables Ã  trier 
            
            List<bool> backInTimeUsed = new List<bool>();
            backInTimeUsed.Add(false);
            for (int i = 0; i < opponentCount; i++)
            {
                backInTimeUsed.Add(false);
            }
            
            List<Historic> history = new List<Historic>();
            
            Case objectifCase = new Case(-1,-1);
            int lastGameRound = 0;
            
            bool firstRoom = true;
            Case firstPosition = new Case(-1,-1);
            
                
            int nextNeedChangeDir = 0;
            int scorePrec = 1;
            
            int directionChanged = 0;
            int roomStartX = -1;
            int roomStartY = -1;
            int timeInRoom = 0;
            
            int checkVoisinAdv = 0;
            
            bool backAdv = false;
            
            bool oneVSone = false;
            
            //cas particulier 1v1
            if(opponentCount==1)
            {
                roomMode = false;
                firstRoom = false;
                oneVSone = true;
            }
            
            // game loop
            while (true)
            {
                int gameRound = int.Parse(Console.ReadLine());
                
                Console.Error.WriteLine("PART A");
                Console.Error.WriteLine("gameRound : " + gameRound);
                
                //correction du gameRound Ã  caue du referee qui le fait sauter au max d'un coup si le jeu est fini avant le nombre max de rounds
                gameRound = Math.Min(lastGameRound+1,gameRound);
                
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]); // Your x position
                int y = int.Parse(inputs[1]); // Your y position
                int backInTimeLeft = int.Parse(inputs[2]); // Remaining back in time
                backInTimeUsed[0] = backInTimeLeft==0?true:false;
                
                //initialisation de l'info sur la position de dÃ©part
                if(firstPosition.x==-1)
                {
                    firstPosition.x = x;
                    firstPosition.y = y;
                }
                
                List<int> scores = new List<int>();
                scores.Add(0);
                List<Case> positions = new List<Case>();
                List<double> distanceAdv = new List<double>();
                positions.Add(new Case(x,y));
                backAdv = false;
                for (int i = 0; i < opponentCount; i++)
                {
                    scores.Add(0);
                    inputs = Console.ReadLine().Split(' ');
                    int opponentX = int.Parse(inputs[0]); // X position of the opponent
                    int opponentY = int.Parse(inputs[1]); // Y position of the opponent
                    int opponentBackInTimeLeft = int.Parse(inputs[2]); // Remaining back in time of the opponent
                    positions.Add(new Case(opponentX,opponentY));
            
                    //gestion backInTime ennemi
                    if(!backInTimeUsed[i+1] && opponentBackInTimeLeft==0)//backInTime utilisÃ© par un adversaire
                    {
                        if(lastGameRound!=gameRound-1)//backInTime appliquÃ©
                        {
                            Console.Error.WriteLine("BackInTime dÃ©tectÃ©");
                    
                            backInTimeUsed[i+1] = true;
                            backAdv = true;
                            
                            //mise Ã  jour du cmportement de notre IA par rapport Ã  l'historique
                            //on prendr Ã  2 round de diffÃ©rence Ã  cause du dÃ©calage
                            
                            //cas particulier, si c'est le round 1, on revient Ã  zÃ©ro
                            if(gameRound==1)
                            {
                                //cas particulier 1v1
                                if(opponentCount==1)
                                {
                                    roomMode = false;
                                    firstRoom = false;
                                    oneVSone = true;
                                }
                                else
                                {
                                    roomMode = true;
                                    firstRoom = true;
                                    currentDirection = 0;
                                    sensTourne = 0;
                                    timeInRoom = 0;
                                }
                                
                                history.Clear();
                            }
                            else
                            {
                                roomMode = history[gameRound-2].roomMode;
                                firstRoom = history[gameRound-2].firstRoom;
                                sensTourne = history[gameRound-2].sens;
                                currentDirection = history[gameRound-2].direction;
                                timeInRoom = history[gameRound-2].timeRoom;
                                oneVSone = history[gameRound-2].oneVSone;
                                objectifCase.x = -1;
                                objectifCase.y = -1;
                            
                                //purge de la suite de l'historic
                                while(history.Count>gameRound)
                                    history.RemoveAt(history.Count-1);
                            }
                        }
                        
                    }
                    
                }
                
                //vÃ©rification si positionnement de dÃ©part symÃ©trique
                if(isSymetric==-1)
                {
                    //on considÃ¨re symÃ©trie si ennemi Ã  la position inverse de nous
                    for (int i = 1; i < opponentCount; i++)
                    {
                        if(positions[i].x == 34-positions[0].x && positions[i].y == 19-positions[0].y)
                        {
                            isSymetric = 1;
                        }
                    }
                    
                    if(isSymetric==-1)
                        isSymetric = 0;
                        
                    Console.Error.WriteLine("symetrie : "+isSymetric);
                }
                
                Console.Error.WriteLine("PART B");
                
                int maxScoreAdv = 0;
                int nbrVides = 0;
                
                //lecture du terrain
                for (int j = 0; j < sizeY; j++)
                {
                    string line = Console.ReadLine(); // One line of the map ('.' = free, '0' = you, otherwise the id of the opponent)
                    
                    for(int i=0; i<sizeX; i++)
                    {
                        if(line[i]=='.')//vide
                        {
                            statutCase[i][j] = -1;
                            nbrVides++;
                        }
                        else if(line[i]=='0')//moi
                        {
                            statutCase[i][j] = 0;
                            scores[0]++;
                        }
                        else//adversaire
                        {
                            statutCase[i][j] = int.Parse(line[i].ToString());
                            scores[int.Parse(line[i].ToString())]++;
                            
                            //score max des adversaires
                            if(scores[int.Parse(line[i].ToString())]>maxScoreAdv)
                                maxScoreAdv = scores[int.Parse(line[i].ToString())];
                        }
                    }
                }
                
                //reset objectifCase si atteinte
                if(objectifCase.x == x && objectifCase.y==y)
                {
                    roomMode = true;///activation du roomMode pour vÃ©rification
                    objectifCase.x=-1;
                    objectifCase.y=-1;
                }
                
                Console.Error.WriteLine("PART C");
                
                //verif si room finie
                if(roomMode)
                {
                    Console.Error.WriteLine("PART C1");
                
                    bool roomFinish = false;
                    
                    if((x==34 || statutCase[x+1][y]!=-1)
                    && (x==0 || statutCase[x-1][y]!=-1)
                    && (y==0 || statutCase[x][y-1]!=-1)
                    && (y==19 || statutCase[x][y+1]!=-1))
                        roomFinish = true;
                    
                    if(scores[0]>scorePrec+2)//si on a fait un bond en score, c'est qu'on a fini la room
                        roomFinish = true;
                
                    if(roomFinish)
                    {
                        Console.Error.WriteLine("Room terminÃ©e");
                        
                        roomMode = false;
                        firstRoom = false;
                        objectifCase.x = -1;
                        objectifCase.y = -1;
                        currentDirection = 0;
                        timeInRoom = 0;
                        
                        if(opponentCount==1)
                            oneVSone = true;
                        
                        nextNeedChangeDir = 0;
                        directionChanged = 0;
                        roomStartX = -1;
                        roomStartY = -1;
                    }
                }
                
                Console.Error.WriteLine("PART D");
                
                if(roomMode)//recherche de la prochaine position
                {
                    
                    Console.Error.WriteLine("PART D1");
                
                    bool forceChangeDir= false;
                    timeInRoom++;
                    
                    
                    //vÃ©rification si l'on passe depuis 3 tours Ã  cÃ´tÃ© d'un voisin alors qu'on tente de tourner vers lui
                    if(sensTourne==1)
                    {
                        if((currentDirection==1 && x<34 && statutCase[x+1][y]>0)
                        ||(currentDirection==2 && y<19 && statutCase[x][y+1]>0)
                        ||(currentDirection==3 && x>0 && statutCase[x-1][y]>0)
                        ||(currentDirection==4 && y>0 && statutCase[x][y-1]>0))
                            checkVoisinAdv++;
                        
                        if(checkVoisinAdv==3)
                        {
                            sensTourne = 2;
                            forceChangeDir = true;
                        }
                    }
                    else
                    {
                        if((currentDirection==1 && x>0 && statutCase[x-1][y]>0)
                        ||(currentDirection==2 && y>0 && statutCase[x][y-1]>0)
                        ||(currentDirection==3 && x<34 && statutCase[x+1][y]>0)
                        ||(currentDirection==4 && y<19 && statutCase[x][y+1]>0))
                            checkVoisinAdv++;
                        
                        if(checkVoisinAdv==3)
                        {
                            sensTourne = 1;
                            forceChangeDir = true;
                        }
                    }
                    
                    
                    //vÃ©rification si l'on tente de passer proche de nos cases dÃ©jÃ  prises
                    if(currentDirection==1)
                    {
                        if(x>0 && statutCase[x-1][y]==0)
                        {
                            checkVoisinAdv++;
                            
                            if(checkVoisinAdv==4)
                            {   
                                sensTourne = 1;
                                forceChangeDir = true;
                            }
                        }
                        else if(x<34 && statutCase[x+1][y]==0)
                        {
                            checkVoisinAdv++;
                            
                            if(checkVoisinAdv==4)
                            {   
                                sensTourne = 2;
                                forceChangeDir = true;
                            }
                        }
                    }
                    else if(currentDirection==3)
                    {
                        if(x>0 && statutCase[x-1][y]==0)
                        {
                            checkVoisinAdv++;
                            
                            if(checkVoisinAdv==4)
                            {   
                                sensTourne = 2;
                                forceChangeDir = true;
                            }
                        }
                        if(x<34 && statutCase[x+1][y]==0)
                        {
                            checkVoisinAdv++;
                            
                            if(checkVoisinAdv==4)
                            {   
                                sensTourne = 1;
                                forceChangeDir = true;
                            }
                        }
                    }
                    else if(currentDirection==4)
                    {
                        if(y>0 && statutCase[x][y-1]==0)
                        {
                            checkVoisinAdv++;
                            
                            if(checkVoisinAdv==4)
                            {   
                                sensTourne = 2;
                                forceChangeDir = true;
                            }
                        }
                        if(y<19 && statutCase[x][y+1]==0)
                        {
                            checkVoisinAdv++;
                            
                            if(checkVoisinAdv==4)
                            {   
                                sensTourne = 1;
                                forceChangeDir = true;
                            }
                        }
                    }
                    else if(currentDirection==2)
                    {
                        if(y>0 && statutCase[x][y-1]==0)
                        {
                            checkVoisinAdv++;
                            
                            if(checkVoisinAdv==4)
                            {   
                                sensTourne = 1;
                                forceChangeDir = true;
                            }
                        }
                        if(y<19 && statutCase[x][y+1]==0)
                        {
                            checkVoisinAdv++;
                            
                            if(checkVoisinAdv==4)
                            {   
                                sensTourne = 2;
                                forceChangeDir = true;
                            }
                        }
                    }
                    
                   
                    //traitement spÃ©cial premiÃ¨re room
                    if(firstRoom)
                    {
                        if(gameRound==1)//initialisation, avec inversion  si back in time utilisÃ© par adversaire
                        {
                            if(firstPosition.x<=17 && firstPosition.y<10)//on dÃ©bute en haut Ã  gauche
                            {
                                if(17-firstPosition.x < 10-firstPosition.y)//si milieu vertical plus proche du milieu horizontal
                                {
                                    sensTourne = (backAdv)?2:1;//hor
                                    currentDirection = (backAdv)?3:2;//E
                                }
                                else
                                {
                                    sensTourne = (backAdv)?1:2;//ahor
                                    currentDirection = (backAdv)?2:3;//S
                                }
                            }
                            else if(firstPosition.x<=17 && firstPosition.y>=10)//on dÃ©bute en bas Ã  gauche
                            {
                                if(17-firstPosition.x < firstPosition.y-10)//si milieu vertical plus proche du milieu horizontal
                                {
                                    sensTourne = (backAdv)?1:2;//ahor
                                    currentDirection = (backAdv)?1:2;//E
                                }
                                else
                                {
                                    sensTourne = (backAdv)?2:1;//hor
                                    currentDirection = (backAdv)?2:1;//N
                                }
                            }
                            else if(firstPosition.x>17 && firstPosition.y>=10)//on dÃ©bute en bas Ã  droite
                            {
                                if(firstPosition.x-17 < firstPosition.y-10)//si milieu vertical plus proche du milieu horizontal
                                {
                                    sensTourne = (backAdv)?2:1;//hor
                                    currentDirection = (backAdv)?1:4;//O
                                }
                                else
                                {
                                    sensTourne = (backAdv)?1:2;//ahor
                                    currentDirection = (backAdv)?4:1;//N
                                }
                            }
                            else//on dÃ©bute en haut Ã  droite
                            {
                                if(firstPosition.x-17 < 10-firstPosition.y)//si milieu vertical plus proche du milieu horizontal
                                {
                                    sensTourne = (backAdv||isSymetric==0)?1:2;//ahor
                                    currentDirection = (backAdv||isSymetric==0)?3:4;//O
                                }
                                else
                                {
                                    sensTourne = (backAdv||isSymetric==0)?2:1;//hor
                                    currentDirection = (backAdv||isSymetric==0)?4:3;//S
                                }
                            }
                        }
                    }
                    else// en dehors de la premiÃ¨re room
                    {
                        if(currentDirection==0)//arrivÃ©e dans la room Ã  fermer
                        {
                            //lire historique derniÃ¨re position pour voir d'oÃ¹ on vient
                            Case last = new Case(history[gameRound-2].x,history[gameRound-2].y);
                            
                            if(x>last.x)//on vient de la gauche
                            {
                                bool alreadyOne = false;
                                if(last.y>0 && statutCase[last.x][last.y-1]==0)
                                {
                                    alreadyOne = true;
                                    currentDirection = 3;
                                    sensTourne = 2;
                                    roomStartX = x; 
                                }
                                if(last.y<19 && statutCase[last.x][last.y+1]==0)
                                {
                                    currentDirection = 1;
                                    sensTourne = 1;
                                    roomStartX = x; 
                                    
                                    if(alreadyOne)
                                        currentDirection = 1;
                                }
                            }
                            else if(y>last.y)//on vient du haut
                            {
                                bool alreadyOne = false;
                                if(last.x>0 && statutCase[last.x-1][last.y]==0)
                                {
                                    alreadyOne = true;
                                    currentDirection = 2;
                                    sensTourne = 1;
                                    roomStartY = y; 
                                }
                                if(last.x<34 && statutCase[last.x+1][last.y]==0)
                                {
                                    currentDirection = 4;
                                    sensTourne = 2;
                                    roomStartY = y; 
                                    
                                    if(alreadyOne)
                                        currentDirection = 3;
                                }
                            }
                            else if(x<last.x)//on vient de la droite
                            {
                                bool alreadyOne = false;
                                if(last.y>0 && statutCase[last.x][last.y-1]==0)
                                {
                                    alreadyOne = true;
                                    currentDirection = 3;
                                    sensTourne = 1;
                                    roomStartX = x; 
                                }
                                if(last.y<19 && statutCase[last.x][last.y+1]==0)
                                {
                                    currentDirection = 1;
                                    sensTourne = 2;
                                    roomStartX = x; 
                                    
                                    if(alreadyOne)
                                        currentDirection = 3;
                                }
                            }
                            else//on vient du bas
                            {
                                bool alreadyOne = false;
                                if(last.x>0 && statutCase[last.x-1][last.y]==0)
                                {
                                    alreadyOne = true;
                                    currentDirection = 2;
                                    sensTourne = 2;
                                    roomStartY = y; 
                                }
                                if(last.x<34 && statutCase[last.x+1][last.y]==0)
                                {
                                    currentDirection = 4;
                                    sensTourne = 1;
                                    roomStartY = y; 
                                    
                                    if(alreadyOne)
                                        currentDirection = 3;
                                }
                            }
                        }
                    }
                    
                    //recherche de la prochaine case de la room actuelle
                    //il faut pouvoir contourner les possibles obstacles
                    
                    //TODO contournement
                    
                    //cas particulier, si je n'ai pas rÃ©ussi Ã  activer la case sur laquelle je suis, donc un autre joueur avec moi, je reviens en arriÃ¨re
                    if(statutCase[x][y]!=0 && gameRound>1)
                    {
                        objectifCase.x = history[gameRound-2].x;
                        objectifCase.y = history[gameRound-2].y;
                        nextNeedChangeDir = 2;
                    }
                    else
                    {
                        int nbrChangeDir = 0;
                        
                        //force changement de direction aprÃ¨s un certains moments
                        if((opponentCount==1 && timeInRoom%8==0)
                        || (!firstRoom && isSymetric==1 && timeInRoom%8==0)
                        || (!firstRoom && isSymetric==0 && timeInRoom%10==0)
                        || (firstRoom && isSymetric==0 && timeInRoom%10==0))
                            forceChangeDir = true;
                        
                        //gestion de la firstRoom
                        if(firstRoom && isSymetric==1)
                        {
                            if(backInTimeLeft==0)//si back utilisÃ©, on rÃ©duit la firstRoom
                            {
                                if(firstPosition.x<=17 && firstPosition.y<10)//on dÃ©bute en haut Ã  gauche
                                {
                                    if((currentDirection==1 && y==2)
                                    ||(currentDirection==2 && x==15)
                                    ||(currentDirection==3 && y==9)
                                    ||(currentDirection==4 && x==2))
                                        forceChangeDir = true;
                                }
                                else if(firstPosition.x<=17 && firstPosition.y>=10)//on dÃ©bute en bas Ã  gauche
                                {
                                    if((currentDirection==1 && y==10)
                                    ||(currentDirection==2 && x==15)
                                    ||(currentDirection==3 && y==17)
                                    ||(currentDirection==4 && x==2))
                                        forceChangeDir = true;
                                }
                                else if(firstPosition.x>17 && firstPosition.y>=10)//on dÃ©bute en bas Ã  droite
                                {
                                    if((currentDirection==1 && y==10)
                                    ||(currentDirection==2 && x==19)
                                    ||(currentDirection==3 && y==17)
                                    ||(currentDirection==4 && x==32))
                                        forceChangeDir = true;
                                }
                                else//on dÃ©bute en haut Ã  droite
                                {
                                    if((currentDirection==1 && y==2)
                                    ||(currentDirection==2 && x==19)
                                    ||(currentDirection==3 && y==9)
                                    ||(currentDirection==4 && x==32))
                                        forceChangeDir = true;
                                }
                            }
                            else
                            {
                                if(firstPosition.x<=17 && firstPosition.y<10)//on dÃ©bute en haut Ã  gauche
                                {
                                    //on doit rester dans la zone x<=17 et y<10
                                    if((currentDirection==3 && y==((opponentCount>2)?9:10))
                                    ||(currentDirection==2 && x==17))
                                        forceChangeDir = true;
                                }
                                else if(firstPosition.x<=17 && firstPosition.y>=10)//on dÃ©bute en bas Ã  gauche
                                {
                                    //on doit rester dans la zone x<=17 et y>10
                                    if((currentDirection==1 && y==((opponentCount>2)?10:9))
                                    ||(currentDirection==2 && x==17))
                                        forceChangeDir = true;
                                }
                                else if(firstPosition.x>17 && firstPosition.y>=10)//on dÃ©bute en bas Ã  droite
                                {
                                    //on doit rester dans la zone x>=17 et y>10
                                    if((currentDirection==1 && y==((opponentCount>2)?10:9))
                                    ||(currentDirection==4 && x==17))
                                        forceChangeDir = true;
                                }
                                else//on dÃ©bute en haut Ã  droite
                                {
                                    //on doit rester dans la zone x>=17 et y<10
                                    if((currentDirection==3 && y==((opponentCount>2)?9:10))
                                    ||(currentDirection==4 && x==17))
                                        forceChangeDir = true;
                                }
                            }
                        }
                        
                        //tant qu'on a pas trouvÃ© un objectif, on vÃ©rifie si l'on doit tourner (limitÃ© aux 3 changements de direction possibles)
                        while(objectifCase.x==-1 && nbrChangeDir<4)
                        {
                            //vÃ©rification si prochaine case Ã  plus d'un voisin
                            if(currentDirection==1 && y>0)
                            {
                                if((x==0 || statutCase[x-1][y-1]!=-1)//voisin de gauche
                                && (x==34 || statutCase[x+1][y-1]!=-1))//voisin de gauche
                                    forceChangeDir = true;
                            }
                            else if(currentDirection==3 && y<19)
                            {
                                if((x==0 || statutCase[x-1][y+1]!=-1)//voisin de gauche
                                && (x==34 || statutCase[x+1][y+1]!=-1))//voisin de gauche
                                    forceChangeDir = true;
                            }
                            else if(currentDirection==4 && x>0)
                            {
                                if((y==0 || statutCase[x-1][y-1]!=-1)//voisin du haut
                                && (y==19 || statutCase[x-1][y+1]!=-1))//voisin du bas
                                    forceChangeDir = true;
                            }
                            else if(currentDirection==2 && x<34)
                            {
                                if((y==0 || statutCase[x+1][y-1]!=-1)//voisin du haut
                                && (y==19 || statutCase[x+1][y+1]!=-1))//voisin du bas
                                    forceChangeDir = true;
                            }
                            
                            //vÃ©rification si autorisation de continuer dans la direction actuelle
                            //affectation de l'objectif
                            if(!forceChangeDir
                            &&((currentDirection==1 && y>0 && statutCase[x][y-1]==-1)
                            ||(currentDirection==3 && y<19 && statutCase[x][y+1]==-1)
                            ||(currentDirection==2 && x<34 && statutCase[x+1][y]==-1)
                            ||(currentDirection==4 && x>0 && statutCase[x-1][y]==-1)))
                            {
                                objectifCase.x = x;
                                objectifCase.y = y;
                                switch(currentDirection)
                                {
                                case 1:
                                    objectifCase.y = y-1;
                                    break;
                                case 2:
                                    objectifCase.x = x+1;
                                    break;
                                case 3:
                                    objectifCase.y = y+1;
                                    break;
                                case 4:
                                    objectifCase.x = x-1;
                                    break;
                                }
                            }
                            else//sinon changement de direction
                            {
                                nbrChangeDir++;
                                if(sensTourne==1)//horaire
                                {
                                    currentDirection = ((currentDirection)%4)+1;
                                }
                                else//antihoraire
                                {
                                    currentDirection--;
                                    if(currentDirection==0)
                                        currentDirection=4;
                                }
                                
                                timeInRoom = 0;//obligatoire pour permettre la prochain virage
                                checkVoisinAdv = 0;
                            }
                            
                            forceChangeDir = false;
                        }
                        
                        //si on a pas trouvÃ© d'objectif, ce'st forcÃ©ment que la room nous a Ã©tÃ© bloquÃ©e d'une faÃ§on qu'on a pas su gÃ©rer ici
                        if(objectifCase.x==-1)
                        {
                            //on dÃ©sactive le mode room pour repasser en mode recherche
                            roomMode = false;
                            firstRoom = false;
                            currentDirection = 0;
                            timeInRoom = 0;
                            if(opponentCount==1)
                                oneVSone = true;
                        }
                    }
                }
                
                Console.Error.WriteLine("PART E");
                
                //gestion de blocage (aller-retour sur derniers mouvements)
                if(objectifCase.x!=-1 && gameRound>7)//ne s'applique que si on a un objectif en cours
                {
                    //TODO Check historique
                    
                    int nbrOccurences = 0;
                    int x1 = history[gameRound-2].x;
                    int y1 = history[gameRound-2].y;
                    for(int i=3;i<=7;i++)
                    {
                        if(history[gameRound-i].x == x1 && history[gameRound-i].y == y1)
                        {
                            nbrOccurences++;
                        }
                    }
                    
                    //si blocage
                    //supprimer les cases qui bloquent le process, objectifCase reinit pour nouvelle boucle de recherche
                    
                    if(nbrOccurences>=2)
                    {
                        Console.Error.WriteLine("blocage dÃ©tectÃ©");
                        objectifCase.x = -1;
                        objectifCase.y = -1;
                        roomMode = false;
                        for(int i=2;i<=7;i++)
                        {
                            statutCase[history[gameRound-i].x][history[gameRound-i].y]=-2;
                            nbrVides--;
                        }
                    }
                    
                }
                    
                Console.Error.WriteLine("PART F");
                
                if(oneVSone)
                {
                    //calcul distance entre moi et l'adversaire
                    
                    double a = Math.Pow(positions[1].x-x,2)+Math.Pow(positions[1].y-y,2);
                    double dist = Math.Sqrt(a);
                    
                    //on sort de ce cas si il reste X case vides
                    if(nbrVides<20*35/4)
                    {
                        oneVSone = false;
                    }
                    else if(dist>3)//si on est trop loin on va tenter de se raprocher de lui
                    {
                        //il ne faut surtout pas essayer de s'en rapprocher en le ciblant directement car le dÃ©placement sera horizontal avant d'Ãªtre vertical, ce qui nous arrange pas
                        if((x<positions[1].x-1 || positions[1].x+1<x)  && (y<positions[1].y-1 || positions[1].y+1<y) )
                        {
                            if(gameRound%20<=9)
                            {
                                if(y<positions[1].y)
                                {
                                    objectifCase.x=x;
                                    objectifCase.y=y+1;
                                }
                                else
                                {
                                    objectifCase.x=x;
                                    objectifCase.y=y-1;
                                }
                            }
                            else
                            {
                                if(x<positions[1].x)
                                {
                                    objectifCase.x=x+1;
                                    objectifCase.y=y;
                                }
                                else
                                {
                                    objectifCase.x=x-1;
                                    objectifCase.y=y;
                                }
                            }
                        }
                        else
                        {
                            if(Math.Abs(positions[1].x-x) < Math.Abs(positions[1].y-y))
                            {
                                if(y<positions[1].y)
                                {
                                    objectifCase.x=x;
                                    objectifCase.y=y+1;
                                }
                                else
                                {
                                    objectifCase.x=x;
                                    objectifCase.y=y-1;
                                }
                            }
                            else
                            {
                                if(x<positions[1].x)
                                {
                                    objectifCase.x=x+1;
                                    objectifCase.y=y;
                                }
                                else
                                {
                                    objectifCase.x=x-1;
                                    objectifCase.y=y;
                                }
                            }
                        }
                    }
                    else//passage en mode recherche de room
                    {
                        oneVSone = false;
                    }
                }
                 
                //vÃ©rification si on doit rechercher une case vide
                if( !oneVSone &&
                ((!roomMode && objectifCase.x!=-1 && statutCase[objectifCase.x][objectifCase.y]!=-1)//si la case qu'on cherchait Ã  Ã©tÃ© remplie
                || (!roomMode && nbrVides>=1 && objectifCase.x==-1)))//si pas d'objectif en cours
                {
                    
                    Console.Error.WriteLine("PART F1");
                    
                    
                    //prÃ©paration calcul de distances
                    double distance = 10000;
                    List<double> minCaseDistance = new List<double>();
                    List<Case> minCase = new List<Case>();
                    
                    minCaseDistance.Add(10000);
                    minCase.Add(new Case(-1,-1));
                    for (int i = 0; i < opponentCount; i++)
                    {
                        minCaseDistance.Add(10000);
                        minCase.Add(new Case(-1,-1));
                    }
                    
                    objectifCase.x = -1;
                    objectifCase.y = -1;
                    currentDirection = 0;
                    
                    Case minCaseSave = new Case(-1,-1);
                    
                    while(nbrVides>=1 && objectifCase.x==-1)
                    {
                        //recherche d'un plus proche
                        
                        //mode de recherche prÃ©fÃ©rant N,E,S,W en focntion des situations
                        int xx=0;
                        int yy=0;
                        if(y<10)
                        {
                            yy = sizeY-1;
                        }
                        while(true)
                        {
                            if(yy<0 || yy>sizeY-1)
                                break;
                            
                            if(x<=17)
                                xx = sizeX-1;
                            else
                                xx = 0;
                            
                            while(true)
                            {
                                if(xx<0 || xx>sizeX-1)
                                    break;
                                    
                                if(statutCase[xx][yy]==-1)//vide
                                {
                                    if(!(xx==x && yy==y))
                                    {
                                        double a = Math.Pow(xx-x,2)+Math.Pow(yy-y,2);
                                        if(a < minCaseDistance[0]*minCaseDistance[0])
                                        {
                                            minCaseDistance[0] = Math.Sqrt(a);
                                            minCase[0] = new Case(xx,yy);
                                        }
                                    }
                                    
                                    //cases min des adversaires
                                    for (int ii = 1; ii <= opponentCount; ii++)
                                    {
                                        double a = Math.Pow(xx-positions[ii].x,2)+Math.Pow(yy-positions[ii].y,2);
                                        if(a < minCaseDistance[ii]*minCaseDistance[ii])
                                        {
                                            minCaseDistance[ii] = Math.Sqrt(a);
                                            minCase[ii] = new Case(xx,yy);
                                        }
                                    }
                                }
                                
                                if(x<=17)
                                    xx--;
                                else
                                    xx++;
                            }
                            
                            if(y<10)
                                yy--;
                            else
                                yy++;
                        }
                        
                        //recherche qui n'a pas aboutie
                        if(minCase[0].x==-1)
                        {
                            nbrVides--;
                            break;
                        }
                    
                        //enregistrement de la premiÃ¨re case trouvÃ©e
                        if(minCaseSave.x==-1)
                        {
                            minCaseSave.x = minCase[0].x;
                            minCaseSave.y = minCase[0].y;
                        }
                        
                        //vÃ©rification par rapport aux adversaires
                        for (int i = 1; i <= opponentCount; i++)
                        {
                            //adversaire avec minCase plus proche de nous, on prend pas le risque de choisir cette case du coup
                            if(minCase[i].x == minCase[0].x && minCaseDistance[i] <= minCaseDistance[0])
                            {
                                statutCase[minCase[0].x][minCase[0].y]=-2;//on l'Ã©limine du process
                                nbrVides--;
                                break;
                            }
                        }
                        
                        //on a trouvÃ© la case min qu'on voulait
                        if(statutCase[minCase[0].x][minCase[0].y]!=-2)
                        {
                            objectifCase.x = minCase[0].x;
                            objectifCase.y = minCase[0].y;
                        }
                    }
                    
                    //si on a pas trouvÃ© d'objectif min pratique
                    if(objectifCase.x==-1)
                    {
                        //on prend le plus proche quand mÃªme si il existe
                        if(minCaseSave.x!=-1)
                        {
                            objectifCase.x = minCaseSave.x;
                            objectifCase.y = minCaseSave.y;
                        }
                    }
                }
                
                Console.Error.WriteLine("PART G");
                
                //gestion historique
                lastGameRound = gameRound;
                scorePrec = scores[0];
                history.Add(new Historic(x,y,currentDirection,sensTourne,roomMode,firstRoom,timeInRoom,oneVSone));
                
                if(objectifCase.x==-1)//si on a pas trouvÃ© d'objectif,  au hasard
                {
                    Console.Error.WriteLine("Random");
                
                    Random random = new Random();
                    objectifCase.x = random.Next(0, 35);
                    objectifCase.y = random.Next(0, 20);
                    
                    Console.WriteLine(objectifCase.x+" "+objectifCase.y); // action: "x y" to move or "BACK rounds" to go back in time
            
                    objectifCase.x = -1;//important, on doit tout de suite remettre l'objectif Ã  -1 pour forcer la prochaine recherche
                    objectifCase.y = -1;
                }
                else
                {
                    Console.WriteLine(objectifCase.x+" "+objectifCase.y); // action: "x y" to move or "BACK rounds" to go back in time
                }
                
            }
        }
    }
}
