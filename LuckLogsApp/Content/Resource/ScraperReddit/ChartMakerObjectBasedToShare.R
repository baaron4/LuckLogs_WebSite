#Variables to change
bosses = c('vg', 'gorse', 'sab', 'sloth', 'matt', 'kc', 'xera', 'cairn', 'mo', 'sam', 'dei')
patchTitle = 'SeaweedSaladNerf-May2017'

#Load packages
library("plyr")
library("plotly")

#Plotly credentials
Sys.setenv("plotly_username" = "[ADD STUFF]")
Sys.setenv("plotly_api_key" = "[ADD STUFF]")

#Core Team List
coreTeam <- c('Trindine.4076', 
              'Vice.3164', 
              'blitzcreek.7019', 
              'Elestian.6134', 
              'MereLynn.9625', 
              'kratox.3675', 
              'Andrex.8160', 
              'sleepysoul.5743', 
              'LucianTheAngelic.7054',
              'Burkid.4178')

#Colours based on specialization via function for dpply
spec <- c('Guardian', 'Dragonhunter', 
          'Revenant', 'Herald',
          'Warrior', 'Berserker',
          'Engineer', 'Scrapper',
          'Ranger', 'Druid',
          'Thief', 'Daredevil',
          'Elementalist', 'Tempest',
          'Mesmer', 'Chronomancer',
          'Necromancer', 'Reaper')
colour <- c('rgba(127,229,235,1)', 'rgba(28,173,180,1)', 
            'rgba(86,145,255,1)', 'rgba(135,167,225,1)', 
            'rgba(255,247,40,1)', 'rgba(219,210,35,1)',
            'rgba(255,172,20,1)', 'rgba(235,167,49,1)',
            'rgba(167,229,50,1)', 'rgba(178,255,35,1)',
            'rgba(200,200,222,1)', 'rgba(128,128,128,1)',
            'rgba(225,91,91,1)', 'rgba(255,158,158,1)',
            'rgba(252,127,255,1)', 'rgba(222,152,255,1)',
            'rgba(131,230,166,1)', 'rgba(70,214,121,1)')
SpecColours <- function(x) {colour[which(spec == x)]}

standardError <- function(x) {sqrt(var(x)/length(x))}

#Import data set and add column names
dataset <- read.table(file = 'allData.txt', header = FALSE, sep = '\t')
names(dataset) <- c("Entry","Boss","AccountName","Specialization","BuildType","TotalDPS","PowerDPS","CondiDPS","KillTime","Subgroup","Date", "Notes")
plotColumn <- ddply(dataset, .(Entry), summarize, plotColumn = paste(Entry, AccountName, Specialization, BuildType, sep = "-"), SpecColour = SpecColours(Specialization))
plotColumn <- plotColumn[,2:3]aw 
dashFinder <- regexpr('-', plotColumn$plotColumn)
plotColumn$plotText <- substr(plotColumn$plotColumn, dashFinder + 1, nchar(plotColumn$plotColumn) - 1)
dataset <- cbind(dataset, plotColumn)

makeCharts <- function(bossName, patch) {
############ High Scores ############
  #Subset dataset
  subDataset <- dataset[which(dataset$Boss == bossName), ]
  subDataset <- subDataset[order(subDataset$TotalDPS), ]
  subDataset$plotColumn <- factor(subDataset$plotColumn, levels = unique(subDataset$plotColumn)[order(subDataset$TotalDPS, decreasing = FALSE)])
  
  #Plot highscore data
  subDPSHSPlot <- plot_ly(tail(subDataset, 20),
                         x = ~PowerDPS,
                         y = ~plotColumn,
                         type = 'bar',
                         marker = list(color = ~SpecColour, line = list(color = "rgba(0,0,255,0.25)", width = 2)),
                         orientation = 'h',
                         hoverinfo = 'none'
  ) %>%
    add_trace(x = ~CondiDPS,
              name = 'Condi',
              showlegend = FALSE,
              marker = list(line = list(color = "rgba(255,0,0,0.25)", width = 2)),
              hoverinfo = 'none'
              ) %>%
    add_annotations(text = paste("<b>", tail(subDataset$plotText, 20), "</b>-", tail(subDataset$Date, 20), sep = ""),
                    x = 25,
                    y = ~plotColumn,
                    font = list(family = 'Arial',
                                size = 12,
                                color = 'black'),
                    showarrow = FALSE,
                    xanchor = 'left',
                    hovertext = ~paste("<b>Power DPS: </b>", PowerDPS, " <b>Condi DPS: </b>", CondiDPS, "<br><b>Total DPS: </b>", TotalDPS, " - <i>", round(TotalDPS/TotalDPS[20]*100), "%</i> <b>Kill time: </b>", KillTime, "s", sep = "")
                    ) %>%
    layout(title = 'DPS HighScores',
           yaxis = list(
             visible = c(FALSE)
           ),
           plot_bgcolor = 'rgba(222,222,222,1)',
           paper_bgcolor = 'rgba(222,222,222,1)',
           margin = list(
             t = 50
           ),
           barmode = 'stack',
           hovermode = 'closest')
  plotFileName = paste("GW2/", patch, "/",bossName, "-Highscore-DPS-", patch, sep = "")
  api_create(subDPSHSPlot, filename = plotFileName, sharing = c('public'))

######## Average DPS #############
  #Subset Average DPS data
  AvgData <- ddply(subDataset, .(AccountName, Specialization, BuildType), summarize, AvgTotalDPS = mean(TotalDPS), AvgKillTime = mean(KillTime), DataPoints = length(Notes), SE = standardError(TotalDPS), AvgPowerDPS = mean(PowerDPS), AvgCondiDPS = mean(CondiDPS))
  AvgData$SE[which(is.na(AvgData$SE))] = 0
  plotColumn <- paste(AvgData$AccountName, "-", AvgData$Specialization, "-", AvgData$BuildType, " (", AvgData$DataPoints, ")", sep = "")
  AvgData <- cbind(AvgData, plotColumn)
  AvgData <- AvgData[order(AvgData$AvgTotalDPS), ]
  AvgData$plotColumn <- factor(AvgData$plotColumn, levels = unique(AvgData$plotColumn)[order(AvgData$AvgTotalDPS, decreasing = FALSE)])
  SpecColour <- ddply(AvgData, .(plotColumn), summarize, SpecColour = SpecColours(Specialization))
  SpecColour <- SpecColour$SpecColour
  AvgData <- cbind(AvgData, SpecColour)
  
  #Plot average DPS
  AvgPlot <- plot_ly(tail(AvgData, 20),
                       x = ~AvgPowerDPS,
                       y = ~plotColumn,
                       name = 'Power',
                       type = 'bar',
                       orientation = 'h',
                       marker = list(color = ~SpecColour, line = list(color = "rgba(0,0,255,0.25)", width = 2)),
                       hoverinfo = 'none'
  ) %>%
    add_trace(
      x = ~AvgCondiDPS,
      name = 'Condi',
      error_x = list(type = "data", array = (tail(AvgData$SE, 20)), color = 'white'),
      marker = list(line = list(color = "rgba(255,0,0,0.25)", width = 2)),
      showlegend = FALSE,
      hoverinfo = 'none'
    ) %>%
    add_annotations(text = paste("<b>", tail(AvgData$plotColumn, 20), "</b>", sep = ""),
                    x = 25,
                    y = ~plotColumn,
                    font = list(family = 'Arial',
                                size = 12,
                                color = 'black'),
                    showarrow = FALSE,
                    xanchor = 'left',
                    align = 'left',
                    hovertext = ~paste('<b>Power DPS: </b>', round(AvgPowerDPS), " <b>Condi DPS: </b>", round(AvgCondiDPS), '<br><b>Total Avg DPS: </b>', round(AvgTotalDPS), " - <i>", round(AvgTotalDPS/AvgTotalDPS[20]*100), "%</i>", sep = "")
                    ) %>%
    layout(title = 'Average DPS',
           yaxis = list(
             visible = c(FALSE)
           ),
           plot_bgcolor = 'rgba(222,222,222,1)',
           paper_bgcolor = 'rgba(222,222,222,1)',
           margin = list(
             t = 50
           ),
           barmode = 'stack'
           )
  plotFileName = paste("GW2/", patch, "/",bossName, "-Average-DPS-", patch, sep = "")
  api_create(AvgPlot, filename = plotFileName, sharing = c('public'))
  
  ################ Day DPS #############
  #Subset day DPS Data
  DayData <- subDataset[order(-subDataset$Date), ]
  DayData <- DayData[1:10,]
  DayData$plotColumn <- factor(DayData$plotColumn, levels = unique(DayData$plotColumn)[order(DayData$TotalDPS, decreasing = FALSE)])
  
  #Plot day DPS data
  DayPlot <- plot_ly(DayData,
                       x = ~PowerDPS,
                       y = ~plotColumn,
                       type = 'bar',
                       marker = list(color = ~SpecColour, line = list(color = "rgba(0,0,255,0.25)", width = 2)),
                       orientation = 'h',
                       hoverinfo = 'none'
  ) %>%
    add_trace(x = ~CondiDPS,
      name = 'Condi',
      showlegend = FALSE,
      marker = list(line = list(color = "rgba(255,0,0,0.25)", width = 2)),
      hoverinfo = 'none'
    ) %>%
    add_annotations(text = paste("<b>", DayData$plotText, "</b>", sep = ""),
                    x = 25,
                    y = ~plotColumn,
                    font = list(family = 'Arial',
                                size = 14,
                                color = 'black'),
                    showarrow = FALSE,
                    xanchor = 'left',
                    hovertext = ~paste('<b>Power DPS: </b>', PowerDPS, " <b>Condi DPS: </b>", CondiDPS, '<br><b>Total DPS: </b>', TotalDPS, " <i>", round(TotalDPS/sum(TotalDPS[1:10])*100), "% of Total</i>", sep = "")
                    ) %>%
    layout(title = paste(DayData$Date[1], " - DPS", sep = ""),
           yaxis = list(
             visible = c(FALSE)
           ),
           plot_bgcolor = 'rgba(222,222,222,1)',
           paper_bgcolor = 'rgba(222,222,222,1)',
           margin = list(
             t = 50
           ),
           barmode = 'stack',
           annotations = c(text = paste("<b>Group DPS: </b>", sum(DayData$TotalDPS), "<br><b>Kill time:</b> ", DayData$KillTime[1], "s<br><b>Diff from best: </b>", round(DayData$KillTime[1] - min(subDataset$KillTime), digits = 1), "s", sep = ""),
                           x = ~TotalDPS[10]*0.9, 
                           y = 1.5,
                           xref = "x",
                           yref = "y",
                           showarrow = FALSE,
                           bgcolor = "white",
                           bordercolor = 'black',
                           ax = 0,
                           ay = 0,
                           align = 'left',
                           borderpad = '5'
                           )) 
  
  plotFileName = paste("GW2/", patch, "/",bossName, "-Day-DPS-", patch, sep = "")
  api_create(DayPlot, filename = plotFileName, sharing = c('public'))
  
  ######### Weekly Killtimes ###############
  #Add proportion column for each row individually and coreTeam check
  for (row in 1:nrow(subDataset)) {
    rowDate <- subDataset$Date[row]
    weekTotalDPS <- sum(subDataset$TotalDPS[which(subDataset$Date == rowDate)])
    subDataset$Proportion[row] <- subDataset$TotalDPS[row] / weekTotalDPS 
    if (any(grepl(subDataset$AccountName[row], coreTeam))) {
      subDataset$Core[row] <- 1
    } else {
      subDataset$Core[row] <- 0
    }
  }
  #Create new dataframe by date and use players as columns
  weeklyKillTimeData <- ddply(subDataset, .(Date), summarize, Player1 = Proportion[1]*KillTime[1], 
                              Player2 = Proportion[2]*KillTime[1],
                              Player3 = Proportion[3]*KillTime[1],
                              Player4 = Proportion[4]*KillTime[1],
                              Player5 = Proportion[5]*KillTime[1],
                              Player6 = Proportion[6]*KillTime[1],
                              Player7 = Proportion[7]*KillTime[1],
                              Player8 = Proportion[8]*KillTime[1],
                              Player9 = Proportion[9]*KillTime[1],
                              Player10 = Proportion[10]*KillTime[1],
                              P1PlotText = plotText[1],
                              P2PlotText = plotText[2],
                              P3PlotText = plotText[3],
                              P4PlotText = plotText[4],
                              P5PlotText = plotText[5],
                              P6PlotText = plotText[6],
                              P7PlotText = plotText[7],
                              P8PlotText = plotText[8],
                              P9PlotText = plotText[9],
                              P10PlotText = plotText[10],
                              P1SpecColor = SpecColour[1],
                              P2SpecColor = SpecColour[2],
                              P3SpecColor = SpecColour[3],
                              P4SpecColor = SpecColour[4],
                              P5SpecColor = SpecColour[5],
                              P6SpecColor = SpecColour[6],
                              P7SpecColor = SpecColour[7],
                              P8SpecColor = SpecColour[8],
                              P9SpecColor = SpecColour[9],
                              P10SpecColor = SpecColour[10],
                              KillTime = KillTime[1],
                              PowerDPS = sum(PowerDPS),
                              CondiDPS = sum(CondiDPS),
                              TotalDPS = sum(TotalDPS),
                              TeamCount = sum(Core))
  weeklyKillTimeData <- weeklyKillTimeData[order(-weeklyKillTimeData$KillTime),]
  weeklyKillTimeData$Date <- factor(weeklyKillTimeData$Date, levels = unique(weeklyKillTimeData$Date)[order(-weeklyKillTimeData$KillTime, decreasing = TRUE)])
  
  WeeksPlot <- plot_ly(weeklyKillTimeData,
                       x = ~Player1,
                       y = ~Date,
                       type = 'bar',
                       orientation = 'h',
                       marker = list(color = ~P1SpecColor, line = list(color = "white", width = 2)),
                       hovertext = ~paste("<b>", P1PlotText, "</b> <i>", round(Player1/KillTime*100), "%</i>", sep = ""),
                       hoverinfo = "text"
                       ) %>%
    add_trace(x = ~Player2,
              marker = list(color = ~P2SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P2PlotText, "</b> <i>", round(Player2/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_trace(x = ~Player3,
              marker = list(color = ~P3SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P3PlotText, "</b> <i>", round(Player3/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_trace(x = ~Player4,
              marker = list(color = ~P4SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P4PlotText, "</b> <i>", round(Player4/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_trace(x = ~Player5,
              marker = list(color = ~P5SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P5PlotText, "</b> <i>", round(Player5/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_trace(x = ~Player6,
              marker = list(color = ~P6SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P6PlotText, "</b> <i>", round(Player6/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_trace(x = ~Player7,
              marker = list(color = ~P7SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P7PlotText, "</b> <i>", round(Player7/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_trace(x = ~Player8,
              marker = list(color = ~P8SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P8PlotText, "</b> <i>", round(Player8/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_trace(x = ~Player9,
              marker = list(color = ~P9SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P9PlotText, "</b> <i>", round(Player9/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_trace(x = ~Player10,
              marker = list(color = ~P10SpecColor, line = list(color = "white", width = 2)),
              showlegend = FALSE,
              hovertext = ~paste("<b>", P10PlotText, "</b> <i>", round(Player10/KillTime*100), "%</i>", sep = "")
              ) %>%
    add_annotations(text = ~paste("Power DPS: ", PowerDPS, " Condi DPS: ", CondiDPS, " <b>Total DPS: </b>", TotalDPS, "<br><b> Team Count:</b> ", TeamCount, " <b>(", KillTime, "s)</b>", sep = ""),
                    x = ~KillTime,
                    y = ~Date,
                    font = list(family = 'Arial',
                                size = 14,
                                color = 'black'),
                    align = 'right',
                    showarrow = FALSE,
                    xanchor = 'right') %>%
    layout(title = 'Kill Time by Week',
           yaxis = list(title = ""),
           xaxis = list(title = 'Kill Time (s)'),
           plot_bgcolor = 'rgba(222,222,222,1)',
           paper_bgcolor = 'rgba(222,222,222,1)',
           margin = list(t = 50, l = 75),
           barmode = 'stack',
           hovermode = 'closest')
  plotFileName = paste("GW2/", patch, "/",bossName, "-Weekly-KillTime-", patch, sep = "")
  api_create(WeeksPlot, filename = plotFileName, sharing = c('public'))
  
  paste("Charts complete", sep = "")
}

chartMaker <- paste("makeCharts('", bosses, "', patchTitle)", sep = "")
eval(parse(text = chartMaker))