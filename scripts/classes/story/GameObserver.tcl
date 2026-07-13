def_class GameObserver none info 0 {} {

// ObjInit
	obj_init {
		set ObserverList	{3 4 5 6 7 8 9 10}	;# Liste alle Gameobserver
		set bServerMode		0					;# Servermodus

		// DeathMatch-Vars
		set PlayerAlive		{0 0 0 0 0 0 0 0}

		proc wait_time {t {fin "X"}} {state_disable this;action this wait $t "if \{\"$fin\" != \"X\"\} \{eval $fin\};state_enable this" "state_enable this"}

		proc ExecuteAll {code} {
			call_method this ExecuteAll $code
		}

		proc loadtextwin {name} {
			textwin run data/scripts/text/multiplayer/$name\_[locale].tcl
			textwin show
		}

		proc DMPlayerLose {iPlayer} {
			if {[net localid] == $iPlayer} {
				loadtextwin Lose_DM
			} else {
				newsticker new [net localid] -text "[lmsg player_lost_[expr $iPlayer + 1]]"
//				newsticker new [net localid] -text "Spieler [expr $iPlayer + 1] ist ausgeschieden"
			}
		}

		proc DMPlayerWin {iPlayer} {
			if {[net localid] == $iPlayer} {
				loadtextwin Win_DM
			} else {
				newsticker new [net localid] -text "[lmsg player_won_[expr $iPlayer + 1]]"
//				newsticker new [net localid] -text "Spieler [expr $iPlayer + 1] hat gewonnen"
			}
		}

		proc CWPlayerLose {iPlayer} {
			if {[net localid] == $iPlayer} {
				loadtextwin Lose_CW
			} else {
				newsticker new [net localid] -text "[lmsg player_lost_[expr $iPlayer + 1]]"
//				newsticker new [net localid] -text "Spieler [expr $iPlayer + 1] ist ausgeschieden"
			}
		}

		proc CWPlayerWin {iPlayer} {
			if {[net localid] == $iPlayer} {
				loadtextwin Win_CW
			} else {
				if { $iPlayer == 0 } {
					newsticker new [net localid] -text "Counters haben gewonnen"
				} else {
					newsticker new [net localid] -text "Terrors haben gewonnen"
				}
			}
		}

		proc CFPlayerLose {iPlayer} {
			if {[net localid] == $iPlayer} {
				loadtextwin Lose_CF
			} else {
				newsticker new [net localid] -text "[lmsg player_lost_[expr $iPlayer + 1]]"
//				newsticker new [net localid] -text "Spieler [expr $iPlayer + 1] ist ausgeschieden"
			}
		}

		proc CFPlayerWin {iPlayer} {
			if {[net localid] == $iPlayer} {
				loadtextwin Win_CF
			} else {
				newsticker new [net localid] -text "[lmsg player_won_[expr $iPlayer + 1]]"
//				newsticker new [net localid] -text "Spieler [expr $iPlayer + 1] hat gewonnen"
			}
		}

		proc SMPlayerLose {iPlayer} {
			if {[net localid] == $iPlayer} {
				GameOver
			} else {
				newsticker new [net localid] -text "[lmsg player_lost_[expr $iPlayer + 1]]"
//				newsticker new [net localid] -text "Spieler [expr $iPlayer + 1] ist ausgeschieden"
			}
		}

		proc SMPlayerWin {iPlayer} {
			if {[net localid] == $iPlayer} {
				loadtextwin Win_DM
			} else {
				GameOver
			}
		}

        proc GameOver {} {
            show_loading 1

            set MapOffset [map getoffset]
            set iXN [lindex $MapOffset 0]
            set iYN [lindex $MapOffset 1]
            set iXP [expr $iXN + 100]
            set iYP [expr $iYN + 100]

            reset_map $iXN $iYN $iXP $iYP

            set iXMid [expr ($iXN + $iXP) / 2.0]
            set iYMid [expr ($iYN + $iYP) / 2.0]
            set_pos this "$iXMid $iYMid 15"
            log "************ $iXMid $iYMid 15"
            set_fogofwar this -50 -50

            sel /obj
            set_view 32.2 32.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)
            call templates/unq_tod.tcl
            MapTemplateSet [expr 16 + $iXN] [expr 16 + $iYN]
            show_loading no
            state_disable this
            state_trigger this disabled
        }


		proc SetStartView {} {
			set l [obj_query 0 -owner [net localid] -class Feuerstelle]
			if {$l==0} {
				set l [obj_query 0 -owner [net localid] -class Zwerg]
			}
			set_view [get_posx [lindex $l 0]] [get_posy [lindex $l 0]] 1.5 -0.425 0
		}

		proc SetStartViewCW {} {
			set l [obj_query 0 -owner [net localid] -class Zwerg]
			set_view [get_posx [lindex $l 0]] [get_posy [lindex $l 0]] 1.5 -0.425 0
		}

		set ReleaseList [list]
		proc ReleaseAll {} {
			global ReleaseList
			foreach item $ReleaseList {
				call_method $item ShutGame
			}
		}

		proc RespawnAll {} {
			global ReleaseList
			foreach item $ReleaseList {
				call_method $item StartGame
			}
		}

		proc AppendRelease {obj} {
			global ReleaseList
			lappend ReleaseList $obj
		}

		proc StartObj {obj} {
			set ret [call_method $obj StartGame]
			AppendRelease $obj
			return $ret
		}


		set GnomeSpawnPoints [list]
		proc LoadGnomeSpawnPoints {} {
			global GnomeSpawnPoints iNumGnomes

			set newlist [obj_query this -class GnomeStartPoint -owner any]
			if { $newlist == 0 } {
				log "GameObserver: Error: no GnomeStartPoint's found !!!"
			}

			set ownercouns {0 0 0 0 0 0 0 0}
			foreach item $newlist {
				set iO [get_owner $item]
				set iOC [lindex $ownercouns $iO]

				if { $iOC < $iNumGnomes } {
					lrep ownercouns $iO [expr $iOC + 1]
					lappend GnomeSpawnPoints $item
				}
			}

		}

		proc SpawnGnomes {} {
			global GnomeSpawnPoints
			foreach item $GnomeSpawnPoints {
				StartObj $item
			}
		}

		proc ActivateGnomeSpawnPoints {} {
			global GnomeSpawnPoints CWPlayerTeams

			set NewGnomeSpawnPoints [list]
			foreach item $GnomeSpawnPoints {
				// AddToConfirm
				// item wird in der übernächsten Zeile zu NULL
				set mitem $item
				set iNewOwner [call_method this GetNextCWPart $item]
				if { $iNewOwner == -1 } {
					call_method $mitem destroy
				} else {
					lappend NewGnomeSpawnPoints $mitem
					set iTeam [lindex $CWPlayerTeams $iNewOwner]
					AddToConfirm
					call_method  this ExecuteAt $iNewOwner "set_owner $mitem $iNewOwner {log ERR} \{call_method $mitem CWInit $iTeam;call_method [get_ref this] RemoveFromConfirm \}"

					//call_method $mitem CWInit [lindex $CWPlayerTeams $iNewOwner]
					//call_method $mitem change_owner $iNewOwner
				}


			}
			set GnomeSpawnPoints $NewGnomeSpawnPoints
		}

		proc SpawnGnomesCW {} {
			global GnomeSpawnPoints GnomeOwnerTable CWPlayerTeams
			set CT [list]
			set TT [list]
			foreach item $GnomeSpawnPoints {
				if { [get_owner $item] == 0 } {
					lappend CT $item
				} else {
					lappend TT $item
				}
			}

			set CCnt 0
			set TCnt 0
			set iOwner 0
			foreach item $GnomeOwnerTable {
				for {set i 0} {$i < $item} {incr i} {
					set iTeam [lindex $CWPlayerTeams $iOwner]


					if { $iTeam == 0 } {
						set NextSpawnPoint [lindex $CT [expr [incr CCnt]]]
					} else {
						set NextSpawnPoint [lindex $TT [expr [incr TCnt]]]
					}
                    //log "---> $NextSpawnPoint : $iOwner"
					call_method $NextSpawnPoint SetTargetOwner $iOwner
					call_method $NextSpawnPoint StartGame
					AppendRelease $NextSpawnPoint
				}
				incr iOwner
			}

		}


		set HostageSpawnPoints [list]
		set RescueZone -1
		set HostageList [list]
		proc LoadHostageSpawnPoints {} {
			global HostageSpawnPoints RescueZone
			set HostageSpawnPoints [obj_query this -class CS_HostagePoint -owner any]
			if { $HostageSpawnPoints == 0 } {
				log "GameObserver: Warning: no CS_HostagePoint's found !!!"
				set HostageSpawnPoints [list]
				return
			}
			foreach item $HostageSpawnPoints {
				set_owner $item [get_owner this]
			}
			set RescueZone [obj_query this -class CS_RescueZone -owner any]
			if { $RescueZone == 0 } {
				log "GameObserver: Error: no CS_RescueZone found !!!"
			}
			set_owner $RescueZone [get_owner this]
			StartObj $RescueZone
		}

		proc SpawnHostages {} {
			global HostageSpawnPoints HostageList
			foreach item $HostageSpawnPoints {
				lappend HostageList [StartObj $item]
			}
		}

		set FlagSpawnPoints [list]
		set FlagList [list]
		proc LoadFlagSpawnPoints {} {
			global FlagSpawnPoints
			set FlagSpawnPoints [obj_query this -class CF_FlagPoint -owner any]
			if { $FlagSpawnPoints == 0 } {
				log "GameObserver: Warning: no CF_FlagPoint's found !!!"
				set FlagSpawnPoints [list]
				return
			}
		}

		set FlagStatus -1
		proc SpawnFlags {} {
			global FlagSpawnPoints FlagList
			foreach item $FlagSpawnPoints {
				set newflag [StartObj $item]
				lappend FlagList $newflag
			}
		}

		set FlagRescuePointSpawnPoints [list]
		set FlagRescuePointList [list]
		proc LoadFlagRescuePointSpawnPoints {} {
			global FlagRescuePointSpawnPoints
			set FlagRescuePointSpawnPoints [obj_query this -class CF_FlagRescuePoint -owner any]
			if { $FlagRescuePointSpawnPoints == 0 } {
				log "GameObserver: Warning: no CF_FlagRescuePoint's found !!!"
				set FlagRescuePointSpawnPoints [list]
				return
			}
		}

		proc SpawnFlagRescuePoints {} {
			global FlagRescuePointSpawnPoints
			foreach item $FlagRescuePointSpawnPoints {
				lappend FlagRescuePointList [StartObj $item]
			}
		}


		set HostageRescueCount 0
		set HostageDeathCount 0
		proc CheckHostages {} {
			global HostageList RescueZone HostageDeathCount HostageRescueCount HostageSpawnPoints WinStatus
			set iCnt 0
			foreach item $HostageList {
				if { $item != -1 } {
    				if { ![obj_valid $item] } {
    					// Geisel wurde getötet !
    					log "Geisel wurde getötet !"

    					incr HostageDeathCount

    					//set WinStatus (wer hat sie getötet?)

    					// Geisel wechdamit
    					lrep HostageList $iCnt -1

    				} else {
    					set posRS [get_pos $RescueZone]
    					set posHO [get_pos $item]

    					set Range [call_method $RescueZone GetRange]

    					if { [vector_dist3d $posRS $posHO] <= $Range } {

    						// Geisel zurückgebracht
    						log "Geisel zurückgebracht"

    						incr HostageRescueCount

        					// Geisel wechdamit
        					call_method [lindex $HostageSpawnPoints $iCnt] ShutGame
        					lrep HostageList $iCnt -1
    					}
    				}
    			}
    			incr iCnt
			}

			if { [lcount $HostageList "-1"] == [llength $HostageList] } {
				// Keine Geiseln mehr !!
				if { $HostageRescueCount > 0 } {
					// Geiseln wurden befreit !!
					log "Counterwiggles win!!!"
					set WinStatus 0
				}
			}
		}


		set BombSpawnPoint -1
		proc LoadBombSpawnPoint {} {
			global BombSpawnPoint
			set BombSpawnPoint [obj_query this -class CS_Bomb -owner any -limit 1]
			if { $BombSpawnPoint == 0 } {
				log "GameObserver: Warning: no CS_Bomb found !!!"
			}
		}

		set Bomb -1
		proc SpawnBomb {} {
			global BombSpawnPoint Bomb
			if { $BombSpawnPoint != 0 } {
    	   		set Bomb [StartObj $BombSpawnPoint]
    	   		call_method $Bomb set_on_callback	"call_method [get_ref this] BombOnCallback"
    	   		call_method $Bomb set_def_callback  "call_method [get_ref this] BombDefCallback"
    	   		call_method $Bomb set_exp_callback  "call_method [get_ref this] BombExpCallback"
	    	}
		}


		set BombStatus 0
		proc CheckBomb {} {
			global BombStatus WinStatus
			switch $BombStatus {
				// Angeschaltet
				1	{}

				// Defused
				2   {set WinStatus 0;log "Bombe entschärft"}

				// Explodiert
				3   {set WinStatus 1;log "Bombe explodiert"}
			}
		}

		set BombPlaceSpawnPoints [list]
		proc LoadBombPlaceSpawnPoints {} {
			global BombPlaceSpawnPoints
			set BombPlaceSpawnPoints [obj_query this -class CS_BombPlace]
			if { $BombPlaceSpawnPoints == 0 } {
				log "GameObserver: Warning: no CS_BombPlace found !!!"
				set BombPlaceSpawnPoints [list]
			}
		}

		proc SpawnBombPlaces {} {
			global BombPlaceSpawnPoints
			foreach item $BombPlaceSpawnPoints {
    	   		StartObj $item
	    	}
		}

		set cs_obj_list {Kleiner_Heiltrank Heiltrank Grosser_Heiltrank Unverwundbarkeitstrank Unsichtbarkeitstrank Grillpilz Grillhamster \
				 AK47 MP5 M4 Para M3_super_90 Duals Awp Deagle}

		set ItemSpawnPoints [list]
		proc LoadItemSpawnPoints {} {
			global cs_obj_list ItemSpawnPoints

			set iCnt 0
			foreach item $cs_obj_list {
				lrep cs_obj_list $iCnt CS_OP_$item
				incr iCnt
			}

			set ItemSpawnPoints [obj_query this -class $cs_obj_list]
			if { $ItemSpawnPoints == 0 } {
				log "GameObserver: Warning: no objectSpawnPint found !!!"
				set ItemSpawnPoints [list]
			}
		}

		proc SpawnItems {} {
			global ItemSpawnPoints
			foreach item $ItemSpawnPoints {
				StartObj $item
			}
		}

		set PlayerAlive {0 0 0 0 0 0 0 0}
		set PlayerAliveTableInitialized 0
		proc InitPlayerAliveTable {} {
    		global PlayerAlive bServerMode PlayerAliveTableInitialized
    		;# untersuchen, welche Spieler am Spiel teilnehmen
    		set PlayerAlive {0 0 0 0 0 0 0 0}

    		;# alle Zwergenbesitzer in liste eintragen
    		set GnomeList [obj_query 0 -class Zwerg]
    		foreach id $GnomeList {
    			set GnomeOwner [get_owner $id]
    			if {($GnomeOwner>=0) && ($GnomeOwner<8)} {
    				lrep PlayerAlive $GnomeOwner 1
    			}
    		}
    		set PlayerAliveTableInitialized 1
		}

		proc InitPlayerAliveTableCW {} {
    		global PlayerAlive bServerMode PlayerAliveTableInitialized GnomeOwnerTable
    		;# untersuchen, welche Spieler am Spiel teilnehmen
    		set PlayerAlive {0 0 0 0 0 0 0 0}

    		;# alle Zwergenbesitzer in liste eintragen
    		set iOwner 0
    		foreach id $GnomeOwnerTable {
    			if { $id > 0 } {
   					lrep PlayerAlive $iOwner 1
   				}
   				incr iOwner
    		}
    		set PlayerAliveTableInitialized 1
		}


		set WinStatus	 -1
		set PlayerStatus {-1 -1 -1 -1 -1 -1 -1 -1}
		set CheckPlayersLivingMinCount 8
		proc CheckPlayersLiving {} {
			global PlayerAlive LastPlayerAlive PlayerAliveCount PlayerStatus WinStatus PlayerAliveTableInitialized CheckPlayersLivingMinCount

			if { $PlayerAliveTableInitialized == 0 } {
				return
			}
			if { $CheckPlayersLivingMinCount > 0 } {
				incr CheckPlayersLivingMinCount -1
				return
			}

    		set LastPlayerAlive $PlayerAlive
    		set PlayerAlive {0 0 0 0 0 0 0 0}

    		;# Neuberechnung der lebenden Spieler
    		set GnomeList [obj_query 0 -class Zwerg]
    		foreach id $GnomeList {
    			set GnomeOwner [get_owner $id]
    			if {($GnomeOwner>=0) && ($GnomeOwner<8)} {
    				lrep PlayerAlive $GnomeOwner 1
    			}
    		}

    		;# Checken, welche ausgeschieden sind und wieviele noch am Leben sind
    		set PlayerAliveCount 0
    		set iLastPlayerIdx -1
    		for {set i 0} {$i < 8} {incr i} {
    			if {[lindex $PlayerAlive $i] != [lindex $LastPlayerAlive $i]} {
    				lrep PlayerStatus $i "lose"
    			}
    			if {[lindex $PlayerAlive $i]} {
    				incr PlayerAliveCount
    				set iLastPlayerIdx $i
    			}
    		}

			// Singleplayer Skirmish
			set LivingCountDip 0
			for {set i 1} {$i < 8} {incr i} {
				set pa [lindex $PlayerAlive $i]
				if { $pa } {
					if { [get_diplomacy 0 $i] != "ally" } {
						incr LivingCountDip
					}
				}
			}
			if { $LivingCountDip == 0 } {
				set WinStatus 0
			}

			//log "----------------------------------------- $LivingCountDip"

    		;# Spielende checken
    		if {$PlayerAliveCount<=1} {
    			log "Alle Spieler tot"
    			set WinStatus $iLastPlayerIdx
    		}
    	}

		proc DMWinCondition {} {
			global WinStatus PlayerStatus

			for {set i 0} {$i < 8} {incr i} {
				if { [lindex $PlayerStatus $i] == "lose" } {
					lrep PlayerStatus $i 0
       				call_method this ExecuteAll "
       					DMPlayerLose $i
       				"
				}
			}

			if { $WinStatus >= 0 } {
       			call_method this ExecuteAll "
       				log \"game over\"
       				DMPlayerWin $WinStatus
       				gui_new_game
       			"

       			state_trigger this disabled
       			state_disable this
			}
		}

		proc CWWinCondition {} {
			global WinStatus PlayerStatus

			for {set i 0} {$i < 8} {incr i} {
				if { [lindex PlayerStatus $i] == "lose" } {
					lrep PlayerStatus $i 0
       				call_method this ExecuteAll "
       					CWPlayerLose $i
       				"
				}
			}

			if { $WinStatus >= 0 } {
       			call_method this ExecuteAll "
       				log \"game over\"
       				CWPlayerWin $WinStatus
       				//gui_new_game
       			"

       			state_trigger this disabled
       			state_disable this
			}
		}

		proc CFWinCondition {} {
			global WinStatus PlayerStatus FlagStatus

			for {set i 0} {$i < 8} {incr i} {
				if { [lindex PlayerStatus $i] == "lose" } {
					lrep PlayerStatus $i 0
       				call_method this ExecuteAll "
       					CFPlayerLose $i
       				"
				}
			}

			//log "-> $WinStatus $FlagStatus"
			set WinStatus $FlagStatus

			if { $WinStatus >= 0 } {
       			call_method this ExecuteAll "
       				log \"game over\"
       				CFPlayerWin $WinStatus
       				//gui_new_game
       			"

       			state_trigger this disabled
       			state_disable this
			}
		}

		proc SMWinCondition {} {
			global WinStatus PlayerStatus FlagStatus

			for {set i 0} {$i < 8} {incr i} {
				if { [lindex $PlayerStatus $i] == "lose" } {
					lrep PlayerStatus $i 0
       				SMPlayerLose $i
				}
			}

			//log "-> $WinStatus"
			//set WinStatus $FlagStatus

			if { $WinStatus >= 0 } {
       			SMPlayerWin $WinStatus

       			state_trigger this disabled
       			state_disable this
			}
		}

		proc RestartCW {} {
			global BombStatus CheckPlayersLivingMinCount WinStatus

			set BombStatus 0
			set CheckPlayersLivingMinCount 8
			set WinStatus -1

			ReleaseAll
			RespawnAll
    		InitPlayerAliveTable
    		state_triggerfresh this CounterWigglesState
		}

		proc RestartCF {} {
			global FlagStatus CheckPlayersLivingMinCount WinStatus

			set FlagStatus -1
			set CheckPlayersLivingMinCount 8
			set WinStatus -1

			ReleaseAll
			RespawnAll
    		InitPlayerAliveTable
    		state_triggerfresh this CaptureTheFlagState
		}

		set IsCW 0
		set GnomeCreateTableC {0 0 0 0 0 0 0 0}
		set GnomeCreateTableT {0 0 0 0 0 0 0 0}

		set iGnomeSpawnPointConfirmCount 0
		set ConfirmCode ""
		proc WaitForConfirm {code} {
			global ConfirmCode iGnomeSpawnPointConfirmCount
			set ConfirmCode $code
			state_triggerfresh this WaitForConfirmCount
		}
    	proc AddToConfirm {} {
    		global iGnomeSpawnPointConfirmCount
    		incr iGnomeSpawnPointConfirmCount 1
    	}

    	proc RemoveFromConfirm {} {
    		global iGnomeSpawnPointConfirmCount
    		incr iGnomeSpawnPointConfirmCount -1
    	}

    	set iNumGnomes 5

	} ;# // obj_init

	method InitNumGnomes {iGnomes} {
		set iNumGnomes $iGnomes
	}

	method BombOnCallback {} {
		set BombStatus 1
	}

	method BombDefCallback {} {
		set BombStatus 2
	}

	method BombExpCallback {} {
		set BombStatus 3
	}

	method GetWinStatus {} {
		return $WinStatus
	}

	method FlagCallback {iOwner} {
		set FlagStatus $iOwner
	}


// Init-Methoden
	method Init {GameMode} {
		global bServerMode

		set bServerMode [expr [get_owner this] == 0]

		if {$bServerMode} {
			set ObsList [obj_query 0 -class GameObserver]

			foreach id $ObsList {
				call_method $id InitObserverList $ObsList
			}

			log "************************* MainObs [get_ref this]"
		}

		switch $GameMode {
			"DM"	{	call_method this InitDeathMatch	}
			"CW"	{	call_method this InitCounterWiggles	}
			"CF"	{	call_method this InitCaptureTheFlag	}
			"SM"	{	call_method this InitSkirmish	}
		}
	}

	method InitPlayerData { PlayerData } {
		set PlayerTypes 	[list]
		set PlayerRaces 	[list]
		set PlayerColors 	[list]
		set PlayerTeams 	[list]

		set CWPlayerTeams 	[list]

		for {set i 0} {$i < 8} {incr i} {
        	set actplayer [lindex $PlayerData $i]

        	lappend PlayerTypes	  [lindex $actplayer 0]
        	lappend PlayerRaces   [lindex $actplayer 1]
        	lappend PlayerColors  [lindex $actplayer 2]
        	lappend PlayerTeams   [lindex $actplayer 3]

			set iTeam [expr ([lindex $actplayer 3] + 1) % 2]
			if { [lindex $PlayerTypes $i] != 1 } {
				set iTeam -1
			}
        	lappend CWPlayerTeams $iTeam

        	//if { [lindex $actplayer 0]  != 0 } {
        	//	lappend teamlist [lindex $actplayer 3]
        	//}

        }

		set CWNumCounters 0
		set CWNumTerrors 0

        foreach item $CWPlayerTeams {
        	if { $item == 0 } {
        		incr CWNumCounters
        	} elseif { $item == 1 } {
        		incr CWNumTerrors
        	}
        }
		set CWNumTeams [expr $CWNumCounters + $CWNumTerrors]

		set CWMaxGnomesPerTeam 10
		set CWOptGnomesPerTeam 3

		//set MaxNumPlayersPerTeam [hmax $CWNumCounters $CWNumTerrors]
		set NumGnomesC [expr $CWMaxGnomesPerTeam / [hmax 1 $CWNumCounters]]
		set NumGnomesT [expr $CWMaxGnomesPerTeam / [hmax 1 $CWNumTerrors]]

		set NumGnomesC [hmin $NumGnomesC $CWOptGnomesPerTeam]
		set NumGnomesT [hmin $NumGnomesT $CWOptGnomesPerTeam]

		set GnomeOwnerTable {0 0 0 0 0 0 0 0}
		set iCnt 0
        foreach item $CWPlayerTeams {
        	if { $item == 0 } {
        		lrep GnomeOwnerTable $iCnt $NumGnomesC
        	} elseif { $item == 1 } {
        		lrep GnomeOwnerTable $iCnt $NumGnomesT
        	}
        	incr iCnt
        }

        log "++ CWD: $CWNumTeams $CWNumCounters $CWNumTerrors : $NumGnomesC $NumGnomesT"
	}

	method GetNextCWPart {caller} {
		global GnomeCreateTableC GnomeCreateTableT CWPlayerTeams GnomeOwnerTable
		set iOrgOwner [get_owner $caller]

		if { $iOrgOwner == 0 } {

			set iOwner 0
			foreach item $GnomeCreateTableC {
				if { [lindex $CWPlayerTeams $iOwner] == $iOrgOwner  && $item < [lindex $GnomeOwnerTable $iOwner] } {
					lrep GnomeCreateTableC $iOwner [expr $item + 1]
					//set_owner $caller $iOwner {}
					return $iOwner
				}
				incr iOwner
			}

		} else {

			set iOwner 0
			foreach item $GnomeCreateTableT {
				if { [lindex $CWPlayerTeams $iOwner] == $iOrgOwner  && $item < [lindex $GnomeOwnerTable $iOwner] } {
					lrep GnomeCreateTableT $iOwner [expr $item + 1]
					//set_owner $caller $iOwner {}
					return $iOwner
				}
				incr iOwner
			}
		}

        return -1
	}

	method RemoveFromConfirm {} {
		RemoveFromConfirm
	}

	state WaitForConfirmCount {
		log "~~~~ $iGnomeSpawnPointConfirmCount"
		if { $iGnomeSpawnPointConfirmCount <= 0 } {
			log "suuuuuuuup"
			eval $ConfirmCode
			return
		}
		//incr iGnomeSpawnPointConfirmCount -1
		wait_time 0.1
	}

// Serverliste eintragen
	method InitObserverList {ObsList} {
		global ObserverList

		foreach id $ObsList {
			lrep ObserverList [get_owner $id] $id
		}
	}

// Execute
	method Execute {Code} {
		eval $Code
	}

// ExecuteAll
	method ExecuteAll {Code} {
		global bServerMode ObserverList
		if {!$bServerMode} {log "WARNING: ExecuteAll@!Server"}

		foreach id $ObserverList {
			call_method $id Execute $Code
		}
	}

// ExecuteAt
	method ExecuteAt {iOwner Code} {
		//log "---------------- [get_owner this]"
		//set GO [obj_query this -class GameObserver -owner $own -limit 1]
		global ObserverList
		set own [get_owner this]
		set GO [lindex $ObserverList $own]
		if { $GO == 0 } {
			log "obj_query this -class GameObserver -owner $own -limit 1 == [obj_query this -class GameObserver -owner $own -limit 1]"
			log "GameObserver: (ExecuteAt) Error no GameObserver found !!"
			gametime factor 0.0
			return
		}
		call_method [call_method $GO GetObserver $iOwner] Execute $Code
	}

	method GetObserver {iOwner} {
		global bServerMode ObserverList
		return [lindex $ObserverList $iOwner]
	}

	//static method remote
	method ExecBroadcast {Code} {
		set GO [obj_query this -class GameObserver -owner own]
		if { $GO == 0 } {
			log "GameObserver: (ExecBroadcast) Error no GameObserver found !!"
			return
		}
		call_method $GO ExecuteAll $Code
	}

	method ExecBroadcastWin {Code} {
		set GO [obj_query this -class GameObserver -owner 0]
		if { $GO == 0 } {
			log "GameObserver: (ExecBroadcastWin) Error no GameObserver found !!"
			return
		}
		set iWinner [call_method $GO GetWinStatus]
		set GO [obj_query this -class GameObserver -owner $iWinner]
		if { $GO == 0 } {
			log "GameObserver: Error no GameObserver found !!"
			return
		}
		call_method $GO ExecuteAll $Code
	}

	method InitCW {} {
		global IsCW
		set IsCW 1
	}

	method IsCW {caller} {
		global IsCW
		log "--> [get_ref this]"
		return $IsCW
	}

//////////////////////////////
///// DeathMatch

	method InitDeathMatch {} {
		global PlayerAlive bServerMode

		if {$bServerMode} {

    		LoadGnomeSpawnPoints
    		SpawnGnomes
    		LoadItemSpawnPoints
    		SpawnItems
    		InitPlayerAliveTable

			call_method this ExecuteAll "loadtextwin Start_DM"
			state_triggerfresh this DeathMatchState
		} else {
			state_trigger this disabled
			state_disable this
		}

		SetStartView
	}

	state DeathMatchState {

		CheckPlayersLiving
		DMWinCondition

		wait_time 1
	};

//////////////////////////////
///// CounterWiggles

	method InitCounterWiggles {} {

		if {$bServerMode} {

            LoadHostageSpawnPoints
            SpawnHostages

    		LoadItemSpawnPoints
    		SpawnItems

    		LoadBombSpawnPoint
    		SpawnBomb

    		LoadBombPlaceSpawnPoints
    		SpawnBombPlaces

			LoadGnomeSpawnPoints
			ActivateGnomeSpawnPoints

			call_method this ExecuteAll "loadtextwin Start_CW"
			//state_trigger this CounterWigglesState

			// Die Startpunkte müssen sich erst auf die Clients transferieren
			WaitForConfirm "state_triggerfresh this CounterWigglesState ; SpawnGnomes ; call_method this ExecuteAll SetStartViewCW ; InitPlayerAliveTableCW"


		} else {
			state_trigger this disabled
			state_disable this
		}

	}

	state CounterWigglesState {

		CheckPlayersLiving
		CheckHostages
		CheckBomb

		CWWinCondition

    	wait_time 1
	};

//////////////////////////////
///// CaptureTheFlag

	method InitCaptureTheFlag {} {

		if {$bServerMode} {

			LoadFlagSpawnPoints
			SpawnFlags

			LoadFlagRescuePointSpawnPoints
			SpawnFlagRescuePoints

    		LoadGnomeSpawnPoints
    		SpawnGnomes

			call_method this ExecuteAll "loadtextwin Start_CF"
			state_triggerfresh this CaptureTheFlagState
		} else {
			state_trigger this disabled
			state_disable this
		}
		SetStartView
	}

	state CaptureTheFlagState {

		CheckPlayersLiving
		CFWinCondition

    	wait_time 1
	};

//////////////////////////////
///// Skirmish

	method InitSkirmish {} {
		global PlayerAlive bServerMode

   		LoadGnomeSpawnPoints
   		SpawnGnomes

//   		LoadItemSpawnPoints
//   		SpawnItems
//   		InitPlayerAliveTable
//			call_method this ExecuteAll "loadtextwin Start_DM"

		InitPlayerAliveTable

		state_triggerfresh this SkirmishState

		SetStartView
	}

	state SkirmishState {

		//CheckPlayersLiving
		//DMWinCondition

		CheckPlayersLiving
		SMWinCondition

		wait_time 1
	};


//////////////////////////////
/////

	state disabled {
		log "GameObserver Warning: illegal entered state: 'disabled' "
    	state_disable this
	};
}
