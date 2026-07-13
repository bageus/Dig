//# IFNOT FULL
def_class Kristallbrut none dummy 0 {} {}
def_class Lavabrut none dummy 0 {} {}
//# ENDIF
//# STOPIFNOT FULL
foreach brutclass {Kristallbrut Lavabrut} {
def_class $brutclass none monster 0 {} {

	call scripts/misc/animclassinit.tcl	// anim members

	class_fightdist 1.0

	def_event evt_timer0
	def_event evt_task_defend
	def_event evt_task_attack

	set_class_anim	standstill		brut.warten_a
	set_class_anim	rotateleft		brut.umdrehen_l_90
	set_class_anim	rotateright		brut.umdrehen_r_90
	set_class_anim	turn180left		brut.umdrehen_l_180
	set_class_anim	turn180right	brut.umdrehen_r_180

	set_class_anim	idlea			brut.warten_a
	set_class_anim	idleb			brut.warten_b
	set_class_anim	idlec			brut.warten_c

	set_class_anim	roara			brut.bruellen_a
	set_class_anim	roarb			brut.bruellen_b
	set_class_anim	angrya			brut.stampfen
	set_class_anim	restless		brut.trappeln
	set_class_anim	angryb			brut.trappeln_schnell
	set_class_anim	jump			brut.sprung
	set_class_anim	peestart		brut.pinkeln_start
	set_class_anim	peeloop			brut.pinkeln_loop
	set_class_anim	peestop			brut.pinkeln_end

	set_class_anim	saltoforward	brut.salto_vor
	set_class_anim	saltobackward	brut.salto_zurueck

	set_class_anim	standstillsalto		brut.warten_a
	set_class_anim	rotateleftsalto		brut.salto_vor
	set_class_anim	rotaterightsalto	brut.salto_vor
	set_class_anim	turn180leftsalto	brut.salto_vor
	set_class_anim	turn180rightsalto	brut.salto_vor
	set_class_anim	standstilljump		brut.warten_a
	set_class_anim	rotateleftjump		brut.sprung
	set_class_anim	rotaterightjump		brut.sprung
	set_class_anim	turn180leftjump		brut.sprung
	set_class_anim	turn180rightjump	brut.sprung
	set_class_anim	standstillbite		brut.warten_a
	set_class_anim	rotateleftbite		brut.sprung_beissen
	set_class_anim	rotaterightbite		brut.sprung_beissen
	set_class_anim	turn180leftbite		brut.sprung_beissen
	set_class_anim	turn180rightbite	brut.sprung_beissen

	set_class_anim	kungfustillani	brut.warten_a
	set_class_anim	climbstillani	brut.warten_a
	set_class_anim	jumpbite		brut.sprung_beissen
	set_class_anim	bitea			brut.beissen_a
	set_class_anim	biteb			brut.beissen_b
	set_class_anim	bitec			brut.beissen_c
	set_class_anim	hornstrike		brut.hornstoss
	set_class_anim	kick			brut.fusstritt
	set_class_anim	falldown		brut.bruellen_a
	set_class_anim	falldownhit		brut.hinten_get_leicht
	set_class_anim	falldowndead	brut.hinten_get_tot

	set_class_anim	gettrapped		brut.plattmach_tot
	set_class_anim	trappedtostand	brut.plattmach_reanim
	set_class_anim	getstoned		brut.versteinert_tot
	set_class_anim	stonedtostand	brut.versteinert_reanim

	set_class_animset 0 {
		{standard			brut.warten_a			}
		{walk_start			brut.laufen_start		}
		{walk_loop			brut.laufen_loop		}
		{walk_stop			brut.laufen_end			}

		{turn_left_90		brut.umdrehen_l_90		}
		{turn_right_90		brut.umdrehen_r_90		}
		{turn_left_180		brut.umdrehen_l_180		}
		{turn_right_180		brut.umdrehen_r_180		}
	}

	set_class_animset 1 {
		{standard			brut.warten_a			}
		{walk_start			brut.rollen_start		}
		{walk_loop			brut.rollen_loop		}
		{walk_stop			brut.rollen_end			}

		{turn_left_90		brut.umdrehen_l_90		}
		{turn_right_90		brut.umdrehen_r_90		}
		{turn_left_180		brut.umdrehen_l_180		}
		{turn_right_180		brut.umdrehen_r_180		}
	}

	set_class_animset 2 {
		{standard			brut.warten_a			}
		{walk_start			brut.schleichen_start	}
		{walk_loop			brut.schleichen_loop	}
		{walk_stop			brut.schleichen_end		}

		{turn_left_90		brut.umdrehen_l_90		}
		{turn_right_90		brut.umdrehen_r_90		}
		{turn_left_180		brut.umdrehen_l_180		}
		{turn_right_180		brut.umdrehen_r_180		}
	}

	handle_event evt_task_defend {
		//log "Brut defend"
		set attack_item [event_get this -subject1]
		if {[vector_dist3d [get_pos this] [get_pos $attack_item]]>1.5} {return}
		tasklist_clear this
		set attack_behaviour "offensive"
		set approach 0
		fight_startfight
	}

	handle_event evt_task_attack {
	}

	handle_event evt_timer0 {
	}

	call scripts/misc/genericfight.tcl
	call scripts/classes/characters/prisoned_monsters.tcl
	call scripts/misc/aggr_events.tcl

	method is_escaping {} {
		log "I won't escape !!!!"
		return 0
	}

	method get_trapped {type} {
		set trap_type $type
		if {$type=="petrify"} {
			set trap_time 30
			set trap_mode 0
			set trap_anim "getstoned"
			set trap_reviveanim "stonedtostand"
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
		set info_string $ifo
		foreach entry $ifo {
			switch [lindex $entry 0] {
				"aggr"		{ set player_aggressivity [lindex $entry 1] }
				"prisoned"	{ set prisoned [lindex $entry 1] }
				"aggrmax" {set aggr_max [lindex $entry 1]}
			}
		}
	}

	method implant_enemy {enemy} {
		lappend remote_enemies $enemy
	}

	method do {cmd} {eval $cmd}

	state idle {
		if {[get_attrib this atr_Hitpoints]<0.01} {
			destroy
		}

		if {$prisoned} {
			state_triggerfresh this prisoned
			return
		}

		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
			eval $command
			return
		}
		if { [ find_enemy_near ] } { return }
		if { [ find_enemy_far ] } { return }

		switch [irandom 8] {
		"0" {tasklist_add this "walk_rnd [irandom 2 6]"}
		"1" {tasklist_add this "play_anim restless"}
		"2" {tasklist_add this "play_anim idlea"}
		"3" {tasklist_add this "play_anim idleb"}
		"4" {tasklist_add this "play_anim idlec"}
		"5" {tasklist_add this "play_anim roara"}
		"6" {tasklist_add this "play_anim roarb"}
		"7" {if {![irandom 3]} {pinkeln}}
		}

		//state_disable this
		//action this wait 2 {state_enable this}
	}


	state trapped {
		if {$trap_mode==0} {
			set trap_mode 1
			play_anim $trap_anim
			return
		}
		if {$trap_mode==1} {
			set trap_mode 2
			if {$trap_type=="petrify"} {
				set_physic this 1
				set_textureanimation this 0 2
				set_textureanimation this 1 2
			}
			state_disable this
			action this wait $trap_time {state_enable this}
			return
		}
		if {$trap_mode==2} {
			set_physic this 0
			if {$myclass=="Lavabrut"} {
				set_texturevariation this 0 1
				set_texturevariation this 1 1
			} else {
				set_texturevariation this 0 0
				set_texturevariation this 1 0
			}
			if {[get_attrib this atr_Hitpoints] >= 0.01} {
				play_anim $trap_reviveanim
				state_trigger this idle
			} else {
				destroy
			}
			return
		}
	}

	state_leave trapped {
		set_physic this 0
	}

	obj_init {

		call scripts/misc/animclassinit.tcl	// anim members
		call scripts/misc/genericfight.tcl
		call scripts/classes/characters/prisoned_monsters.tcl
		call scripts/misc/aggr_events.tcl

		set_anim this brut.warten_a 0 $ANIM_STILL
		set_climbability this 0
		set_collision this 1
		set_attrib this hitpoints 1
		set_hoverable this 1
		set_selectable this 0
		state_triggerfresh this idle
		set myref [get_ref this]

		set myclass [get_objclass this]
		if {$myclass=="Lavabrut"} {
			set is_lava 1
			set walk_animset 0
			set run_animset 1
			set scan_range 25
			set_texturevariation this 0 1
			set_texturevariation this 1 1
		} else {
			set is_lava 0
			set walk_animset 2
			set run_animset 0
			set scan_range 10
		}
		set close_range 2
		set weapon_range 0
		set current_weapon_item 0
		set current_shield_item 0
		set current_enemy 0
		set current_enemydist 0
		set remote_enemies {}
		set info_string {{prisoned 0}}

		// Kampfprocs

		proc get_enemy_classes {} {
			return {Zwerg Drachenbaby Wuker Schwefelwuker Troll Spinne}
		}

		if {$is_lava} {
			proc fight_trick {} {
				global attack_item myref
				if {[tasklist_cnt this]>0} {
					set cmd [tasklist_get this 0]
					tasklist_rem this 0
					eval $cmd
					return 1
				}
			//	if {rand()<0.5} {return 0}
				set crot [get_roty this]
				set enemypos [get_pos $attack_item]
				set ownpos [get_pos this]
				set angle [get_anglexz $ownpos $enemypos]
				set diff [expr {$angle-$crot}]
				if {$diff<-3.14} {fincr diff 6.28} {if {$diff>3.14} {fincr diff -6.28}}
				if {abs($diff)>0.7} {log "fighttrick: wrong angle $diff";return 0}
				set frontpos [vector_add $ownpos [get_vectorxz $crot 2.0]]
				set frontplace [get_place -center $frontpos -circle 1.5]
				if {[lindex $frontplace 0]<1} {log "fighttrick: no space behind enemy ($ownpos) $crot ($frontpos)";return 0}
				set behindpos [vector_add $ownpos [get_vectorxz [expr {$crot+3.14}] 2.0]]
				set blist [obj_query this -class Lavabrut -pos $behindpos -boundingbox {-1.5 -0.5 -3 1.5 0.5 3}]
				if {$blist==0} {log "fighttrick: no friend behind me ($ownpos) $crot ($behindpos)";return 0}
				placelock_set $frontplace 2 $myref
				spin_jump $frontplace
				return 1
			}
		}

		proc find_enemy_near {} {
			global close_range attack_item attack_behaviour approach
			set drange [expr {$close_range * 2.0 }]
			set elist [obj_query this -class [get_enemy_classes] -boundingbox "-$close_range -1 -$drange $close_range 1 $drange"]
			if {$elist==0} {return 0}
			init_aggr_contact
			set crot [get_roty this]
			set ownpos [get_pos this]
			set enemylist {}
			foreach enemy $elist {
				if { [get_attrib $enemy atr_Hitpoints] < 0.01 } {continue}
				set enemypos [get_pos $enemy]
				set dist [vector_dist3d $ownpos $enemypos]
				if {$dist>$close_range} {continue}
				set angle [get_anglexz $ownpos $enemypos]
				set diff [expr {$angle-$crot}]
				if {$diff<-3.14} {fincr diff 6.28} {if {$diff>3.14} {fincr diff -6.28}}
				set diff [expr {abs($diff)}]
				set reach [expr {$diff*[hmax $diff 0.5]}]
				lappend enemylist [list $enemy $dist $reach]
			}
			if {$enemylist==""} {return 0}
			foreach enemy [lsort -index 2 -real $enemylist] {
				set attack_item [lindex $enemy 0]
				set attack_behaviour "offensive"
				log "[get_objname this]: [get_objname $attack_item] has [get_attrib $attack_item atr_Hitpoints] HP"
				if { [get_attack_pos this $attack_item] == 0 } { continue }
				set approach 1
				fight_startfight
				return 1
			}
			return 0
		}

		proc find_enemy_far {} {
			global scan_range attack_item current_enemy current_enemydist remote_enemies
			set elist [obj_query this -class [get_enemy_classes] -boundingbox "-$scan_range -3 -10 $scan_range 2 10"]
			set rlist {}
			foreach r $remote_enemies {
				if {[obj_valid $r]&&[dist_between this $r]<$scan_range*2} {
					lappend rlist $r
				} else {
					set remote_enemies [lnand $r $remote_enemies]
				}
			}
			if {$elist==0} {set elist {}}
			set elist [lor $elist $rlist]
			if {$elist==""} {return 0}
			set ownpos [get_pos this]
			set enemylist {}
			foreach enemy $elist {
				if { [get_attrib $enemy atr_Hitpoints] < 0.01 } {continue}
				set enemypos [get_pos $enemy]
				set dist [vector_dist3d $ownpos $enemypos]
				lappend enemylist [list $enemy $dist]
			}
			if {$enemylist==""} {log "all enemies too weak";return 0}
			set enemy [lindex [lsort -index 1 -real $enemylist] 0]
			set current_enemy [lindex $enemy 0]
			set current_enemydist 0
			call_others
			tasklist_add this "try_reach_enemy"
			return 1
		}

		proc call_others {} {
			global myclass current_enemy
			set blist [obj_query this -class $myclass -range 10]
			if {$blist!=0} {
				foreach b $blist {
					call_method $b implant_enemy $current_enemy
				}
			}
			roar
		}

		proc try_reach_enemy {} {
			global current_enemy scan_range current_enemydist myref
			global run_animset walk_animset is_lava
			//log "trying to reach enemy $current_enemy ($current_enemydist)"
			if {$current_enemy} {
				if {[obj_valid $current_enemy]} {
					if {[set dist [dist_between this $current_enemy]]<$scan_range*2} {
						set epos [get_pos $current_enemy]
						set mpos [get_pos this]
						set ex [lindex $epos 0]
						set mx [lindex $mpos 0]
						set x [expr {$mx-$ex}]
						if {$x<0} {
							set xn [expr {$x+1}]
							set xp [expr {-1-$current_enemydist}]
						} else {
							set xn [expr {1+$current_enemydist}]
							set xp [expr {$x-1}]
						}
						set place [get_place -center $epos -rect $xn -6 $xp 6 -nearpos [get_pos this] -placelockidexcept $myref]
						//log "reachtry $dist $ex $mx $x ($place) $xn $xp"
						if {[lindex $place 0]>0} {
							state_disable this
							placelock_set $place $dist $myref
							action this walk "-target \{$place\} -animsets \{$walk_animset $run_animset $walk_animset\} -speedtype 3 -canclimb 0 -useobjects 0" {
								state_enable this
								placelock_rem $myref
								if {[get_walkresult this]!=4} {
									fincr current_enemydist 1.0
									//log "repeat try (walkfail)"
									tasklist_add this "try_reach_enemy"
								}
							} {
								placelock_rem [get_ref this]
							}
						} elseif {$is_lava&&$dist-$current_enemydist<2.0&&[check_hmap $x]} {
							if {$x>0} {set xn -3.5;set xp -1} else {set xn 1;set xp 3.5}
							set place [get_place -center $mpos -rect $xn -4 $xp 4 -placelockidexcept $myref]
							log "place for jumping: $place"
							if {[lindex $place 0]<1} {log "no approachplace found for jumping ($xn -4 $xp 4)";return}
							placelock_set $place 2 $myref
							salto_forward $place
						} else {
							if {$dist-$current_enemydist>0.4} {
								//log "repeat try (placefail) $dist $x"
								tasklist_add this "try_reach_enemy"
								fincr current_enemydist 1.0
							} else {
								log "no approachplace found for walking ($xn -4 $xp 4)"
							}
						}
						return
					}
				}
			}
			set current_enemy 0
		}

		proc check_hmap {dir} {
			set mx [get_posx this]
			set my [get_posy this]
			if {$dir>0} {
				set xp [expr {int(ceil($mx))}]
				set xn [expr {$xp-2}]
			} else {
				set xn [expr {int(ceil($mx))}]
				set xp [expr {$xn+2}]
			}
			set yp [expr {int(ceil($my))+1}]
			set yn [expr {$yp-3}]
			for {set i $xn} {$i<$xp} {incr i} {
				for {set j $yn} {$j<$yp} {incr j} {
					if {[get_hmap $i $j]>12.5} {log "checking hmap $dir (0), $xn $xp $yn $yp";return 0}
				}
			}
			log "checking hmap $dir (1), $xn $xp $yn $yp";
			return 1
		}

		proc destroy {} {
			action this wait 2 {destruct this;del this} {destruct this;del this}
		}

		// Bewegung

		proc play_anim {anim} {
			state_disable this
			action this anim $anim {state_enable this}
			return true
		}

		proc pinkeln {} {
			tasklist_add this "play_anim peestart"
			tasklist_add this "play_anim peeloop"
			tasklist_add this "play_anim peestop"
		}

		proc rotate_toright {} {state_disable this;action this rotate 4.71 {state_enable this}}
		proc rotate_toleft {} {state_disable this;action this rotate 1.57 {state_enable this}}
		proc rotate_toback {} {state_disable this;action this rotate 3.14 {state_enable this}}
		proc rotate_tofront {} {state_disable this;action this rotate 0 {state_enable this}}

		proc rotate_toangle {angle} {state_disable this;action this rotate $angle {state_enable this}}

		proc walk_pos {pos} {
			global walk_animset
			state_disable this
			set pos [vector_fix $pos]
			action this walk "-target \{$pos\} -animsets \{$walk_animset -1 $walk_animset\} -speedtype 1 -canclimb 0 -useobjects 0" {state_enable this}
			return true
		}

		proc run_pos_obj {pos obj {dist 1.2}} {
			global walk_animset
			state_disable this
			log "run_pos_obj now $pos $obj"
			set pos [vector_fix $pos]
			action this walk "-target \{$pos\} -objbreak \{$obj $dist\} -animsets \{$walk_animset $walk_animset $walk_animset\} -canclimb 0 -useobjects 0" "state_enable this;run_pos_stop $obj"
			return true
		}

		proc calc_vel {angle dist anim} {
			switch $anim {
				"salto" {set animlen 1.3}
				default {set animlen 1.3}
			}
			set fract [expr {1.0/$animlen}]
			return [list [expr {-sin($angle)*$dist*$fract}] 0 [expr {2*cos($angle)*$dist*$fract}]]
		}

		proc set_dist_and_angle {pos cpos crot} {
			if {[llength $pos]>1} {
				set angle [get_anglexz $cpos $pos]
				set diff [expr {$angle-$crot}]
				if {$diff<-3.14} {fincr diff 6.28} {if {$diff>3.14} {fincr diff -6.28}}
				if {abs($diff)>0.3} {
					log "correcting angle"
					rotate_toangle $angle
				}
				set dist [vector_dist3d $cpos $pos]
			} else {
				set dist $pos
				set angle $crot
			}
			return "set dist $dist;set angle $angle"
		}

		proc salto_forward {pos {anim salto}} {
			set crot [get_roty this]
			set cpos [get_pos this]
			eval [set_dist_and_angle $pos $cpos $crot]
			log "jumping to ($pos), $crot $dist $angle ($cpos)"
			tasklist_add this "exec_salto \{[calc_vel $angle $dist $anim]\}"
		}

		proc exec_salto {vel} {
			set_vel this $vel
			state_disable this
			action this anim saltoforward {
				state_enable this
				set_vel this {0 0 0}
			} {
				set_vel this {0 0 0}
			}
		}

		proc salto_back {} {
			global is_tricking
			set_vel this {1.5 0 0}
			state_disable this
			set is_tricking 1
			action this anim saltobackward {
				state_enable this
				set_vel this {0 0 0}
				set is_tricking 0
			} {
				set_vel this {0 0 0}
				set is_tricking 0
			}
		}

		proc spin_jump {pos {anim salto}} {
			set crot [get_roty this]
			set cpos [get_pos this]
			eval [set_dist_and_angle $pos $cpos $crot]
			tasklist_add this "exec_spin \{[calc_vel $angle $dist $anim]\} $angle"
		}

		proc exec_spin {vel angle} {
			global is_tricking ANIM_ONCE
			log "starting spin_jump"
			set is_tricking 1
			state_disable this
			set_anim this brut.salto_vor 0 $ANIM_ONCE
			set_vel this $vel
			set_attackinprogress this 1
			action this rotate "[expr {$angle+3.1416}] salto" {
				state_enable this
				log "finishing spinjump"
				set_attackinprogress this 0
				set_vel this {0 0 0}
				set is_tricking 0
			} {
				log "breaked spinjump"
				set_attackinprogress this 0
				set_vel this {0 0 0}
				set is_tricking 0
			}
		}

		// Idle-Animationen

		proc roar {} {
			play_anim roar[string index ab [irandom 2]]
		}

		proc walk_rnd {plength} {
			global walk_animset
//        	log "Drachenbaby: st_walk_rnd"
        	state_disable this
        	action this walk "-randompath $plength -animsets \{$walk_animset -1 $walk_animset\} -randomz 4 -speedtype 1 -canclimb 0" {state_enable this}
        	return true
        }
	}

}

}
