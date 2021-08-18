from operator import le
import re
import numpy as np
import math as m
import matplotlib.pyplot as plt
from numpy.core.function_base import linspace
from numpy.lib.function_base import append, hamming

#Script parameters
filename="E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Length of 16 Migration Adaptation period 20 and crossovers mutation rate of 2/I3N3L8A1B005C2_M5AP20_I30P100_LeadersMP.csv"
numberOfIslands=3
numberOfAgents=9
numberOfIterations=30
numberOfMusicalPatterns=100
lengthOfMP=8

#Converts the csv array obtained with the Unity simulations into a numpy array
def csvToArray(name_of_file,convertToFloat=True):
    stringdata=np.genfromtxt(name_of_file, delimiter=';',dtype=str)
    if not convertToFloat:
        return stringdata
    data=np.empty(np.shape(stringdata),dtype=float)
    data[:] = np.nan
    for i in range (np.shape(stringdata)[0]):
        for j in range (np.shape(stringdata)[1]):
            if stringdata[i][j]=='':
                data[i][j]=None
            else:
                data[i][j]=float(stringdata[i][j].replace(',','.'))
    return data

#Return the mean performance of each agent and island during all iterations
def meanPerformance(dataArray,iterationsNumber,musicalPatternNumber,significanceThreshold):
    meanData=np.empty((np.shape(dataArray)[1],musicalPatternNumber+1),dtype=float)
    #"print(np.shape(meanData))
    meanData[:] = np.nan
    for i in range(np.shape(dataArray)[1]):
        meanData[i][0]=dataArray[0][i]
    for i in range(1,musicalPatternNumber+1):
        for j in range(np.shape(dataArray)[1]):
            sum=0
            occurence=0
            for k in range(iterationsNumber):
                if(dataArray[i+k*(musicalPatternNumber+1)][j]!=None):
                    sum+=dataArray[i+k*(musicalPatternNumber+1)][j];
                    occurence+=1
            if(occurence>m.floor(significanceThreshold*iterationsNumber)):
                meanData[j][i]=sum/occurence
    #print(meanData)
    return meanData

#Computes the minimum island total utility for each iteration
def minPerformance(dataArray,iterationsNumber,musicalPatternNumber):
    minData=np.empty(iterationsNumber)
    minData[:]=np.nan
    for k in range(iterationsNumber):
        min=dataArray[1+k*(musicalPatternNumber+1)][0]
        for i in range(1,musicalPatternNumber+1):
            if(dataArray[i+k*(musicalPatternNumber+1)][0]<min):
                min=dataArray[i+k*(musicalPatternNumber+1)][0]
        minData[k]=min
    return minData

#Computes the maximum island total utility for each iteration
def maxPerformance(dataArray,iterationsNumber,musicalPatternNumber):
    maxData=np.empty(iterationsNumber)
    maxData[:]=np.nan
    for k in range(iterationsNumber):
        max=dataArray[1+k*(musicalPatternNumber+1)][0]
        for i in range(1,musicalPatternNumber+1):
            if(dataArray[i+k*(musicalPatternNumber+1)][0]>max):
                min=dataArray[i+k*(musicalPatternNumber+1)][0]
        maxData[k]=min
    return maxData

def meanMinMax(dataArray,iterationsNumber,musicalPatternNumber,significanceThreshold):
    islandUtilityList = meanPerformance(dataArray,iterationsNumber,musicalPatternNumber,significanceThreshold)
    islandMean = np.mean(islandUtilityList[0][1:])
    islandMinUtilityList=minPerformance(dataArray,iterationsNumber,musicalPatternNumber)
    islandMin = np.mean(islandMinUtilityList)
    islandMaxUtilityList=maxPerformance(dataArray,iterationsNumber,musicalPatternNumber)
    islandMax = np.mean(islandMaxUtilityList)
    return islandMean,islandMin,islandMax,islandUtilityList,islandMinUtilityList,islandMaxUtilityList


def migratingAgentsPerformance(dataArray,musicalPatternNumber):
    L=[]
    for i in range(1,np.shape(dataArray)[0]):
        for j in range(np.shape(dataArray)[1]):
            if(not(m.isnan(dataArray[i][j])) and m.isnan(dataArray[i-1][j]) and (i-1)%(musicalPatternNumber+1)!=0):
                L.append([])
                k=0
                while((i+k)<np.shape(dataArray)[0] and not(m.isnan(dataArray[i+k][j]))):
                    L[-1].append(dataArray[i+k][j])
                    k+=1
    return L

def migratingAgentsPatterns(dataArray,PatternsArray,musicalPatternNumber):
    L=[]
    for i in range(1,np.shape(dataArray)[0]):
        for j in range(np.shape(dataArray)[1]):
            if(not(m.isnan(dataArray[i][j])) and m.isnan(dataArray[i-1][j]) and (i-1)%(musicalPatternNumber+1)!=0):
                L.append(([],[]))
                k=0
                #While the migrating agent stays on its new island the musical pattern of the leader is saved
                while((i+k)<np.shape(dataArray)[0] and not(m.isnan(dataArray[i+k][j]))):
                    L[-1][1].append(PatternsArray[i+k-1][2*(j//(numberOfAgents+1))+1])
                    k+=1
                k=-1
                n=0
                agentPos=j%(numberOfAgents+1)
                while(not((i+k)%(musicalPatternNumber+1)==0) and m.isnan(dataArray[i+k][agentPos+(numberOfAgents+1)*n])):
                    n+=1
                    n=n%numberOfIslands
                    if(n==0):
                        k=k-1
                #The snapshot of the former leader's musical pattern
                L[-1][0].append(PatternsArray[i+k-1][2*n+1])
                
    return L

def islandSnapshotDistance(PatternsList):
    L=[]
    for x in PatternsList:
        L.append([])
        for i in range(len(x[1])):
            L[-1].append(hammingDistance(list(x[0][0]),list(x[1][i])))
    return L

def averageSnapshotDistance(distanceList,signficancePercentage=0.5):
    M=np.zeros(numberOfMusicalPatterns)
    L=np.zeros(numberOfMusicalPatterns)
    for i in range(numberOfMusicalPatterns):
        for j in range(len(distanceList)):
            if(i<len(distanceList[j])):
                M[i]+=1
                L[i]+=distanceList[j][i]
    k=0
    while(k<numberOfMusicalPatterns and M[k]/M[0]>signficancePercentage):
        L[k]=L[k]/M[k]
        k+=1
    L=L[0:k]
    return L

def plotAverageSnapShotDistance(averageDistanceList,AP):
    X=np.linspace(1,len(averageDistanceList),len(averageDistanceList))
    plt.plot(X,averageDistanceList,label="average distance of the leader with MPc after a migrating agent arrived in the Island with Ap="+AP)
    plt.legend(loc="lower left")

def migratingAgentsSplitPerf(dataArray,musicalPatternNumber):
    L=[]
    for i in range(1,np.shape(dataArray)[0]):
        for j in range(0,np.shape(dataArray)[1]-1,2):
            if(not(m.isnan(dataArray[i][j]) and m.isnan(dataArray[i][j+1])) and m.isnan(dataArray[i-1][j]) and m.isnan(dataArray[i-1][j+1])):
                L.append([])
                k=0
                while((i+k)<np.shape(dataArray)[0] and not(m.isnan(dataArray[i+k][j] or m.isnan(dataArray[i+k][j+1])))):
                    L[-1].append((dataArray[i+k][j],dataArray[i+k][j+1]))
                    k+=1
    return L

def plotMigAgentsPerf(PerfList,significancePercentage,legend):
    numberOfMigratingAgents=len(PerfList)
    migAgentsPercentage=1
    i=0
    Y=[]
    while(migAgentsPercentage>=significancePercentage):
        sum=0
        agentsLeft=0
        for j in range(len(PerfList)):
            if(len(PerfList[j])>i):
                sum+=PerfList[j][i]
                agentsLeft+=1
        i+=1
        if(agentsLeft==0):
            break
        else:
            migAgentsPercentage=agentsLeft/numberOfMigratingAgents
            Y.append(sum/agentsLeft)
    X=np.linspace(1,len(Y),len(Y))
    plt.plot(X,Y,label=legend)
    plt.legend(loc="upper right")

def plotMigAgentsUtilities(PerfList,significancePercentage):
    numberOfMigratingAgents=len(PerfList)
    migAgentsPercentage=1
    YCur=[]
    YFor=[]
    i=0
    while(migAgentsPercentage>=significancePercentage):
        sumCur=0
        sumFor=0
        agentsLeft=0
        for j in range(len(PerfList)):
            if(len(PerfList[j])>i):
                sumFor+=PerfList[j][i][0]
                sumCur+=PerfList[j][i][1]
                agentsLeft+=1
        i+=1
        if(agentsLeft==0):
            break
        else:
            migAgentsPercentage=agentsLeft/numberOfMigratingAgents
            YCur.append(sumCur/agentsLeft)
            YFor.append(sumFor/agentsLeft)
    X=np.linspace(1,len(YCur),len(YCur))
    plt.plot(X,YCur,label="Migrating agents mean current island utility when arriving on a new Island")
    plt.plot(X,YFor,label="Migrating agents mean former island utility when arriving on a new Island")
    plt.xticks(np.arange(min(X), max(X)+1, 1.0))
    plt.legend(loc="upper right")

def plotMigAgentsDev(PerfList,significancePercentage,legend):
    numberOfMigratingAgents=len(PerfList)
    migAgentsPercentage=1
    i=0
    Dev=[]
    Y=[]
    while(migAgentsPercentage>=significancePercentage):
        Dev=[]
        agentsLeft=0
        for j in range(len(PerfList)):
            if(len(PerfList[j])>i):
                Dev.append(PerfList[j][i])
                agentsLeft+=1
        i+=1
        if(agentsLeft==0):
            break
        else:
            deviation=np.std(Dev)
            migAgentsPercentage=agentsLeft/numberOfMigratingAgents
            Y.append(deviation)
    X=np.linspace(1,len(Y),len(Y))
    plt.plot(X,Y,label=legend)
    plt.legend(loc="upper right")

def weightedMeans(dataArray,islandsNumber,agentsNumber,iterationNumber,mPnumber):
    Means=np.empty(islandsNumber)
    MeansIslandY=np.empty((islandsNumber,mPnumber))
    for i in range(islandsNumber):
        sum=0
        for j in range(1,np.shape(dataArray)[0]):
            weightedRowUtility=0
            agentsHere=0
            for k in range (agentsNumber):
                if(not(m.isnan(dataArray[j][i*(agentsNumber+1)+k+1]))):
                    weightedRowUtility+=dataArray[j][i*(agentsNumber+1)+k+1]
                    agentsHere+=1
            if(agentsHere!=0):
                weightedRowUtility=weightedRowUtility/agentsHere
                MeansIslandY[i][(j-1)%(mPnumber+1)]+=weightedRowUtility
            sum+=weightedRowUtility
        Means[i]=sum
    return Means/(iterationNumber*mPnumber),MeansIslandY/iterationNumber

def mergePerfs(dataVector,musicalPatternNumber):
    Y=np.empty(musicalPatternNumber)
    for i in range(musicalPatternNumber):
        k=0
        while(k*(musicalPatternNumber+1)+i<np.shape(dataVector)[0]):
            Y[i]+=dataVector[k*(musicalPatternNumber+1)+i]
            k+=1
        Y[i]

def splitPatterns(dataArray,iterationNumber,musicalPatternNumber):
    splitData=[]
    for i in range (1,np.shape(dataArray)[1],2):
        splitData.append([])
        for j in range (iterationNumber):
            L=[]
            for k in range (musicalPatternNumber):
                L.append(list(dataArray[j*(musicalPatternNumber+1)+k][i]))
            splitData[-1].append(L)
    return splitData

def extractLeaderPatternsAfterMigration(patternsArray,migAgentsArray):
    L=[[]]
    for i in range(1,np.shape(migAgentsArray)[0]):
        for j in range(np.shape(migAgentsArray)[1]):
            if(not(m.isnan(migAgentsArray[i][j])) and m.isnan(migAgentsArray[i-1][j]) and (i-1)%(numberOfMusicalPatterns+1)!=0):
                L[-1].append([])
                k=0
                while((i+k)<np.shape(migAgentsArray)[0] and not(m.isnan(migAgentsArray[i+k][j]))):
                    L[-1][-1].append(patternsArray[i+k-1][2*(j//(numberOfAgents+1))+1])
                    k+=1
    return L

def extractLeaderNumbersAfterMigration(patternsArray,migAgentsArray):
    L=[]
    for i in range(1,np.shape(migAgentsArray)[0]):
        for j in range(np.shape(migAgentsArray)[1]):
            if(not(m.isnan(migAgentsArray[i][j])) and m.isnan(migAgentsArray[i-1][j]) and (i-1)%(numberOfMusicalPatterns+1)!=0):
                L.append(([j%(numberOfAgents+1)-1],[]))
                k=0
                while((i+k)<np.shape(migAgentsArray)[0] and not(m.isnan(migAgentsArray[i+k][j]))):
                    L[-1][1].append(int(patternsArray[i+k-1][2*(j//(numberOfAgents+1))]))
                    k+=1
    return L

def plotMigratingAgentsLeadership(agentsNumberList):
    L=[]
    for x in agentsNumberList:
        i=0
        while(i<len(x[1]) and not x[0][0]==x[1][i]):
            i+=1
        if(i<len(x[1]) and x[0][0]==x[1][i]):
            L.append(i)
    X=np.linspace(1,numberOfMusicalPatterns,numberOfMusicalPatterns)
    print(len(L))
    plt.hist(L,bins=X,label="Number of migrating agents that took leadership for the first time on this pattern ("+str(len(agentsNumberList))+" migrating agents in total).")
    plt.ylabel('Number of migrating agents')
    plt.xlabel('Musical pattern after arrival');
    plt.legend(loc="upper right")

def plotMigrationsAgentsTakeoverSideBySide(agentsNumberList1,agentsNumberList2):
    L1=[]
    for x in agentsNumberList1:
        i=0
        while(i<len(x[1]) and not x[0][0]==x[1][i]):
            i+=1
        if(i<len(x[1]) and x[0][0]==x[1][i]):
            L1.append(i)
    L2=[]
    for x in agentsNumberList2:
        i=0
        while(i<len(x[1]) and not x[0][0]==x[1][i]):
            i+=1
        if(i<len(x[1]) and x[0][0]==x[1][i]):
            L2.append(i)
    X=np.linspace(1,numberOfMusicalPatterns,numberOfMusicalPatterns)
    plt.hist([L1,L2],bins=X,label=["Migrating agent first takeover with a Ap=20 ("+str(len(agentsNumberList1))+" migrating agents in total).","Migrating agent first takeover with a Ap=100 ("+str(len(agentsNumberList2))+" migrating agents in total)."])
    plt.ylabel('Number of migrating agents')
    plt.xlabel('Musical pattern after arrival');
    plt.legend(loc="upper right")


def autocorrelationPatterns(splitDataArray,significancePercentage=0.3):
    L=[]
    M=[]
    for i in range(len(splitDataArray)):
        L.append(np.zeros(numberOfMusicalPatterns))
        M.append(np.zeros(numberOfMusicalPatterns))
        for j in range(len(splitDataArray[i])):
            for l in range(len(splitDataArray[i][j])):
                M[i][l]+=1
                sum=0
                k=0
                while(l+k<len(splitDataArray[i][j])):
                    sum+=hammingDistance(splitDataArray[i][j][l+k],splitDataArray[i][j][k])
                    k+=1
                if k==0:
                    break
                else:
                    L[-1][l]+=sum/k
        for j in range (len(L[i])):
            if(M[-1][j]/M[-1][0]<significancePercentage):
                L[-1][j]=0
            else:
                L[-1][j]=L[-1][j]/M[-1][j]
    for i in range(len(L)):
        L[i]=(lengthOfMP-2*L[i])/lengthOfMP
    return L

def plotIslandsDistance(splitDataArray):
    L=[]
    X=np.linspace(0,numberOfMusicalPatterns,numberOfMusicalPatterns)
    for i in range(len(splitDataArray)):
        for h in range(i+1,len(splitDataArray)):
            L.append(np.zeros(numberOfMusicalPatterns))
            for j in range(len(splitDataArray[0])):
                for l in range(len(splitDataArray[0][0])):
                    sum=0
                    k=0
                    while(l+k<len(splitDataArray[0][0])):
                        sum+=hammingDistance(splitDataArray[i][j][l+k],splitDataArray[h][j][k])
                        k+=1
                    if k==0:
                        break
                    else:
                        L[-1][l]+=sum/k
            L[-1]=L[-1]/len(splitDataArray[0])
            plt.plot(X,L[-1],label="mean Hamming distance between Island"+str(i)+" and Island"+str(h))
            
    plt.legend(loc="lower right")
    return L

def hammingDistance(ListA,ListB):
    H=0
    for i in range (len(ListA)):
        if not ListA[i]==ListB[i]:
            H+=1
    return H

def plotAutocorrelation(dataArray,customLabel=False,customLegend=""):
    X=np.linspace(0,len(dataArray[0]),len(dataArray[0]))
    for i in range (len(dataArray)):
        if not customLabel:
            plt.plot(X,dataArray[i],label="mean autocorrelation of leader's pattern for Island "+str(i)+" without migrations")
        else:
            plt.plot(X,dataArray[i],label=customLegend)
    plt.xlabel("Musical pattern")
    plt.ylabel("Autocorrelation")
    plt.legend(loc="upper right")


def plotIslandUtilities(dataArray,islandsNumber,agentsnumber,iterationNumber,musicalPatternNumber):
    X=np.linspace(1,musicalPatternNumber,musicalPatternNumber)
    for i in range (islandsNumber):
        plt.plot(X,dataArray[i],label="mean utility of Island "+str(i)+" for an Agent")
            #else:
                #plt.plot(X,dataArray[j][1:],label="agent "+str(j-1))
    plt.legend(loc="upper right")

def baselineSoloJamMean(dataArray, musicalPatternNumber, iterationNumber):
    S=np.zeros([musicalPatternNumber+1,np.shape(dataArray)[1]])
    stdDev=np.zeros(iterationNumber)
    for i in range (iterationNumber):
        S=np.add(S,dataArray[i*(musicalPatternNumber+1):1+i*(musicalPatternNumber+1)+musicalPatternNumber,:])
        stdDev[i]=np.std(dataArray[1+i*(musicalPatternNumber+1):1+i*(musicalPatternNumber+1)+musicalPatternNumber,0])
    S=S/iterationNumber
    S[0,:]=dataArray[0,:]
    return S, stdDev


def plotBaselineSoloJam(dataArray, agentsNumber,musicalPatterNumber):
    X=np.linspace(1,musicalPatterNumber,musicalPatterNumber)
    #print(dataArray)
    S=np.zeros(musicalPatterNumber)
    for i in range(agentsNumber+1):
        if(i==0):
            plt.plot(X,dataArray[1:,i],label="overall utility")
        else:
            plt.plot(X,dataArray[1:,i],label="agent "+str(i-1))
    plt.legend(loc="upper right")



#Gets autocorrelation of a leader's MP after the arrival of a migrating agent
#MigData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period  and crossovers full data/I3N3L8A1B005C2_genP60_M5AP5_I30P100_3.csv",True)
#PatData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period  and crossovers full data/I3N3L8A1B005C2_genP60_M5AP5_I30P100_3_LeadersMP.csv",False)
#MPs=extractLeaderPatternsAfterMigration(PatData,MigData)
#correlationData = autocorrelationPatterns(MPs)
#plotAutocorrelation(correlationData,True,"mean autocorrelation of a leader's pattern after the arrival of a migrating agent with and adaptation period of 5 musical patterns")

#Gets autocorrelation of a leader's MP after the arrival of a migrating agent
#MigData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 20 and crossovers/I3N3L8A1B005C2_M5AP20_I30P100.csv",True)
#PatData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 20 and crossovers/I3N3L8A1B005C2_M5AP20_I30P100_LeadersMP.csv",False)
#MPs=extractLeaderPatternsAfterMigration(PatData,MigData)
#correlationData = autocorrelationPatterns(MPs)
#plotAutocorrelation(correlationData,True,"mean autocorrelation of a leader's pattern after the arrival of a migrating agent with and adaptation period of 20 musical patterns")

#Gets autocorrelation of a leader's MP after the arrival of a migrating agent
#MigData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 100 and crossovers/I3N3L8A1B005C2_M5AP100_I30P100.csv",True)
#PatData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 100 and crossovers/I3N3L8A1B005C2_I30P100_M5AP100_LeadersMP.csv",False)
#MPs=extractLeaderPatternsAfterMigration(PatData,MigData)
#correlationData = autocorrelationPatterns(MPs)
#plotAutocorrelation(correlationData,True,"mean autocorrelation of a leader's pattern after the arrival of a migrating agent with and adaptation period of 100 musical patterns")

#Plots when a migrating agent takes the lead in its new group
MigData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration no Adaptation period crossovers full data/I3N3L8A1B005C2_genP60_M5AP0_I30P100.csv",True)
PatData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration no Adaptation period crossovers full data/I3N3L8A1B005C2_genP60_M5AP0_I30P100_LeadersMP.csv",False)
AgentNumbers1=extractLeaderNumbersAfterMigration(PatData,MigData)
plotMigratingAgentsLeadership(AgentNumbers1)

#Plots when a migrating agent takes the lead in its new group
#MigData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 100 and crossovers/I3N3L8A1B005C2_M5AP100_I30P100.csv",True)
#PatData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 100 and crossovers/I3N3L8A1B005C2_I30P100_M5AP100_LeadersMP.csv",False)
#AgentNumbers2=extractLeaderNumbersAfterMigration(PatData,MigData)

#plotMigrationsAgentsTakeoverSideBySide(AgentNumbers1,AgentNumbers2)



#Plots the autocorrelation of Islands' leader patterns
#data=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/No Migration 3 Islands crossovers full data/I3N3L8A1B005C2_I30P100_LeadersMP.csv",False)
#splitData=splitPatterns(data,numberOfIterations,numberOfMusicalPatterns)
#correlationData = autocorrelationPatterns(splitData)
#plotAutocorrelation(correlationData,False)

#Plots the distance between islands over time
#data=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 20 and crossovers/I3N3L8A1B005C2_M5AP20_I30P100_LeadersMP.csv",False)
#splitData=splitPatterns(data,numberOfIterations,numberOfMusicalPatterns)
#plt.subplot(1, 2, 1)
#_=plotIslandsDistance(splitData)

#Plots the distance between islands over time
#data=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 100 and crossovers/I3N3L8A1B005C2_I30P100_M5AP100_LeadersMP.csv",False)
#splitData=splitPatterns(data,numberOfIterations,numberOfMusicalPatterns)
#plt.subplot(1, 2, 2)
#_=plotIslandsDistance(splitData)

#Plots the distance between the island's leader pattern and the snapshot of the former islands pattern after a migrating agent has arrived
#MigData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 20 and crossovers/I3N3L8A1B005C2_M5AP20_I30P100.csv",True)
#PatData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 20 and crossovers/I3N3L8A1B005C2_M5AP20_I30P100_LeadersMP.csv",False)
#migratingAgentsMP=migratingAgentsPatterns(MigData,PatData,numberOfMusicalPatterns)
#MigratingAgentsDistances=islandSnapshotDistance(migratingAgentsMP)
#Y=averageSnapshotDistance(MigratingAgentsDistances)
#plotAverageSnapShotDistance(Y,"20")

#Plots the distance between the island's leader pattern and the snapshot of the former islands pattern after a migrating agent has arrived
#MigData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 100 and crossovers/I3N3L8A1B005C2_M5AP100_I30P100.csv",True)
#PatData=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 100 and crossovers/I3N3L8A1B005C2_I30P100_M5AP100_LeadersMP.csv",False)
#migratingAgentsMP=migratingAgentsPatterns(MigData,PatData,numberOfMusicalPatterns)
#MigratingAgentsDistances=islandSnapshotDistance(migratingAgentsMP)
#Y=averageSnapshotDistance(MigratingAgentsDistances)
#plotAverageSnapShotDistance(Y,"100")



#data2=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Length of 16 Migration Adaptation period 20 and crossovers mutation rate of 2/I3N3L8A1B005C2_M5AP20_I30P100.csv")
#Perf=migratingAgentsPerformance(data2,numberOfMusicalPatterns)
#plotMigAgentsPerf(Perf,0.5,"Migrating agents mean utility when arriving on a new Island with new utility function")

#data=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 20 and crossovers/I3N3L8A1B005C2_M5AP20_I30P100_MigDetail.csv",True)
#Perf1=migratingAgentsPerformance(data,numberOfMusicalPatterns)
#plotMigAgentsPerf(Perf1,0.5,"Migrating agents mean utility when arriving on a new Island with new utility function")
#Perf=migratingAgentsSplitPerf(data,numberOfMusicalPatterns)
#print(Perf)
#print(Perf)
#plt.subplot(1, 2, 1)
#plotMigAgentsUtilities(Perf,0.5)

#data=csvToArray("E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/Migration Adaptation period of 100 and crossovers/I3N3L8A1B005C2_I30P100_M5AP100_MigDetail.csv",True)
#Perf1=migratingAgentsPerformance(data,numberOfMusicalPatterns)
#plotMigAgentsPerf(Perf1,0.5,"Migrating agents mean utility when arriving on a new Island with new utility function")
#Perf=migratingAgentsSplitPerf(data,numberOfMusicalPatterns)
#print(Perf)
#print(Perf)
#plt.subplot(1, 2, 2)
#plotMigAgentsUtilities(Perf,0.5)


#print(Perf1)
#plotMigAgentsDev(Perf1,0.5,"deviation rising crossprob")

#plotMigAgentsDev(Perf,0.5,"deviation classic crossprob")
#Perfs,IslandPerfs=weightedMeans(data,numberOfIslands,numberOfAgents,numberOfIterations,numberOfMusicalPatterns)
#print(Perfs)
#meanData,stdD=baselineSoloJamMean(data, numberOfMusicalPatterns, numberOfIterations)
#print(stdD)
#print(np.mean(stdD))
#plotBaselineSoloJam(meanData, numberOfAgents, numberOfMusicalPatterns)

#plotIslandUtilities(IslandPerfs,numberOfIslands,numberOfAgents,numberOfIterations,numberOfMusicalPatterns)
plt.show()
#print(meanMinMax(data,numberOfIterations,numberOfMusicalPatterns,0.1)[0:3])
#data=meanPerformance(data,numberOfIterations,numberOfMusicalPatterns,0.1)
#print(weightedMeans(data,numberOfIslands,numberOfAgents,numberOfIterations,numberOfMusicalPatterns))