//# IFNOT FULL
foreach classname {Alienpflanze Fresspflanze} {
	def_class $classname none dummy 0 {} "
		call scripts/misc/animclassinit.tcl
		set animname [string tolower $classname]
		class_defaultanim \$\{animname\}.standard
		class_physic 1
		class_viewinfog 1
		class_disablescripting
		# keine obj_init für Dummy Classen
	"
}
//# ENDIF
//# STOPIFNOT FULL
def_class Alienpflanze none monster 0 {} {

	class_fightdist 1.0

	def_event evt_timer0
	def_event evt_attrib_update
	def_event evt_poisoning
	def_event evt_accelerator

	call scripts/misc/animclassinit.tcl
	call scripts/classes/items/calls/takeitems.tcl
	call scripts/misc/aggr_events.tcl

	set_class_anim	standard	alienpflanze.standard

	set_class_anim	pulse		alienpflanze.zu_loop
	set_class_anim	openstart	alienpflanze.oeffnen_ani
	set_class_anim	openloop	alienpflanze.auf_loop
	set_class_anim	openstop	alienpflanze.schliessen_ani

	handle_event evt_timer0 {
		set mypos [get_pos this]
		set_particle_strength
		set hitpoints [get_attrib this atr_Hitpoints]
		set last_recharge [gettime]
	}

	handle_event evt_poisoning {
		search_for_victims
	}

	handle_event evt_accelerator {
		poison_victims
	}

	handle_event evt_attrib_update {
		if {rand()<0.1} {check_for_player_contact}
		if {$poison_strength>0.1} {
			if {$is_firing} {
				fincr poison_strength -0.015
			} else {
				fincr poison_strength -0.001
			}
			set_particle_strength
		}
		if {!$is_open} {
			recharge_poison
		}
		check_damage
	}

	method Editor_Set_Info {ifo} {
		set info_string $ifo
		foreach entry $ifo {
			switch [lindex $entry 0] {
				"aggr"		{ set player_aggressivity [lindex $entry 1] }
				"aggrmax" {set aggr_max [lindex $entry 1]}
			}
		}
	}

	method check_first_strike {caller} {
		return 1
	}
	
	obj_init {

		call scripts/misc/animclassinit.tcl
		call scripts/classes/items/calls/takeitems.tcl
		call scripts/misc/aggr_events.tcl

		set stillanim alienpflanze.zu_loop
		set openanim alienpflanze.auf_loop
		set_anim this $stillanim 0 $ANIM_STILL
		set_hoverable this 1
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr {[gettime] + 0.5}]
		timer_event this evt_attrib_update -repeat -1 -userid 0 -interval 0.8 -attime [expr {[gettime]+1}]

		// initializing particles

		change_particlesource this 0 12 {0.0 -0.5 0.0} {0.0 0.0 0.0} 4 1 0
		set particle_vecs bla; set particle_rots bla
		lappend particle_vecs {0.0 -0.6 -0.3};lappend particle_rots  {0.0 0.2 0.2}
		lappend particle_vecs {0.15 -0.6 0.0} ;lappend particle_rots {0.1 0.2 0.0}
		lappend particle_vecs {0.0 -0.6 0.3} ;lappend particle_rots {0.0 0.2 -0.2}
		lappend particle_vecs {-0.15 -0.6 0.0} ;lappend particle_rots {0.1 0.2 0.0}
		lappend particle_vecs {0.1 -0.6 -0.21} ;lappend particle_rots {-0.07 0.2 0.14}
		lappend particle_vecs {0.1 -0.6 0.21} ;lappend particle_rots {-0.07 0.2 -0.14}
		lappend particle_vecs {-0.1 -0.6 0.21} ;lappend particle_rots {0.07 0.2 -0.14}
		lappend particle_vecs {-0.1 -0.6 -0.21} ;lappend particle_rots {0.07 0.2 0.14}
		set particle_cnt 9

		set poison_strength 1.0

		proc switch_particles {onoff} {
			global particle_cnt
			for {set i 0} {$i<$particle_cnt} {incr i} {
				set_particlesource this $i $onoff
			}
		}

		proc set_particle_strength {} {
			global particle_cnt particle_vecs particle_rots poison_strength
			set strength [expr {int($poison_strength*35.0)}]
			for {set i 1} {$i<$particle_cnt} {incr i} {
				change_particlesource this $i 18 [lindex $particle_vecs $i] [lindex $particle_rots $i] $strength 1 0
			}
		}

		// initializing poisoning

		set scan_range 2
		set is_open 0
		set is_firing 0
		set close_cnt 0
		set current_victims ""
		set old_list ""
		set died_in_fight 0

		timer_event this evt_poisoning -repeat -1 -userid 1 -interval 1.6 -attime [expr {[gettime]+1}]

		proc get_enemy_classes {} {
			return {Zwerg Wuker Schwefelwuker Troll Drachenbaby Spinne Kristallbrut Lavabrut}
		}

		proc search_for_victims {} {
			global mypos scan_range current_victims is_open is_firing close_cnt old_list
			set zrange [expr {$scan_range*2.0}]
			set vlist [obj_query this -class [get_enemy_classes] -boundingbox "-$scan_range -2 -$zrange $scan_range 1 $zrange"]
			if {$vlist==0} {set vlist {}}
			set ol $current_victims
			set ool $old_list
			set old_list {}
			set current_victims {}
			foreach v $vlist {
				if {[vector_dist3d $mypos [get_pos $v]]<$scan_range} {
					lappend current_victims $v
					if {[state_get $v]!="fight_dispatch"&&[check_method [get_objclass $v] cause_fleeing]} {
						if {[lsearch $ol $v]!=-1} {
							lappend old_list $v
							if {[lsearch $ool $v]!=-1} {call_method $v cause_fleeing}
						}
					}
				}
			}
			if {$current_victims==""} {
				if {$close_cnt} {incr close_cnt -1;return}
				if {$is_firing} {switch_particles 0;set is_firing 0;return}
				if {$is_open} {close_myself}
			} else {
				if {!$is_open} {open_myself;return}
				if {!$is_firing} {
					set close_cnt 4
					switch_particles 1
					set is_firing 1
					timer_event this evt_accelerator -repeat -1 -interval 1.6 -userid 2 -attime [expr {[gettime]+1}]
					return
				}
				poison_victims
			}
		}

		proc poison_victims {} {
			global current_victims poison_strength mypos scan_range
			if {$current_victims==""} {catch {timer_unset this 2}}
			set full_damage [expr {0.08*$poison_strength}]
			set half_damage [expr {$full_damage*0.5}]
			foreach victim $current_victims {
				if {![obj_valid $victim]} {continue}
				set dist [vector_dist3d $mypos [get_pos $victim]]
				if {$dist>$scan_range} {continue}
				if {[call_method $victim get_current_shield_out]} {
					set damage $half_damage
				} else {
					set damage $full_damage
				}
				call_method $victim cause_damage [expr {-1.0*[hmin 1.0 [expr {$scan_range-$dist}]]*$damage}]
			}
		}

		proc open_myself {} {
			global is_open
			init_aggr_contact
			set is_open 1
			action this anim openstart {
				set_anim this $openanim 0 $ANIM_LOOP
			}
		}

		proc close_myself {} {
			global is_open last_recharge
			set is_open 0
			set last_recharge [gettime]
			action this anim openstop {
				set_anim this $stillanim 0 $ANIM_STILL
			}
		}

		// Verluste

		proc check_damage {} {
			global hitpoints particle_cnt
			set newhp [get_attrib this atr_Hitpoints]
			if {$newhp<0.01} {destroy; return}
			set diff [expr {$hitpoints-$newhp}]
			if {$diff>0.001} {
				change_particlesource this $particle_cnt 27 {0 0 0} {0 0 0} [expr {2<<int([hmin $diff 0.2]*40)}] 2 0 0 0 1
				set_particlesource this $particle_cnt 1
				set hitpoints $newhp
			} else {
				set_particlesource this $particle_cnt 0
			}
			set srcid [expr {$particle_cnt+1}]
			if {$hitpoints<0.6} {
				change_particlesource this $srcid 27 {0 0 0} {0 0 0} [expr {2<<int(6-$hitpoints*10)}] 4 0 0 0 1
				set_particlesource this $srcid 1
			} else {
				set_particlesource this $srcid 0
			}
			add_attrib this atr_Hitpoints 0.002
		}

		proc recharge_poison {} {
			global poison_strength last_recharge hitpoints
			set loss [expr {1.0-$poison_strength}]
			if {$loss<0.05} {return}
			set ct [gettime]
			if {$ct-$last_recharge>$loss*20+7-$hitpoints*5} {
				//log "recharging [expr {int($loss*9)+1}] times ($loss) $poison_strength"
				recharge_anim [expr {int($loss*9)+1}]
			}
		}

		proc recharge_anim {cnt} {
			global poison_strength last_recharge
			set last_recharge [gettime]
			if {$cnt} {
				fincr poison_strength 0.05
				incr cnt -1
				action this anim pulse "recharge_anim $cnt"
			}
		}

		proc destroy {} {
			global particle_cnt mypos
			set myrotz [get_rotz this]
			foreach item [inv_list this] {
				inv_rem this $item
				if {$myrotz<0.1} {
					set_pos $item $mypos
				} else {
					set_pos $item [vector_add $mypos {0 0.6 0}]
				}
				set_visibility $item 1
				set_physic $item 1
			}
			switch_particles 0
			adjust_player_aggr
			set_particlesource this $particle_cnt 0
			incr particle_cnt
			set_particlesource this $particle_cnt 0
			catch {timer_unset this 1}
			catch {timer_unset this 2}
			action this wait 1 {destruct this;del this} {destruct this;del this}
		}
	}
}

def_class Fresspflanze none monster 0 {} {

	def_event evt_timer0
	def_event evt_attrib_update
	def_event evt_targetting

	call scripts/misc/animclassinit.tcl
	call scripts/misc/aggr_events.tcl

	class_fightdist 1.0

	set_class_anim	standard			fresspflanze.standard

	set_class_anim	pulse				fresspflanze.zu_loop
	set_class_anim	openstart			fresspflanze.oeffnen_ani
	set_class_anim	openloop			fresspflanze.auf_loop
	set_class_anim	openstop			fresspflanze.schliessen_ani

	set_class_anim	standstill			fresspflanze.zu_loop
	set_class_anim	rotateleft			fresspflanze.zu_loop
	set_class_anim	rotateright			fresspflanze.zu_loop
	set_class_anim	turn180left			fresspflanze.zu_loop
	set_class_anim	turn180right		fresspflanze.zu_loop

	set_class_anim	standstillopen		fresspflanze.auf_loop
	set_class_anim	rotateleftopen		fresspflanze.auf_loop
	set_class_anim	rotaterightopen		fresspflanze.auf_loop
	set_class_anim	turn180leftopen		fresspflanze.auf_loop
	set_class_anim	turn180rightopen	fresspflanze.auf_loop

	handle_event evt_timer0 {
		set mypos [get_pos this]
		set hitpoints [get_attrib this atr_Hitpoints]
		set partpos [vector_add $mypos {0 -0.5 0}]
	}

	handle_event evt_attrib_update {
		if {rand()<0.1} {check_for_player_contact}
		check_damage
	}

	handle_event evt_targetting {find_victims}

	method Editor_Set_Info {ifo} {
		set info_string $ifo
		foreach entry $ifo {
			switch [lindex $entry 0] {
				"aggr"		{ set player_aggressivity [lindex $entry 1] }
				"aggrmax" {set aggr_max [lindex $entry 1]}
			}
		}
	}

	method check_first_strike {caller} {
		return 1
	}

	method set_damage {damage} {
		add_attrib this atr_Hitpoints $damage
	}

	obj_init {

		call scripts/misc/animclassinit.tcl
		call scripts/misc/aggr_events.tcl

		set stillanim fresspflanze.zu_loop
		set openanim fresspflanze.auf_loop
		set_anim this $stillanim 0 $ANIM_STILL
		set_hoverable this 1

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr {[gettime] + 0.5}]
		timer_event this evt_attrib_update -repeat -1 -userid 0 -interval 1 -attime [expr {[gettime]+1}]

		set target_angle 0.0
		set rotate_dir ""
		set is_rotating 0
		set rotate_counter 0

		proc rotate_to {angle} {
			global target_angle rotate_dir is_rotating is_open ANIM_LOOP stillanim rotate_counter
			set target_angle $angle
			if {!$is_open} {
				if {$rotate_counter} {incr rotate_counter -1;return} {set rotate_counter 6}
			}
			set crot [expr {[get_roty this]-0.4}]
			set diff [expr {$angle-$crot}]
			if {($diff>-0.05&&$diff<0.05)||($diff>6.23&&$diff<6.33)||($diff>-6.33&&$diff<-6.23)} {
				rotate_stop
				return
			}
			if {$diff>3.14} {
				fincr diff -6.28
				if {$diff>3.14} {fincr diff -6.28}
			}
			if {$diff<-3.14} {fincr diff 6.28}
			if {$diff<0.0} {set rotate_dir "-"} {set rotate_dir ""}
			state_triggerfresh this rotating
			if {!$is_open&&!$is_rotating} {
				set_anim this $stillanim 0 $ANIM_LOOP
			}
			set is_rotating 1
		}

		proc rotate_cont {} {
			global target_angle rotate_dir is_rotating is_open
			if {$is_open} {set step 0.03} {set step 0.02}
			set crot [expr {[get_roty this]-0.4}]
			set diff [expr {$target_angle-$crot}]
			if {($diff>-0.05&&$diff<0.05)||($diff>6.23&&$diff<6.33)||($diff>-6.33&&$diff<-6.23)} {
				rotate_stop
			} else {
				set_roty this [expr $crot+0.4+${rotate_dir}$step]
			}
		}

		proc rotate_stop {} {
			global is_open stillanim ANIM_STILL is_rotating
			if {!$is_rotating} {return}
			if {!$is_open} {
				set_anim this $stillanim 0 $ANIM_STILL
			}
			state_disable this
			set is_rotating 0
		}

		proc open_myself {} {
			global is_open
			init_aggr_contact
			set is_open 1
			action this anim openstart {
				set_anim this $openanim 0 $ANIM_LOOP
			}
		}

		proc close_myself {} {
			global is_open
			set is_open 0
			action this anim openstop {
				set_anim this $stillanim 0 $ANIM_STILL
			}
		}

		// Opfer

		set current_victim 0
		set new_victim 0
		set scan_range 3.5
		set is_open 0
		set is_firing 0
		set primary_victim 0
		set old_victim 0
		set died_in_fight 0

		timer_event this evt_targetting -repeat -1 -userid 1 -interval 1.3 -attime [expr {[gettime]+1}]

		proc get_enemy_classes {} {
			return {Zwerg Wuker Schwefelwuker Troll Drachenbaby Spinne Kristallbrut Lavabrut}
		}

		proc find_victims {} {
			global current_victim new_victim is_firing old_victim
			global scan_range is_open primary_victim mypos partpos
			set victim [lindex $current_victim 0]
			if {$victim&&[obj_valid $victim]} {
				if {[vector_dist3d [get_pos $victim] [lindex $current_victim 1]]<0.8} {
					if {[call_method $victim get_current_shield_out]} {
						set damage 0.03
					} else {
						set damage 0.06
					}
					call_method $victim cause_damage -$damage
					if {$old_victim==$victim} {
						if {[state_get $victim]!="fight_dispatch"&&[check_method [get_objclass $victim] cause_fleeing]} {
							call_method $victim cause_fleeing
						}
					}
					set old_victim $victim
				}
			}
			set current_victim $new_victim
			set zrange [expr {$scan_range*2.0}]
			set crot [expr {[get_roty this]-0.4}]
			set vlist [obj_query this -class [get_enemy_classes] -boundingbox "-$scan_range -2 -$zrange $scan_range 1 $zrange"]
			if {$vlist==0} {set vlist {}}
			set victims {}
			foreach v $vlist {
				if {[vector_dist3d $mypos [get_pos $v]]<$scan_range} {
					lappend victims $v
				}
			}
			if {$victims==""} {
				if {$is_open} {
					close_myself
					return
				}
				set vlist [obj_query this -class [get_enemy_classes] -boundingbox {-6 -2 -12 6 1 12}]
				foreach v $vlist {
					if {[set diff [vector_dist3d $mypos [get_pos $v]]]<6} {
						lappend victims [list $v $diff]
					}
				}
				if {$victims==""} {
					rotate_stop
					return
				}
				set angle [vector_angle $mypos [get_pos [lindex [lsort -index 1 -real $victims] 0]]]
				fincr angle 1.57
				rotate_to $angle
			} else {
				if {!$is_open} {
					open_myself
					return
				}
				set primvics {}
				if {$is_firing} {
					set is_firing 0
					return
				} else {
					foreach v $victims {
						set vpos [get_pos $v]
						set efp [vector_sub $vpos $mypos]
						set efx [lindex $efp 0]
						set efy [lindex $efp 1]
						set efz [expr {[lindex $efp 2]*0.5}]
						set efl [expr {sqrt($efx*$efx+$efy*$efy+$efz*$efz)}]
						if {abs($efz)<0.01} {
							if {$efx<0.0} {set angle 1.57} {set angle 4.71}
						} else {
							set angle [expr {atan(-$efx/$efz)}]
							if {$efz<0.0} {fincr angle 3.14}
						}
						set anglediff [expr {$angle - $crot}]
						if {$anglediff<-3.14} {fincr anglediff 6.28} {if {$anglediff>3.14} {fincr anglediff -6.28}}
						lappend primvics [list $v [expr {abs($anglediff)}] $angle]
					}
					set primvic [lindex [lsort -index 1 -real $primvics] 0]
					set v [lindex $primvic 0]
					set diff [lindex $primvic 1]
					set angle [lindex $primvic 2]
					if {$diff<0.5} {
						set xf [expr {-sin($angle)}]
						set zf [expr {2*cos($angle)}]
						set diff [vector_dist3d $mypos $vpos]
						if {$diff<3.0} {
							set w [hmax 0.0 [expr {-0.01+0.03*$diff}]]
							set h [expr {-0.13+0.01*$diff}]
						} else {
							set w [expr {0.05+0.01*$diff}]
							set h [expr {-0.07-0.01*$diff}]
						}
						set dir [list [expr {$w*$xf}] $h [expr {$w*$zf}]]
						create_particlesource 15 $partpos $dir 4 1
						set new_victim [list $v $vpos]
					}
					if {$diff>0.2} {rotate_to $angle} {rotate_stop}
				}
			}

		}

		// Verluste

		proc check_damage {} {
			global hitpoints
			set newhp [get_attrib this atr_Hitpoints]
			if {$newhp<0.01} {destroy; return}
			set diff [expr {$hitpoints-$newhp}]
			if {$diff>0.001} {
				change_particlesource this 0 27 {0 0 0} {0 0 0} [expr {2<<int([hmin $diff 0.2]*40)}] 2 0 0 0 1
				set_particlesource this 0 1
				set hitpoints $newhp
			} else {
				set_particlesource this 0 0
			}
			if {$hitpoints<0.6} {
				change_particlesource this 1 27 {0 0 0} {0 0 0} [expr {2<<int(6-$hitpoints*10)}] 4 0 0 0 1
				set_particlesource this 1 1
			} else {
				set_particlesource this 1 0
			}
			add_attrib this atr_Hitpoints 0.002
		}

		proc destroy {} {
			set_particlesource this 0 0
			set_particlesource this 1 0
			set_particlesource this 2 0
			adjust_player_aggr
			action this wait 1 {destruct this;del this} {destruct this;del this}
		}
	}

	state rotating {
		rotate_cont
	}
}
