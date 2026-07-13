call scripts/misc/utility.tcl

def_class Titanic_abpumpen wood production 0 {} {}


def_class Riesentor none dummy 0 {} {
	method set_open {} {
		set_pf_influence this 0 0 0 0 0 0
	}

	method set_closed {} {
		set_pf_influence this -2 -60 +2 +16 INT_MAX 0
	}


	def_event evt_timer_init
	handle_event evt_timer_init {
		call_method this set_closed
	}


	class_defaultanim riesentor.standard

	obj_init {
		set_anim this riesentor.standard 0 0
		set_collision this 1
		set_physic this 0
		set_visibility this 1
		set_viewinfog this 1
		set_buildupstate this 1
		timer_event this evt_timer_init -repeat 0 -userid 0 -attime [expr [gettime] + 0.1]
	}
}


def_class Blutfleck none dummy 0 {} {
	def_event evt_dummy
	handle_event evt_dummy {}
	method dummy {} {}
	call scripts/misc/autodef.tcl


	obj_init {
		call scripts/misc/autodef.tcl
		set_physic this 0
		set_collision this 1
		set_anim this blutfleck.standard 0 $ANIM_STILL
	}
}

def_class TitanicPumpe none production 0 {} {
	call scripts/misc/genericprod.tcl

	method prod_item_actions item {
		set rlst [list]
		//lappend rlst "prod_gopos \{$pos\}"
		//lappend rlst "prod_turnback"
		//lappend rlst "prod_anim $ani"
		//lappend rlst "prod_callmethod $sch druecken"

		lappend rlst "prod_goworkdummy 0"
		lappend rlst "pumpe_activate [get_ref this]"
		return $rlst
	}

	method activate {} {
		p_activate
	}


	obj_init {
		call scripts/misc/genericprod.tcl
		set_anim this titanic_pumpe.standard 0 0
		set activated 0

		catch { sm_add_event Titanic_Pumpe1_aktiviert }
		catch { sm_add_event Titanic_Pumpe2_aktiviert }

		set_prod_directevents this 1
		set_prod_switchmode this 1
		set_hoverable this 0
		set_selectable this 0

		proc p_activate {} {
			global activated
			if { $activated == 1 } {
				return
			}
			set activated 1
			set wabsl [obj_query this "-class WasserabsaugTitanic -range 200"]
			if { $wabsl != 0 } {
				foreach wabs $wabsl {
					call_method $wabs start -1
				}
				sel /obj
				set tr [new Trigger_Swf_unq_titanic_pumpe]
				set_pos $tr [get_pos this]
				set_owner this -1
				set_hoverable this 0
				set_selectable this 0
			} else {
				log "TitanicPumpe: warning 'WasserabsaugTitanic' not found !"
			}
		}
	}
	method get_uniquename {} {return "TitanicPumpe"}
	//method schalten {dummy1 dummy2} {activate}
	//method schliessen {} {activate}
	method door_ident {} {}
}

def_class TitanicKolben none dummy 0 {} {

	def_event evt_timer0
	def_event evt_init
	def_event evt_shut

	set_class_anim stamp titanic_kolben.stampfen

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		set_anim this titanic_kolben.standard 0 0

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		set initialized 0
		set shut 0

		proc stamp {} {
    		state_disable this
			action this anim stamp {state_enable this}
		}

		proc wait {time} {
    		state_disable this
    		action this wait $time {state_enable this}
		}

		proc steam {pos} {
			set xdir [expr [random 0.3] - 0.15]
			set pos [vector_add $pos [vector_add [vector_random 0.2 0.2 0] {-0.1 -0.1 0}]]
			create_particlesource 6 $pos {0 -0.1 0.1} [expr [irandom 12] + 8] 2
		}

		proc shut {} {
    		action this anim stamp {} {}
    		state_disable this
    		timer_event this evt_shut -repeat 0 -userid 1 -attime [expr [gettime]+1.2]
		}
	}

	handle_event evt_timer0 {
		if { $initialized == 0 } {
			set klist [lnand 0 [obj_query this "-class TitanicKolben -range 20"]]
			set inc 0

			foreach item $klist {
				call_method $item init_timer $inc
				fincr inc 1.0
			}
			call_method this init
			set initialized 1
		}
	}

	method shut {} {
		set shut 1
	}

	handle_event evt_shut {
		state_disable this
		set_anim this titanic_kolben.stampfen 12 0
	}

	handle_event evt_init {
		call_method this init
	}

	method init {} {
		action this wait 0.1
		state_reset this
		state_triggerfresh this idle
	}

	method init_timer {time} {
		timer_event this evt_init -repeat 0 -userid 0 -attime [expr [gettime]+$time]
	}

	state idle {
		if { $shut } {
			shut
			state_disable this
			return
		}

		set pos [get_pos this]
		set ppos $pos
		set pos [vector_add $pos {0 0 1.6}]

		set ppos [vector_add $pos {0 -4 2}]

		set zl [lnand 0 [obj_query this "-class Zwerg -pos \{$pos\} -boundingbox {-1.2 -1 -5 1.2 1 5}"]]
		foreach item $zl {
			log "Hit $item !!!!!"
			steam $ppos
			steam $ppos
			call_method $item get_trapped splat
			add_attrib $item atr_Hitpoints  -0.3
		}

		steam $ppos
   		stamp
	}

}

def_class Schwefelbruecke metal dummy 3 {} {
	call scripts/misc/animclassinit.tcl

	method set_repaired {} {
		set_pf_influence this 0 0 0 0 0 0
	}


	method set_destroyed {} {
		set_pf_influence this -60 -10 60 10 INT_MAX 0
	}


	def_event evt_timer_init
	handle_event evt_timer_init {
		call_method this set_destroyed
	}


	class_defaultanim swf_bruecke_a.standard

	obj_init {
		set_anim this swf_bruecke_a.standard 0 0
		set_visibility this 1
		set_viewinfog this 1
		set_buildupstate this 1
		timer_event this evt_timer_init -repeat 0 -userid 0 -attime [expr [gettime] + 0.1]
	}
}


def_class Eierbecher metal dummy 3 {} {
	call scripts/misc/animclassinit.tcl

	method let_me_be_an_obj {} {}

	class_defaultanim swf_eierbecher.standard

	obj_init {
		set_viewinfog this 1
		set_anim this swf_eierbecher.standard 0 0
	}
}





