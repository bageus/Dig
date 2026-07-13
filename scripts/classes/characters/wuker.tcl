foreach wukerclass {Wuker Schwefelwuker} {
def_class $wukerclass none monster 1 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/classes/characters/w_globals.tcl

	class_fightdist 1.2
	
	obj_init {

		set_hoverable this 1
		set scan_range 7
		set sniff_range 13
		set enemy_classes {Zwerg Spinne Drachenbaby Troll Lavabrut Kristallbrut}

		set died_in_fight 0
		set is_dying 0

		set current_weapon_out 0
		set current_weapon_item 0
		set current_shield_out 0
		set current_shield_item 0

		set attack_behaviour "none"
		set attack_item 0
		set current_fightmode 0
		set walk_timeout 0

		set info_string ""

		// Idle anims für Sequenzen (Statistenrollen)
		set seq_idle_anims [list]
		lappend seq_idle_anims {1 {kratzen_a}}
		lappend seq_idle_anims {1 {kratzen_b}}
		lappend seq_idle_anims {1 {kratzen_c_start kratzen_c_loop kratzen_c_loop kratzen_c_loop kratzen_c_end}}
		lappend seq_idle_anims {1 {schuetteln_a}}
		lappend seq_idle_anims {1 {schuetteln_a}}
		lappend seq_idle_anims {1 {kletter_standanim}}
		call data/scripts/misc/seq_idle.tcl

		set_anim this wuker.stand_atmen_a 0 $ANIM_LOOP				;# set standard anim
		set_fogofwar this 14 8										;# uncover fog of war area
		set_autolight this 1
		set_collision this 1										;# turn on light at gnome position

		if {[get_objclass this]=="Schwefelwuker"} {
			set_textureanimation this 0 1 0 0
		}

		set_attrib this hitpoints 1

		state_reset this
		state_trigger this idle
		state_enable this

		#timer_event this evt_wuker_ -repeat -1 -interval 1

		call scripts/classes/characters/w_procs.tcl		// misc procs
		call scripts/misc/genericfight.tcl
		call scripts/classes/characters/prisoned_monsters.tcl
		call scripts/misc/aggr_events.tcl

	}

	call scripts/misc/genericfight.tcl
	call scripts/classes/characters/w_methods.tcl
	call scripts/classes/characters/prisoned_monsters.tcl
	call scripts/misc/aggr_events.tcl


	handle_event evt_wuker_die {
		if { $is_dying } {return}
		set is_dying 1
		state_trigger this
		state_disable this
		state_trigger this
		if { [get_diedinfight this] == 0 && [state_get this] != "trapped" } {
			if {[isunderwater [vector_add [get_pos this] {0 -1.0 0}]]} {
				action this anim drown { call_method [get_ref this] destroy } { call_method [get_ref this] destroy }
			} else {
				action this anim diea  { call_method [get_ref this] destroy } { call_method [get_ref this] destroy }
			}
		} else {
			action this wait 3 	{ call_method [get_ref this] destroy } { call_method [get_ref this] destroy }
		}
	}

	handle_event evt_task_defend {
		tasklist_clear this
		set attack_item [event_get this -subject1]
		set attack_behaviour "offensive"
		set approach 0
		fight_startfight
	}

	state trapped {
		if {$trap_mode==0} {
			set trap_mode 1
			if {$trap_type=="petrify"} {
				set_anim this petrified 0 1
				state_disable this
				action this wait 0.8 {state_enable this}
			} else {
				set_anim this splattrap 0 1
				state_disable this
				action this wait 1.3 {state_enable this}
			}
			return
		}
		if {$trap_mode==1} {
			if {$trap_type=="petrify"} {
				set_anim this petrified 8 0
				set_textureanimation this 0 2
				set_physic this 1
			} else {
				set_anim this splattrap 13 0
			}
			set trap_mode 2
			state_disable this
			action this wait $trap_time {state_enable this}
			return
		}
		if {$trap_mode==2} {
			set trap_mode 0
			state_trigger this idle
			set_physic this 0
			if {$trap_type=="petrify"} {
				if {[get_objclass this]=="Wuker"} {
					set_textureanimation this 0 0 0 0
				} else {
					set_textureanimation this 0 1 0 0
				}
			} elseif {[get_attrib this atr_Hitpoints]>0.01} {
				state_disable this
				set_anim this splattrap 13 1
				action this wait 0.9 {state_enable this}
			} else {
				state_disable this
				set_event this evt_wuker_die -target this
			}
			return
		}
	}
	
	state_leave trapped {
		set_physic this 0
	}

	state idle {

		if {$prisoned} {
			state_triggerfresh this prisoned
			return
		}

		if { [is_contained this] } {
			wait_time 1.0
			return
		}

		if { [isunderwater [vector_add [get_pos this] {0 -1.0 0}]] } {
			add_attrib this atr_Hitpoints  -0.1
		}

		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			set_event this evt_wuker_die -target this
		}

		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
			#log "Wuker:Task to do:'$command'"
			eval $command
			return
		}
		if {rand()<0.3} {check_for_player_contact}
		set findres [find_enemy]

		// Wuker sind immer neutral
		if { [get_owner this] != -1 } { set_owner this -1 }

		switch $findres {
			1 { return }
			2 { tasklist_add this "sniff_random [expr 3 + [hf2i [random 2]]]" ;return}
		}

		set_idle_anim	;#set idle anim

		set rnd [random]
		if { $rnd < 0.8 } {					;# 80%-rumlaufen 20%-filler
			tasklist_add this "walk_random [expr 2 + [hf2i [random 2]]]"
		} else {
			if { [get_gnomeposition this] == 0 } {
				set rnd [hf2i [random 8]]
				switch $rnd {
					0 {tasklist_add this "scratchc"}
					1 {tasklist_add this "play_anim scratcha"}
					2 {tasklist_add this "play_anim scratchb"}
					3 {tasklist_add this "play_anim shakea"}
					4 {tasklist_add this "looking"}
					5 {tasklist_add this "jumping"}
					6 {tasklist_add this "play_anim bitea"}
					7 {tasklist_add this "wait_time 3"}
				}
			} else {
				#log "climb down"
				set walkplace [find_free_place [ground_pos [get_pos this]] -10 0 10 10 0 0]
				tasklist_add this "walk_pos \{$walkplace\}"
			}
		}
	}
}

}
