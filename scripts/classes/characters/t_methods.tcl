# troll methods

method get_trapped {type} {
	if {$type=="petrify"} {
		set trap_time 30
		set trap_mode 0
		set trap_anim "petrified"
		set trap_type "petrify"
	}
	if {$type=="splat"} {
		set trap_time 3
		set trap_mode 0
		set trap_anim "dieminea"
		set trap_type "splat"
	}
	state_triggerfresh this trapped
}

method get_info {name} {
	global info_string
	foreach item $info_string {
		set inam [lindex $item 0]
		set ival [lindex $item 1]
		if { $name == $inam } {
			return $ival
		}
	}
	return 0
}


method Editor_Set_Info {ifo} {
	// Anti-Verklammerungs-Filter '}{' -> '} {'
	set ifo [string map {"\}\{" "\} \{"} $ifo]
	set info_string $ifo
	foreach entry $ifo {
		switch [lindex $entry 0] {
			"aggr"		{ set player_aggressivity [lindex $entry 1] }
			"aggrmax" {set aggr_max [lindex $entry 1]}
		}
	}
}

method mstart_attack {pos} {
	log "alertet: [get_objname this] trapped($is_trapped)"
	if { $is_trapped == 0 } {
		action this wait 0.1 	;#break actions
		state_enable this
		tasklist_clear this
		start_attack $pos
	}
}


method destroy {} {
	if { [string first "guard" $occupation] != -1 && [get_info "speier"] == 0 } {
		log "Waechter"
		set tlist [lnand 0 [obj_query this "-class Troll -range 100"]]
		set tnew 0
		foreach item $tlist {
			set occ [call_method $item get_occupation]
			if { $occ == "sleep" } {
				call_method $item transfer_mission "guard" $scan_range $alarm_poslist $guard_poslist $range_list $action_list $action_list2 $lastpos $pos_walklist
				break
			}
		}
	} elseif { $occupation == "dicing" } {
		log "Waechter"
		set tlist [obj_query this "-class Troll -range 100"]
		set tnew 0
		foreach item $tlist {
			set occ [call_method $item get_occupation]
			if { $occ == "dicing" } {
				call_method $item set_dran 1
				break
			}
		}
	}

	global current_weapon current_shield
	if { $current_weapon } { catch { del  $current_weapon } }
	if { $current_shield } { catch { del  $current_shield } }

	foreach item [inv_list this] {
		log "Troll dies: dropping [get_objname $item]"
		inv_rem this $item
		set_hoverable $item 1
		set_selectable $item 1
		set_physic $item 1
		set_pos $item [vector_add [get_pos this] {0 -1 0}]
		set_visibility $item 1
	}

	log "[get_objname this] was destroyed .... "

	destruct this
	del this
}

method transfer_mission {tm1 tm2 tm3 tm4 tm5 tm6 tm7 tm8 tm9} {
	log "Mission info recieved: $tm1 $tm2 $tm3 $tm4 $tm5 $tm6 $tm7 $tm8 $tm9"
	set occupation $tm1
	set scan_range $tm2
	set alarm_poslist $tm3
	set guard_poslist $tm4
	set range_list $tm5
	set action_list $tm6
	set action_list2 $tm7
	set lastpos $tm8
	set pos_walklist $tm9
	get_up
}

method hit_me {who} {
	log "hitme!"
	set hitafter $who
}

method enable {} {
	set enabled 1
}

method disable {} {
	set enabled 0
}

method get_occupation {} {
	return $occupation
}

method set_occupation {occup} {
	set occupation $occup
}

method get_standstate {} {
	return $standstate
}

method get_actionstate {} {
	return $action_state
}

method die_breaked {} {
	log "diebreak!!!!!!!!!!!"
	set is_dying 0
}

method set_next_gambler {ngl} {
	set IDX -1
	for {set i 0} {$i < [llength $ngl]} {incr i} {
		if { [lindex $ngl $i] == [get_ref this] } {
			set IDX $i
		}
	}
	if { $IDX != -1 } {
		//set ngl [lreplace $ngl $IDX $IDX]
		lrem ngl $IDX
	}
	set next_gambler $ngl
}

method set_dran {rnd} {
	global round
	set dran 1
	set round $rnd
}

method burn {} {
	if { $burning } { return }
	set burning 1
	add_attrib this atr_Hitpoints -1.2
	action this anim burn {state_enable this} {state_enable this}
	change_particlesource this 0 27 {0 0 0} {0 0 0} 256 16 0 0 0 1
	set_particlesource this 0 1
}

method get_burning {} {
	return $burning
}

method seq_idle {} {
	seq_idle
}
