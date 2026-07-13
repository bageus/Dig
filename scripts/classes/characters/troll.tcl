def_class Troll none monster 1 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/classes/characters/t_globals.tcl

	class_fightdist 1.2

	obj_init {

		set died_in_fight 0
		set is_dying 0
		set scan_range 5

		set attack_behaviour "none"
		set attack_item 0
		set current_fightmode 0
		set walk_timeout 0
		set hitafter 0
		set intitialized 0
		set current_weapon_out 0
		set current_weapon_item 0
		set current_shield_out 0
		set current_shield_item 0
		##
		set occupation "guard"
		set troll_startpos {0 0 0}
		set troll_startroty 0
		set alarm_poslist [list]
		set guard_poslist [list]
		set range_list [list]
		set action_list [list]
		set action_list2 [list]
		set pos_walklist [list]
		set troll_texturetype 0

		set current_weapon 0
		set weapon_name "stehen"
		set weapon_out 0

		set current_shield 0
		set shield_name "stehen"
		set shield_out 0

		set lastpos 0
		set info_string ""
		set alarmed 0
		set action_state "loop"
		set standstate "standing"
		set is_trapped 0
		set trap_mode 0
		set standard_wake_time 20
		set wake_timer 20
		set part_z_active 0
		set game_actions {1 1}
		set next_gambler -1
		set dran 0
		set enabled 1
		set bedfloor 0
		set round 0
		set burning 0
		##


		// Idle anims für Sequenzen (Statistenrollen)
		set seq_idle_anims [list]
		lappend seq_idle_anims {3 {stehen_gaehnen}}
		lappend seq_idle_anims {3 {stehen_jucken}}
		lappend seq_idle_anims {2 {stehen_kniebeuge}}
		lappend seq_idle_anims {2 {stehen_salutieren}}
		lappend seq_idle_anims {4 {stehen_warten_a}}
		lappend seq_idle_anims {4 {stehen_warten_b}}
		lappend seq_idle_anims {4 {stehen_warten_c}}
		lappend seq_idle_anims {1 {stehen_plakat}}
		lappend seq_idle_anims {1 {stehen_zu_handstand handstand_a handstand_b handstand_a handstand_b handstand_zu_stehen}}
		lappend seq_idle_anims {1 {stehen_zu_liegestuetze liegestuetze_a liegestuetze_a liegestuetze_a liegestuetze_a liegestuetze_a liegestuetze_a liegestuetze_zu_stehen}}
		lappend seq_idle_anims {1 {stehen_zu_liegestuetze liegestuetze_a_zu_liegestuetze_b liegestuetze_b liegestuetze_b liegestuetze_b liegestuetze_b liegestuetze_b_zu_liegestuetze_a liegestuetze_zu_stehen}}
		lappend seq_idle_anims {1 {stehen_tanzen_start stehen_tanzen_loop stehen_tanzen_loop stehen_tanzen_loop stehen_tanzen_end}}
		lappend seq_idle_anims {1 {stehen_zu_spies spies_warten_a spies_warten_a spies_warten_a spies_warten_a spies_warten_a spies_zu_stehen}}
		lappend seq_idle_anims {w1 {klettern_warten}}
        call data/scripts/misc/seq_idle.tcl

		set_anim this troll.stehen_warten_a 0 $ANIM_LOOP				;# set standard anim
		set_fogofwar this 14 8										;# uncover fog of war area
		set_autolight this 0
		set_collision this 1
		set_hoverable this 1									;# turn on light at gnome position
		set_physic this 0

		set_boxed this 0

		set_attrib this hitpoints 1



		call scripts/misc/genericfight.tcl
		call scripts/classes/characters/t_procs.tcl		// misc procs
		call scripts/misc/aggr_events.tcl

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		timer_event this evt_scan -repeat -1 -interval 1 -userid 1 -attime [expr [gettime]+2]
	}

	call scripts/misc/genericfight.tcl
	call scripts/classes/characters/t_methods.tcl
	call scripts/misc/aggr_events.tcl


	handle_event evt_timer0 {
		troll_init
		state_reset this
		state_trigger this idle
		state_enable this
		set intitialized 1
	}

	handle_event evt_scan {
		
		if { $trap_mode } { return }

		if { [isunderwater [vector_add [get_pos this] {0 -1.0 0}]] } {
			add_attrib this atr_Hitpoints  -0.1
			set burning 0
		}

		if { $burning == 1 } {
			return
		}

		if { $is_trapped == 0 && $is_dying == 0 } {
			if { $enabled } {
				if { [state_get this] != "fight_dispatch" } {
//					log "*********** [state_get this]"
					scan_for_enemy
				}
			}
		}
	}

	handle_event evt_troll_die {
		if { $is_dying } {return}
		set is_dying 1
		set_attackinprogress this 1
		timer_unset this 1
		state_trigger this
		state_disable this
		state_trigger this
		if { [get_diedinfight this] == 0 && [state_get this] != "trapped" } {
			if {[isunderwater [vector_add [get_pos this] {0 -1.0 0}]]} {
				state_disable this
				action this anim drown {call_method [get_ref this] destroy} {call_method [get_ref this] destroy}
			} else {
				state_disable this
				action this anim splat {call_method [get_ref this] destroy} {call_method [get_ref this] destroy}
			}
		} else {
			state_disable this
			action this wait 3 	" call_method [get_ref this] destroy " "call_method [get_ref this] die_breaked"
		}
	}

	handle_event evt_task_defend {
		tasklist_clear this
		set attack_item [event_get this -subject1]
		set attack_behaviour "offensive"
		set approach 0
		fight_startfight
	}

	state_leave fight_dispatch {
		set wake_timer $standard_wake_time
		set action_state "wait"
	}

	state_enter fight_dispatch {
		set action_state "fight"
	}

	state trapped {
		if {$trap_mode==0} {
			set trap_mode 1
			if {$trap_type=="petrify"} {
				set_anim this petrified 0 1
				state_disable this
				action this wait 3.0 {state_enable this}
			} else {
				play_anim splat
			}
			return
		}
		if {$trap_mode==1} {
			if {$trap_type=="petrify"} {
				set_physic this 1
				set_anim this petrified 30 0
				set_textureanimation this 0 6
				set_textureanimation this 1 6
			}
			set trap_mode 2
			state_disable this
			action this wait $trap_time {state_enable this}
			return
		}
		if {$trap_mode==2} {
			set trap_mode 0
			state_trigger this idle
			if {$trap_type=="petrify"} {
				set_physic this 0
				set_textureanimation this 0 $troll_texturetype
				set_textureanimation this 1 $troll_texturetype
			} elseif {[get_attrib this atr_Hitpoints]>=0.01} {
				play_anim splatgetup
			} else {
				set_event this evt_troll_die -target this
			}
			return
		}
	}
	
	state_leave trapped {
		set_physic this 0
	}

	state idle {
		//#log "Troll: state:idle"
		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			set_event this evt_troll_die -target this
			return
		}

		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
			//log "[get_objname this]: oc:'$occupation' ss:'$standstate' as:'$action_state' (dr:$dran) -> Ttd:'$command'"
			eval $command
			return
		}

		// Trolle sind immer boese
		if { [get_owner this] != -1 } { set_owner this -1 }


		if {rand()<0.3} {check_for_player_contact}

		if { $burning == 1 } {
			#tasklist_add this "play_anim jump"
			return
		}
		if { $intitialized == 0 } {
			wait_time 1.0
			return
		}
		if { $alarmed } {
			if { [find_enemy 8] } {
				return
			}
		}

		if { [check_occupation] } {
			handle_$occupation
		} else {
			#log "[get_objname this]: warning invalid occupation: $occupation"
			walk_random [expr [irandom 30] + 20]
			#del
		}

		return
		tasklist_add this "wait_time 3"
	}

}

