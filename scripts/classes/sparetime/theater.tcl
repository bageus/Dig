//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class _Theatervorstellung service material 0 {} {}

def_class Theater service production 3 {} {

	class_fightdist 1.8

	method prod_item_actions item {
		global quality current_worker myref
		set exp_incr [call_method this prod_item_exp_incr $item]
		set exp_infl [call_method this prod_item_exp_infl $item]
		set quality [prod_getgnomeexper $current_worker $exp_infl]
		set rlst [list]

     	lappend rlst "prod_goworkdummy 3"	;# Vorhang oeffnen, husten
        lappend rlst "prod_turnleft"
        lappend rlst "prod_anim work"
        lappend rlst "prod_machineanim theater.vorhang_oeffnen once"
        lappend rlst "prod_waittime 2"
        lappend rlst "prod_turnfront"
        lappend rlst "prod_anim cough"

        lappend rlst "prod_turnleft" 	;# Vorstellung geben
        lappend rlst "prod_machineanim theater.vorhang_auf"
		lappend rlst "prod_anim stagestart"
        lappend rlst "prod_anim stage_a"
        lappend rlst "prod_anim stage_b"
        lappend rlst "prod_anim stage_a"
        lappend rlst "prod_exp $exp_incr \[call_method $myref get_exp_fract\]"
        lappend rlst "prod_anim stage_a"
        lappend rlst "prod_anim stage_b"
        lappend rlst "prod_exp $exp_incr \[call_method $myref get_exp_fract\]"
        lappend rlst "prod_anim stage_a"
        lappend rlst "prod_anim stage_b"
        lappend rlst "prod_exp $exp_incr \[call_method $myref get_exp_fract\];prod_call_method perform_finish"
        lappend rlst "prod_anim stagestop"
		lappend rlst "prod_turnleft"		;# Vorhang zu, aufatmen
		lappend rlst "prod_anim work"
		lappend rlst "prod_machineanim theater.vorhang_schliess once"
		lappend rlst "prod_waittime 2"
        lappend rlst "prod_turnfront"
        lappend rlst "prod_anim breathe"
        lappend rlst "prod_machineanim theater.standard"

		return $rlst
	}

	method get_inactive {} {
		if {[get_prod_slot_cnt this _Theatervorstellung]} {
			return 0
		} else {
			return 1
		}
	}
	method ask_for_seat {} {
		return [ask_for_free_seat]
	}
	method get_random_seat {} {
		return [get_random_seat]
	}
	method ask_for_reserve {} {
		for {set i $prod_guest_seats} {$i<$prod_guest_waits} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				return $i
			}
		}
		return 0
	}
	method reserve_seat {ref} {
		for {set i $prod_guest_seats} {$i<$prod_guest_waits} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				prod_guest guestset this $i $ref
				return $i
			}
		}
		return 0
	}
	method default_link {} {return $prod_guest_seats}
	method ask_seat_cnt {} {
		set retval -1
		for {set i 0} {$i<$prod_guest_waits} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				incr retval
			}
		}
		return $retval
	}
	method get_next_action {gnome} {
		return [get_next_action $gnome]
	}
	method remove_from_guestlist {gnome} {
		global guests gueststates guesttimer cheercnt current_worker
		log "leaving Theater: [get_objname $gnome] ($gnome) ($guests) ($gueststates)"
		if {[set id [lsearch $guests $gnome]]==-1} {return ""}
		set ret ""
		if {[lindex $cheercnt $id]>0} {
			lappend ret "sparetime_place_finished"
		} elseif {[lindex $guesttimer $id]>20} {
			if {!$current_worker} {
				lappend ret "sparetime_place_fail tns"
			} elseif {[lcount $guests 0]<2} {
				lappend ret "sparetime_place_fail tfl"
			}
		}
		if {[lindex $gueststates $id]==2} {lappend ret "play_anim couchstop"}
		remove_guest $id
		return $ret
	}
	method guest_stateset {link st} {}
	method get_exp_fract {} {
		return [expr {0.3-[lcount [lrange $guests 0 4] 0]*0.05}]
	}
	method perform_finish {} {
		set performed 1
		timer_event this evt_perform_reset -attime [expr {[gettime]+10}]
	}
	def_event evt_perform_reset
	handle_event evt_perform_reset {
		set performed 0
	}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim theater.standard
	class_flagoffset 2 2.4

	obj_init {
		set_anim this theater.standard 0 $ANIM_LOOP
		call scripts/misc/genericprod.tcl

		set_inventoryslotuse this 1
		set_energyconsumption this 0
		set_collision this 1

		set_prod_materialneed this 0
		set_prod_switchmode this 1

		set build_dummys [list 12 13 14 15 16 17 18 18 19]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechtsholz oben_linksholz oben_rechtsholz oben_rechtsholz oben_linksholz oben_linksholz oben_rechtsholz oben_linksholz unten_rechtsholz}
		set damage_dummys {20 27}

		prod_guest addlink this 7
		prod_guest addlink this 6
		prod_guest addlink this 8
		prod_guest addlink this 9
		prod_guest addlink this 5
		set prod_guest_seats 5
		prod_guest addlink this 0
		prod_guest addlink this 1
		set prod_guest_waits 7

		set myref [get_ref this]
		set performed 0
		set quality 0.0
		set guests {0 0 0 0 0 0 0}
		set gueststates {0 0 0 0 0 0 0}
		set guesttimer {0 0 0 0 0 0 0}
		set cheercnt {0 0 0 0 0 0 0}

		proc ask_for_free_seat {{waiter 0}} {
			global gueststates prod_guest_seats prod_guest_waits
			if {[prod_guest guestfree this]/$prod_guest_seats} {return 0} {
				if {!$waiter&&[lcount [lrange $gueststates $prod_guest_seats [expr {$prod_guest_waits-1}]] 0]!=$prod_guest_waits-$prod_guest_seats} {
					return 0
				} else {
					return 1
				}
			}
		}
		proc get_random_seat {} {
			global prod_guest_seats
			set rlst {}
			for {set i 0} {$i<$prod_guest_seats} {incr i} {
				if {[prod_guest guestget this $i]==0} {
					lappend rlst $i
				}
			}
			return [lindex $rlst [irandom [llength $rlst]]]
		}
		proc set_guest_state {id state} {
			global gueststates
			lrep gueststates $id $state
			reset_guest_timer $id
		}
		proc incr_guest_timer {id} {
			global guesttimer
			set timer [lindex $guesttimer $id]
			lrep guesttimer $id [expr $timer+1]
		}
		proc incr_cheer_count {id} {
			global cheercnt
			set count [lindex $cheercnt $id]
			lrep cheercnt $id [expr $count+1]
		}
		proc reset_guest_timer {id} {
			global guesttimer
			lrep guesttimer $id 0
		}
		proc add_guest {ref} {
			global guests prod_guest_waits prod_guest_seats
			for {set i 0} {$i<$prod_guest_waits} {incr i} {
				if {[prod_guest guestget this $i]==$ref} {
					break
				}
			}
			if {$i==$prod_guest_waits} {
				log "WARNING: $ref [get_objname $ref] is not in Theater"
				for {set i 0} {$i<$prod_guest_waits} {incr i} {
					log "$i: [prod_guest guestget this $i]"
				}
				return -1
			}
			lrep guests $i $ref
			return $i
		}
		proc remove_guest {id} {
			global guests gueststates guesttimer cheercnt
			lrep guests $id 0
			lrep gueststates $id 0
			lrep guesttimer $id 0
			lrep cheercnt $id 0
		}
		proc get_remaining_prodsteps {} {
			global prod_currentstep prod_maxsteps
			return [expr {$prod_maxsteps-$prod_currentstep}]
		}
		proc get_next_action {gnome} {
			global guests gueststates guesttimer myref performed cheercnt
			global prod_guest_seats prod_guest_waits current_worker
			set rlst [list]
			set gid [lsearch $guests $gnome]
			if {$gid==-1} {set gid [add_guest $gnome]}
			if {$gid==-1} {return "sparetime_place_end"}
			set gst [lindex $gueststates $gid]
			set gti [lindex $guesttimer $gid]
			if {$gti>20} {return {{sparetime_place_end}}}
			switch $gst {
				0 {
					lappend rlst "rotate_toback"
					if {$gid<5} {
						set_guest_state $gid 1
					} else {
						if {[ask_for_free_seat 1]} {
							lappend rlst "sparetime_take_seat $myref"
							//set_guest_state $gid 10
							log "[get_objname $gnome] sitting down (0->1)"
						} else {
							lappend rlst "sparetime_idle_loop"
							lappend rlst "sparetime_idle_loop"
							lappend rlst "sparetime_idle_loop"
						}
					}
				}
				1 {
					if {$current_worker} {
						if {[get_remaining_prodsteps]>10} {
							lappend rlst "rotate_toback"
							lappend rlst "play_anim couchstart"
							set_guest_state $gid 2
						} else {
							set_guest_state $gid 3
						}
					} else {
						lappend rlst "rotate_toangle [random 6.2]"
						lappend rlst "sparetime_place_relief scout 0.02"
						lappend rlst "play_anim wait"
					}
				}
				2 {
					if {$current_worker} {
						if {[get_remaining_prodsteps]>10} {
							lappend rlst "play_anim couchloopa"
							lappend rlst "play_anim couchloopa"
							lappend rlst "play_anim couchloopa"
						} else {
							lappend rlst "play_anim couchstop"
							set_guest_state $gid 3
						}
					} else {
						set_guest_state $gid 1
						lappend rlst "play_anim couchstop"
					}
				}
				3 {
					if {$current_worker||$performed} {
						global quality
						set ranim [lindex {boo applaud cheer} [expr {int($quality*2.6)}]]
						set moodgain [hmax [expr {$quality*0.1+0.02}] 0.12]
						lappend rlst "sparetime_place_relief $ranim $moodgain"
						lappend rlst "sparetime_place_relief $ranim $moodgain"
						lappend rlst "sparetime_place_relief $ranim $moodgain"
						incr_cheer_count $gid
						if {[lindex $cheercnt $gid]>2} {
							lappend rlst "sparetime_place_end"
						} else {
							set_guest_state $gid 1
						}
					} else {
						set_guest_state $gid 1
					}
				}
			}
			incr_guest_timer $gid
			return $rlst
		}

		sparetime this announce fun

	}

	obj_exit {
		sparetime this disannounce
	}

}

