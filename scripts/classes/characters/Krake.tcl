def_class Krake none monster 0 {} {
	obj_init {
		set_anim this krake_select.standard 0 0

		proc HP_Transfer {} {
    		set hp [get_attrib this atr_Hitpoints]
    		call_method /obj/Krake_real invoke_hit $hp
		}

		state_triggerfresh this idle
	}
	method please_let_me_be_an_obj {} {}
	method im_attacking_you {} {}
	method check_first_strike {caller} {
		return 1
	}

	def_event evt_task_defend

	handle_event evt_task_defend {
		HP_Transfer
	}

	state idle {
		HP_Transfer
		state_disable this
		action this wait 1.0 {state_enable this} {state_enable this}
	}
}

def_class Krake_ none monster 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members
	def_event evt_timer0
	def_event evt_task_defend
	def_event evt_timer_hit
	def_event evt_timer_release

	handle_event evt_timer0 {
		if { $initialized == 0 } {
			init_fight_data

			sel /obj
			set kdummy [new Krake]
	    	set_pos $kdummy [get_pos this]
	    	set_rot $kdummy [get_rot this]
	    	set_hoverable $kdummy 1
	    	set_objname $kdummy "Krake"

			set initialized 1
		}
	}

	handle_event evt_timer_hit {
		log "Krake event_timer_hit with ($evt_hit_data)"
		if { [string is integer [lindex $evt_hit_data 0]] } {
    		set ownpos [get_pos this]
    		foreach item $evt_hit_data {

    			set dpos {0 0 0}
    	    	catch { set dpos [get_linkpos this $item] }

    	    	set HitDist 2.9
    	    	//if { $item == 19 || $item == 99 } {
    	    	//	set HitDist 3.5
    	    	//}

    	    	set HitPos [vector_add $dpos $ownpos]

    	    	// Fake Dummy !!!
    	    	if { $item == 99 } {
    	    		set HitPos [vector_add {-4.06 0 2.653} $ownpos]
    	    	}

    	    	fight_setactions_strikeback this $HitDist true $HitPos
    	    	log "** fight_setactions_strikeback this $HitDist true $HitPos"
    		}
    	} else {
    		log "speccmd: $evt_hit_data"
    		eval $evt_hit_data
    	}
		set_next_hit_evt
	}

	handle_event evt_timer_release {
		log "Krake release"
		link_obj $link_gnome
		set ownpos [get_pos this]
   	    set dpos [get_linkpos this $last_link]
   	    set newpos [vector_add [vector_add $ownpos $dpos] {0 0 1.3}]
   	    set newz [hmin [lindex $newpos 2] 13.0]
   	    lrep newpos 2 $newz
        set_pos $link_gnome $newpos
        state_disable $link_gnome
        action $link_gnome fall "" "tasklist_clear $link_gnome;state_enable $link_gnome"
	}

	handle_event evt_task_defend {
		// Es wird nur der KrakenHoverDummy angegriffen !!
	}

	// virtual
	method im_attacking_you {} {}

    set_class_anim standard krake.standard

    set_class_anim stairback krake.treppenbucht_schalter

    set_class_anim idle		krake.warten
    set_class_anim hit_a	krake.schlag_fuchteln		{{cmd {auto 1}} {hits {3 {99} 6 {18} 8 {17} 10 {19} 12 {16} 14 {99} 28 {99} 30 {18} 32 {17} 35 {16} 37 {99} }}}
    set_class_anim hit_b	krake.schlag_hinten	    	{{cmd {auto 1}} {hits {10 {17 18 99} 48 {16}}}}
    set_class_anim hit_c	krake.schlag_schalter       {{cmd {ex {check_gnome_near_button}}} {hits {14 {catch_gnome 21 3 "buttonpos"} 19 {19} }}}
    set_class_anim hit_d	krake.schlag_treppenbucht   {{cmd {ex {check_gnome_near_20    }}} {hits {1 {move_coll_button up} 8 {99} 15 {catch_gnome 21 90} 34 {99} 95 {move_coll_button down}}} {endanim stairback}}
    set_class_anim hit_e	krake.schlag_vorne	        {{cmd {auto 1}} {hits {12 {19} 15 {17 18} 19 {99} 35 {19} 48 {16}}}}
    set_class_anim hit_f	krake.warten				{{cmd {auto 1}}}

    set_class_anim die_a	krake.tod
    set_class_anim die_b	krake.tod_durch_kronleuchter
    set_class_anim gothit	krake.treffer
    set_class_anim back		krake.treppenbucht_schalter

   	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members
		set_anim this krake.standard 0 $ANIM_LOOP		;# set standard anim
		set_collision this 0
		set_hoverable this 0
		set_selectable this 0
		state_triggerfresh this idle
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 1]
		set_objname this "Krake_real"
    	set_attrib this atr_Hitpoints 4.0
    	set kdummy -1

        set f_cmd_list [list]
        set f_hit_list [list]
        set f_end_list [list]
        set fblist [list]
        set initialized 0
        set near_gnomes [list]
        set evt_hit_list [list]
        set evt_hit_data ""
        set buttonpos 0
        set is_dying 0
        set coll_front "invalid"
        set coll_button "invalid"
        set coll_button_pos {0 0 0}
        set link_gnome -1
        set last_link -1
        set last_hit_time 0

        proc init_fight_data {} {
        	global f_cmd_list f_hit_list fblist coll_front coll_button coll_button_pos f_end_list
            set fblist {a b c d e f}

            set f_cmd_list [list]
            set f_hit_list [list]
            set f_end_list [list]
            foreach item $fblist {
            	lappend f_cmd_list {}
            	lappend f_hit_list {}
            	lappend f_end_list {}
            }

            set iCnt 0
            foreach item $fblist {
            	set animinfo [get_classaniminfo hit_$item]

            	foreach inf $animinfo {
            		set name [lindex $inf 0]
            		set data [lindex $inf 1]
            		switch $name {
            			"cmd"	{
            						lrep f_cmd_list $iCnt $data
            					}
            			"hits"	{
            						lrep f_hit_list $iCnt $data
            					}
            			"endanim" 	{
            							lrep f_end_list $iCnt $data
            						}
            		}
            	}
            	incr iCnt
            }
            set il [obj_query this "-type info -range 25"]
            foreach item $il {
            	set on 0
            	catch {
            		set on [call_method $item get_info name]
            	}
            	if { [string compare $on "Krake_Front"] == 0 } {
            		set coll_front $item
            	} elseif { [string compare $on "Krake_Button"] == 0 } {
            		set coll_button $item
            		set coll_button_pos [get_pos $item]
            	}
            }
            if { $coll_front == "invalid" || $coll_button == "invalid" } {
            	log "Krake Warning: unexpected error Collision Objects (Krake_Front,Krake_Button) not found !!!"
            }
        }

        proc build_hit_list {id} {
        	global fblist f_hit_list evt_hit_list
            set evt_hit_list [list]
            set tnow [gettime]
            set hl [lindex $f_hit_list $id]
            for {set i 0} {$i < [llength $hl]} {incr i 2} {
            	set item [lindex $hl $i]
            	set data [lindex $hl [expr {$i + 1}]]
            	set ntime [lindex $item 0]
            	set ntime [expr ($ntime / 10.0) + $tnow]
            	lappend evt_hit_list "$ntime \{$data\}"
            }
            log "hit_list: ($id) $evt_hit_list"
        }

        proc set_next_hit_evt {} {
        	global evt_hit_list evt_hit_data
        	if { [llength $evt_hit_list] < 1 } {
        		log "hit_list empty"
        		return
        	}
        	set nextevent [lrem evt_hit_list 0]
        	set nexttime [lindex $nextevent 0]
        	set evt_hit_data [lindex $nextevent 1]
        	timer_event this evt_timer_hit -repeat 1 -interval 1 -userid 0 -attime $nexttime
        	log "hitevent invoke: at ($nexttime) now: ([gettime])"
        }

        proc fight_act {id} {
        	global fblist f_hit_list evt_hit_list f_end_list

        	set anim "hit_[lindex $fblist $id]"
        	set finish_code {state_enable this}

			build_hit_list $id
			set_next_hit_evt

            set endanim [lindex $f_end_list $id]
            if { $endanim != "" } {
	        	set finish_code "state_enable this;play_anim $endanim"
            }

        	log "Krake hit: $anim"
        	state_disable this
        	action this anim $anim $finish_code
        }

        proc play_anim {anim} {
        	global is_dying
        	log "Krake:    !!  play_anim $anim"
        	if { $is_dying == 0 } {
        		state_disable this
        		if {$anim == "gothit" } {
        			move_coll_button up
        			action this anim $anim {state_enable this; move_coll_button down} {state_enable this; move_coll_button down}
        			return
        		}
        		action this anim $anim {state_enable this}
        	}
        }

        proc check_gnome_near_button {} {
        	global buttonpos
        	if { $buttonpos == 0 } {
        		set btn [obj_query this "-class Schalter_knopf_metall -range 20 -limit 1"]
        		if { $btn == 0 } {
        			log "Krake Warning: unexpected error Schalter_knopf_metall not found !!!"
        			return
        		}
        		set buttonpos [vector_add [get_pos $btn] {0 0 3}]
        	}
        	set zw [obj_query this "-class Zwerg -pos \{$buttonpos\} -range 3 -limit 1"]
        	set zw [living_filter $zw]
        	if { $zw != 0 } {
        		log "Zwerg near Button"
        		return 1
        	}
        	return 0
        }

		proc check_gnome_near_20 {} {
			if {[irandom 7] == 4} {log "Hand wegnehmen" ;return 1}
    		set ownpos [get_pos this]
   	    	set dpos [get_linkpos this 20]
 			set zw [obj_query this "-class Zwerg -pos \{[vector_add $ownpos $dpos]\} -range 2.3 -limit 1"]
 			set zw [living_filter $zw]
        	if { $zw != 0 } {
        		log "Zwerg near 20"
        		return 1
        	}

			return 0
		}

		proc move_coll_button {dir} {
			global coll_button coll_button_pos
			switch $dir {
				"up"	{
							set_pos $coll_button [vector_add $coll_button_pos {0 0 -6}]
						}
				"down"	{
							set_pos $coll_button $coll_button_pos
						}
			}
		}

		proc living_filter {ol} {
			if { $ol == 0 } {
				return 0
			}
			set nl [list]
			foreach item $ol {
				if {$item != 0 && [obj_valid $item]} {
					set hp [get_attrib $item atr_Hitpoints]
					if { $hp >= 0.01 } {
						lappend nl $item
					}
				}
			}
			return $nl
		}

		proc catch_gnome {link time {posvar "none"}} {
			global link_gnome last_link
    		set ownpos [get_pos this]
   	    	set dpos [get_linkpos this $link]
 			set rpos [vector_add $ownpos $dpos]

   	    	if { $posvar != "none" } {
   	    		global $posvar
   	    		set rpos [subst $$posvar]
   	    	}

 			set zw [obj_query this "-class Zwerg -pos \{$rpos\} -range 3.0 -limit 1"]
 			set zw [living_filter $zw]
			log "catch gnome ([vector_add $ownpos $dpos])"
        	if { $zw != 0 } {
        		log "catch gnome ->>>>>>>>>>>>>>>>>>><"
        		link_obj $zw this $link
        		set last_link $link
        		set link_gnome $zw
        		timer_event this evt_timer_release -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + ($time / 10.0)]
        	}

		}

	}

	state idle {
		if { $is_dying } {
			state_disable this
			return
		}
		if { $last_hit_time > 0 } {
			if { [expr {$last_hit_time + 0.5}] >= [gettime] } {
				set $last_hit_time 0
				play_anim gothit
				return
			}
		}
		set zl [obj_query this "-class Zwerg -range 15"]
		set zl [living_filter $zl]
		if { $initialized == 1 && $zl != 0 } {
			set near_gnomes $zl
			set immed -1
			set iIdx 0
			set ch_list [list]
			foreach cmditem $f_cmd_list {
				set cmd [lindex $cmditem 0]
				set data [lindex $cmditem 1]
				switch $cmd {
					"auto"	{
								for {set i 0} {$i < $data} {incr i} {lappend ch_list $iIdx}
							}
					"ex"	{
								if { [eval $data] } {
									set immed $iIdx
								}
							}
				}
				if { $immed != -1} {
					break
				}
				incr iIdx
			}
			if { $immed != -1} {
				fight_act $immed
			} else {
				set rnd [irandom [llength $ch_list]]
				fight_act [lindex $ch_list $rnd]
			}
		} else {
			set anim idle
			state_disable this
			action this anim $anim "state_enable [get_ref this]"
		}
	}


	state fight_dispatch {
	}

	method kill_switch {} {
		if { $is_dying == 0 } {
    		set is_dying 1
    		del /obj/Krake
    		del $coll_front
    		del $coll_button
    		state_disable this
    		action this anim die_b ""
    		log "Krake Kill !!!!!!!!!!!!!!!"
    	}
	}

	method invoke_hit {hp} {

		set oldHP [get_attrib this atr_Hitpoints]
		if { abs($oldHP - $hp) > 0.001 } {
			set last_hit_time [gettime]
		}

 		set_attrib this atr_Hitpoints $hp
		//log "!!!Krake was hit remhp: [get_attrib this atr_Hitpoints]"
		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			call_method_static Trigger_swf_121_Ex create
		}
	}
}
