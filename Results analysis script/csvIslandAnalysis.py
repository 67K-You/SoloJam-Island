import re
import numpy as np
import math as m
import matplotlib.pyplot as plt
from numpy.lib.function_base import append

#Script parameters
filename="E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/I3N3L8A1B005C2_genP60_M5_I30P100.csv"
numberOfIslands=3
numberOfAgents=9
numberOfIterations=30
numberOfMusicalPatterns=100

#Converts the csv array obtained with the Unity simulations into a numpy array
def csvToArray(name_of_file):
    stringdata=np.genfromtxt(name_of_file, delimiter=';',dtype=str)
    data=np.empty(np.shape(stringdata),dtype=float)
    data[:] = np.nan
    for i in range (np.shape(stringdata)[0]):
        for j in range (np.shape(stringdata)[1]):
            if stringdata[i][j]=='':
                data[i][j]=None
            else:
                data[i][j]=float(stringdata[i][j].replace(',','.'))
    return data

#Return the mean performance during all of each agent and island
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
    print(np.shape(dataArray))
    for i in range(1,np.shape(dataArray)[0]):
        for j in range(np.shape(dataArray)[1]):
            if(not(m.isnan(dataArray[i][j])) and m.isnan(dataArray[i-1][j]) and (i-1)%(musicalPatternNumber+1)!=0):
                print(dataArray[i][j])
                L.append([])
                k=0
                while((i+k)<np.shape(dataArray)[0] and not(m.isnan(dataArray[i+k][j]))):
                    L[-1].append(dataArray[i+k][j])
                    k+=1
    return L

def plotMigAgentsPerf(PerfList,significancePercentage):
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
    plt.plot(X,Y,label="Migrating agents mean utility when arriving in a new Island")
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

def plotIslandUtilities(dataArray,islandsNumber,agentsnumber,iterationNumber,musicalPatternNumber):
    X=np.linspace(1,musicalPatternNumber,musicalPatternNumber)
    for i in range (islandsNumber):
        plt.plot(X,dataArray[i],label="mean utility of Island "+str(i)+" for an Agent")
            #else:
                #plt.plot(X,dataArray[j][1:],label="agent "+str(j-1))
    plt.legend(loc="upper right")




data=csvToArray(filename)
print(data)
Perf=migratingAgentsPerformance(data,numberOfMusicalPatterns)
plotMigAgentsPerf(Perf,0.5)
Perfs,IslandPerfs=weightedMeans(data,numberOfIslands,numberOfAgents,numberOfIterations,numberOfMusicalPatterns)
print(Perfs)
#plotIslandUtilities(IslandPerfs,numberOfIslands,numberOfAgents,numberOfIterations,numberOfMusicalPatterns)
plt.show()
#print(meanMinMax(data,numberOfIterations,numberOfMusicalPatterns,0.1)[0:3])
#data=meanPerformance(data,numberOfIterations,numberOfMusicalPatterns,0.1)
#print(weightedMeans(data,numberOfIslands,numberOfAgents))