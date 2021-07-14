import re
import numpy as np
import math as m
import matplotlib.pyplot as plt
from numpy.lib.function_base import append

#Script parameters
filename="E:/Unity Projects/SoloJam Implementation/Results/30 runs comparison/N3L8A1B005C2_genP60_30I100P.csv"
numberOfIslands=1
numberOfAgents=3
numberOfIterations=30
numberOfMusicalPatterns=100


def csvToArray(name_of_file):
    stringdata=np.genfromtxt(name_of_file, delimiter=';',dtype=str)
    #print(stringdata)
    #print(np.shape(stringdata))
    data=np.empty(np.shape(stringdata),dtype=float)
    data[:] = np.nan
    for i in range (np.shape(stringdata)[0]):
        for j in range (np.shape(stringdata)[1]):
            if stringdata[i][j]=='':
                data[i][j]=None;
            else:
                data[i][j]=float(stringdata[i][j].replace(',','.'))
    return data
        
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
    for i in range(np.shape(dataArray)[0]):
        for j in range(np.shape(1,dataArray[1])):
            if(dataArray[i][j]!=None and dataArray[i-1][j]==None and (i-1)%(musicalPatternNumber+1)!=0):
                L.append(dataArray[i][j])
    return L


def weightedMeans(dataArray,islandsNumber,agentsNumber):
    Means=np.array(islandsNumber)
    for i in range(islandsNumber):
        sum=0
        for j in range(1,np.shape(dataArray)[0]):
            weightedRowUtility=0
            agentsHere=0
            for k in range (agentsNumber):
                if(dataArray[j][islandsNumber*(agentsNumber+1)+k]!=None):
                    weightedRowUtility+=dataArray[j][islandsNumber*(agentsNumber+1)+k]
                    agentsHere+=1
            if(agentsHere!=0):
                weightedRowUtility=weightedRowUtility/agentsHere
            sum+=weightedRowUtility
        Means[i]=sum
    return Means

def plotIslandUtilities(dataArray,islandsNumber):
    X=np.linspace(1,np.shape(dataArray)[1],np.shape(dataArray)[1]-1)
    #print(X)
    for i in range (islandsNumber):
        for j in range(np.shape(dataArray)[0]):
            if(j==0):
                plt.plot(X,dataArray[j][1:],label="overall utility")
            else:
                plt.plot(X,dataArray[j][1:],label="agent "+str(j-1))
    plt.legend(loc="upper right")    
    plt.show()


#dataanalysis = np.loadtxt(filename,delimiter=';')
#print("shape of data:",dataanalysis.shape)
#print("datatype of data:",dataanalysis.dtype)


data=csvToArray(filename)
print(meanMinMax(data,numberOfIterations,numberOfMusicalPatterns,0.1)[0:3])
data=meanPerformance(data,numberOfIterations,numberOfMusicalPatterns,0.1)
#print(weightedMeans(data,numberOfIslands,numberOfAgents))
plotIslandUtilities(data,numberOfIslands)