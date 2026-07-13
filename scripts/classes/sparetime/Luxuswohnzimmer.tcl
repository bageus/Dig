//# IFNOT FULL
def_class Luxuswohnzimmer none dummy 0 {} {}
//# ENDIF
//# STOPIFNOT FULL
call scripts/misc/utility.tcl


def_class Luxuswohnzimmer metal production 1 {} {
	
	class_fightdist 2.0
	
	method prod_items {} {return ""}

	method set_animstate {bit st} {
		if {$st} {
			set anim_state [expr {$bit|$anim_state}]
		} else {
			set anim_state [expr {~$bit&$anim_state}]
		}
		switch_anim
	}
	
	method get_random_seat {} {
		return [get_seat_random]
	}
	
	method get_new_seat {old} {
		return [get_seat_random $old]
	}
	
	method get_next_action {seat} {
		return [next_guest_action $seat]
	}
	
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim golden_wohn.standard
	class_flagoffset 2.5 3.5

	obj_init {
		set_anim this golden_wohn.standard 0 $ANIM_LOOP
		set standard_anim golden_wohn.standard
		call scripts/misc/genericprod.tcl
		set_collision this 1
		
		prod_guest addlink this 1
		prod_guest addlink this 2
		prod_guest addlink this 3
		prod_guest addlink this 21
		prod_guest addlink this 5
		prod_guest addlink this 7
		prod_guest addlink this 8
		prod_guest addlink this 9
		set seat_cnt 8
		
		set anim_state 0
		set myref [get_ref this]

		sparetime this announce home

		set build_dummys [list 24 25 26 27 28 29 30]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechtsstein unten_linksstein unten_rechtsstein unten_linksstein oben_rechtsstein unten_rechtsstein unten_rechtsstein}
		set damage_dummys {23 30}

		proc switch_anim {} {
			global anim_state
			switch $anim_state {
				0 {set_anim this indust_wohn.standard 0 0}
				1 {set_anim this indust_wohn.ohneschaukel 0 0}
				2 {set_anim this indust_wohn.ohnespinn 0 0}
				3 {set_anim this indust_wohn.ohnebeides 0 0}
			}
		}
		
		proc get_seat_random {{last -1}} {
			global seat_cnt
			set sl {}
			set sc 0
			for {set i 0} {$i<$seat_cnt} {incr i} {
				if {$i==$last||$i>4&&$last>4} {continue}
				if {[prod_guest guestget this $i]==0} {
					lappend sl $i
					incr sc
				}
			}
			if {$sl==""} {return -1}
			return [lindex $sl [irandom $sc]]
		}
		
		proc next_guest_action {seat} {
			global myref
			set rlst [list]
			switch $seat {
				0 {
					// Telefon
					lappend rlst "rotate_toleft"
					lappend rlst "sparetime_home_relief scratchhead 0.03"
					lappend rlst "sparetime_home_relief scratch 0.03"
					lappend rlst "sparetime_home_relief talkacngc 0.03"
					lappend rlst "rotate_toangle 4.9"
					lappend rlst "sparetime_home_relief dontknow 0.03 1"
				}
				1 {
					// Klavier
					lappend rlst "rotate_toleft"
					lappend rlst "play_anim pianogoldenstart"
					for {set i 0} {$i<7} {incr i} {
						lappend rlst "sparetime_home_relief pianogoldenloop 0.02"
					}
					lappend rlst "sparetime_home_relief pianogoldenloop 0.02 1"
					lappend rlst "play_anim pianogoldenstop"
				}
				2 {
					// Fernseher
					lappend rlst "rotate_toangle 2.3"
					lappend rlst "play_anim tvgoldenstart"
					for {set i 0} {$i<7} {incr i} {
						lappend rlst "sparetime_home_relief tvgoldenloop 0.02"
					}
					lappend rlst "sparetime_home_relief tvgoldenloop 0.02 1"
					lappend rlst "play_anim tvgoldenstop"
				}
				3 {
					// Musikbox
					lappend rlst "rotate_toback"
					lappend rlst "play_anim switchnorm"
					set anim discoa
					for {set i 0} {$i<12} {incr i} {
						set rnd [expr {rand()}]
						if {$anim=="discod"} {
							if {$rnd<0.5} {
								set anim discoa
							} else {
								set anim discoc
							}
						} else {
							if {$rnd<0.1} {
								set anim discod
							} elseif {$rnd<0.3} {
								set anim [string map {oa oc oc oa} $anim]
							}
						}
						if {$i==11} {
							lappend rlst "sparetime_home_relief $anim 0.015 1"
						} else {
							lappend rlst "sparetime_home_relief $anim 0.015"
						}
					}
					lappend rlst "play_anim switchnorm"
				}
				default {
					// Sessel und Canapee
					set angle [lindex {_ _ _ _ 0.8 1.57 1.57 0.0} $seat]
					lappend rlst "rotate_toangle $angle"
					lappend rlst "play_anim couchstart"
					for {set i 0} {$i<5} {incr i} {
						set anim couchloop[string index abcd [irandom 4]]
						lappend rlst "sparetime_home_relief $anim 0.02"
					}
					set anim couchloop[string index abcd [irandom 4]]
					lappend rlst "sparetime_home_relief $anim 0.02 1"
					lappend rlst "play_anim couchstop"
				}
			}
			lappend rlst "sparetime_home_change_seat"
			return $rlst
		}
		
	}
	
	obj_exit {
		sparetime this disannounce
	}
	
}

