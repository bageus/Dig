// fuer Monster hinter Gittern (Wuker, Spinne, Brut)

if {[in_class_def]} {
	def_event evt_timer_prisoned
	
	handle_event evt_timer_prisoned {
		//log "EVT_TIMER_PRISONED"
		if {$prisoned} {
			log "set state to prisoned"
			state_triggerfresh this prisoned
		}
	}
	
	state prisoned {
		
		set_attrib this atr_Hitpoints 1.0
		
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
			eval $command
			return
		}
		set enemy [find_enemy_pr]
		if {$enemy} {
			try_walk_near $enemy
		} else {
			play_wait
		}
	}
	
} else {
	
	set prisoned 0
	set myclass [get_objclass this]
	if {$myclass=="Wuker"||$myclass=="Schwefelwuker"} {
		set prisoner_enemies {Zwerg Drachenbaby Troll Spinne Kristallbrut Lavabrut}
		set waitlist {scratcha scratchb scratcha scratchb scratcha scratchb}
		lappend waitlist {scratchcstart scratchcloop scratchcloop scratchcloop scratchcloop scratchcend}
		set angrylist {shakea {rampagestart rampageloop rampageloop rampageloop rampageend}}
		set run_animset 1
	} elseif {$myclass=="Spinne"||$myclass=="Riesenspinne"} {
		set prisoner_enemies {Zwerg Drachenbaby Troll Wuker Schwefelwuker Kristallbrut Lavabrut}
		set waitlist {idleb idleb idleb idleb idleb idleb idleb idleb spinround}
		set angrylist {{stand_to_att kungfustillani kungfustillani kungfustillani att_to_stand}}
		set run_animset 1
	} elseif {$myclass=="Kristallbrut"||$myclass=="Lavabrut"} {
		set prisoner_enemies {Zwerg Drachenbaby Troll Wuker Schwefelwuker Spinne}
		set waitlist {idlea idlec idlea idlec idlea idlec idlea idlec restless}
		lappend waitlist {peestart peeloop peeloop peeloop peeloop peestop}
		set angrylist {roara roarb hornstrike {angryb angryb angryb} {angrya angrya angrya}}
	} else {
		set prisoner_enemies {}
		set waitlist standard
		set angrylist standard
		set run_animset 0
	}
	set waitcnt [llength $waitlist]
	set angrycnt [llength $angrylist]
	
	timer_event this evt_timer_prisoned -attime [expr {[gettime] + 0.1}]
	log "TIMING EVENT"
	
	proc find_enemy_pr {} {
		global prisoner_enemies
		set enemy [obj_query this -class $prisoner_enemies -boundingbox {-15 -0.3 -15 15 0.3 15} -limit 1]
		return $enemy
	}
	
	proc try_walk_near {item} {
		global run_animset
		set place [get_place -center [get_pos $item] -circle 3 -mindist 1 -nearpos [get_pos this] -except this]
		if {[lindex $place 0]>0} {
			state_disable this
			action this walk "-target \{$place\} -animsets \{$run_animset $run_animset $run_animset\} -speedtype 3 -useobjects 0" {
				if {[get_walkresult this]!=4} {
					play_angry
					state_enable this
				} else {
					set prisoned 0
					state_triggerfresh this idle
				}
			}
		}
	}
	
	proc play_angry {} {
		global angrylist angrycnt
		set animlist [lindex $angrylist [irandom $angrycnt]]
		foreach anim $animlist {
			tasklist_add this "play_anim $anim"
		}
	}
	
	proc play_wait {} {
		global waitlist waitcnt
		set animlist [lindex $waitlist [irandom $waitcnt]]
		foreach anim $animlist {
			tasklist_add this "play_anim $anim"
		}
	}
	
}
