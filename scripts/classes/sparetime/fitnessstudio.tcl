//# STOPIFNOT FULL
call scripts/misc/utility.tcl


def_class Fitnessstudio wood production 3 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	method prod_item_actions item {
		return [list]
	}

	method get_inactive {} { return 0 }
	method set_animstate {bit st gnome} {
		global userstates anim_state
		if {$st} {
			lrep userstates $bit $gnome
			set anim_state [expr {$bit|$anim_state}]
		} else {
			lrep userstates $bit 0
			set anim_state [expr {~$bit&$anim_state}]
		}
		studio_anim
	}
	method ask_for_seat {} {
		if {[prod_guest guestfree this]==-1} {return 0} {return 1}
	}
	method ask_for_reserve {} {
		return 0
	}
	method default_link {} {return $prod_guest_seats}
	method ask_seat_cnt {} {
		set retval -1
		for {set i 0} {$i<$prod_guest_seats} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				incr retval
			}
		}
		return $retval
	}
	method get_next_action {gnome} {
		return [get_next_guest_action $gnome]
	}
	method remove_from_guestlist {gnome} {
		global guests gueststates guesttimer users userstates used_stations
		log "leaving FStudio: [get_objname $gnome] ($gnome) ($guests) ($gueststates)"
		if {[set id [lsearch $guests $gnome]]==-1} {return ""}
		set ret ""
		if {[lindex $gueststates $id]==2} {
			lappend ret "sparetime_place_finished"
		} elseif {[lindex $guesttimer $id]>20} {
			lappend ret "sparetime_place_fail ffl"
		}
		set uidx [lsearch $users $gnome]
		if {$uidx!=-1} {
			lrem users $uidx
			lrem used_stations $uidx
		}
		set stidx [lsearch userstates $gnome]
		if {$stidx<1} {
			call_method this set_animstate $stidx 0 $gnome
			if {$stidx==2} {
				lappend ret "play_anim handlestemstopb"
			}
		}
		remove_guest $id
		return $ret
	}
	method guest_stateset {id val} {set_guest_state $id $val}
	method removeorder {id} {prod_guest resetorder this $id}

	class_defaultanim fitnessstudio.standard
	class_flagoffset 3.3 0.7

	obj_init {
		set_anim this fitnessstudio.standard 0 $ANIM_LOOP
		call scripts/misc/genericprod.tcl
		set_energyconsumption this 2
		set_collision this 1

		prod_guest addlink this 8
		prod_guest addlink this 9
		prod_guest addlink this 10
		prod_guest addlink this 11
		set prod_guest_seats 4

		set myref [get_ref this]
		set anim_state 0
		set guests {0 0 0 0}
		set gueststates {0 0 0 0}
		set guesttimer {0 0 0 0}
		set used_stations {}
		set users {}
		set userstates {0 0 0}

		set build_dummys [list 16 17 18 19 20 21 22 23]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_linksmetall unten_rechtsmetall oben_rechtsmetall oben_linksmetall unten_rechtsmetall unten_rechtsmetall unten_rechtsmetall oben_rechtsmetall}
		set damage_dummys {24 31}

		proc studio_anim {} {
			global anim_state
			switch $anim_state {
				0 {set_anim this fitnessstudio.standard 0 0}
				1 {set_anim this fitnessstudio.ohne_punchball 0 0}
				2 {set_anim this fitnessstudio.ohne_hantel 0 0}
				3 {set_anim this fitnessstudio.ohne_beides 0 0}
			}
		}

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
		proc add_guest {ref} {
			global guests prod_guest_seats
			for {set i 0} {$i<$prod_guest_seats} {incr i} {
				if {[prod_guest guestget this $i]==$ref} {
					break
				}
			}
			if {$i==$prod_guest_seats} {
				log "WARNING: $ref [get_objname $ref] is not in Studio"
				for {set i 0} {$i<$prod_guest_seats} {incr i} {
					log "$i: [prod_guest guestget this $i]"
				}
				return -1
			}
			lrep guests $i $ref
			return $i
		}
		proc set_guest_state {id state} {
			global gueststates
			lrep gueststates $id $state
			reset_guest_timer $id
		}
		proc lock_for_guest {id} {
			global guests myref
			lrep guests $id $myref
			prod_guest guestset this $id $myref
		}
		proc incr_guest_timer {id} {
			global guesttimer
			set timer [lindex $guesttimer $id]
			lrep guesttimer $id [expr $timer+1]
		}
		proc reset_guest_timer {id} {
			global guesttimer
			lrep guesttimer $id 0
		}
		proc remove_guest {id} {
			global guests gueststates guesttimer
			lrep guests $id 0
			lrep gueststates $id 0
			lrep guesttimer $id 0
		}
		proc get_next_guest_action {gnome} {
			global guests gueststates guesttimer myref used_stations users
			global prod_guest_seats prod_guest_waits current_worker
			global looktochief
			set rlst [list]
			set gid [lsearch $guests $gnome]
			if {$gid==-1} {set gid [add_guest $gnome]}
			if {$gid==-1} {return "sparetime_place_end"}
			set gst [lindex $gueststates $gid]
			set gti [lindex $guesttimer $gid]
			if {$gti>20} {return "sparetime_place_end"}
			set gdy [prod_guest getlink this $gid]
			switch $gst {
				// 1-9 wartend, 10-19 schwitzend
				0 {	;# angemeldet ankommend
					lappend rlst "rotate_tofront"
					foreach entry [warmups] {
						lappend rlst "sparetime_place_relief $entry 0.01"
					}
					set_guest_state $gid 1
 				}
				1 { ;# after warmups at entry
					if {[llength $used_stations]<2} {
						set usables [lnand [join $used_stations] {2 3 4 6 7}]
						set endstations [land {4 6} $usables]
						set otherstations [lnand {4 6} $usables]
						set endstation [lindex $endstations [irandom [llength $endstations]]]
						set otherstation [lindex $otherstations [irandom [llength $otherstations]]]
						log "$gnome -> $otherstation $endstation"
						lappend rlst "walk_dummy $myref $otherstation"
						foreach entry [station $otherstation $gnome] {
							lappend rlst $entry
						}
						lappend rlst "walk_dummy $myref $endstation"
						foreach entry [station $endstation $gnome] {
							lappend rlst $entry
						}
						lappend used_stations [list $otherstation $endstation]
						lappend users $gnome
						set_guest_state $gid 2
						lappend rlst "sparetime_place_end"
					} else {
						lappend rlst "rotate_towards $myref"
						lappend rlst "play_anim scout"
						lappend rlst "sparetime_idle_loop"
					}
				}
				2 { ;# warmup at 7
					lappend rlst "sparetime_place_end"
				}
			}
			incr_guest_timer $gid
			return $rlst
		}
		proc warmups {} {
			set rlst {}
			switch [irandom 4] {
				0 {return {kneebend kneebend kneebend warmupe pressupstart pressuploop pressuploop pressuploop pressuploop pressuploop pressuploop pressupstop tired}}
				1 {return {stretch warmupe warmupcstart warmupcloop warmupcloop warmupcloop warmupcstop tired handstandstart handstandloop handstandloop handstandloop handstandstop kneebend}}
				2 {return {kneebend warmupcstart warmupcloop warmupcloop warmupcstop standjogstart standjogloop standjogloop standjogloop standjogloop standjogstop}}
				3 {return {warmupe jumproping jumproping jumproping jumproping tired warmupcstart warmupcloop warmupcloop warmupcstop kneebend warmupe stretch}}
			}
		}
		proc station {nr g} {
			global myref
			set strength [expr {[get_attrib $g exp_Kampf]*5+5}]
			set rlst {}
			if {$nr==6} {
				log "$g -> punching"
				lappend rlst "rotate_toleft"
				lappend rlst "prod_callmethod $myref set_animstate 1 1 $g;play_anim punchingballa"
				set acc [expr {int(rand()*$strength*0.8+2)}]
				for {set i 0} {$i<$strength} {incr i} {
					lappend rlst "sparetime_place_relief punchingballa 0.05"
					if {$acc==$i} {lappend rlst "play_anim punchingballacc"}
				}
				lappend rlst "sparetime_place_relief punchingballa 0.05"
				lappend rlst "prod_callmethod $myref set_animstate 1 0 $g"

			} elseif {$nr==4} {
				log "$g -> stemming"
				lappend rlst "rotate_toleft"
				lappend rlst "play_anim handlestemstarta"
				lappend rlst "prod_callmethod $myref set_animstate 2 1 $g;play_anim handlestemstartb"
				for {set i 0} {$i<$strength} {incr i} {
					lappend rlst "sparetime_place_relief handlestemloop 0.05"
				}
				lappend rlst "play_anim handlestemstopa"
				lappend rlst "prod_callmethod $myref set_animstate 2 0 $g;play_anim handlestemstopb"
			} else {
				log "$g -> kicking"
				set side [expr {int(rand()*$strength*1.2)}]
				lappend rlst "rotate_toback"
				for {set i 0} {$i<$strength} {incr i} {
					set rnd [irandom 6]
					lappend rlst "sparetime_place_relief [lindex {kicka kickb kickc} [expr {$rnd/2}]] 0.02"
					if {$i==$side} {
						lappend rlst "rotate_toright"
						lappend rlst "sparetime_place_relief [lindex {punchside kickd} [expr {$rnd&1}]] 0.03"
						lappend rlst "rotate_toback"
					}
				}
			}
			return $rlst
		}

		sparetime this announce fun

	}

	obj_exit {
		sparetime this disannounce
	}


}

