//# IFNOT FULL
def_class Industriewohnzimmer none dummy 0 {} {}
//# ENDIF
//# STOPIFNOT FULL
call scripts/misc/utility.tcl


def_class Industriewohnzimmer metal production 1 {} {
	
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

	class_defaultanim indust_wohn.standard
	class_flagoffset 4.3 3.0

	obj_init {
		set_anim this indust_wohn.standard 0 $ANIM_LOOP
		set standard_anim indust_wohn.standard
		call scripts/misc/genericprod.tcl
		set_collision this 1
		
		prod_guest addlink this 1
		prod_guest addlink this 2
		prod_guest addlink this 3
		prod_guest addlink this 4
		prod_guest addlink this 8
		prod_guest addlink this 5
		prod_guest addlink this 6
		prod_guest addlink this 7
		set seat_cnt 8
		
		set anim_state 0
		set myref [get_ref this]
		
		sparetime this announce home

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
					lappend rlst "rotate_tofront"
					lappend rlst "play_anim planestart"
					for {set i 0} {$i<7} {incr i} {lappend rlst "sparetime_home_relief planeloop 0.02"}
					lappend rlst "sparetime_home_relief planeloop 0.02 1"
					lappend rlst "play_anim planeend"
				}
				1 {
					lappend rlst "rotate_toangle 1.95"
					lappend rlst "play_anim couchstart"
					for {set i 0} {$i<5} {incr i} {
						set anim couchloop[string index abcd [irandom 4]]
						lappend rlst "sparetime_home_relief $anim 0.02"
					}
					set anim couchloop[string index abcd [irandom 4]]
					lappend rlst "sparetime_home_relief $anim 0.02 1"
					lappend rlst "play_anim couchstop"
				}
				2 {
					lappend rlst "rotate_toback"
					for {set i 0} {$i<3} {incr i} {lappend rlst "sparetime_home_relief workatfloor 0.03"}
					lappend rlst "sparetime_home_relief workatfloor 0.03 1"
				}
				3 {
					// Grammophon
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
				4 {
					// Liegestuhl
					lappend rlst "rotate_toangle 0.65"
					lappend rlst "play_anim campinduststart"
					for {set i 0} {$i<7} {incr i} {lappend rlst "sparetime_home_relief campindustloop 0.02"}
					lappend rlst "sparetime_home_relief campindustloop 0.02 1"
					lappend rlst "play_anim campinduststop"
				}
				default {
					set angle [lindex {_ _ _ _ _ 1.745 4.31 5.12} $seat]
					lappend rlst "rotate_toangle $angle"
					lappend rlst "play_anim sitdown_chair"
					for {set i 0} {$i<7} {incr i} {lappend rlst "sparetime_home_relief sitchairloop 0.01"}
					lappend rlst "sparetime_home_relief sitchairloop 0.01 1"
					lappend rlst "play_anim standup_chair"
				}
			}
			lappend rlst "sparetime_home_change_seat"
			return $rlst
		}
		
		set build_dummys [list 16 17 18 19 20 21 22 23]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechts unten_rechts unten_links oben_rechts oben_links unten_rechts oben_rechts unten_rechts}
		set damage_dummys {23 28}
	}
	
	obj_exit {
		sparetime this disannounce
	}
	
}

