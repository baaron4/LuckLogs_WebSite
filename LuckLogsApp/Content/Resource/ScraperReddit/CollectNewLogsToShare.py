import os

# Boss list
bossNames = 'Matthias Gabrel', 'Slothasor', 'Sabetha the Saboteur', 'Gorseval the Multifarious', 'Vale Guardian', 'Xera', 'Keep Construct', 'Deimos', 'Samarog', 'Mursaat Overseer', 'Cairn the Indomitable'

# Iterate through bosses
for boss in bossNames:
    # List files in boss directory and sort them by ascending date
    files = os.listdir(boss)
    sorted(files)

    # If there are any file, run the last one
    if len(files) > 0:
        filePath = boss + "/" + files[-1]
        raidHeroesCMD = 'raid_heroes "[ADD STUFF]' + filePath + '"'
        os.system(raidHeroesCMD)

        # Move the last one to archive
        newFilePath = "evtc Archive/" + boss + "/" + files[-1]
        os.rename(filePath, newFilePath)

        # Generates files list again to move the rest to archive
        files = os.listdir(boss)

        #Iterates through and moves each file to archive
        for file in files:
            filePath = boss + "/" + file
            newFilePath = "evtc Archive/Unparsed/" + file
            os.rename(filePath, newFilePath)
