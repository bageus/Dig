//#ifnot FULL
def_class Spinne none monster 0 {} {}
def_class Spinne_tot none dummy 0 {} {}
//#else
def_class Spinne none monster 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members
	
	class_fightdist 1.0
	
	def_event evt_timer0
	def_event evt_task_defend
	def_event evt_task_attack
	def_event evt_check_contact

	set_class_anim idlea spinne.stehen_warten_a
	set_class_anim idleb spinne.stehen_warten_b

	set_class_anim standstill spinne.stehen_warten_b
	set_class_anim climbstillani spinne.stehen_warten_a
	set_class_anim rotateleft spinne.stehen_drehen_l_90
	set_class_anim rotateright spinne.stehen_drehen_r_90
	set_class_anim turn180left spinne.stehen_drehen_l_90_schnell
	set_class_anim turn180right spinne.stehen_drehen_r_90_schnell

	set_class_animset 0 {
		{standard			spinne.standard				}
		{walk_start			spinne.stehen_gehen			}
		{walk_loop			spinne.stehen_gehen			}
		{walk_stop			spinne.stehen_gehen			}

		{turn_left_90		spinne.stehen_drehen_l_90	}
		{turn_right_90		spinne.stehen_drehen_r_90	}
		{turn_left_180		spinne.stehen_drehen_l_90_schnell	}
		{turn_right_180		spinne.stehen_drehen_r_90_schnell	}

		{climb_standard		spinne.stehen_warten_a		}
		{climb_up			spinne.klettern_auf			}
		{climb_down			spinne.klettern_ab			}
		{climb_right		spinne.klettern_rechts		}
		{climb_left			spinne.klettern_links		}

		{ladder_climb_up	spinne.klettern_auf			}
		{ladder_climb_down	spinne.klettern_ab			}
		{ground_to_wall		spinne.stehen_zu_klettern	}
		{wall_to_ground		spinne.klettern_zu_stehen	}
		{ladder_to_ground	spinne.klettern_zu_stehen	}
	}
	set_class_animset 1 {
		{standard			spinne.standard				}
		{walk_start			spinne.stehen_laufen		}
		{walk_loop			spinne.stehen_laufen		}
		{walk_stop			spinne.stehen_laufen		}

		{turn_left_90		spinne.stehen_drehen_l_90_schnell	}
		{turn_right_90		spinne.stehen_drehen_r_90_schnell	}
		{turn_left_180		spinne.stehen_umspringen_l_180		}
		{turn_right_180		spinne.stehen_umspringen_r_180		}

		{climb_standard		spinne.stehen_warten_a		}
		{climb_up			spinne.klettern_auf			}
		{climb_down			spinne.klettern_ab			}
		{climb_right		spinne.klettern_rechts		}
		{climb_left			spinne.klettern_links		}

		{ladder_climb_up	spinne.klettern_auf			}
		{ladder_climb_down	spinne.klettern_ab			}
		{ground_to_wall		spinne.stehen_zu_klettern	}
		{wall_to_ground		spinne.klettern_zu_stehen	}
		{ladder_to_ground	spinne.klettern_zu_stehen	}
	}
	set_class_animset 2 {
		{standard			spinne.standard				}
		{walk_start			spinne.stehen_springen_weit	}
		{walk_loop			spinne.stehen_springen_weit	}
		{walk_stop			spinne.stehen_springen_weit	}

		{turn_left_90		spinne.stehen_drehen_l_90_schnell	}
		{turn_right_90		spinne.stehen_drehen_r_90_schnell	}
		{turn_left_180		spinne.stehen_umspringen_l_180		}
		{turn_right_180		spinne.stehen_umspringen_r_180		}
	}
	set_class_anim stand_to_att		spinne.stehen_zu_drohen
	set_class_anim att_to_stand		spinne.drohen_zu_stehen
	set_class_anim kungfustillani 	spinne.drohen

	set_class_anim hangstill		spinne.haengen_warten
	set_class_anim hangtofall		spinne.haengen_zu_fallen
	set_class_anim falldown			spinne.fallen_loop
	set_class_anim falldownhit		spinne.fallen_zu_stehen
	set_class_anim falldowndead		spinne.fallen_zu_stehen

	set_class_anim spinround		spinne.stehen_einspinnen

	set_class_anim gettrapped		spinne.stehen_plattmach_tot
	set_class_anim trappedtostand	spinne.stehen_plattmach_reanim
	set_class_anim deada			spinne.drohen_get_tot_a
	set_class_anim deadb			spinne.tot_a_zu_tod_b
	set_class_anim deadtofight		spinne.tot_a_zu_drohen
	set_class_anim deadtostand		spinne.tot_a_zu_stehen

	handle_event evt_task_defend {
		//log "Spinne defend"
		set attack_item [event_get this -subject1]
		if {[vector_dist3d [get_pos this] [get_pos $attack_item]]>1.5} {return}
		tasklist_clear this
		end_deceiving $attack_item
	}

	handle_event evt_task_attack {
//		log "Drachenbaby attack"
//		tasklist_clear this
//		set attack_item [event_get this -subject1]
//		set attack_behaviour "offensive"
//		set approach 0
//		fight_startfight
	}

	handle_event evt_timer0 {
		log "EVTTIMER0 $spidertype"
		if {$spidertype=="explore"} {
			if {[initiate_exploring]} {log "SWITCH TO STILL";set spidertype "still"}
		}
		switch $spidertype {
			"explore" {
				log "SEARCH PLAYER"
				search_for_player
			}
			"hang" {
				set_anim this spinne.haengen_warten 0 $ANIM_STILL
				state_triggerfresh this "hanging"
			}
			"dead" {
				set_anim this spinne.tot_a_stellen 0 $ANIM_STILL
				state_triggerfresh this "deceiving"
			}
			"still" {state_triggerfresh this "waiting"}
			"stray" {state_triggerfresh this "straying"}
			"prison" {state_triggerfresh this "prisoned"}
		}
		// Ich bin bööööse !!!
		set_owner this -1
	}

	handle_event evt_check_contact {
		check_contact
	}

	call scripts/misc/genericfight.tcl
	call scripts/classes/characters/prisoned_monsters.tcl
	call scripts/misc/aggr_events.tcl



	method idle_anim {} {
		idle_anim
	}

	method is_escaping {} {
		log "I won't escape !!!!"
		return 0
	}

	method get_trapped {type} {
		if {$type=="petrify"} {
			// geht nicht :-) Spinne ist aus Kristall
			return
		}
		if {$type=="splat"} {
			set trap_time 3
			set trap_mode 0
			set trap_anim "gettrapped"
			set trap_reviveanim "trappedtostand"
		}
		state_triggerfresh this trapped
	}

	method Editor_Set_Info {ifo} {
		global info_string
		set info_string $ifo
		foreach entry $info_string {
			switch [lindex $entry 0] {
				"aggr"		{ set player_aggressivity [lindex $entry 1] }
				"aggrmax" {set aggr_max [lindex $entry 1]}
				"type" {set spidertype [lindex $entry 1]}
				"nummer" {set basenr [lindex $entry 1]}
				"fallon" {set fallon [lindex $entry 1]}
				"baserange" {set baserange [lindex $entry 1]}
				"explorerange" {set explorerange [lindex $entry 1]}
				"prisoned" {set prisoned 1;set spidertype "prison"}
			}
		}
	}

	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members
		call scripts/misc/genericfight.tcl
		call scripts/classes/characters/prisoned_monsters.tcl
		call scripts/misc/aggr_events.tcl


		set_anim this spinne.stehen_warten_b 0 $ANIM_STILL		;# set standard anim
		set_collision this 1
		set_attrib this hitpoints 1
		set_hoverable this 1
		set_selectable this 0
		timer_event this evt_timer0 -repeat 0 -interval 1 -userid 0 -attime [expr [gettime] + 1]

		set act_pose "stand"
		set lastrnd -1
		set scan_range 6
		set current_weapon_item 0
		set current_shield_item 0

		set info_string ""
		set spidertype "stray"
		set base ""
		set baserot 0.0
		set basenr -1
		set baserange 5
		set poslist ""
		set explorerange 40
		set fallon "approach"
		set seen_gnome 0
		set exploring_finished 0
		set gnome_under {"" ""}

		proc is_single {} {
			if {[is_storymgr]} {
				return 1
			} else {
				return 0
			}
		}

		proc initiate_exploring {} {
			global basenr baserange base baserot
			set baselist [obj_query this -class Info_Pos_Spinne -range $baserange]
			if {$baselist!=0} {
				if {$basenr==-1} {
					set b [lindex $baselist 0]
					set base [get_pos $b]
					set baserot [get_roty $b]
				} else {
					foreach b $baselist {
						if {[call_method $b get_info "nummer"]==$basenr} {
							set base [get_pos $b]
							set baserot [get_roty $b]
							break
						}
					}
				}
				if {$base==""} {
					set base [list $base 0]
					return 1
				}
				return 0
			}
			return 1
		}

		proc search_for_player {} {
			global base explorerange poslist
			if {[is_single]} {set players_list {0}} {set players_list {0 1 2 3 4 5 6 7}}
			set gnomelist [obj_query this -class Zwerg -owner $players_list -range 500]
			log "FOUND gl: $gnomelist"
			set placelist [obj_query this -type {production energy} -owner $players_list -range 500]
			log "FOUND pl: $placelist"
			if {$gnomelist==0} {set gnomelist ""}
			if {$placelist==0} {set placelist ""}
			set veclist [concat $gnomelist $placelist]
			if {$veclist==""} {return [get_pos this]}
			set center {0.0 0.0 0.0}
			set number 0
			foreach item $veclist {
				set center [vector_add $center [get_pos $item]]
				incr number
			}
			set center [vector_mul $center [expr {1.0/$number}]]
			set cpos [get_pos this]
			set explorerange [expr {8*int(abs($explorerange)*0.125)}]
			set poslist {}
			for {set i -$explorerange} {$i<=$explorerange} {incr i 8} {
				for {set j  -$explorerange} {$j<=$explorerange} {incr j 8} {
					set pos "$i $j 0"
					if {[vector_abs $pos]>=$explorerange+1} {continue}
					set pos [vector_add $base $pos]
					set point [get_place_long $pos 10 1 1]
					if {[lindex $point 0]<1} {continue}
					set dist [vector_dist $pos $center]
					lappend poslist [list $pos $dist]
				}
			}
			set poslist [lsort -real -index 1 $poslist]
			log "PL: ($poslist)"
			state_triggerfresh this exploring
			//log "minpos ($minpos) ($center)"
			tasklist_add this "explore_walk"
			timer_event this evt_check_contact -userid 1 -interval 1 -repeat -1 -attime [expr {[gettime]+1}]
		}

		proc explore_walk {} {
			global poslist
			state_disable this
			if {$poslist!=""} {
				set pos [vector_fix [lindex [lrem poslist 0] 0]]
			} else {
				set pos [get_pos this]
			}
			log "WALKING TO $pos"
			action this walk "-target \{$pos\} -animsets \{0 -1 0\} -speedtype 1 -useobjects 0" {
				state_enable this
				if {[get_walkresult this]!=4} {
					log "WALK FAILED"
					tasklist_add this "explore_walk"
				} else {
					log "WALK SUCCESSFULL"
					set exploring_finished 1
				}
			}
		}

		proc get_enemy_classes {} {
			set classes "Troll Zwerg Wuker Schwefelwuker Drachenbaby Kristallbrut Lavabrut"
			return $classes
		}

		proc get_random_of {str} {
			set which [irandom [string length $str]]
			return [string index $str $which]
		}

		proc idle_anim {} {
		//	set anim "idle[get_random_of a]"
			if {[get_gnomeposition this]} {
				set anim spinne.klettern_auf
				set_anim this $anim 0 0
			} else {
				set anim idleb
				set_anim this $anim 0 2
			}
		}

		proc st_walk_pos {pos} {
			state_disable this
			set pos [vector_fix $pos]
			action this walk "-target \{$pos\} -animsets \{0 -1 0\} -speedtype 1 -useobjects 0" {state_enable this}
			return true
		}
		proc st_run_pos {pos} {
			state_disable this
			set pos [vector_fix $pos]
			action this walk "-target \{$pos\} -animsets \{0 1 0\} -speedtype 2 -useobjects 0" {state_enable this}
			return true
		}
		proc jump_to {obj} {
			set pos [get_pos $obj]
			set mypos [get_pos this]
			set dist [vector_dist3d $mypos $pos]
			if {$dist<0.01} {return}
			set factor [expr {($dist-1.2)/$dist}]
			if {$factor<0.0} {return}
			set sub [vector_sub $pos $mypos]
			set sub [vector_mul $sub $factor]
			set pos [vector_add $mypos $sub]
		//	log "jumping to $pos ($mypos) $factor $dist"
			state_disable this
			action this walk "-target \{$pos\} -animsets \{2 2 2\} -dontstop -useobjects 0" {state_enable this}
		}

		proc run_pos_obj {pos obj {dist 1.8}} {
			state_disable this
		//	log "run_pos_obj now $pos $obj"
			set pos [vector_fix $pos]
			action this walk "-target \{$pos\} -objbreak \{$obj $dist\} -animsets \{1 1 1\} -useobjects 0" "state_enable this;run_pos_stop $obj"
			return true
		}

		proc st_walk_rnd {} {
			set pos [get_place -center [get_pos this] -rect -2 -4 2 4 -except this]
			if {[lindex $pos 0]>0} {
				st_walk_pos $pos
				return 1
			}
			return 0
		}

		proc st_rotate_toright {} {state_disable this;action this rotate 4.71 {state_enable this}}
		proc st_rotate_toleft {} {state_disable this;action this rotate 1.57 {state_enable this}}
		proc st_rotate_toback {} {state_disable this;action this rotate 3.14 {state_enable this}}
		proc st_rotate_tofront {} {state_disable this;action this rotate 0 {state_enable this}}

		proc st_rotate_toangle {angle} {state_disable this;action this rotate $angle {state_enable this}}

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

		proc fall_down {} {
			state_disable this
			action this anim hangtofall {action this fall falldown {state_enable this}}
			state_trigger this straying
		}

		proc play_anim {anim} {
			state_disable this
			action this anim $anim {state_enable this}
			return true
		}

		proc change_pose {np} {
			global act_pose
			//log "changing pose to $np"
			if {$np != $act_pose} {
				set act_pose $np
				switch $np {
					"att"  	{play_anim stand_to_att}
					"stand"	{play_anim att_to_stand}
					default {log "Spinne WARNING: invalid pose: $np"}
				}
				return 1
			}
			return 0
		}

		proc st_rand_idle {} {
			set anim "idle[get_random_of a]"
			play_anim $anim
		}

		proc end_deceiving {item} {
			global attack_behaviour approach
			set mypos [get_pos this]
			set itempos [get_pos $item]
			set angle [vector_angle $mypos $itempos]
			fincr angle 1.57
			//log "($mypos) ($itempos) $angle"
			if {$angle>1.0&&$angle<5.3||$angle<-1.0} {
				play_anim deadtostand
			} else {
				play_anim deadtofight
			}
			set attack_behaviour "offensive"
			set approach 0
			tasklist_add this "fight_startfight"
			state_triggerfresh this task
		}

		proc find_enemy {} {
			global scan_range attack_behaviour attack_item look_dir new_fight_pos approach
			set mindist 1000
			set attack_item 0
			set fzwerg_list [obj_query this -range $scan_range -class [get_enemy_classes]]
			if { $fzwerg_list == 0 } {
				return 0
			}
			foreach fzwerg $fzwerg_list {
				set ownpos [get_pos this]
				set enemypos [get_pos $fzwerg]
				set dist [vector_dist $ownpos $enemypos]
				//if { $dist < $mindist } {
				//	set mindist $dist					;# nahen zwerg suchen für schnüffeln
				//}
				set attack_behaviour "offensive"
				set attack_item $fzwerg
				//if { $dist > $scan_range } {
				//	continue
				//}
				//if { [state_get $attack_item] == "fight_dispatch"  } {
				//	continue
				//}

				log "[get_objname this]: [get_objname $attack_item]has [get_attrib $attack_item atr_Hitpoints] HP"
				if { [get_attrib $attack_item atr_Hitpoints] < 0.01 } {
					continue
				}

				if { [get_attack_pos this $attack_item] == 0 } { continue }
				set approach 1
				fight_startfight
				return 1
			}
			//if { $mindist < $sniff_range } {
			//	return 2
			//}
			return 0
		}

		proc check_contact {} {
			global base exploring_finished
			if {$exploring_finished} {
				if {[obj_query this -class [get_enemy_classes] -boundingbox {-1.5 -0.5 -5 1.5 0.5 5} -limit 1]} {
					state_triggerfresh this straying
				}
			} else {
				if {[return_to_base]} {
					set exploring_finished 1
					return
				}
			}
		}

		proc return_to_base {} {
			global base baserot
			set view [get_view]
			set viewpos "[lrange $view 0 1] 14"
			if {[vector_dist $viewpos [get_pos this]]<6*[lindex $view 2]} {
				set scan_range 5
			} else {
				set scan_range 3
			}
			set enemy [obj_query this -class [get_enemy_classes] -range $scan_range -limit 1]
			if {$enemy} {
				init_aggr_contact
				log "ENEMY: $enemy"
				st_run_pos $base
				tasklist_add this "timer_unset this 1"
				tasklist_add this "st_rotate_toangle $baserot"
				state_trigger this waiting
				timer_event this evt_check_contact -userid 1 -interval 1 -repeat 6 -attime [expr {[gettime]+1}]
				return 1
			}
			return 0
		}

		proc see_enemy {} {
			global scan_range
			set enemylist [obj_query this -class [get_enemy_classes] -range 12]
			if {$enemylist==0} {return 0}
			set mypos [get_pos this]
			set myy [lindex $mypos 1]
			foreach enemy $enemylist {
				if {abs($myy-[get_posy $enemy])>3} {continue}
				set dist [vector_dist3d $mypos [get_pos $enemy]]
			//	log "seeing $enemy at dist of $dist"
				if {$dist>6} {continue}
				if {$dist>3} {
					jump_to $enemy
				} else {
					run_pos_obj [get_pos $enemy] $enemy 1.0
				}
				tasklist_clear this
				state_trigger this straying
				return 1
			}
			return 0
		}

		proc watch_passers {} {
			global fallon gnome_under baserange
			set closerange [expr {$baserange*0.4}]
			switch $fallon {
				"under" {
					set glist [obj_query this -class [get_enemy_classes] -boundingbox "-$closerange 0 -12 $closerange $baserange 12"]
					if {$glist==0} {return 0}
					fall_down
					return 1
				}
				"approach" {
					set glist [obj_query this -class [get_enemy_classes] -boundingbox "-$baserange 0 -12 $baserange $baserange 12"]
					if {$glist==0} {return 0}
					fall_down
					return 1
				}
				"passed" {
					set leftgnomes [obj_query this -class [get_enemy_classes] -boundingbox "-$baserange 0 -12 -$closerange $baserange 12"]
					set rightgnomes [obj_query this -class [get_enemy_classes] -boundingbox "$closerange 0 -12 $baserange $baserange 12"]
					if {$leftgnomes==0} {set leftgnomes ""}
					if {$rightgnomes==0} {set rightgnomes ""}
					set passers [land $leftgnomes [lindex $gnome_under 1]]
					eval "lappend passers [land $rightgnomes [lindex $gnome_under 0]]"
					if {$passers!=""} {
						//log "Spinne falls ($leftgnomes) ($rightgnomes) $gnome_under"
						fall_down;return 1
					}
					set leftgnomes [lor $leftgnomes [lindex $gnome_under 0]]
					set rightgnomes [lor $rightgnomes [lindex $gnome_under 1]]
					set gnome_under [list $leftgnomes $rightgnomes]
					return 0
				}
			}
		}

		proc destroy_myself {} {
			sel /obj
			new Spinne_tot "" [get_pos this] [get_rot this]
			state_disable this
			action this wait 1 {del this} {del this}
		}

		proc after_fight {} {
			set mattr [get_attrib this atr_Hitpoints]
			if { $mattr < 0.8 } {
				if { $mattr < 0.01 } {
					//log "[get_objname this] is dying (idle)"
					destroy_myself
					return 1
				} else {
					set slist [obj_query this -class Spinne -range 7]
					set glist [obj_query this -class Zwerg -range 8]
					if {$slist==0||$glist==0} {return 0}
					set sattr 0.0
					set gattr 0.0
					foreach s $slist {
						if {[state_get $s]=="fight_dispatch"} {fincr sattr [get_attrib $s atr_Hitpoints]}
					}
					foreach g $glist {
						if {[state_get $g]=="fight_dispatch"} {fincr gattr [get_attrib $g atr_Hitpoints]}
					}
					if {$sattr==0.0||$gattr==0.0} {return 0}
					if {$sattr<$mattr&&$gattr>$sattr*1.5} {
						play_anim stand_to_att
						tasklist_add this "play_anim deada"
						state_trigger this deceiving
						//log "[get_objname this] is deceiving"
						return 1
					}
				}
			}
			return 0
		}

	}

	state idle {

		if { [after_fight] } { return }

		if {$prisoned} {
			state_triggerfresh this prisoned
			return
		}

		state_trigger this straying
		return

		if { [find_enemy] } { return }

		if { [tasklist_cnt this] > 0 } {
			state_trigger this task
			return
		}
		set rnd [irandom 15]
		if { $lastrnd == $rnd } {
			incr rnd
		}
		switch $rnd {
			0	{tasklist_add this "st_walk_rnd [irandom 3 5]"}
			default {tasklist_add this "st_rand_idle"}
		}
		set lastrnd $rnd

	}


	state trapped {
		if {$trap_mode==0} {
			set trap_mode 1
			play_anim $trap_anim
			return
		}
		if {$trap_mode==1} {
			set trap_mode 2
			state_disable this
			if {[get_attrib this atr_Hitpoints] >= 0.01} {
				action this wait $trap_time {play_anim $trap_reviveanim}
			} else {
				action this wait $trap_time {
					sel /obj
					set ore [new Kristallerz]
					set_pos $ore [get_pos this]
					set_physic $ore 1
					set_anim $ore spinne.panzer_b 0 0
					del this
				} {
					sel /obj
					set ore [new Kristallerz]
					set_pos $ore [get_pos this]
					set_physic $ore 1
					set_anim $ore spinne.panzer_b 0 0
					del this
				}
			}
			return
		}
		if {$trap_mode==2} {
			state_trigger this idle
			return
		}
	}


	state task {
		//log "Spinne [get_objname this] - state task: [tasklist_cnt this]"
		if { [tasklist_cnt this] == 0 } {
			state_trigger this idle
		} else {
			set command [tasklist_get this 0]
			if { [string range $command 0 2] == "st_" } {
				if { [change_pose "stand"] == 1 } {
					//log "Spi: chpose"
					return
				}
			} elseif { [string range $command 0 2] == "at_" } {
				if { [change_pose "att"] == 1 } {
					//log "Spi: chpose"
					return
				}
			}
			tasklist_rem this 0
			//log "Spinne - command: $command"
			eval $command
		}
	}

	state straying {

		if { [find_enemy] } { return }
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
			eval $command
			return
		}
		if {rand()<0.3} {check_for_player_contact}
		set rnd [irandom 8]
		if { $lastrnd == $rnd } {
			incr rnd
		}
		switch $rnd {
			0	{ if {[st_walk_rnd]} {return} }
			1	{
				tasklist_add this "play_anim spinround"
				tasklist_add this "play_anim spinround"
				tasklist_add this "play_anim spinround"
				return
			}
		}
		idle_anim
		state_disable this
		action this wait 1 {state_enable this}
		set lastrnd $rnd

	}

	state exploring {
		log "STATE EXPLORING"
		if { [return_to_base] } { log "RETURN"; return }
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
			eval $command
			return
		}
		if {[get_gnomeposition this]==0&&rand()<0.1} {
			if {[st_walk_rnd]} {return}
		}
		idle_anim
		state_disable this
		action this wait 3 {state_enable this}
	}

	state hanging {
		if { [watch_passers] } { return }
		if {rand()<0.2} {check_for_player_contact}
		state_disable this
		action this wait 2 {state_enable this}
	}

	state waiting {
		//catch {timer_unset this 1}
		if { [see_enemy] } { return }
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
			eval $command
			return
		}
		if {rand()<0.2} {check_for_player_contact}
		set_anim this spinne.stehen_warten_a 0 $ANIM_STILL
		state_disable this
		action this wait 2 {state_enable this}
	}

	state deceiving {
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
			eval $command
			return
		}
		if {[set attack_item [obj_query this -class [get_enemy_classes] -boundingbox {-1.5 -0.5 -3 1.5 0.5 3} -limit 1]]!=0} {
			end_deceiving $attack_item
			return
		}
		if {rand()<0.2} {check_for_player_contact}
		set_anim this spinne.tot_a_stellen 0 $ANIM_STILL
		state_disable this
		action this wait 2 {state_enable this}
	}
}

def_class Spinne_tot none material 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/aggr_events.tcl

	set_class_anim	deadb	spinne.tot_a_zu_tod_b

	handle_event evt_timer0 {
		init
	}

	method release_content {} {
		destroy
	}

	method Editor_Set_Info {ifo} {
		set info_string $ifo
		foreach entry $ifo {
			switch [lindex $entry 0] {
				"init" {set initial [lindex $entry 1]}
				"aggr" {set player_aggressivity [lindex $entry 1]}
				"aggrmax" {set aggr_max [lindex $entry 1]}
			}
		}
	}

	obj_init {
		call scripts/misc/animclassinit.tcl
		call scripts/misc/aggr_events.tcl
		set_anim this spinne.tot_a_stellen 0 $ANIM_STILL

		set info_string ""
		set initial "random"

		timer_event this evt_timer0 -repeat 0 -interval 1 -userid 0 -attime [expr [gettime] + 1]

		proc init {} {
			global initial
			if {$initial=="random"} {
				if {rand()<0.3} {
					set initial "erz"
				} else {
					set initial "spinne"
				}
			}
			if {$initial=="erz"} {
				destroy
			} else {
			}
		}

		proc destroy {} {
			action this anim deadb {
				sel /obj
				set ore [new Kristallerz "" [get_pos this] [get_rot this]]
				set_physic $ore 1
				set_anim $ore spinne.panzer_b 0 0
				del this
			}
		}
	}
}

//#endif

