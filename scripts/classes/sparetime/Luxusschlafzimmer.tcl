//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class Luxusschlafzimmer wood production 0 {} {

	class_fightdist 2.0

	method prod_items {} {return ""}
	
	method get_random_seat {} {
		set pl {}
		set pc 0
		for {set i 0} {$i<2} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				lappend pl $i
				incr pc
			}
		}
		if {$pl==""} {
			return -1
		} else {
			return [lindex $pl [irandom $pc]]
		}
	}
	
	method get_partner_seat {ref} {
		set found 0
		for {set i 0} {$i<4} {incr i} {
			if {[prod_guest guestget this $i]==$ref} {
				set found 1
				break
			}
		}
		if {$found} {
			set otherseat [expr {$i^1}]
			if {[prod_guest guestget this $otherseat]==0} {
				return $otherseat
			}
		}
		return -1
	}
	
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl
	
	class_defaultanim golden_schlaf.standard
	class_flagoffset 3.0 3.4

	obj_init {
		call scripts/misc/genericprod.tcl

		set_anim this golden_schlaf.standard 0 $ANIM_STILL
		set standard_anim golden_schlaf.standard
		set_energyconsumption this 0
		set_collision this 1

		sparetime this announce sleep

		prod_guest addlink this 3
		//prod_guest addlink this 1
		//prod_guest addlink this 2
		prod_guest addlink this 4

		set build_dummys [list 16 17 18 19 20 21 22]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {oben_rechts oben_rechts unten_rechts unten_links unten_rechts unten_rechts unten_links}
		set damage_dummys {20 30}

	}
	
	obj_exit {
		sparetime this disannounce
	}
	
}

