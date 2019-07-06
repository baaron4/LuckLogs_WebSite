#Load packages
library("rvest")

colNames <- c('Entry', 'Boss', 'PlayerName', 'Specialization', 'BuildType', 'TotalDPS', 'PowerDPS', 'CondiDPS', 'KillTime', 'Subgroup', 'Date', 'Notes')
allData <- read.table(file = 'alldata.txt', header = FALSE, col.names = colNames, sep = '\t')
allData <- allData[,2:12]

#Set working directory to data file folder
setwd('[ADD STUFF]')

#Creates list of all data files
fileNames <- list.files(getwd())
fileCount <- length(fileNames)

#Preps data frame for all data, commented out new data frame
#allData <- data.frame(Boss = NULL, PlayerName = NULL, Specialization = NULL, BuildType = NULL, TotalDPS = NULL, PowerDPS = NULL, CondiDPS = NULL, KillTime = NULL, Subgroup = NULL, Date = NULL, Notes = NULL)
notes <- '.'


for (file in 1:fileCount) {
  #Select file
  fileSelecter <- fileNames[file]
  
  #Check if the file was parsed correctly
  fileInfo <- file.info(fileSelecter)
  if (fileInfo$size < 500000) {
    print(paste(fileSelecter, " did not parse correctly.", paste = ""))
  } else {
    bossFile <- read_html(fileSelecter)
    
    #Look for center nodes for pull information
    centerNodes <- bossFile %>%
      html_nodes("center")
    
    #Boss name
    boss <- substr(fileSelecter, 22, nchar(fileSelecter) - 5)
    
    #Kill date
    date <- substr(fileSelecter, 1, 8)
    
    #Kill time
    #Minutes component
    regex <- gregexpr('([[:digit:]]+)( minutes)', centerNodes[1])
    regex <- regex[[1]]
    
    start <- regex[1]
    end <- regex[1] + attr(regex, "match.length") - 8
    
    minutes <- substr(centerNodes[1], start, end)
    killTime <- as.numeric(minutes) * 60
    
    #Seconds component
    regex <- gregexpr('(minutes )([[:digit:]]+)([[:punct:]])([[:digit:]])', centerNodes[1])
    regex <- regex[[1]]
    
    start <- regex[1] + 8
    end <- regex[1] + attr(regex, 'match.length') - 1
    
    seconds <- substr(centerNodes[1], start, end)
    killTime <- killTime + as.numeric(seconds)
    
    #Looks for tr nodes for dps table
    trNodes <- bossFile %>%
      html_nodes("tr")
    
    #Find the correct tr nodes for subgroups and builds subgroup vectors
    subgroups <- grep('(width: 100px; border: 1px)', trNodes)
    subgroupVectors <- paste("subgroup", subgroups, paste = "")
    subgroupVectors <- gsub(" ", "", subgroupVectors, fixed = TRUE)
    subgrouper <- paste(subgroupVectors, " <- trNodes[", subgroups, "]")
    eval(parse(text=subgrouper))
    
    #Find the correct tr nodes for dps table
    dpsTable <- grep('<td>[1-5]</td>+', trNodes, perl=TRUE, value=FALSE)
    
    #Number of players
    playerNumbers1 <- c(1:10)
    playerNumbers2 <- c(1:10)
    
    #Make strings of the tr node lines
    playerStringer <- paste('player', playerNumbers1, ' <- toString((trNodes[dpsTable[', playerNumbers2, ']]))', sep = "")
    eval(parse(text = playerStringer))
    
    #Iterate through all players
    for (player in 1:10) {
      #Selects the player
      playerSelecter <- paste('player', player, sep = "")
      
      #Find DPS values
      dpsPattern <- '(dmg">)([[:digit:]]+)(<)'
      regexMaker <- paste("regex <- gregexpr('", dpsPattern, "', ", playerSelecter, ")", sep = "")
      eval(parse(text = regexMaker))
      regex = regex[[1]]
      
      #Power DPS
      start <- regex[2] + 5
      end <- regex[2] + attr(regex, 'match.length')[2] - 2
      
      powerDPSFinder <- paste('powerDPS <- substr(', playerSelecter, ', start, end)', sep = "")
      eval(parse(text = powerDPSFinder))
      powerDPS <- as.numeric(powerDPS)
      if (is.na(powerDPS)) {
        powerDPS <- 0
      }
      
      #Condi DPS
      start <- regex[3] + 5
      end <- regex[3] + attr(regex, 'match.length')[3] - 2
      
      condiDPSFinder <- paste('condiDPS <- substr(', playerSelecter, ', start, end)', sep = "")
      eval(parse(text = condiDPSFinder))
      condiDPS <- as.numeric(condiDPS)
      if (is.na(condiDPS)) {
        condiDPS <- 0
      }
      
      #Total DPS
      totalDPS <- powerDPS + condiDPS
      
      #Find subgroup value
      subgroupPattern <- '([[:digit:]]{1})'
      regexMaker <- paste("regex <- gregexpr('", subgroupPattern, "', ", playerSelecter, ")", sep = "")
      eval(parse(text = regexMaker))
      regex = regex[[1]]
      
      start <- regex[1]
      end <- regex[1] + attr(regex, 'match.length')[1] - 1
      
      subgroupFinder <- paste('subgroup <- substr(', playerSelecter, ', start, end)', sep = "")
      eval(parse(text = subgroupFinder))
      
      #Build type
      #Find character name first 
      charNamePattern <- '(t">)([[:print:]]{7,9})'
      regexMaker <- paste("regex <- gregexpr('", charNamePattern, "', ", playerSelecter, ")", sep = "")
      eval(parse(text = regexMaker))
      regex = regex[[1]]
      
      start <- regex[1] + 3
      end <- regex [1] + attr(regex, 'match.length')[1] 
      
      charNameFinder <- paste('charName <- substr(', playerSelecter, ', start, end)', sep = "")
      eval(parse(text = charNameFinder))
      
      #Fix POV giving extra spaces to charName string
      try({
        if (grep('([[:space:]])(<)', charName)) {
          charName <- substr(charName, 1, regexpr('( <)', charName) - 1)
        }
      }, silent = TRUE)
      
      #Fix doublecharacters like AE
      try({
        if (grep('([∆яжн])', charName)) {
          charName <- substr(charName, 1, nchar(charName) - 1)
        }
      }, silent = TRUE)
        
      #Find the string of build info based on character name
      buildTypePattern <- paste('([[:print:]]+)(', charName, ')', sep = "")
      regexMaker <- paste("regex <- gregexpr('", buildTypePattern, "', subgroup", subgroup, ")", sep = "")
      eval(parse(text = regexMaker))
      regex <- regex[[1]]
      
      start <- regex[1]
      end <- regex[1] + attr(regex, 'match.length') - 1
      
      buildTypeStringer <- paste('buildTypeString <- substr(subgroup', subgroup, ", ", start, ", ", end, ")", sep = "")
      eval(parse(text = buildTypeStringer))
      
      #Write the build type using if statements and concatenate
      buildType <- c()
      
      try({
        if (grep('Condition Damage', buildTypeString)) {
          buildType <- paste(buildType, "Condition Damage.", sep = "")
        } 
      }, silent = TRUE)
      
      try({
        if (grep('Toughness', buildTypeString)) {
          buildType <- paste(buildType, "Toughness.", sep = "")
        }
      }, silent = TRUE)
      
      try({
        if (grep('Healing Power', buildTypeString)) {
          buildType <- paste(buildType, "Healing Power.", sep = "")
        }
      }, silent = TRUE)
      
      try({
        if (is.null(buildType) && powerDPS < 1000) {
          buildType <- paste(buildType, "Other.", sep = "")
        } else if (is.null(buildType)) {
          buildType <- paste(buildType, "Power.", sep = "")
        }
      }, silent = TRUE)
      
      #Find player name values
      playerNamePattern <- '([[:alpha:]]+)([[:punct:]{1}])([[:digit:]]{4})'
      regexMaker <- paste("regex <- gregexpr('", playerNamePattern, "', ", playerSelecter, ")", sep = "")
      eval(parse(text = regexMaker))
      regex = regex[[1]]
      
      #Player name
      start <- regex[1]
      end <- regex[1] + attr(regex, 'match.length')[1] - 1
      
      playerNameFinder <- paste('playerName <- substr(', playerSelecter, ', start, end)', sep = "")
      eval(parse(text = playerNameFinder))
      
      #Find specialization values
      specPattern <- '(title=")([[:alpha:]]+)'
      regexMaker <- paste("regex <- gregexpr('", specPattern, "', ", playerSelecter, ")", sep = "")
      eval(parse(text = regexMaker))
      regex = regex[[1]]
      
      #Specialization
      start <- regex[1] + 7
      end <- regex[1] + attr(regex, 'match.length')[1] - 1
      
      specializationFinder <- paste('specialization <- substr(', playerSelecter, ', start, end)', sep = "")
      eval(parse(text = specializationFinder))
      
      
      #Adds info to the big dataframe
      temp <- data.frame(Boss = boss, PlayerName = playerName, Specialization = specialization, BuildType = buildType, TotalDPS = totalDPS, PowerDPS = powerDPS, CondiDPS = condiDPS, KillTime = killTime, Subgroup = subgroup, Date = date, Notes = notes)
      
      allData <- rbind(allData, temp) 
    }
  }
}

#Move files to archive
fileNames2 <- fileNames
fileMover <- paste("file.rename(from = '[ADD STUFF]", fileNames, "', to = '[ADD STUFF]", fileNames2, "')", sep = "")
eval(parse(text = fileMover))
 
#Write/append to data file
write.table(allData, "[ADD STFF]", sep = "\t", append = FALSE, col.names = FALSE)
