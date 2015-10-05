##### #     ##### ##### #####       ####  #####
#     #     #   # #     #           #   # #
#     #     ##### ##### #####       ####  #
#     #     #   #     #     #       #   # #
##### ##### #   # ##### #####       ##### #####

# You'll find the most important features commented, enjoy :)

import sys
import math

time_left=0
todo=[]
non_dec=0
already=0
offset=0
adest=-1
bdest=-1
mode=0
front=[]
opponent_count = int(input())
n=20
m=35
test=[(1,0),(-1,0),(0,1),(0,-1)]
val=[[-1 for j in range(m)] for i in range(n)]
color=-1
nbback=opponent_count

# Euclidian distance
def dist(a,b,x,y):
    return(abs(a-x)+abs(b-y))
 
# Potential score calculation
def score_test():
    h=[[0 for j in range(m+2)] for i in range(n+2)]
    ll=[]
    for i in range(n):
        for j in range(m):
            if t[i][j]=='.':h[i+1][j+1]=-1
            elif t[i][j]=='0':h[i+1][j+1]=0
            else:
                h[i+1][j+1]=1
                ll+=[(i+1,j+1)]
    for i in range(n+2):
        h[i][0]=1
        h[i][m+1]=1
        ll+=[(i,0)]
        ll+=[(i,m+1)]
    for j in range(m+2):
        h[0][j]=1
        h[n+1][j]=1
        ll+=[(0,j)]
        ll+=[(n+1,j)]
    while ll!=[]:
        a,b=ll[0]
        for (d,e) in [(-1,-1),(-1,1),(1,-1),(1,1),(-1,0),(1,0),(0,-1),(0,1)]:
            if 0<=a+d<n+2 and 0<=b+e<m+2 and h[a+d][b+e]==-1:
                ll+=[(a+d,b+e)]
                h[a+d][b+e]=1
        ll.remove((a,b))
    scoring=0
    for i in range(1,n+1):
        for j in range(1,m+1):
            if h[i][j]==-1:scoring+=1
    return(scoring)
    
# When trying to go in an opponent zone, check whether there is an ennemy (1v1 only)
def verif(x,y,d,e):
    a,b=opp[0]
    if d==1:return(a>x)
    if d==-1:return(a<x)
    if e==1:return(b>y)
    if e==-1:return(b<y)
    
# Minimum distance of a zone to an ennemy
def min_dist_opp(x,y):
    min_dist=10000
    for (ww1,ww2) in opp:
        xx=dist(x,y,ww1,ww2)
        if xx<min_dist:min_dist=xx
    return(min_dist)
    
# Maximal distance of a zone to an ennemy
def max_dist_opp(x,y):
    max_dist=-1
    for (ww1,ww2) in opp:
        xx=dist(x,y,ww1,ww2)
        if xx>max_dist:max_dist=xx
    return(max_dist)

while 1:
    if time_left>0:time_left-=1
    game_round = int(input())
    if game_round>18:non_dec=0
    y,x, back_in_time_left = [int(i) for i in input().split()]
    opp=[]
    for i in range(opponent_count):
        opponent_y, opponent_x, opponent_back_in_time_left = [int(j) for j in input().split()]
        opp+=[(opponent_x,opponent_y)]
    t=[]
    for i in range(20):
        h=[j for j in input()]
        t+=[h]
		
    score=[0,0,0,0]
    vide=[0,0,0]
	
	# Calculation in early game where are the empty places (1v1v1)
    for i in range(n):
        for j in range(m):
            if t[i][j]!='.' and t[i][j]!='0':
                if j<11:vide[0]+=1
                if j>23:vide[2]+=1
                if 11<j<23:vide[1]+=1
				
	# Actual score calculation
    for i in range(20):
        for j in range(35):
            aa=t[i][j]
            if aa!='.':score[int(aa)]+=1
			
	# Back in time if score is too low after game_round 60
    if score[0]<70 and game_round>60 and back_in_time_left==1:
        back=0
        print("BACK 25")
    else:
        if mode>0: # If there are moves left to do
            a,b=todo[mode]
            print(b,a,mode)
            mode-=1
        else:
            ccc=False
			
			# Early game 1v1v1 feature to see where to go
            if game_round<22 and opponent_count==2 and non_dec<8:
                a,b=opp[0]
                c,d=opp[1]
                if 10<y<24 and 10<b<24 and 10<d<24:
                    if y>=17 and vide[2]<10:
                        print(y+1,x,"cas 5")
                        ccc=True
                    elif vide[0]<10:
                        print(y-1,x,"cas 6")
                        ccc=True
                if ccc==False and y>10 and b>10 and d>10 and vide[0]<10 and (not (y>20 and b<24 and d<24)):
                    print(y-1,x,"cas 1")
                    ccc=True
                elif ccc==False and y<24 and b<24 and d<24 and vide[2]<10 and (not (y<14 and b>10 and d>10)):
                    print(y+1,x,"cas 2")
                    ccc=True
                elif ccc==False and (y<11 or y>23) and (b<11 or b>23) and (d<11 or d>23) and vide[1]<10:
                    if y<11 and (b<11 or d<11):
                        print(y+1,x,"cas 3")
                        ccc=True
                    elif y>23 and (b>23 or d>23):
                        print(y-1,x,"cas 4")
                        ccc=True
						
			# Early game 1v1v1v1 feature to see where to go
            if game_round<20 and opponent_count==3 and non_dec<6:
                a,b=opp[0]
                c,d=opp[1]
                e,f=opp[2]
                if x<10 and y<17 and ((a<9 and b<12) or (c<9 and d<12) or (e<9 and f<12)):
                    if (a>9 or b<10) and (c>9 or d<21) and (e>9 or f<21):
                        print(y+1,x,"cas 1.1")
                        ccc=True
                    elif (a<10 or b>13) and (c<10 or d>13) and (e<10 or f>13):
                        print(y,x+1,"cas 1.2")
                        ccc=True
                elif x>9 and y>17 and ((a>10 and b>22) or (c>10 and d>22) or (e>10 and f>22)):
                    if (a>9 or b<21) and (c>9 or d<21) and (e>9 or f<21):
                        print(y,x-1,"cas 2.1")
                        ccc=True
                    elif (a<10 or b>13) and (c<10 or d>13) and (e<10 or f>13):
                        print(y-1,x,"cas 2.2")
                        ccc=True
                elif x<10 and y>17 and ((a<9 and b>22) or (c<9 and d>22) or (e<9 and f>22)):
                    if (a>9 or b>13) and (c>9 or d>13) and (e>9 or f>13):
                        print(y-1,x,"cas 3.1")
                        ccc=True
                    elif (a<10 or b<21) and (c<10 or d<21) and (e<10 or f<21):
                        print(y,x+1,"cas 3.2")
                        ccc=True
                elif x>9 and y<17 and ((a>10 and b<12) or (c>10 and d<12) or (e>10 and f<12)):
                    if (a>9 or b>13) and (c>9 or d>13) and (e>9 or f>13):
                        print(y,x-1,"cas 4.1")
                        ccc=True
                    elif (a<10 or b<21) and (c<10 or d<21) and (e<10 or f<21):
                        print(y+1,x,"cas 4.2")
                        ccc=True
                    
            if ccc:offset+=1
            if ccc:non_dec=0
            else:non_dec+=1
            if ccc==False:
                bbb=True
				
				# Check if I can improve my score by going in a line with 1, 2, 3, 4 or 5 cases in any direction
                if ((opponent_count==1 and game_round>42) or (opponent_count==2 and game_round>50+offset) or (opponent_count==3 and game_round>50+offset)):
                    for (d,e) in test:
                        if 0<=x+d<n and 0<=y+e<m and t[x+d][y+e]=='.' and bbb:
                            t[x+d][y+e]='0'
                            st=score_test()
                            t[x+d][y+e]='.'
                            if st>2:
                                print(y+e,x+d,st,1)
                                bbb=False
                    for (d,e) in test:
                        if 0<=x+2*d<n and 0<=y+2*e<m and t[x+d][y+e]=='.' and t[x+2*d][y+2*e]=='.' and bbb:
                            t[x+d][y+e]='0'
                            t[x+2*d][y+2*e]='0'
                            st=score_test()
                            t[x+d][y+e]='.'
                            t[x+2*d][y+2*e]='.'
                            if st>12:
                                print(y+e,x+d,st,2)
                                bbb=False
                    for (d,e) in test:
                        if 0<=x+3*d<n and 0<=y+3*e<m and t[x+d][y+e]=='.' and t[x+2*d][y+2*e]=='.' and t[x+3*d][y+3*e]=='.' and bbb:
                            t[x+d][y+e]='0'
                            t[x+2*d][y+2*e]='0'
                            t[x+3*d][y+3*e]='0'
                            st=score_test()
                            t[x+d][y+e]='.'
                            t[x+2*d][y+2*e]='.'
                            t[x+3*d][y+3*e]='.'
                            if (opponent_count==1 and st>26) or (opponent_count==2 and st>20) or (opponent_count==3 and st>15):
                                print(y+e,x+d,st,3)
                                bbb=False
                    for (d,e) in test:
                        if 0<=x+4*d<n and 0<=y+4*e<m and t[x+d][y+e]=='.' and t[x+2*d][y+2*e]=='.' and t[x+3*d][y+3*e]=='.' and t[x+4*d][y+4*e]=='.' and bbb:
                            t[x+d][y+e]='0'
                            t[x+2*d][y+2*e]='0'
                            t[x+3*d][y+3*e]='0'
                            t[x+4*d][y+4*e]='0'
                            st=score_test()
                            t[x+d][y+e]='.'
                            t[x+2*d][y+2*e]='.'
                            t[x+3*d][y+3*e]='.'
                            t[x+4*d][y+4*e]='.'
                            if (opponent_count==1 and st>30) or (opponent_count==2 and st>20) or (opponent_count==3 and st>18):
                                print(y+e,x+d,st,3)
                                bbb=False
                    for (d,e) in test:
                        if opponent_count>=2 and 0<=x+5*d<n and 0<=y+5*e<m and t[x+d][y+e]=='.' and t[x+2*d][y+2*e]=='.' and t[x+3*d][y+3*e]=='.' and t[x+4*d][y+4*e]=='.' and t[x+5*d][y+5*e]=='.' and bbb:
                            t[x+d][y+e]='0'
                            t[x+2*d][y+2*e]='0'
                            t[x+3*d][y+3*e]='0'
                            t[x+4*d][y+4*e]='0'
                            t[x+5*d][y+5*e]='0'
                            st=score_test()
                            t[x+d][y+e]='.'
                            t[x+2*d][y+2*e]='.'
                            t[x+3*d][y+3*e]='.'
                            t[x+4*d][y+4*e]='.'
                            t[x+5*d][y+5*e]='.'
                            if (opponent_count==1 and st>40) or (opponent_count==2 and st>30) or (opponent_count==3 and st>20):
                                print(y+e,x+d,st,3)
                                bbb=False
								
				# Feature to check and maybe go if there exists an ennemy zone nearby
                if bbb and time_left==0 and opponent_count!=3:
                    for (d,e) in test:
                        if bbb and 0<=x+3*d<n and 0<=y+3*e<m and 0<=x+2*e<n and 0<=x-2*e<n and 0<=y+2*d<m and 0<=y-2*d<m:
                            col=t[x+d][y+e]
                            if col!='.' and col!='0' and t[x+d+e][y+e+d]==col and t[x+d+2*e][y+e+2*d]==col and t[x+d-e][y+e-d]==col and t[x+d-2*e][y+e-2*d]==col:
                                if t[x+2*d+e][y+2*e+d]=='.' and t[x+2*d+2*e][y+2*e+2*d]=='.' and t[x+2*d-e][y+2*e-d]=='.' and t[x+2*d-2*e][y+2*e-2*d]=='.' and t[x+2*d][y+2*e]=='.':
                                    if (opponent_count!=1 or verif(x,y,d,e)) and t[x+3*d+e][y+3*e+d]=='.' and t[x+3*d+2*e][y+3*e+2*d]=='.' and t[x+3*d-e][y+3*e-d]=='.' and t[x+3*d-2*e][y+3*e-2*d]=='.' and t[x+3*d][y+3*e]=='.':
                                        ggg=True
                                        if score[0]<90 and game_round<85:
                                            if True:
                                                mode=3
                                                todo=[(1,1),(x,y),(x+d,y+e),(x+2*d,y+2*e)]
                                                time_left=20
                                            else:ggg=False
                                        elif already<2 or opponent_count==1:
                                            mode=1
                                            todo=[(1,1),(x+2*d,y+2*e)]
                                            time_left=20
                                        else:ggg=False
                                        if ggg:
                                            bbb=False
                                            already+=1
                                            print(y+e,x+d)
                if bbb:
                    val=[[-1 for j in range(m)] for i in range(n)]
                    color=0
                    for i in range(n):
                        for j in range(m):
                            if t[i][j]=='.':val[i][j]=0
							
					# In early game this feature draws deadzone in the map
					# val reprensents the zones (calculated after), for the moment just adding -1 to decide not to go where I shouldn't
                    if ((opponent_count==1 and game_round<53) or (opponent_count==2 and game_round<70+offset) or game_round<55+offset) and score[0]<79:
                        if opponent_count==3:
                            for i in range(n):
                                for j in range(13,22):
                                    val[i][j]=-1
                            for j in range(m):
                                if x>=10:val[9][j]=-1
                                else:val[10][j]=-1
                        elif opponent_count==2:
                            for i in range(n):
                                val[i][11]=-1
                                val[i][23]=-1
                            for j in range(m):
                                val[0][j]=-1
                                val[1][j]=-1
                                val[18][j]=-1
                                val[19][j]=-1
                        else:
                            for i in range(n):
                                if y<17:val[i][18]=-1
                                else:val[i][16]=-1
                                val[i][0]=-1
                                val[i][1]=-1
                                val[i][2]=-1
                                val[i][3]=-1
                                #val[i][4]=-1
                                val[i][31]=-1
                                #val[i][30]=-1
                                val[i][34]=-1
                                val[i][33]=-1
                                val[i][32]=-1
                    if opponent_count==1:
                        for i in range(n):
                            val[i][17]=-1
                    if (opponent_count==1 and game_round<85 and (score[0]<90 or score[1]<90)):
                        for i in range(n):
                            val[i][0]=-1
                            val[i][1]=-1
                            val[i][2]=-1
                            val[i][3]=-1
                            #val[i][4]=-1
                            val[i][31]=-1
                            #val[i][30]=-1
                            val[i][34]=-1
                            val[i][33]=-1
                            val[i][32]=-1
                            #val[i][16]=-1
                            val[i][17]=-1
                            #val[i][18]=-1
                            if score[0]>=90:
                                val[i][20]=-1
                                val[i][19]=-1
                                val[i][15]=-1
                                val[i][14]=-1
                                val[i][16]=-1
                                val[i][18]=-1
                                val[i][13]=-1
                                val[i][21]=-1
                        for j in range(m):
                            val[16][j]=-1
                            val[17][j]=-1
                            val[18][j]=-1
                            val[19][j]=-1
                            val[0][j]=-1
                            val[1][j]=-1
                            val[2][j]=-1
                            val[3][j]=-1
							
					# Deletion of single line paths
                    for i in range(n):
                        for j in range(m):
                            if val[i][j]!=-1:
                                tes=[0,0,0,0]
                                nb=0
                                for k in range(4):
                                    d,e=test[k]
                                    if 0<=i+d<n and 0<=j+e<m and (val[i+d][j+e]==val[i][j] or t[i+d][j+e]=='0'):
                                        tes[k]=1
                                        nb+=1
                                if (nb==2 and tes[0]*tes[1]+tes[2]*tes[3]==1) or (nb<=1):
                                    val[i][j]=-1
                    
                    cc=False
					# Check if there exists a zone (it may not exist because I created deadzones, especially along line paths)
                    for i in range(n):
                        for j in range(m):
                            if val[i][j]!=-1:cc=True
                            
                    if cc:
						# Calculation of the zones (Check by BFS the connected components)
                        for i in range(n):
                            for j in range(m):
                                if val[i][j]==0:
                                    color+=1
                                    val[i][j]=color
                                    ll=[(i,j)]
                                    while(ll!=[]):
                                        a,b=ll[0]
                                        for (d,e) in test:
                                            if 0<=a+d<n and 0<=b+e<m and val[a+d][b+e]==0:
                                                ll+=[(a+d,b+e)]
                                                val[a+d][b+e]=color
                                        ll.remove((a,b))
                        puiss=[0 for k in range(color+1)] # Reprensent initially the surface of the zone
                        disti=[100000 for k in range(color+1)] # My distance to the zone
                        diste=[100000 for k in range(color+1)] # The shortest distance of an ennemy to the zone
                        fronti=[0 for k in range(color+1)] # Border of the zone to fill (without the border with my color)
                        for i in range(n):
                            for j in range(m):
                                if val[i][j]!=-1:
                                    dis=dist(x,y,i,j)
                                    k=val[i][j]
                                    puiss[k]+=1
                                    disti[k]=min(disti[k],dis)
                                    diste[k]=min(diste[k],min_dist_opp(i,j))
                                    nb=0
                                    for (d,e) in [(-1,-1),(-1,1),(1,-1),(1,1),(-1,0),(1,0),(0,-1),(0,1)]:
                                        if 0<=i+d<n and 0<=j+e<m and (val[i+d][j+e]==val[i][j] or t[i+d][j+e]=='0'):
                                            nb+=1
                                    if nb!=8:fronti[k]+=1
                                    
						# Mix of the parameters to make a greedy choice
                        for k in range(1,color+1):
                            puiss[k]+=3*(puiss[k]//fronti[k])
                            puiss[k]+=(diste[k]//4)-disti[k]
                        kmax=-1
                        vmax=-100000
						# Selection of the zone
                        for k in range(1,color+1):
                            if puiss[k]>vmax:
                                vmax=puiss[k]
                                kmax=k
								
						# If I'm already on a zone, I finish it
                        if val[x][y]>=1:
                            kmax=val[x][y]
						
						# If a zone is near to me (1 case away) go to it
                        for (d,e) in [(-1,-1),(-1,1),(1,-1),(1,1),(-1,0),(1,0),(0,-1),(0,1)]:
                            if 0<=x+d<n and 0<=y+e<m and val[x+d][y+e]>=1:
                                kmax=val[x+d][y+e]
								
						# Find the border cases of the selected zone
                        front=[]
                        for i in range(n):
                            for j in range(m):
                                if val[i][j]==kmax:
                                    nb=0
                                    for (d,e) in [(-1,-1),(-1,1),(1,-1),(1,1),(-1,0),(1,0),(0,-1),(0,1)]:
                                        if 0<=i+d<n and 0<=j+e<m and (val[i+d][j+e]==val[i][j] or t[i+d][j+e]=='0'):
                                            nb+=1
                                    if nb!=8:front+=[(i,j)]
									
						# Go to the nearest case of the border of the zone
						# If two at the same distance go to the one close to the center of the map (needed in early game to avoid being eaten by others)
                        amin=0
                        bmin=0
                        distmin=10000000
                        distcenter=10000000
                        for (a,b) in front:
                            dis=dist(x,y,a,b)
                            dis2=dist(10,17,a,b)
                            if dis<distmin:
                                distmin=dis
                                distcenter=dis2
                                amin=a
                                bmin=b
                            elif dis==distmin and dis2<distcenter:
                                distmin=dis
                                distcenter=dis2
                                amin=a
                                bmin=b
                        if amin!=x or bmin!=y: # If the case choice isn't stupid (stay at the same case)
                            adest=amin
                            bdest=bmin
                            print(bmin,amin)
                        else:
							# If it is, in last resort find the closest case to go
                            amin=0
                            bmin=0
                            distmin=10000000
                            for i in range(n):
                                for j in range(m):
                                    if t[i][j]=='.' and min_dist_opp(i,j)>2:
                                        bb=True
                                        for (d,e) in opp:
                                            if i==d and j==e:bb=False
                                        if bb:
                                            dis=dist(x,y,i,j)
                                            if dis<distmin:
                                                distmin=dis
                                                amin=i
                                                bmin=j
                            print(bmin,amin)
                    else:
						# Same but in the case where no zone was found
                        amin=0
                        bmin=0
                        distmin=10000000
                        for i in range(n):
                            for j in range(m):
                                if t[i][j]=='.' and min_dist_opp(i,j)>2:
                                    bb=True
                                    for (d,e) in opp:
                                        if i==d and j==e:bb=False
                                    if bb:
                                        dis=dist(x,y,i,j)
                                        if dis<distmin:
                                            distmin=dis
                                            amin=i
                                            bmin=j
                        print(bmin,amin)
                                    