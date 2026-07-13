//# IFNOT FULL
def_class Mittelalterbad none dummy 0 {} {}
//# ENDIF
//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class Mittelalterbad wood production 0 {} {

	class_fightdist 1.7

	method prod_items {} {return ""}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim mittel_bad.standard
	class_flagoffset 1.3 1.9

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
		lappend rlst "rotate_tofront"
		if {$link} {
			lappend rlst "call_method $myref set_fire 1"
			lappend rlst "play_anim hatofhead"
			lappend rlst "del_current_muetze"
			lappend rlst "play_anim hatofhand"
			lappend rlst "play_anim hatofgone"
			lappend rlst "play_anim superman"
			lappend rlst "sparetime_change_clothes 1"
			lappend rlst "play_anim showera;call_method $myref set_water 1"
			lappend rlst "play_anim showerb"
			lappend rlst "play_anim showerb"
			lappend rlst "play_anim showerb"
			lappend rlst "play_anim showera"
			lappend rlst "play_anim showerb"
			lappend rlst "play_anim showerb"
			lappend rlst "play_anim showerb;call_method $myref set_water 0"
			lappend rlst "play_anim superman"
			lappend rlst "sparetime_reset_clothes"
			lappend rlst "prod_change_muetze sparetime"
		} else {
			lappend rlst "call_method $myref set_fire 1"
			lappend rlst "play_anim toiletmiddlestart"
			lappend rlst "state_disable this;set_anim this mann.mittelbad_klo_loop 0 2;wait_time 8"
			lappend rlst "play_anim toiletmiddlestop"
        	lappend rlst "call_method $myref set_handwash 1 "
			lappend rlst "play_anim handwashmiddle"
			lappend rlst "call_method $myref set_handwash 0 "
		}
		return $rlst
	}


    method set_water {bool} {
		set_particlesource this 0 $bool
    	log "Duschwasser $bool"
    }

    method set_handwash {bool} {
		set_particlesource this 1 $bool
    	log "Händewaschen $bool"
    }

    method set_fire {bool} {
		set_particlesource this 2 $bool
		set_particlesource this 3 $bool
    }



	method remove_from_bath {ref link} {
		set_particlesource this [expr {!$link}] 0
		log "Wasser aus auf Seite $link"

		if {[prod_guest guestget this [expr {!$link}]]==0} {
				set_particlesource this 2 0
				set_particlesource this 3 0
				log "Remove: Andere Seite leer !"
		}

		log "Bad aufgeraeumt"
	}

    method init {} {

	// Duschen
    change_particlesource this 0 21 {0.1 -1 -3} {0 0 0} 64 4 0 0 0 0
	set_particlesource this 0 0
	// Hände waschen
	change_particlesource this 1 21 {-0.5 -0.6 -1} {0 0 0} 16 4 0 0 0 0
	set_particlesource this 1 0
	// Feuer links
	change_particlesource this 2 2 {0 -0.15 0.1} {0 0 0} 64 1 0 10 0 0
	set_particlesource this 2 0
	// Feuer rechts
	change_particlesource this 3 2 {0 -0.15 0.1} {0 0 0} 64 1 0 9 0 0
	set_particlesource this 3 0

	log "Init gemacht"
	}



	obj_init {
		call scripts/misc/genericprod.tcl

		set_anim this mittel_bad.standard 0 $ANIM_STILL
		set standard_anim mittel_bad.standard
		set_energyconsumption this 0
		set_collision this 1

		prod_guest addlink this 1
		prod_guest addlink this 2
		prod_guest addlink this 0

		set myref [get_ref this]

		sparetime this announce bath

		set build_dummys [list 16 17 18 19 20 21 22]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechts unten_rechts unten_links unten_rechts unten_rechts oben_links oben_rechts}
		set damage_dummys {23 30}

		timer_event this evt_timer_init -repeat 1 -interval 1 -userid 0 -attime [expr [gettime]+1]
	}

	obj_exit {
		sparetime this disannounce
	}

}

