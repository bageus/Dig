//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class Mittelalterschlafzimmer wood production 0 {} {

	class_fightdist 2.0

	method prod_items {} {return ""}
	
	method get_random_seat {} {
		set pl {}
		set pc 0
		if {[prod_guest guestget this 0]==0} {
			lappend pl 0
			incr pc
		}
		if {[prod_guest guestget this 1]==0} {
			lappend pl 1
			incr pc
		}
		if {$pl==""} {
			return -1
		} else {
			return [lindex $pl [irandom $pc]]
		}
	}
	
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl
	
	class_defaultanim mittel_schlaf.standard
	class_flagoffset 1.6 2.5

	obj_init {
		call scripts/misc/genericprod.tcl

		set_anim this mittel_schlaf.standard 0 $ANIM_STILL
		set standard_anim mittel_schlaf.standard
		set_energyconsumption this 0
		set_collision this 1

		sparetime this announce sleep

		prod_guest addlink this 2
		prod_guest addlink this 1

		set build_dummys [list 16 17 18 19 20 21 22]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_links unten_links oben_rechts oben_rechts oben_links unten_rechts unten_rechts}
		set damage_dummys {21 30}

	}
	
	obj_exit {
		sparetime this disannounce
	}
	
}

