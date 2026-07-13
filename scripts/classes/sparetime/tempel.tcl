//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class Tempel stone production 4 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_fightdist 3.0

	method prod_item_actions item {
		global current_worker BOXED_CLASSES
		set exp_incr [call_method this prod_item_exp_incr $item]
		set exp_infls [call_method this prod_item_exp_infl $item]
		set exp_infl [prod_getgnomeexper $current_worker $exp_infls]
		set materiallist [call_method this prod_item_materials $item]
		set rlst [list]


		// Tempel ausfahren

		lappend rlst "prod_go_near_workdummy 1 0.5 0 0"
		lappend rlst "prod_turnback"
		lappend rlst "prod_anim kickmachine"
		lappend rlst "prod_machineanim tempel.ausfahren once"
		lappend rlst "prod_waittime 1.3"
		lappend rlst "prod_call_method set_fire 1"
		lappend rlst "prod_machineanim tempel.oben"

		// alle Items auf den Tempel tragen

        foreach material $materiallist {
            if {[check_method [get_objclass this] "prod_actions_$material"]} {
                set rlst [concat $rlst [call_method this "prod_actions_$material" "$material" "$exp_infl"]]
                lappend rlst "prod_exp $exp_incr [expr 1.0/[llength $materiallist]]"
            } else {
            	log "WARNING: tempel.tcl: no prod_actions method for $material (calling prod_actions_default)"
                set rlst [concat $rlst [call_method this "prod_actions_default" "$material" "$exp_infl"]]
            }
        }

        // alle geopferten Items vernichten

		lappend rlst "prod_go_near_workdummy 3 0.3 0 0"
		lappend rlst "prod_turnback"
		lappend rlst "prod_anim kickmachine"
		lappend rlst "prod_machineanim tempel.einschalt once"
		lappend rlst "prod_waittime 1.3"
		lappend rlst "prod_machineanim tempel.pendeln"
		lappend rlst "prod_call_method set_blood 1; prod_call_method set_bloodrain 1"

		lappend rlst "prod_go_near_workdummy 16 0 0 2.5"
		lappend rlst "prod_turnback"
		lappend rlst "prod_anim praystart"
       	lappend rlst "prod_anim_loop_expinfl prayloop 3 7 $exp_infl"

        foreach material $materiallist {
   	 		if {$item == "Wiederbelebung"  &&  $material == "Zipfelmuetze"} {
				lappend rlst "prod_ressurection \{[vector_add [get_pos this] [get_linkpos this 3]]\}"
			}
        	lappend rlst "prod_consume_from_workplace $material"
		} 

		lappend rlst "prod_call_method set_blood 0; prod_call_method set_bloodrain 0"
		lappend rlst "prod_anim praystop"

		lappend rlst "prod_call_method set_fire 0"
		lappend rlst "prod_machineanim tempel.einfahren once"
		lappend rlst "prod_waittime 1.5"
		lappend rlst "prod_machineanim tempel.standard"

 		if {$item != "Wiederbelebung"} {
			lappend rlst "prod_goworkdummy 2"
        	lappend rlst "prod_createproduct_rndrot $item"
        }

        lappend rlst "prod_turnfront"

		return $rlst
	}



	// Zipfelmuetze

    method prod_actions_Zipfelmuetze {itemtype exp_infl} {
        return [call_method this prod_actions_default $itemtype $exp_infl]
    }


	// Gold

    method prod_actions_Gold {itemtype exp_infl} {
        return [call_method this prod_actions_default $itemtype $exp_infl]
    }


	// Kristall

    method prod_actions_Kristall {itemtype exp_infl} {
        return [call_method this prod_actions_default $itemtype $exp_infl]
    }



	// Hamster

    method prod_actions_Hamster {itemtype exp_infl} {
        return [call_method this prod_actions_default $itemtype $exp_infl]
    }


	// default

    method prod_actions_default {itemtype exp_infl} {
		set rlst [list]

		lappend rlst "prod_walk_and_hide_itemtype $itemtype"
		lappend rlst "prod_goworkdummy 16"
		lappend rlst "prod_turnfront"
		lappend rlst "prod_anim tempelwalkup"
		lappend rlst "prod_beam_itemtype_near_dummypos $itemtype 7 -[random 0.5] 0 -[random 0.5]"
		lappend rlst "next_item_of_itemtype $itemtype"
		lappend rlst "prod_anim tempelwalkdown"

		return $rlst
    }


	method set_fire {bool} {
		set_particlesource this 0 $bool
		set_particlesource this 1 $bool
		set_particlesource this 2 $bool
		set_particlesource this 3 $bool
	}

	method set_bloodrain {bool} {
		set_particlesource this 4 $bool
		set_particlesource this 5 $bool
	}

	method set_blood {bool} {
		set_particlesource this 6 $bool
	}


	method deinit_production {} {
		call_method this set_fire 0
	}

    method init {} {
    	set_collision this 1

		change_particlesource this 0 2 {0 -0.2 0} {0 0 0} 32 1 0 12					;// 4 Feuer für die Kelche
		change_particlesource this 1 2 {0 -0.2 0} {0 0 0} 32 1 0 13					;// 4 Feuer für die Kelche
		change_particlesource this 2 2 {0 -0.2 0} {0 0 0} 32 1 0 14					;// 4 Feuer für die Kelche
		change_particlesource this 3 2 {0 -0.2 0} {0 0 0} 32 1 0 15					;// 4 Feuer für die Kelche
		set_particlesource this 0 0
		set_particlesource this 1 0
		set_particlesource this 2 0
		set_particlesource this 3 0

		change_particlesource this 4 8 {0 0 0} {0 0 0} 32 4 0 30					;// Blutfontänen
		change_particlesource this 5 8 {0 0 0} {0 0 0} 32 4 0 4
		set_particlesource this 4 0
		set_particlesource this 5 0
		change_particlesource this 6 8 {-0.3 0 -0.5} {0 -0.2 0} 64 4 0 7
		set_particlesource this 6 0
    }


	class_defaultanim tempel.standard
	class_flagoffset 2.2 2.9

	obj_init {
		call scripts/misc/genericprod.tcl

		set_anim this tempel.standard 0 $ANIM_LOOP
		set_energyclass this $tttenergyclass_Tempel
        set_energyconsumption this $tttenergycons_Tempel
		set_inventoryslotuse this 1

		timer_event this evt_timer_init -repeat 1 -interval 1 -userid 0 -attime [expr [gettime]+1]

		set build_dummys [list 16 17 18 19 20 21 22]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechtsstein unten_linksstein oben_rechtsstein oben_linksstein oben_linksstein oben_rechtsstein oben_linksstein}
		set damage_dummys {23 29}
	}
}

