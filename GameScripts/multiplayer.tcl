call data/scripts/misc/mp_tools.tcl
call data/scripts/init/mpinit.tcl

log "Starting Multiplayer Game ..."

/// Std Settings
set GameType 	"DM"
set UseMap 		1
set Zone 		"Urwald"
set TempRatio 	0.3
set Generate 	1
///


/// Gamedata
set GameData [net gamedata]
log "GameData: $GameData"

set GameSave 	"Data/GameScripts/[lindex $GameData 0]"
set GameType 	[lindex $GameData 1]
set UseMap 		[lindex $GameData 2]
set Density		[lindex $GameData 3]
set Resources	[lindex $GameData 4]
set Set			[lindex $GameData 5]
set Size		[lindex $GameData 6]
set NumGnomes	[lindex $GameData 7]
///

set SKirm [lindex $GameSave 1]
set GameSave [lindex $GameSave 0]

set bSkirmish 0
if { $SKirm == "skirmish" } {
	set bSkirmish 1
	log "Starting Skirmish..."
}

switch $GameType {
	0 {set GameType "CW"}
	1 {set GameType "CF"}
	2 {set GameType "DM"}
}

switch $Set {
	0 {set Zone "Urwald"}
	1 {set Zone "Metall"}
	2 {set Zone "Kristall"}
	3 {set Zone "Lava"}
}

set map_size "[lindex $mapsizelist $Size] [lindex $mapsizelist $Size]"

set TempRatio [lindex $densitylist $Density]


log "starttemplatelist: $starttemplatelist"


/// Playerdata
set PlayerData [net playerdata]
log "PlayerData: $PlayerData"

set teamlist [list]

for {set i 0} {$i < 8} {incr i} {
	set actplayer [lindex $PlayerData $i]
	
	set_owner_attrib $i PlayerAggressivity 0.3

	set PlayerType_$i	[lindex $actplayer 0]
	set PlayerRace_$i   [lindex $actplayer 1]
	set PlayerColor_$i  [lindex $actplayer 2]
	set PlayerTeam_$i   [lindex $actplayer 3]
	set PlayerReady_$i  [lindex $actplayer 4]

	set PlayerCWTeam_$i   [expr ([lindex $actplayer 3] + 1) % 2]

	if { [lindex $actplayer 0]  != 0 } {
		lappend teamlist [lindex $actplayer 3]
	}

	if { [lindex $actplayer 0] == 2 } {
		ai init $i data/scripts/ai/std_ai.tcl
		ai enable $i
		log "init ai for $i"
	}
}



//	enum PlayerType
//	{
//		PT_None,
//		PT_Player,
//		PT_AI
//	};


////
// Create GameObserver:
if { $bSkirmish } {
   	set go [new GameObserver]
   	set_owner $go 0
    call_method $go InitPlayerData $PlayerData
    call_method $go InitNumGnomes $NumGnomes

    for {set i 1} {$i < 4} {incr i} {
    	set actplayer [lindex $PlayerData $i]
    	set PT	[lindex $actplayer 0]
   		ai init $i data/scripts/ai/std_ai.tcl
   		ai enable $i

   		set style "normal"
   		switch $PT {
   			0	{set style "aggressive"
   				 set_diplomacy $i 0 enemy
   				 set_diplomacy 0 $i enemy
   				}
   			1	{set style "normal"
   				 set_diplomacy $i 0 enemy
   				 set_diplomacy 0 $i enemy
   				}
   			2	{set style "defensive"
   				 set_diplomacy $i 0 neutral
   				 set_diplomacy 0 $i neutral
   				}
   			3	{set style "debug"}
   		}

		ai exec $i "set_ai_style $style"
   		log "init ai for $i as $style"
    }

} else {
    set GameObserverList [list]
    for {set i 0} {$i < 8} {incr i} {
    	set go [new GameObserver]
    	set_owner $go $i
    	lappend GameObserverList $go
    }
    call_method [lindex $GameObserverList 0] InitCW
    call_method [lindex $GameObserverList 0] InitPlayerData $PlayerData
    call_method [lindex $GameObserverList 0] InitNumGnomes $NumGnomes
}

/// Wiggles-Rassen initialisieren !
net initplayers

///



/// Server Client
set ClientID [net localid]
set bServer [expr {$ClientID == 0}]
///


/// Callbacks
proc NetStatusMessageCallback {sMsg iSender} {

	log "NetStatusMessage ($iSender) '$sMsg'"

	if { [lindex $sMsg 0] == "level" } {
		lrem sMsg 0

    	set MapW [lrem sMsg 0]
		set MapH [lrem sMsg 0]

		map create $MapW $MapH

		catch { apply_level $sMsg }
		net objloadingdone
		load_info "waiting for players..."
		return
	}

}

proc NetLoadingDoneCallback {sMsg iSender} {
	global GameObserverList GameType
	log "Loading done..."
	show_loading 0
	gametime factor 1.0
	gametime start

	call_method [lindex $GameObserverList [net localid]] Init $GameType
}
///


/// Loading
if { $bSkirmish } {

	call $GameSave
	set MapW [lindex $map_size 0]
	set MapH [lindex $map_size 1]
	map create $MapW $MapH

	lg_tp_clear
	lg_tp_addtemplatesets 	" $Zone.Std "
	lg_set_templateratio 0 $TempRatio 1.0
	lg_set_area 16 16 [expr $MapW - 16] [expr $MapH - 16]

	sm_add_zone "Urwald"
	sm_add_zone "Metall"
	sm_add_zone "Kristall"
	sm_add_zone "Lava"

	sm_force_zone $Zone

    set matlist [list]
	foreach item $temp_list {
		if { [lindex $item 0] == "mat" } {
			set matlist $item
		} else {
			lg_add_preset "[lindex $item 2].tcl" [lindex $item 0] [lindex $item 1]
		}
	}

	lg_set_leveltype base
	lg_start
	set level [lg_get_level]

	lappend level $matlist

    apply_level $level

	show_loading 0
	gametime factor 1.0
	gametime start
	call_method $go Init "SM"

} elseif { $bServer } {

	send_status_msg "starting Multiplayer '$GameSave' ..." 1

	if { $UseMap } {
		call $GameSave
	} else {
		log "TODO create starttemplates !!!!!!!!!!!!!!"
		set stposlist [get_mpstartpos [expr [lindex $map_size 0] - 32] [expr [lindex $map_size 0] - 32] $teamlist]
		log "---> $stposlist"

		set temp_list [list]

		set iCnt 0
		foreach item $stposlist {
			set plist [lindex $starttemplatelist $iCnt]
			set zlist [lindex $plist $Set]
			set idx [irandom [llength $zlist]]

			lappend temp_list "[lindex $item 0] [lindex $item 1] [lindex $zlist $idx]"

			incr iCnt
		}
	}

	set MapW [lindex $map_size 0]
	set MapH [lindex $map_size 1]

	send_status_msg "creating Map ..." 1

	map create $MapW $MapH

	log "creating map ([lindex $map_size 0] x [lindex $map_size 1]) ..."


	lg_tp_clear
	lg_tp_addtemplatesets 	" $Zone.Std "
	lg_set_templateratio 0 $TempRatio 1.0
	lg_set_area 16 16 [expr $MapW - 16] [expr $MapH - 16]

    set matlist [list]
	foreach item $temp_list {
		if { [lindex $item 0] == "mat" } {
			set matlist $item
		} else {
			lg_add_preset "[lindex $item 2].tcl" [lindex $item 0] [lindex $item 1]
		}
	}

	lg_set_leveltype base
	lg_start
	set level [lg_get_level]

	lappend level $matlist

	//set level {{minimaltest 4 4}}

	log "Level: $level"
		send_status_msg "level $MapW $MapH $level"



    apply_level $level

	net objloadingdone
    send_status_msg "loading done" 1
    load_info "waiting for players..."


} else {
	// Client muﬂ warten
	load_info "waiting for server..."
}

//map create 150 100
//map_template data/templates/unq_start.pmp 4 4

//log "Multiplayer ready."




