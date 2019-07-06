


function filterTable() {
    // Declare variables
    var weekInput, weekFilter,bossInput,bossFilter, table, tr, td, i;
    weekInput = document.getElementById("WeekSelector");
    weekFilter = weekInput.value.toUpperCase();
    bossInput = document.getElementById("BNamesSelector");
    bossFilter = bossInput.value.toUpperCase();
    table = document.getElementById("myTable");
    tr = table.getElementsByTagName("tr");

    // Loop through all table rows, and hide those who don't match the search query
    for (i = 0; i < tr.length; i++) {
        tdBossName = tr[i].getElementsByTagName("td")[0];
        tdWeek = tr[i].getElementsByTagName("td")[1];
        if (tdWeek && tdBossName) {
            var dateTOISO = tdWeek.innerHTML.toUpperCase().substring(44, 54);
            var week = tdWeek.innerHTML.toUpperCase().substring(44, 48) + "-W" + getWeekNumber(dateTOISO.substring(0, 4), dateTOISO.substring(5, 7), dateTOISO.substring(8, 11));
            
            if ((week.indexOf(weekFilter) > -1) && (tdBossName.innerHTML.toUpperCase().indexOf(bossFilter) > -1)) {
                tr[i].style.display = "";
               // dateTOISO.substring(0, 4), dateTOISO.substring(5, 7), dateTOISO.substring(8, 11)+ "    " + weekFilter);
            } else {
                tr[i].style.display = "none";
               // console.log(dateTOISO);
            }
        }
    }
}
function getWeekNumber(year,month,day) {
    
    // Copy date so don't modify original
    d = new Date(year,month,day);
    d.setHours(0, 0, 0, 0);
    // Set to nearest Monday: current date + 1 - current day number
    // Make Sunday's day number 7
    d.setDate(d.getDate() + 1 - (d.getDay() || 7));
    // Get first day of year
    var yearStart = new Date(d.getFullYear(), 0, 1);
    // Calculate full weeks to nearest Monday (-4 because off by a month?)
    var weekNo = Math.ceil((((d - yearStart) / 86400000) + 1) / 7)-4;
    // Return array of year and week number
    
    return (weekNo);
}

function getDateOfISOWeek(w, y) {
    var simple = new Date(y, 0, 1 + (w - 1) * 7);
    var dow = simple.getDay();
    var ISOweekStart = simple;
    if (dow <= 4)
        ISOweekStart.setDate(simple.getDate() - simple.getDay() + 1);
    else
        ISOweekStart.setDate(simple.getDate() + 8 - simple.getDay());
    return ISOweekStart;
}

function filterRankTable() {
    // Declare variables
    var profInput, profFilter, patchInput, patchFilter, table, tr, td, i;
    profInput = document.getElementById("rank-prof-select");
    profFilter = profInput.value.toUpperCase();
    patchInput = document.getElementById("rank-patch-select");
    patchFilter = patchInput.value.toUpperCase();
    table = document.getElementById("dpsRank-table");
    tr = table.getElementsByTagName("tr");

    // Loop through all table rows, and hide those who don't match the search query
    for (i = 0; i < tr.length; i++) {
        tdProfName = tr[i].getElementsByTagName("td")[3].getElementsByTagName("img")[0].getAttribute("alt");
        //tdPatch = tr[i].getElementsByTagName("td")[1];//ATM this needs work
        if ( tdProfName) {
            if ( tdProfName.innerHTML.toUpperCase().indexOf(profFilter) > -1) {
                tr[i].style.display = "";
            } else {
                tr[i].style.display = "none";
            }
        }
    }
}

