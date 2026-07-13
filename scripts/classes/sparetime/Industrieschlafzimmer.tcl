//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class Industrieschlafzimmer wood production 0 {} {

	method prod_items {} {return ""}
	
	method get_random_seat {} {
		set pl {}
		set pc 0
		for {set i 0} {$i<6} {incr i} {
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
		for {set i 0} {$i<6} {incr i} {
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
	
	class_defaultanim indust_schlaf.standard
	class_flagoffset 1.8 3.9

	obj_init {
		call scripts/misc/genericprod.tcl

		set_anim this indust_schlaf.standard 0 $ANIM_STILL
		set standard_anim indust_schlaf.standard
		set_energyconsumption this 0
		set_collision this 1

		sparetime this announce sleep

		prod_guest addlink this 2
		prod_guest addlink this 3
		prod_guest addlink this 4
		prod_guest addlink this 5
		prod_guest addlink this 6
		prod_guest addlink this 7

		set build_dummys [list 16 17 18 19 20 21 22 23]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_links unten_rechts unten_rechts oben_rechts unten_rechts oben_rechts unten_rechts unten_links}
		set damage_dummys {22 26}

	}
	
	obj_exit {
		sparetime this disannounce
	}
	
}

