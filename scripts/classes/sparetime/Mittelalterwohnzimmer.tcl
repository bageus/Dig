//# IFNOT FULL
def_class Mittelwohnzimmer none dummy 0 {} {}
//# ENDIF
//# STOPIFNOT FULL
call scripts/misc/utility.tcl


def_class Mittelalterwohnzimmer wood production 1 {} {

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

	class_defaultanim mittel_wohn.standard
	class_flagoffset 1.8 2.5

	obj_init {
		set_anim this mittel_wohn.standard 0 $ANIM_LOOP
		set standard_anim mittel_wohn.standard
		call scripts/misc/genericprod.tcl
		set_collision this 1

		prod_guest addlink this 7
		prod_guest addlink this 9
		prod_guest addlink this 1
		prod_guest addlink this 3
		prod_guest addlink this 6
		prod_guest addlink this 5
		set seat_cnt 6

		set anim_state 0
		set myref [get_ref this]

		sparetime this announce home

		proc switch_anim {} {
			global anim_state
			switch $anim_state {
				0 {set_anim this mittel_wohn.standard 0 0}
				1 {set_anim this mittel_wohn.ohneschaukel 0 0}
				2 {set_anim this mittel_wohn.ohnespinn 0 0}
				3 {set_anim this mittel_wohn.ohnebeides 0 0}
			}
		}

		proc get_seat_random {{last -1}} {
			global seat_cnt
			set sl {}
			set sc 0
			for {set i 0} {$i<$seat_cnt} {incr i} {
				if {$i==$last||$i>2&&$last>2} {continue}
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
					lappend rlst "rotate_toangle 0.9"
					lappend rlst "prod_callmethod $myref set_animstate 1 1;play_anim rockchairstart"
					for {set i 0} {$i<7} {incr i} {lappend rlst "sparetime_home_relief rockchairloop 0.02"}
					lappend rlst "sparetime_home_relief rockchairloop 0.02 1"
					lappend rlst "play_anim rockchairstop"
					lappend rlst "prod_callmethod $myref set_animstate 1 0"
				}
				1 {
					lappend rlst "rotate_toangle 3.67"
					lappend rlst "prod_callmethod $myref set_animstate 2 1;play_anim spinstart"
					for {set i 0} {$i<7} {incr i} {lappend rlst "sparetime_home_relief spinloop 0.02"}
					lappend rlst "sparetime_home_relief spinloop 0.02 1"
					lappend rlst "play_anim spinstop"
					lappend rlst "prod_callmethod $myref set_animstate 2 0"
				}
				2 {
					//Trainierbogen wird von selbst in die Hand genommen
					lappend rlst "rotate_toback"
					//lappend rlst "change_tool Trainierbogen 1"
					for {set i 0} {$i<7} {incr i} {lappend rlst "sparetime_home_relief shootbow 0.02"}
					lappend rlst "sparetime_home_relief shootbow 0.02 1"
					//lappend rlst "change_tool 0 1"
				}
				default {
					if {$seat==3} {
						lappend rlst "rotate_toright"
					} else {
						lappend rlst "rotate_tofront"
					}
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

		set build_dummys [list 16 17 18 19 20 21 22 23]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechts oben_links oben_rechts oben_rechts oben_rechts unten_rechts unten_links unten_links}
		set damage_dummys {21 31}
	}

	obj_exit {
		sparetime this disannounce
	}

}

