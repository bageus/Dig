//# IFNOT FULL
def_class Luxusbad none dummy 0 {} {}
//# ENDIF
//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class Luxusbad wood production 0 {} {

	class_fightdist 2.0

	method prod_items {} {return ""}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim golden_bad.standard
	class_flagoffset 3.3 4.5

	method is_wait_dummy {dummy} {
		return [expr {$dummy==2}]
	}
	method get_otherdummy {dummy} {
		if {$dummy==0} {
			if {[prod_guest guestget this 1]} {return -1} {return 1}
		} elseif {$dummy==1} {
			if {[prod_guest guestget this 0]} {return -1} {return 0}
		} else {
			return -1
		}
	}
	method get_bath_actions {ref link} {
		set rlst [list]
		if {$link} {
			lappend rlst "rotate_toangle 4.0"
			lappend rlst "play_anim hatofhead"
			lappend rlst "del_current_muetze"
			lappend rlst "play_anim hatofhand"
			lappend rlst "play_anim hatofgone"
			lappend rlst "play_anim superman"
			lappend rlst "sparetime_change_clothes 1"
			lappend rlst "play_anim bathgoldenastart"
			lappend rlst "call_method $myref set_water 1"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "play_anim bathgoldenbloop"
			lappend rlst "call_method $myref set_water 0"
			lappend rlst "play_anim bathgoldenbstop"
			lappend rlst "play_anim superman"
			lappend rlst "sparetime_reset_clothes"
			lappend rlst "prod_change_muetze sparetime"
	    } else {
			lappend rlst "rotate_toangle 0.5"
			lappend rlst "play_anim toiletinduststart"
			lappend rlst "play_anim toiletindustloop"
			lappend rlst "play_anim toiletindustloop"
			lappend rlst "play_anim toiletindustloop"
			lappend rlst "play_anim toiletindustloop"
			lappend rlst "play_anim toiletindustloop"
			lappend rlst "play_anim toiletinduststop"
			lappend rlst "walk_dummy $myref 3"
			lappend rlst "rotate_toback"
			lappend rlst "call_method $myref set_handwash 1"
			lappend rlst "play_anim handwashindust"
			lappend rlst "call_method $myref set_handwash 0"
		}
		return $rlst
	}

	method remove_from_bath {ref link} {
		set_particlesource this $link 0
		log "Wasser aus auf Seite $link"

	}

    method set_handwash {bool} {
		set_particlesource this 0 $bool
    	log "Händewaschen $bool"
    }

    method set_water {bool} {
		set_particlesource this 1 $bool
    	log "Wannenwasser $bool"
    }

	method init {} {

	// Hände waschen
	change_particlesource this 0 21 {0 0 0} {0 0 0} 16 4 0 10 0 0
	set_particlesource this 0 0
	// Badewanne
	change_particlesource this 1 21 {0 -0.1 0.4} {0 0 0} 25 16 0 8 0 0
	set_particlesource this 1 0

	log "Init gemacht"
	}


	obj_init {
		call scripts/misc/genericprod.tcl

		set_anim this golden_bad.standard 0 $ANIM_STILL
		set standard_anim golden_bad.standard
		set_energyconsumption this 0
		set_collision this 1

		prod_guest addlink this 2
		prod_guest addlink this 5
		prod_guest addlink this 0

		set myref [get_ref this]

		sparetime this announce bath

		set build_dummys [list 16 17 18 19 20 21]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {oben_rechts oben_rechts oben_links unten_rechts unten_rechts unten_rechts}
		set damage_dummys {19 29}

    	timer_event this evt_timer_init -repeat 1 -interval 1 -userid 0 -attime [expr [gettime]+1]
	}

	obj_exit {
		sparetime this disannounce
	}

}

