def_class Drachenbaby none monster 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members

	class_fightdist 1.0

	def_event evt_timer0
	def_event evt_task_defend
	def_event evt_task_attack
	def_event evt_task_walk
	def_event evt_burnstart
	def_event evt_burnstop
	def_event evt_delitem
	def_event evt_drbaby_attribupdate


    set_class_anim idlea drache01.sitzen_warten_a
    set_class_anim idleb drache01.sitzen_warten_b
    set_class_anim idlec drache01.sitzen_warten_c

    set_class_anim kungfustillani drache01.stehen_warten

    set_class_anim climbup				drache01.stehen_schweben_oben_loop
    set_class_anim climbdown			drache01.stehen_schweben_unten_loop
    set_class_anim climbright			drache01.stehen_schweben_loop
    set_class_anim climbleft			drache01.stehen_schweben_loop

    set_class_anim ladderclimbup		drache01.stehen_schweben_loop
    set_class_anim ladderclimbdown		drache01.stehen_schweben_loop

    set_class_anim ladderupstart		drache01.stehen_schweben_start
    set_class_anim ladderuploop			drache01.stehen_schweben_loop

    set_class_anim ladderdownstart		drache01.stehen_schweben_start
    set_class_anim ladderdownloop		drache01.stehen_schweben_loop
    set_class_anim ladderdownend		drache01.stehen_schweben_end

    set_class_anim ladderstill			drache01.stehen_schweben_loop
    set_class_anim ladderend			drache01.stehen_schweben_end

    set_class_anim climbtostand			drache01.stehen_schweben_end
    set_class_anim standtoclimb			drache01.stehen_schweben_start
    set_class_anim climbstill			drache01.stehen_schweben_loop
    set_class_anim climbstillani		drache01.stehen_schweben_loop

    set_class_animset 0 {
    	{standard			drache01.standard					}
    	{walk_start			drache01.stehen_laufen_start		}
    	{walk_loop			drache01.stehen_laufen_loop			}
    	{walk_end			drache01.stehen_laufen_end			}

    	{turn_left_90		drache01.stehen_umspringen_l_90		}
    	{turn_right_90		drache01.stehen_umspringen_r_90		}
    	{turn_left_180		drache01.stehen_wenden_l_180		}
    	{turn_right_180		drache01.stehen_wenden_r_180		}

    	{climb_standard		drache01.stehen_schweben_loop		}
    	{climb_up			drache01.stehen_schweben_oben_loop	}
    	{climb_down			drache01.stehen_schweben_unten_loop	}
    	{climb_right		drache01.stehen_schweben_loop		}
    	{climb_left			drache01.stehen_schweben_loop		}

    	{ladder_climb_up	drache01.stehen_schweben_oben_loop	}
    	{ladder_climb_down	drache01.stehen_schweben_unten_loop	}
    	{ground_to_wall		drache01.stehen_schweben_start		}
    	{wall_to_ground		drache01.stehen_schweben_end		}
    	{ladder_to_ground	drache01.stehen_schweben_end		}
    }

    set_class_animset 1 {
    	{walk_start			drache01.stehen_hoppsen_start		}
    	{walk_loop			drache01.stehen_hoppsen_loop		}
    	{walk_end			drache01.stehen_hoppsen_end			}

    	{climb_standard		drache01.stehen_schweben_loop		}
    	{climb_up			drache01.stehen_schweben_oben_loop	}
    	{climb_down			drache01.stehen_schweben_unten_loop	}
    	{climb_right		drache01.stehen_schweben_loop		}
    	{climb_left			drache01.stehen_schweben_loop		}
    }

    set_class_anim stand_to_sit			drache01.stehen_zu_sitzen
    set_class_anim sit_to_stand         drache01.sitzen_zu_stehen

    set_class_anim scratch 				drache01.sitzen_kratzen
    set_class_anim gnaw 				drache01.sitzen_nagen
    set_class_anim lookaraundl 			drache01.sitzen_umschauen_l
    set_class_anim lookaraundr 			drache01.sitzen_umschauen_r
    set_class_anim wagtail 				drache01.sitzen_wedeln
    set_class_anim trapple 				drache01.stehen_trappeln

    set_class_anim si_fire_start		drache01.sitzen_speien_start
    set_class_anim si_fire_loop			drache01.sitzen_speien_loop
    set_class_anim si_fire_end			drache01.sitzen_speien_end

    set_class_anim st_fire_start		drache01.stehen_speien_start
    set_class_anim st_fire_loop			drache01.stehen_speien_loop
    set_class_anim st_fire_end			drache01.stehen_speien_end

	handle_event evt_timer0 {
	}

	handle_event evt_task_defend {
//		log "Drachenbaby defend"
		tasklist_clear this
		set attack_item [event_get this -subject1]
		set attack_behaviour "offensive"
		set approach 0
		fight_startfight
	}

	handle_event evt_task_attack {
//		log "Drachenbaby attack"
		tasklist_clear this
		set attack_item [event_get this -subject1]
		set attack_behaviour "offensive"
		set approach 0
		fight_startfight	}

	handle_event evt_burnstart {
		burnstart
	}

	handle_event evt_burnstop {
		burnend
	}


    handle_event evt_task_walk {
    	set evtpos [event_get this -pos1]
    	state_triggerfresh this task
    	set own_gnome [obj_query this " -pos \{$evtpos\} -class Zwerg -range 20 -owner own"]
		if {$own_gnome == 0} {
			log "Keine eigene Gnome dort gefunden, Drachenbaby wird dorthin nicht laufen"
			return
		}
    	tasklist_add this "st_hopp_pos \{$evtpos\}"
    	//tasklist_add this "st_walk_pos \{$evtpos\}"
    }

    handle_event evt_delitem {
    	if { [obj_valid $delitem] } {
    		if { [catch {call_method $delitem destroy}] } {
    			del $delitem
    		}
    	}
    	set delitem -1
    }

    handle_event evt_drbaby_attribupdate {
    	set cur_hitpoints [get_attrib this atr_Hitpoints]
		if {$cur_hitpoints < 1.0} {
			set_attrib this atr_Hitpoints [expr $cur_hitpoints + 0.01]
		}
	}

	method seq_idle {} {
		seq_idle
	}

	call scripts/misc/genericfight.tcl

   	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members
		call scripts/misc/genericfight.tcl

		set_anim this drache01.sitzen_warten_a 0 $ANIM_LOOP		;# set standard anim
		set_collision this 1
		set_attrib this hitpoints 1
		set_hoverable this 1
		set_selectable this 1
		state_triggerfresh this idle
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 1]
		set_objname this "Drachenbaby"

		set_attrib this atr_Hitpoints 4.0
		set_fogofwar this 14 8

		// Idle anims für Sequenzen (Statistenrollen)
		set seq_idle_anims [list]
		lappend seq_idle_anims {3 {sitzen_kratzen}}
		lappend seq_idle_anims {3 {sitzen_nagen}}
		lappend seq_idle_anims {2 {sitzen_umschauen_l}}
		lappend seq_idle_anims {2 {sitzen_umschauen_r}}
		lappend seq_idle_anims {4 {sitzen_warten_a}}
		lappend seq_idle_anims {4 {sitzen_warten_b}}
		lappend seq_idle_anims {4 {sitzen_warten_c}}
		lappend seq_idle_anims {1 {sitzen_wedeln}}
		lappend seq_idle_anims {1 {stehen_trappeln}}
		lappend seq_idle_anims {1 {stehen_hoppsen_start stehen_hoppsen_loop stehen_hoppsen_end}}
		lappend seq_idle_anims {w1 {stehen_schweben_loop}}
        call data/scripts/misc/seq_idle.tcl






		set act_pose "stand"
		set lastrnd 0
		set enemy -1
		set delitem -1
		set enmoldpos {0 0 0}
		set idlecount 0
		set lastanim "sitzen"

//		set close_range 2
//		set weapon_range 0
//		set current_weapon_item 0
//		set current_shield_item 0

        proc get_enemy_classes {} {
        	set classes "Troll Zwerg"
			catch {
				if  { [sm_get_event Drache_angegriffen]  ||  [sm_get_event Drachenmama_angegriffen] } {
					set classes "Troll Zwerg"
					set_owner this -1
					set_selectable this 0
					if {[get_selectedobject] == [get_ref this]} {
						set_selectedobject 0
					}
				} else {
					set classes "Troll"
				}
			}
			return $classes

        }

		proc get_random_of {str} {
        	set rlist [split $str ""]
        	set which [irandom [llength $rlist]]
        	return [lindex $rlist $which]
        }

        proc idle_anim {} {
    		set anim "idle[get_random_of abc]"
    		set_anim this $anim 0 2
        }

        proc st_walk_pos {pos} {
//        	log "Drachenbaby: st_walk_pos"
        	state_disable this
        	set pos [vector_fix $pos]
        	action this walk "-target \{$pos\} -animsets 0 -speedtype 1 -climbrot 0" {state_enable this}
        	return true
        }

    	proc run_pos_obj {pos obj {dist 1.8}} {
    		st_walk_pos $pos
    		return 1
    	}

        proc st_hopp_pos {pos} {
//        	log "Drachenbaby: st_hopp_pos"
        	state_disable this
        	set pos [vector_fix $pos]
        	action this walk "-target \{$pos\} -animsets 1 -speedtype 1 -climbrot 0" {state_enable this}
        	return true
        }

        proc st_walk_rnd {plength} {
//        	log "Drachenbaby: st_walk_rnd"
        	state_disable this
        	action this walk "-randompath $plength -animsets 0 -randomz 4 -speedtype 1 -climbrot 0" {state_enable this}
        	return true
        }

        proc st_rotate_toright {} {state_disable this;action this rotate 4.71 {state_enable this}}
        proc st_rotate_toleft {} {state_disable this;action this rotate 1.57 {state_enable this}}
        proc st_rotate_toback {} {state_disable this;action this rotate 3.14 {state_enable this}}
        proc st_rotate_tofront {} {state_disable this;action this rotate 0 {state_enable this}}

        proc st_rotate_towards_pos {pos} {
        	state_disable this
        	action this rotate [expr 1.57+[vector_angle [get_pos this] $pos]] {state_enable this}
        	return true
		}

        proc st_rotate_towards {obj} {
        	set pos [get_pos $obj]
        	state_disable this
        	action this rotate [expr 1.57+[vector_angle [get_pos this] $pos]] {state_enable this}
        	return true
		}

        proc play_anim {anim} {
        	state_disable this
        	action this anim $anim {state_enable this}
        	return true
        }

        proc change_pose {np} {
        	global act_pose
        	if {$np != $act_pose} {
        		set act_pose $np
        		switch $np {
        			"sit"  	{play_anim stand_to_sit}
        			"stand"	{play_anim sit_to_stand}
        			default {log "Drachenbaby WARNING: invalid pose: $np"}
        		}
        		return 1
        	}
        	return 0
        }

		proc si_scratch {} {
			play_anim scratch
		}

		proc si_gnaw {} {
			play_anim gnaw
		}

		proc si_lookaraund {} {
			play_anim "lookaraund[get_random_of rl]"
		}

		proc si_wagtail {} {
			play_anim wagtail
		}

		proc st_trapple {} {
			play_anim trapple
		}


        proc si_rand_idle {} {
    		set anim "idle[get_random_of abc]"
    		play_anim $anim
        }

        proc burnstart {} {
        	if { [check_enemy] } {
    			change_particlesource this 0 28 {0 0 0} [getfdir] 511 8 0 4
    		}
    		set_particlesource this 0 1
    		//Aufhören nach 5 sek
    		timer_event this evt_burnstop -repeat 1 -userid 0 -attime [expr [gettime] + 5]
        }

        proc burnend {} {
        	if { [check_enemy] } {
        		change_particlesource this 0 28 {0 0 0} [getfdir] 511 8 0 -1
				affect_enemies
        	}
    		set_particlesource this 0 0
        }

        proc burn_sd {} {
        	global act_pose enemy enmoldpos
        	set ap [string range $act_pose 0 1]
        	set ol [obj_query this -class {Stein Pilz Pilzstamm Pilzhut} -range 4 -limit 1]
        	if { $ol != 0 } {
        		set enemy [lindex $ol 0]
        		set enmoldpos [get_pos $enemy]
        		tasklist_add this "st_rotate_towards $enemy"
        		tasklist_add this "play_anim $ap\_fire_start"
        		tasklist_add this "play_anim $ap\_fire_loop;burnstart"
        		return 1
        	}
        	return 0
        }

       	proc getfdir {} {
       		global enemy
    		set ownpos [get_pos this]
    		set dpos [get_linkpos this 4]
    		set dpos [vector_add $ownpos $dpos]
            set epos [get_pos $enemy]
            set vc [vector_mul [get_velcomp $enemy] 8]
            set epos [vector_add $epos $vc]

            set dif [vector_sub $epos $dpos]
            set dir [vector_mul $dif 0.03125]
            #log "fd: $dir"
			return $dir
       	}

		proc check_enemy {} {
			global enemy
			if { ![obj_valid $enemy] } {
				return false
			}
			if { [get_attrib $enemy atr_Hitpoints] < 0.01 } {
				return false
			}
			return true
		}

		proc get_next_zwerg {} {
		    set all_own_gnomes [obj_query this "-class Zwerg -owner own"]
            if {$all_own_gnomes == 0} {return -1}
            //nechster Zwerg
            log "DRACHENBABY: geht zum zwerg [lindex $all_own_gnomes 0]: [get_objname [lindex $all_own_gnomes 0]]"
            return [lindex $all_own_gnomes 0]
		}

		proc walk_near_pos {pos range} {
			set thispos [get_pos this]
            set near_pos [get_place -center $pos -nearpos $thispos -mindist 2 -circle $range]
            if { [lindex $near_pos 0] > 0 } {
            	tasklist_add this "st_walk_pos \{$near_pos\}"
				return 1
			}
			log "Platz nicht gefunden: near_pos = $near_pos"
			return 0
		}

		proc walk_near_zwerg {pos range} {
			//set thispos [get_pos this]
            set near_pos [get_place -center $pos -mindist 2 -circle $range]
            if { [lindex $near_pos 0] > 0 } {
            	tasklist_add this "st_walk_pos \{$near_pos\}"
				return 1
			}
			log "Platz nicht gefunden: near_pos = $near_pos"
			return 0
		}

        proc affect_enemies {} {
        	global enmoldpos enemy delitem
  			set el [lnand 0 [obj_query this "-class {Zwerg Troll} -pos \{$enmoldpos\} -range 1.1"]]
 			//log "Objq: $el"
 			foreach item $el {
 				call_method $item burn
 			}
 			if { $enemy > 0 } {
     			if { ![obj_valid $enemy] } {
     				set enemy -1
     				return
     			}
 				#...
 				change_particlesource $enemy 4 27 {0 0 0} {0 0 0} 256 16 0 0 0 1
            	set_particlesource $enemy 4 1
				timer_event this evt_delitem -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 1.0]
				set delitem $enemy
 				set enemy -1
 			}
        }
    	timer_event this evt_drbaby_attribupdate -repeat -1 -interval 10

	}

	method idle_anim {} {
		idle_anim
	}

	state idle {
		global lastanim
//		log "idle: lastanim = $lastanim"
		call scripts/misc/animclassinit.tcl	// anim members

    	if {[get_gnomeposition this]} {
			if {$lastanim != "schweben"} {
				set lastanim "schweben"
				set_anim this drache01.stehen_schweben_loop 0 $ANIM_LOOP ;# set fliege anim
			}
		} else {
			if {$lastanim != "sitzen"} {
				set lastanim "sitzen"
				set_anim this drache01.sitzen_warten_a 0 $ANIM_LOOP		;# set standard anim
			}
		}

		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			catch { sm_set_event Drachenbaby_tot }
    		state_disable this
    		action this wait 1 "del this" "del this"
    		return
		}

        if {$idlecount > 5} {
			set idlecount 0
			set near_zwerg [obj_query this "-class Zwerg -range 5 -owner own"]
			if {$near_zwerg==0} {
				set next_zwerg [get_next_zwerg]
				if {$next_zwerg != -1} {
						//walk_near_pos [get_pos $next_zwerg] 6
						walk_near_zwerg [get_pos $next_zwerg] 6
						return
				}
			}
		}
		incr idlecount


		if { [tasklist_cnt this] > 0 } {
			state_trigger this task
			return
		}

		if {[get_gnomeposition this]} {
			return
		}

		set rnd [irandom 15]
		if { $lastrnd == $rnd } {
			incr rnd
		}
		if { [irandom 100] == 42 } { ;#
			if { [burn_sd] == 1 } {
				return
			}
		}
		switch $rnd {
			0	{tasklist_add this "st_walk_rnd [irandom 3 5]"}
			1	{tasklist_add this "si_gnaw"}
			2	{tasklist_add this "si_lookaraund"}
			3	{tasklist_add this "si_wagtail"}
			4	{tasklist_add this "st_trapple"}
			5	{tasklist_add this "si_scratch"}
			default {tasklist_add this "si_rand_idle"}
		}
		set lastrnd $rnd

	}

	state task {
		//log "Drachenbaby [get_objname this] - state task: [tasklist_cnt this]"
		if { [tasklist_cnt this] == 0 } {
			state_triggerfresh this idle
		} else {
			set command [tasklist_get this 0]
			if { [string range $command 0 2] == "st_" } {
				if { [change_pose "stand"] == 1 } {
					//log "Drb: chpose"
					return
				}
			} elseif { [string range $command 0 2] == "si_" } {
				if { [change_pose "sit"] == 1 } {
					//log "Drb: chpose"
					return
				}
			}
			tasklist_rem this 0
			//log "Drachenbaby - command: $command"
			eval $command
		}
	}

}
