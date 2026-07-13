// z_fight.tcl
if {[in_class_def]} {

	state fight_dispatch {

		//set fight_log 1	; # delme!

		// ungültiger defender
		if { ! [obj_valid $attack_item] } {
			if { ![fight_find_new_defender] } {
				fight_cancel "no defender near"
			}
			return
		}

		incr hcnt
		if {$fight_log} {log "fight [get_objname this] ($hcnt):$attack_behaviour $current_fightmode ($weapon_range)"}

		//###test
		if { ![fight_checkhp] } {
			fight_exit "i'm dead"
		}

		// Music

		fight_music_handling

		// Kampf beendet
		if { $current_fightmode == -1 } {
			log "fight [get_objname this]:-1"
			state_trigger this idle
			return
		}

		// Ist schon in einer Action
		if { [get_attackinprogress this] } {
			if {$fight_log} {log "i'm in progress"}
			return
		}

		// Waffe und Schild
		if { $current_fightmode == 0 } {
			set current_fightmode 1
			if { [weapon_shield_takeout 1] } {
				return
			}
		}

		// Angriffsposition
		if { $current_fightmode == 1 } {
			set current_fightmode 2
			set fd [fight_doapproach -1 -1]
			if { $fd == 0 } {
				fight_cancel "No free pos found!"
			} elseif { $fd == 1 } {
				return
			}
		}

		// NOP
		if { $current_fightmode == 2 } {
			set current_fightmode 3
			#fight_notifytarget
			//return
		}

		// Standanimation
		if  { $current_fightmode == 3 } {
			set_fight_idleanim
			set current_fightmode 4
			//return
		}

        // Waffen und Schid nochmals überprüfen
		if { [weapon_check] } { return }

		// Singleplayer (Drachen beim Feuerspeien nicht unterbrechen)
   		if { [get_objclass $attack_item] == "Drache" } {
   			if { [call_method $attack_item get_burning] == 1 } {
   				return
   			}
   		}

   		// Hilfe rufen
		if { [fight_help] == 1 } {
			return
		}

		// Flucht ?
		if { [fight_escape] == 1 } {
			fight_exit "escape"
			return
		}

		// Tricks (Brut)
		if { $current_fightmode == 5 && [fight_trick] } {
			return
		}

		//log "[get_objname this] WS:$wait_state "

		// Warteschlange
		if { $wait_state == 1 } {
		//log "[get_objname this] wartet"
			if { ![fight_find_new_defender] } {

				log "[get_objname this] wartet $wait_timeout"

    			incr wait_timeout -1
    			if { $wait_timeout <= 0 } {
    				fight_cancel "wait_timeout"
    				return
    			}

				//log "[get_objname this] rotto: [fight_rotate_towards]"
       	    	if { [fight_rotate_towards] == 2 } {
    				fight_wait_anim
    			} else {
    				// Alles Oki
    				set wait_state 2
    			}
    			return
    		} else {
    			return
    		}


		}

		set ecname [get_objclass $attack_item]
		set ectype [get_class_type $ecname]
		if {$ectype == "production"  ||  $ectype == "elevator"  ||  $ectype == "store"  ||
			$ectype == "energy"  ||  $ectype == "protection"} {
			set_attrib this fight_Counter 0
		} elseif { [string first $ecname "Fresspflanze Alienpflanze Elfenfluegel Riesenlaufrad FenrisFuss"] != -1 } {
			set_attrib this fight_Counter 0
		}

		if { [call_method $attack_item check_first_strike [get_ref this]] == 0 } {
			//log "[get_objname this] darfnichthauen"
			// Darf noch nicht schlagen wegen Erstschlag
			set fd [fight_doapproach]
			if { $fd == 2 } {
				set fr [fight_rotate_towards]
				if { $fr == 2 } {
					fight_wait_anim
    			}
    			state_disable this
    		} elseif { $fd == 0 } {
    			fight_cancel "out_of_range"
    		}
			return
		}

		if {$ecname == "Krake"} {
			if { $weapon_range <= 1.0 } {
    			set fd [fight_doapproach_bal 1]
    			if { $fd == 1 } {
    				return
    			}
    		}

		}

    	// Angriff
    	if { $weapon_range == 1.0 } {
    		// Nahkampf
    		set fresult [fight_setactions this $attack_item "state_enable [get_ref this]" "state_enable $attack_item"]
    	} else {
    		// Fernkampf
    		set fresult [fight_setactions_ballistic this $attack_item "state_enable [get_ref this]"]
    	}


    	if {$fight_log} {log "fres [get_objname this]: $fresult"}

		set WasProd 0

    	foreach item $fresult {
    		// Produktionsstätten und Protection
    		if {$item == "production"} {
    			call_method $attack_item show_damage
    			set myowner [get_owner this]
    			set otherowner [get_owner $attack_item]
				if {$myowner >= 0 &&  $otherowner >= 0} {
    				set_diplomacy $otherowner $myowner enemy
				}
    			if { ![is_living $attack_item] } {
    				log "[get_objname $attack_item] is destroyed"
    				set current_fightmode -1
    				call_method $attack_item destroy
    			}
    			set WasProd 1
    		}

    		// Angriffsziel hat sich geändert
    		if { [lindex $item 0] == "new_defender" } {
    			set attack_item [lindex $item 1]
    			log "[get_objname this]:New Defender [get_objname $attack_item]"
    			break
    		}

    		// Angriff erfolgreich
    		if { [lindex $item 0] == "attack" } {
    			set item [lindex $item 1]
    			fight_notifytarget $item

    			set bFirstStriked 1

    			// Extra Sound
    			if { $WasProd && $weapon_range <= 1.0 } {
					set snd "tuer_schlag_a"
					set pose [get_weapon_pose [get_weapon_class this]]
					if { $pose != "kungfu" } {
						set snd "tuer_schlag_b"
					}
    				set_event this evt_hit_sound_notify -target $attack_item -delay 0.3 -text1 $snd
    			}

				// Gegner zu weit weg?
				if { $weapon_range > 1.0 } {
					set actdist [vector_dist3d [get_pos this] [get_pos $attack_item]]
					if { $actdist > $weapon_range } {
        				set fd [fight_doapproach_bal 1]
        				if { $fd == 1 } {
           					return
            			}
            		}
        		}

    			break
    		}

    		// Angreifer ist zu weit entfernt
    		if { $item == "out_of_range" } {
    			fight_cancel "out_of_range"
    			return
    		}

    		// Tot
    		if { $item == "defender_is_dead" } {
    			fight_cancel "defender is dead"
    		}

    		// Zum Angreifer drehen
    		if { $item == "rerotate" || $item == "rotate" } {
    			fight_rotate_towards
    		}

    		// Zu weit wech
    		if { [lindex $item 0] == "reapproach" } {
    			log "reapproach"
    			set ap [fight_doapproach [lindex $item 1]]
    			if { $ap != 1 } {
    				fight_wait_anim 0.4
    			}
    		}

    		if { $item == "miss" } {
    			if { $weapon_range > 1.0 } {
    				log "missbalistic.............."
    				set fd [fight_doapproach_bal 1]
    				if { $fd == 1 } {
    					return
    				}
    			}
    			//log "--> MISS"
				set fr [fight_rotate_towards]
				//log "-> fr: $fr"
				if { $fr == 2 } {
					//log "******************Wait"
					fight_wait_anim 0.4
    			}
                state_disable this
    			//log "------> se: [state_getenablecnt this]"
    			return

    			//state_disable this
    			//action this wait 0.1 {state_enable this}
    		}
    	}

	}

	state_leave fight_dispatch {
		set help_list [list]
		set was_called 0
		if { [get_objclass this] == "Zwerg" } {
			call_method this reset_fanim_feeling

			//Nach 10 Sekunden MessageID löschen
            timer_event this evt_timer_NTIDReset -repeat 1 -interval 1 -attime [expr [gettime] + 10]
		}
		//log "[get_objname this] leaves fight: (WalkRes: [get_walkresult this]) ???"
	}

	method get_was_called {} {
		return $was_called
	}

	method set_was_called {} {
		set was_called 1
	}

	method cause_damage {val} {
		add_attrib this atr_Hitpoints $val
	}

	method get_fratio {} {
		return $fratio
	}

	method fight_checkhp {} {
		return fight_checkhp
	}

	method get_current_shield_out {} {
		return $current_shield_out
	}

	method is_escaping {} {
		return $is_escaping
	}

	// virtual
	method im_attacking_you {} {
	}

	method is_tricking {} {
		return $is_tricking
	}

	method get_wait_state {} {
		return $wait_state
	}

	method get_attack_item {} {
		return $attack_item
	}

	method fight_getattackbehaviour {} {
		if { [state_get this] == "fight_dispatch" } {
			return $attack_behaviour
		} else  {
			return none
		}
	}

	method check_first_strike {caller} {
		global fight_user bFirstStriked
		if { !$fight_user } {
			// Erstschlag nur bei Usereingriff
			return 1
		}
		if { [get_objclass $caller] == "Zwerg" } {
			// und nicht bei Zwergen
			return 1
		}
		return $bFirstStriked
	}

	def_event evt_fight_get_hurt
	handle_event evt_fight_get_hurt {
		evt_fight_get_hurt_proc
	}

	def_event evt_timer_NTIDReset
	handle_event evt_timer_NTIDReset {
		log "evt_timer_NTIDReset fuer [get_ref this]: [get_objname this] wurde ausgelöst"
		ntid_reset this
	}


} else {
	set fight_log 0

// fight modes:
//	-1 none (initializing)
//	0 invalid
//	1 attack:chase
//	2 attack:turn
//	3 defend:turn
//	4 both:anim

	set current_fightmode -1
	set attacker1 ""
	set attacker2 ""
	set look_dir right
	set fight_trainactions [list]
	set help_list [list]
	set was_called 0
	set help_counter 0
	set fratio 0
	set is_escaping 0
	set is_tricking 0
	set player_aggressivity -1
	set escape_range 5
	set fight_user 0
	set weapon_range 1.0
	set wait_pos 0
	set wait_state 0
	set wait_timeout 20
	set fight_run_pos {0 0 0}
	set fight_last_dist -1
	set fight_action_centre 0
	set bFirstStriked 0
	set died_in_fight 0
	set attack_item 0

	set hcnt 0

	lappend fight_trainactions kungfu {.fistmiddle .fisthead .masterhead .footmiddle .foothead .footbottom \
	.handmiddle .handhead .middleblo .headblo .bottomblo .jumphead .jumpmiddle .jumpbottom}
	lappend fight_trainactions sword {.turn .twist .salut .duck .jump .side .sideb .back \
	.middleblo .headblo .bottomblo .midstroke .midstab .headstroke .headstab .botstroke .botstab .upstroke .downstroke}
	lappend fight_trainactions twohand {.jump .sideb .middleblo .headblo .bottomblo \
	.headdownblo .midstroke .twistroke .headstroke .upstroke .downstroke .botstroke}
	lappend fight_trainactions shoot {}
	lappend fight_trainactions defense {kungfuskip kungfuduck kungfujump kungfuside kungfuback kungfusideb standshieldblo shieldheadblo shieldmiddleblo shieldbottomblo}

	proc evt_fight_get_hurt_proc {} {
		set hploss [event_get this -num1]
		set attacker [event_get this -subject1]

		add_attrib this atr_Hitpoints -$hploss
		set_particlesource this 1 5
		set_particlesource this 2 5
	}

	proc fight_checkhp {} {
		global died_in_fight current_fightmode player_aggressivity
		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			set current_fightmode -1
			if {$player_aggressivity!=-1} {
				adjust_player_aggr
			}
			set died_in_fight 1
			return 0
		}
		return 1
	}

	proc fight_music_handling {} {
		global attack_item
		if { [get_owner this] == [net localid] } {
			set ocl [get_objclass this]
			if { $ocl == "Zwerg" || $ocl == "Drachenbaby" } {

				if { $ocl == "Zwerg" } {
					if { [im_in_tutorial] } {
						adaptive_sound autofight tournament
						return
					}
				}

				set EnemyClass [get_objclass $attack_item]
				set Zone [sm_get_zone [get_posy this]]

				set FightTheme "unknown"

				if { $EnemyClass == "FenrisFuss" } {
				// Fenris
					set FightTheme "fenris"
				} elseif { [string range $EnemyClass 0 11] == "ElfenFluegel" } {
				// Riesenelfe
					set FightTheme "walhalla"
				} elseif { $EnemyClass == "Zwerg" } {
				// Zwerg
					set EnemyOwner [get_owner $attack_item]
					switch $EnemyOwner {
						1	{ set FightTheme "peacer"	}	;# Voodoos
						2	{ set FightTheme "knockers" }	;# Kockers
						3	{ set FightTheme "brains" 	}	;# Brains
						4	{ set FightTheme "mauls" 	}	;# Vampyres
					}
				}
				if { $FightTheme == "unknown" } {
				// Monster
					switch $Zone {
						"Urwald"	{ set FightTheme "trolle"		}
						"Metall"	{ set FightTheme "metalltrolle"	}
						"Kristall"	{ set FightTheme "trollfestung"	}
						"Urwald"	{ set FightTheme "lavatrolle"	}
						default		{
										//log "Warning: couldn't figure out Fight-Theme ([get_ref this]-[get_objname this] vs. [get_ref $attack_item]-[get_objname $attack_item])"
										return
										//set FightTheme "trolle"
									}
					}
				}
                //log "*** adaptive_sound autofight $FightTheme"
				adaptive_sound autofight $FightTheme
			}
		}
	}


	proc fight_find_new_defender {} {
    	global weapon_range attack_item wait_state

    	set oelist [obj_query this -boundingbox { -8 -0.5 -16 8 0.5 16 } -type {gnome monster} -owner { enemy } ]
    	log "[get_objname this]: newdeflist: [get_ref this]  ( $oelist )"
    	if { $oelist != 0 } {
    		foreach item $oelist {

    			if { [get_objtype $item] == "gnome"  &&  [get_owner $item] == -1 } {
					// keine herrenlosen Zwerge (Einsiedler, Torwächterin)
   					continue
    			}

				set thisowner [get_owner this]
				set itemowner [get_owner $item]

				if { $thisowner == $itemowner } {
					continue
				}

				//if { [get_objclass $item] == [get_objclass this] } {
					// Trolle sollen keine Trolle angreifen, aber dafür Wuker
				//	continue
				//}

    			set ehp [get_attrib $item atr_Hitpoints]

    			if { $ehp >= 0.01 } {

        			// ballistic
        			if { $weapon_range > 1.0 } {
        			}

        		    // offensive
        			set apos [get_attack_pos this $item]
        			if { $apos != 0 } {
        				log "[get_objname this]:New Defender found: [get_objname $item] @ ($apos)"
        				set attack_item $item
        				set wait_state 2
        				fight_doapproach
        				return 1
        			}
        		}
    		}
    	}
    	return 0
	}

	proc weapon_check {} {
	    global attack_item weapon_range attack_behaviour current_weapon_out current_weapon_item current_shield_out current_shield_item

		if { ![info exists current_weapon_out] } { return 0 }

	    set bChange [set_weapons]

		if {!$bChange} {
			// waffenzuecken wurde unterbrochen
			if { $current_weapon_out != $current_weapon_item || $current_shield_out != $current_shield_item } {
				weapon_shield_takeout 0
			}
			return 0
		}
		weapon_shield_takeout 1
		return 1
	}

	proc fight_help {} {
		global attack_item help_list help_counter

		// Nur alle X Ticks
		if { $help_counter > 0 } {
			incr help_counter -1
			return
		}

		//if { ![check_method [get_objclass $attack_item] get_fratio] } {
		//	return
		//}

		set help_counter 5

		// Nur eigene Zwerge zu Hilfe rufen
		set objclass [get_objclass this]
		if { $objclass == "Zwerg" } {
			set zl [lnand $help_list [obj_query this "-class Zwerg -owner own -boundingbox {-4 -2 -10 4 2 10}"]]
			if { $zl != 0 } {
				foreach item $zl {
					if { [state_get $item] != "fight_dispatch" } {
						if { [get_attrib $item atr_Hitpoints] > [get_escape_value [get_objclass $item] [get_owner $item]] } {
							if { [call_method $item get_was_called] == 0 } {
    							log "help !! ([get_objname $item])"
    							lappend help_list $item
								if { [state_get $attack_item] != "trapped" } {
    								set_event $item evt_task_attack -target $item -subject1 $attack_item
    							}
    							call_method $item set_was_called
    						}
						}
					}
				}
			}
		}
		return 0
	}


	proc calc_escape_pos {} {
		global escape_range

		set kl [obj_query this -class Krankenhaus -owner own]
		if { $kl != 0 } {
			log "escape to [get_objclass [lindex $kl 0]]"
			set pos [get_pos [lindex $kl 0]]
			set epos [get_place -center $pos -circle 15 -except this -placelockidexcept [get_ref this] -walldist 1.5 -rimdist 0.8]
			if { [lindex $epos 0] != -1  } {
				return $epos
			}
		}
		set kl [obj_query this -class {Feuerstelle Zelt} -owner own]
		if { $kl != 0 } {
			log "escape to [get_objclass [lindex $kl 0]]"
			set pos [get_pos [lindex $kl 0]]
			set epos [get_place -center $pos -circle 15 -except this -placelockidexcept [get_ref this] -walldist 1.5 -rimdist 0.8]
			if { [lindex $epos 0] != -1  } {
				return $epos
			}
		}

		set ocl [get_objclass this]
		set el [list]
		if { [string first $ocl "Zwerg"] != -1 } {
			set enemy_classes {Wuker Troll Drache Krake Spinne Alienpflanze Fresspflanze}
			set el [lor $el [lnand [lor 0 $el] [obj_query this -class Zwerg -owner other -range 10]]]
			set el [lor $el [lnand [lor 0 $el] [obj_query this -class $enemy_classes -range 10]]]
		}
		if { [string first $ocl "Wuker Troll"] != -1 } {
			set el [lor $el [lnand [lor 0 $el] [obj_query this -class Zwerg -range 10]]]
		}

		//log "esc: el: $el"

		set posnum 0
		set posvec {0 0 0}
		set clipping ""
		set ownpos [get_pos this]
		foreach item $el {
			set ipos [get_pos $item]
			incr posnum
			set posvec [vector_add $ipos $posvec]
			set clipping "$clipping -clip [expr [lindex $ipos 0] - 1.5] [expr [lindex $ipos 2] - 1.5] [expr [lindex $ipos 0] + 1.5] [expr [lindex $ipos 2] + 1.5]"
		}

		if { $posnum == 0 } {
			return 0
		}

		set mult [expr 1.0 / $posnum.0]
		set posvec [vector_mul $posvec $mult]
		set dir [vector_mul [vector_normalize [vector_sub $ownpos $posvec]] $escape_range]
		set preft [vector_add $ownpos $dir]
		set npos  [vector_add $ownpos [vector_mul $dir 2]]
		//set npos [lreplace $npos 2 2 14.0]
		lrep npos 2 14.0
		set ev "get_place -center \{$preft\} -nearpos \{$npos\} $clipping -circle 20 -mindist 0"
		set retpos [eval $ev]
		set dist [vector_dist3d $retpos $ownpos]
		//log "-------<> escdist: $dist"
		if { [lindex $retpos 0] == -1 || $dist < 3.0 } {
			set retpos 0
		}
		return $retpos
	}

	proc fight_escape {} {
		global attack_item current_fightmode fight_user
		if { $fight_user == 1 } {
			return
		}
		set escval [get_escape_value [get_objclass this] [get_owner this]]
		if { $escval == 0 } {
			return
		}
		set hp [get_attrib this atr_Hitpoints]
		set frval 0.025
		catch {
			set frval [expr [call_method $attack_item get_fratio] * 0.025]
		}
		if { $escval +  $frval > $hp } {
			log "[get_objname this] Flucht !!!"
			set pos [calc_escape_pos]
			log "----POS: $pos"
			if { $pos != 0 } {
				state_trigger this idle
				run_away $pos
				return 1
			} else {
				set fight_user 1
			}
		}
		return 0
	}

	proc fight_notifytarget {{item 0} {attack 0}} {
		global attack_item attack_behaviour current_fightmode weapon_range
		if { $item == 0 } {
			set item $attack_item
		}

		set bNotify 1
		if { [state_get $item] == "fight_dispatch" } {
            if { [obj_valid $attack_item] } {
            	set CT [get_class_type [get_objclass $attack_item]]
            	if { $CT == "gnome" } { set bNotify 1 }
            	if { $CT == "monster" } { set bNotify 0 }
            }
        }

		if { $bNotify } {
			set escaping 0
			if { $attack == 0 } {
				if { [state_get $item] != "trapped" } {
					set_event $item evt_task_defend -target $item -subject1 [get_ref this]
				}
			} else {
				if { [state_get $item] != "trapped" } {
					set_event $item evt_task_attack -target $item -subject1 [get_ref this]
				}
			}
		}
		return
	}

	// virtual
	proc fight_trick {} {
		return 0
		// wird bei Lavabrut ueberschrieben
	}

	// virtual
	proc adjust_player_aggr {} {
	}

	// virtual
	proc weapon_shield_takeout {{bAnim 0}} {
		return 1
	}

	proc fight_exit {output} {
		log "[get_objname this] leaves fight: '$output' (WalkRes: [get_walkresult this])"
		set current_fightmode -1
		state_trigger this idle
	}

	proc fight_cancel {output} {
		global current_fightmode wait_timeout wait_state is_escaping attack_item
		set zl 0

		if { $is_escaping == 0 } {
            set wait_state 1

			set zl [lnand 0 [obj_query this -range 18 -type {gnome monster} -owner { own } ]]
			foreach item $zl {

				set item [call_method $item get_attack_item]

				if { [is_living $item] } {

    				set itemstate [state_get $item]
    				set itemhp [get_attrib $item atr_Hitpoints]
    				set itempos [get_pos $item]
    				set thispos [get_pos this]

    				if { $itemstate == "fight_dispatch" } {
    					log "*** yd:[expr abs([lindex $thispos 1] - [lindex $itempos 1])]  d:[vector_dist3d $thispos $itempos] "
    					if { [expr abs([lindex $thispos 1] - [lindex $itempos 1])] < 1.0 && [vector_dist3d $thispos $itempos] < 12 } {

    						set attack_item $item
    	    				log "[get_objname this]:New Defender : [get_objname $attack_item] - [vector_dist3d [get_pos this] [get_pos $attack_item]]"
    	    				set wait_timeout 8
    	    				fight_wait_anim
    	    				return
    	    			}
    				}
    			}
			}

			set zl [obj_query [get_ref this] -boundingbox { -8 -0.5 -16 8 0.5 16 } -type {gnome monster} -owner { enemy } ]
			if { $zl != 0 } {
				set iFound 0
				foreach item $zl {
					if { [get_owner $item] == [get_owner this] } {
						// Trolle sollen keine Trolle angreifen, aber dafür Wuker
						continue
					}
                    if { ![is_living $item] } {
                    	// tote zählen nicht
                    	continue
                    }
            		if { [get_objclass $item] == "Krake_" } {
            			continue
            		}
                    incr iFound
				}
                if { $iFound > 0 } {
					log "enemy near ! $item "
					return
				}
			}

		}
        log "------------ [get_objname this] leaves fight!! ($zl) ws: $wait_state"

		log $output
		set current_fightmode -1
		state_trigger this idle
	}

	proc set_fight_idleanim {} {
		set pose [get_weapon_pose [get_weapon_class this]]
		//log "[get_objname this] pose: $pose"
		if { [get_gnomeposition this] == 1 } {
			if { [ catch {  set_anim this climbstillani 0 2 } ] } {
				log "genericfight warning: no climbstillani - classanim defined for [get_objclass this]"
			}
		} else {
			if { $pose != "ballistic" } {
				set_anim this $pose\stillani 0 2
			}
		}
	}

	proc fight_wait_anim {{time 1.0}} {
		if { [get_gnomeposition this] == 1 } {
			if { [ catch {  action this anim climbstillani {state_enable this;set_fight_idleanim} {} } ] } {
				log "genericfight warning: no climbstillani - classanim defined for [get_objclass this]"
            	action this wait $time {state_enable this} {}
            	state_disable this
			}
			return
		}

		set oclass [get_objclass this]
		if { $oclass == "Zwerg" } {
    		set al {kungfu_taenzel_b kungfu_taenzel_c kungfu_taenzel_d}
			set anim [lindex $al [irandom [llength $al]]]
			//log "---idlanim $anim"
			state_disable this
			action this anim  mann.$anim {state_enable this;set_fight_idleanim} {}
			return
    	} elseif { $oclass == "Wuker" } {
			set al {hopser_a kratzen_a schuetteln_a}
			set anim [lindex $al [irandom [llength $al]]]
			//log "---idlanim $anim"
			state_disable this
			action this anim  wuker.$anim {state_enable this;set_fight_idleanim} {}
			return
    	} elseif { $oclass == "Troll" } {
			set al {stehen_drohen stehen_sprung}
			set anim [lindex $al [irandom [llength $al]]]
			//log "---idlanim $anim"
			state_disable this
			action this anim  troll.$anim {state_enable this;set_fight_idleanim} {}
			return
    	}
    	set_fight_idleanim
    	state_disable this
    	action this wait $time {state_enable this} {}
	}

	proc run_pos_stop {obj} {
		global attack_item current_fightmode
		if {![obj_valid $obj]} {
			log "Object $obj existiert nicht mehr"
			return
		}
		if { [get_attrib $obj atr_Hitpoints] < 0.01 } {
			return
		}
		//set fres [fight_setactions this $obj "state_enable [get_ref this]" "state_enable $obj" 1]

		if { [get_walkresult $obj] == 2 } {	;#	WS_NOTFINISHED
    		//if { [string first "attack" $fres] == -1 } {
    			//set current_fightmode 6
    			//fight_doapproach
    		//}
			//log "walk-break"
		} else {
			//log "walk-break : obj isn't walking yet"
		}
	}

	proc fight_try_attack {} {
		global attack_item
		set fres [fight_setactions this $attack_item "state_enable [get_ref this]" "state_enable $attack_item" 1]
	}

	// Warning: never start a new Action in walk callback !
	proc fight_walk_timer_callback {} {
		global fight_run_pos attack_item fight_last_dist fight_action_centre weapon_range

		set bUpdate 0
		set odist -1

		if { ![obj_valid $attack_item] } {
			log "target lost"
			return 0
		}

		set movedist [vector_dist3d $fight_run_pos [get_pos $attack_item]]
		//log "CB: dist: $movedist OldPos:$fight_run_pos NewPos: [get_pos $attack_item]"
		if { $movedist > 0.3 } {
			if { $movedist > 2 } {
				set bUpdate 1
			} else {
				set odist [vector_dist3d [get_pos this] [get_pos $attack_item]]
				if { $odist < 3 } {
					set bUpdate 1
				}
			}
		}

		if { $bUpdate } {
			if { $odist == -1 } {
				set odist [vector_dist3d [get_pos this] [get_pos $attack_item]]
			}
			if { $odist >= 10 } {
				if { $fight_last_dist > 0 && $fight_last_dist < 10 } {
					// target_lost
					log "target_lost"
					return 0
				}
				set fight_last_dist $odist
			}

			if { $fight_action_centre != 0 } {
				set cdist [vector_dist3d $fight_action_centre [get_pos $attack_item]]
				if { $cdist > 10 } {
					log "to far!"
					set walkpos [fight_find_walk_pos $fight_action_centre]
					set sFinishCode "run_pos_obj \{$walkpos\} this -1"
					return $sFinishCode
				}
			}

			if { $weapon_range > 1.0 } {
				set newpos [get_place -center [get_pos $attack_item] -circle [expr $weapon_range + 0.5] -mindist 2.5 -nearpos [get_pos this]]
			} else {
				set newpos [get_attack_pos this $attack_item]
			}

			if { $newpos == 0 } {
				// target_lost
				log "target_lost"
				return 0
			}
			log "new target"
			set sFinishCode "run_pos_obj \{$newpos\} $attack_item"
			if { $odist <= 2 } {
				set sFinishCode "fight_try_attack; $sFinishCode"
			}

			return $sFinishCode
		}
		return 1
	}

	proc fight_doapproach {{fDist -1} {maxfollowdist 10.0}} {
		global attack_item weapon_range wait_state wait_pos

		// hat keine AngriffsPosition gefunden -> ersmal zur WartePosition gehen
		if { $wait_state == 1 } {
			log "run_pos_obj $wait_pos this -1"
			run_pos_obj $wait_pos this -1
			return 1
		}

		// Ballistic
		if {$weapon_range > 1.0} {
			return [fight_doapproach_bal 0 $maxfollowdist]
		}

		// schon nahe genug dran?
		set realdist [vector_dist3d [get_pos this] [get_pos $attack_item]]
		if { $realdist <= [hmax 1.16 $fDist] } {
			return 2
		}

		// neue AttackPos berechnen
		set pos [get_attack_pos this $attack_item]
		// Position gefunden ?
		if { [lindex $pos 0] > 0 } {

			set NewDist [vector_dist3d [get_pos this] $pos]

			if { $NewDist < 0.1 } {
				// Steht schon da !!!
				return 2
			}

			// Angriff abbrechen, wenn Entfernung zu groß
			if {$maxfollowdist > 0} {
				if { $NewDist > $maxfollowdist} {
					log "fight_doapproach target has fled (too far away): [get_pos this] vs. $pos"
					return 0
				}
			}

			set ObjDist [vector_dist3d [get_pos $attack_item] $pos]
			//log "****** ObjDist: $ObjDist ($pos) NewDist: $NewDist"

			// ?
			set_attrib this fight_Counter 0
			if { $fDist > 0.5 } {
				run_pos_obj $pos $attack_item $fDist
			} else {
				if { [get_walkresult $attack_item] == 2 } {
					run_pos_obj $pos $attack_item 1.2
				} else {
					run_pos_obj $pos $attack_item 0.8
				}
			}
			return 1
		}
		return 0
	}

	proc fight_doapproach_bal {{force 0} {maxfollowdist 10.0}} {
		global attack_item weapon_range
		set ipos [get_pos $attack_item]
		set opos [get_pos this]
		set dist [vector_dist $opos $ipos]
		if { $dist <= $weapon_range && ! $force } {return 2}
		if { ! $force && $maxfollowdist > 0 } {
			if { $dist > $maxfollowdist } {
				return 0
			}
		}
		set newpos [get_place -center $ipos -circle [expr $weapon_range + 0.5] -mindist 2.5 -nearpos $opos -random 2]
		if { [lindex $newpos 0] > 0} {

			run_pos_obj $newpos $attack_item [expr {$weapon_range - 1.0}]
			return 1
		}
		return 0
	}

	// virtual return values: 0 - fail   1 - success  2 - no need
    proc fight_rotate_towards {} {
    	global attack_item
    	if {[get_gnomeposition this]} {
    		return 2
    	}
    	if {[llength $attack_item]==1} {
    		if {![obj_valid $attack_item]} {
    			return 0
    		}
    		set itempos [get_pos $attack_item]
    	} else {
    		set itempos $attack_item
    	}
    	set angle [vector_angle "[lindex $itempos 0] 0 [expr [lindex $itempos 2]*0.5]" "[get_posx this] 0 [expr [get_posz this]*0.5]"]
    	fincr angle -1.57
    	if {$angle<0.0} {fincr angle 6.2832}
    	if {$angle>6.2832} {fincr angle -6.2832}
    	set myangle [get_roty this]

		log "--> rot to: $angle my: $myangle"

    	if {abs($angle-$myangle)<0.005||abs($angle-$myangle-6.28)<0.005} {
    		set_roty this $angle
    		return 2
    	}

    	set animset 0

    	state_disable this
    	action this rotate "$angle $animset" {state_enable this} {}
    	return 1
    }

	proc is_living {objref} {
		if { ![obj_valid $objref]  ||  $objref == 0} {
			return false
		}
		if { [get_attrib $objref atr_Hitpoints] < 0.01 } {
			return 0
		}
		return 1
	}

	proc set_weapons {} {
		global attack_item attack_behaviour current_fightmode current_weapon_out current_weapon_item current_shield_out current_shield_item weapon_range whose_turn approach help_list fight_user

		set attack_behaviour "offensive"

		set wpn 0
		set shld 0
		set wpn_id 0

		set bChange 0

		set dist [vector_dist3d [get_pos this] [get_pos $attack_item]]

		set bEnemyAtWall 0
		if { [get_gnomeposition $attack_item] == 1 } {
			set CT [get_class_type [get_objclass $attack_item]]
			if { $CT == "gnome" || $CT == "monster" } {
				set bEnemyAtWall 1
			}
		}
		set bWall 0
		if { [get_gnomeposition this] == 1 } {
			set bWall 1
		}

		set weaponlist {-1 -1}
		if { $dist > 2.2 && $approach && !$bEnemyAtWall && !$bWall } {
			set weaponlist [get_best_weapon this 1]
			set w [lindex $weaponlist 0]
			if { $w != -1 } {
				set wpn $w
				set attack_behaviour "offensive_ballistic"
			}
		}
		if { [lindex $weaponlist 0] == -1 } {
			set weaponlist [get_best_weapon this 0]
		}

		//log "[get_objname this] WPNS: '$weaponlist'"

		set w [lindex $weaponlist 0]
		set s [lindex $weaponlist 1]
		if { $w != -1 } {
			set wpn $w
		}
		if { $s != -1 } {
			set shld $s
		}

		if { $wpn == 0 } {
			set wpn_id 0
		} else {
			set wpn_id [get_weapon_id $wpn true]
			set_weapon_class this $wpn_id
			//log "!!! set wpn_id [get_weapon_id $wpn true]"
		}

        if { $shld == 0 } {
        	set shld_id 0
        } else {
        	set shld_id [get_weapon_id $shld true]
        	set_shield_class this $shld_id
        }

		//log "!!! $current_weapon_item != $wpn"
		if {$current_weapon_item != $wpn} {
			set bChange 1
			if {$wpn} {
				set current_weapon_item $wpn
				if { $current_weapon_out != 0 } {
					link_obj $current_weapon_out
					set_visibility $current_weapon_out 0
				}
				set current_weapon_out 0
				set_weapon_class this $wpn_id
			} else {
				set wpn_id 0
				set_weapon_class this 0
				set current_weapon_item 0
				if { $current_weapon_out != 0 } {
					link_obj $current_weapon_out
					set_visibility $current_weapon_out 0
				}
				set current_weapon_out 0
			}
		}

		if {$current_shield_item != $shld} {
			set bChange 1
			if {$shld} {
				set current_shield_item $shld
				if { $current_shield_out != 0 } {
					link_obj $current_shield_out
					set_visibility $current_shield_out 0
				}
				set current_shield_out 0
				set_shield_class this $shld_id
			} else {
				set_shield_class this 0
				set current_shield_item 0
				if { $current_shield_out != 0 } {
					link_obj $current_shield_out
					set_visibility $current_shield_out 0
				}
				set current_shield_out 0
			}
		}

		set weapon_range [get_weapon_range $wpn_id]

		return $bChange
	}

	proc fight_find_walk_pos {apos} {
        placelock_rem [expr 65536 + [get_ref this]]
   		set walkpos [get_place -center $apos -rect -12 -8 12 8 -except this -placelockidexcept [get_ref this] -walldist 1.5 -rimdist 0.8]
   		placelock_set $walkpos 5 [get_ref this]
   		return $walkpos
	}

	proc fight_startfight {{usrevt 0}} {
		global attack_item attack_behaviour current_fightmode current_weapon_out current_weapon_item current_shield_out current_shield_item
		global weapon_range whose_turn approach help_list fight_user wait_pos wait_state wait_timeout fight_action_centre
		global player_aggressivity bFirstStriked
		set fight_user $usrevt
		set current_fightmode 0
		set wait_state 0
		set bFirstStriked 0

		set_attrib this fight_Counter 0

		log "[get_objname this] starts fight with [get_objname $attack_item] ($attack_behaviour)"

		if { ![catch {set current_weapon_item}] } {
			set_weapons
		}

		if {$fight_user} {
			if {[get_owner this]==0} {
				call_method $attack_item im_attacking_you
			}
		} elseif {$player_aggressivity!=-1} {
			if {[get_owner $attack_item]==0} {
				init_aggr_contact
			}
		}

		set oowner [get_owner this]
		set eowner [get_owner $attack_item]

		if { $oowner != $eowner } {
			if { ($oowner | $eowner ) != -1 } {
				set_diplomacy $oowner $eowner enemy
			}
		}

		set fight_action_centre 0

		if { $usrevt == 1 && $attack_behaviour != "offensive_ballistic" } {
	    	set apos [get_attack_pos this $attack_item]

	    	if { $apos == 0 } {
	    		// kann nicht angreifen
	    		log "cannot attack -> walk"
	    		set apos [get_pos $attack_item]
	    		set walkpos [fight_find_walk_pos $apos]

	    		if { [lindex $walkpos 0] != -1 } {
	    			set wait_pos $walkpos
	    			set wait_state 1
	    			set wait_timeout 20
	    			set fight_action_centre [get_pos $attack_item]
	    		} else {
	    			log "noplacefound *todo*"
	    			return
	    		}

	    	}

		}




		state_triggerfresh this fight_dispatch
	}
}
