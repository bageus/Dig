call scripts/misc/utility.tcl

def_class Zelt wood production 0 {} {

	method prod_items {} {return ""}
	
	method set_standardanim {} {
		global standard_anim ANIM_LOOP
		set_anim this $standard_anim 0 $ANIM_LOOP
	}

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
	
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim zelt_a.standard
	class_defaultanim zelt_b.standard
	class_defaultanim zelt_c.standard
	class_flagoffset 1.5 -2.0

	obj_init {
		call scripts/misc/genericprod.tcl
		set zeltnr [irandom 3]
		set standard_anim zelt_[string index abc $zeltnr].standard
		set_objvariation this $zeltnr

		log "STANIM: $standard_anim"
		set_anim this $standard_anim 0 $ANIM_LOOP
		set_energyconsumption this 0
		set_collision this 1

		sparetime this announce sleep

		prod_guest addlink this 0
		prod_guest addlink this 1

		set build_dummys [list 16 17 18]
		set max_buildup_step [llength $build_dummys]
		if { $zeltnr == 3 } {
			set buidup_anis {unten_rechtsholz unten_linksholz unten_linksholz}
		} else {
			set buidup_anis {unten_rechtsholz oben_rechtsholz unten_linksholz}
		}
		set damage_dummys {20 24}

	}
	
	obj_exit {
		sparetime this disannounce
	}
	
}

