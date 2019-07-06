import os

# Boss list
bossNames = 'Matthias Gabrel', 'Slothasor', 'Sabetha the Saboteur', 'Gorseval the Multifarious', 'Vale Guardian', 'Xera', 'Keep Construct', 'Deimos', 'Samarog', 'Mursaat Overseer', 'Cairn the Indomitable'

# Iterate through bosses
for boss in bossNames:
    # List files in boss directory
    files = os.listdir(boss)

    # Iterate files in boss directory
    for file in files:
        # Parses logs using raid heroes
        filePath = boss + "/" + file
        raidHeroesCMD = 'raid_heroes "[ADD STUFF]' + filePath + '"'
        os.system(raidHeroesCMD)

        # Move parsed evtc to archive
        newFilePath = "evtc Archive/" + boss + "/" + file
        os.rename(filePath, newFilePath)
