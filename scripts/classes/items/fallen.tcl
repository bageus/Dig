call scripts/misc/utility.tcl

def_class Plattmachfalle metal protection 3 {} {

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl
	call scripts/misc/obj_attribs.tcl

	set_class_anim up	plattmachfalle.hoch
	set_class_anim down	plattmachfalle.runter
	class_defaultanim plattmachfalle.standard
	class_fightdist 1.0
	class_physcategory 3

	method init {} {
		set trap_init true
	}

	def_event evt_timer0
	def_event evt_timer1
	
	handle_event evt_timer0 {
		if {[get_attrib this atr_Hitpoints] < 0.01} {
			call_method this destroy
			return
		}
		if {[get_boxed this]} {return}

		if {$trap_init} {
			set trap_init false
			set trap_mode 1
			set last_snap [expr {[gettime]-4}]
			search_victim
		}

		if {[enemies_near]} {
			activate_timer1
			set myowner [get_owner this]
		} else {
			deactivate_timer1
		}
	}
	
	handle_event evt_timer1 {
		global trap_init RELOADTIME

		search_victim
	}


	method destroy {} {
		del this
		destruct this
	}

	obj_init {

		set_anim this plattmachfalle.standard 0 1
		call scripts/misc/genericprod.tcl
		set_placesnapmode this 2 // 2 == floor (obere BBox-ecken muessen in der Decke sein)

		timer_event this evt_timer0 -repeat -1 -interval 3 -userid 0
		timer_event this evt_timer_init -attime [expr {[gettime]+1}]

		set reloadcycles 0
		set RELOADTIME   5					;# zum Tunen der Zuschnapp - Geschwindigkeit
		set trap_mode 0
		set attack_item 0
		set last_victims ""
		set trap_init false
		set timer1_active 0
		set last_snap 0.0
		set myref [get_ref this]
		set myowner -1

		#trap_modes : 	0 - scanning
		#				1 - snap
		#				2 - reload

		set_attrib this weight 0.1
		set_attrib this attackpoints_sr 0.35
		set_physic this 1
		set_prod_directevents this 1

		proc enemies_near {} {
			if {![get_prod_enabled this]} {return 0}
			if {[obj_query this "-class \{Zwerg Wuker Schwefelwuker Troll Spinne Kristallbrut Lavabrut Riesenhamster\} -owner enemy -boundingbox \{-5 -5 -10 5 5 8\} -limit 1"]} {
				return 1
			} else {
				return 0
			}
		}
		
		proc activate_timer1 {} {
			global timer1_active
			if {!$timer1_active} {
				timer_event this evt_timer1 -repeat -1 -interval 0.3 -userid 1
				set timer1_active 1
			}
		}
		
		proc deactivate_timer1 {} {
			global timer1_active
			if {$timer1_active} {
				timer_unset this 1
				set timer1_active 0
			}
		}
		
		proc search_victim {} {
			global trap_mode attack_item last_victims last_snap RELOADTIME myref
			if {![get_prod_enabled this]} {return}
			
			set timediff [expr {[gettime]-$last_snap}]
			if {$trap_mode&&$timediff>3} {
				set trap_mode 0
				action this anim up {} {set_anim this plattmachfalle.oben 0 1}
				return
			}
			if {$timediff<$RELOADTIME} {
				return
			}
			set attack_item_list [lnand 0 [obj_query this "-class \{Zwerg Wuker Schwefelwuker Troll Spinne Kristallbrut Lavabrut Riesenhamster\} -owner enemy -boundingbox \{-0.5 -0.3 -3.0 0.5 0.3 3.0\}"]]
			
			set last_victims [land $last_victims $attack_item_list]
			set attack_item_list [lnand $last_victims $attack_item_list]
			
			foreach item $last_victims {
				if {![obj_valid $item]} {
					set last_victims [lnand $item $last_victims]
				} elseif {[state_get $item]=="fight_dispatch"} {
					if {[call_method $item get_attack_item]==$myref} {
						set last_victims [lnand $item $last_victims]
					}
				}
			}
			
			if {$attack_item_list!=""} {
				log "Plattmachfalle.tcl: attack_item_list: $attack_item_list ($last_victims)"
			}

			set attack_item 0
			foreach item $attack_item_list {
				if {[state_get $item] != "trapped"  &&  [get_attrib $item atr_Hitpoints] >= 0.01} {
					set attack_item $item
					break
				}
			}

			if {$attack_item} {
				snap_victim
			}

		}

		proc snap_victim {} {
			global trap_mode reloadcycles attack_item last_victims RELOADTIME last_snap myowner
			if {$attack_item} {
				call_method $attack_item cause_damage -[get_attrib this attackpoints_sr]
				set zref [get_ref $attack_item]
				call_method $attack_item get_trapped splat
				action this anim down {} {set_anim this plattmachfalle.standard 0 1}
				set last_victims [lor $last_victims $attack_item]
				set item_owner [get_owner $attack_item]
				if {$item_owner>-1} {
					set_diplomacy $item_owner $myowner "enemy"
				}
			} else {
				log "Trap : Target lost"
			}
			set last_snap [gettime]
			set trap_mode 1
		}
	}
}

//# STOPIFNOT FULL

def_class SteinfalleMedusa metal protection 3 {} {

	method prod_preinvented {} {
		return [list]
	}


	method prod_item_materials item {
		return [list]
	}

	method prod_item_tools item {
		return [list]
	}

	method prod_item_blueprint item {
		return [list]
	}


	method prod_item_actions item {
		return [list]
	}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl
	call scripts/misc/obj_attribs.tcl

	class_defaultanim steinfallemedusa.standard
	class_fightdist 1.0
	class_physcategory 3

	def_event evt_timer0
	def_event evt_timer1

	handle_event evt_timer0 {
		if {[get_attrib this atr_Hitpoints] < 0.01} {
			call_method this destroy
			return
		}
		if {[get_boxed this]} {
			return
		}
		if {[enemies_near]} {
			activate_timer1
			set myowner [get_owner this]
		} else {
			deactivate_timer1
		}
	}
	
	handle_event evt_timer1 {

		search_victim
		
	}

	method destroy {} {
		destruct this
		del this
	}

	obj_init {

		set_anim this steinfallemedusa.standard 0 1
		call scripts/misc/genericprod.tcl
		set_placesnapmode this 1 // 1 == wall

		timer_event this evt_timer0 -repeat -1 -interval 3 -userid 0

		set reloadcycles 0
		set RELOADTIMEONE   35					;# zum Tunen der Zuschnapp - Geschwindigkeit
		set RELOADTIMEALL   3					;# zum Tunen der Zuschnapp - Geschwindigkeit
		set trapmode 0
		set attack_item 0
		set last_victims ""
		set old_victims ""
		set timer1_active 0
		set last_snap 0.0
		set myref [get_ref this]
		set myowner -1

		set_attrib this weight 0.1
		set_attrib this attackpoints_sr 0.05
		set_physic this 1
		set_prod_directevents this 1

		proc enemies_near {} {
			if {![get_prod_enabled this]} {return 0}
			if {[obj_query this "-class \{Zwerg Wuker Schwefelwuker Troll Kristallbrut Lavabrut\} -owner enemy -boundingbox \{-5 -5 -10 5 5 8\} -limit 1"]} {
				return 1
			} else {
				return 0
			}
		}
		
		proc activate_timer1 {} {
			global timer1_active
			if {!$timer1_active} {
				timer_event this evt_timer1 -repeat -1 -interval 0.3 -userid 1
				set timer1_active 1
			}
		}
		
		proc deactivate_timer1 {} {
			global timer1_active
			if {$timer1_active} {
				timer_unset this 1
				set timer1_active 0
			}
		}
		
		proc search_victim {} {
			global trapmode attack_item last_victims RELOADTIMEALL RELOADTIMEONE last_snap myref
			global oldvictims
			if {![get_prod_enabled this]} {return}
			set ctime [gettime]
			if {$ctime-$last_snap<$RELOADTIMEALL} {
				return
			}
			set attack_item_list [lnand 0 [obj_query this "-class \{Zwerg Wuker Schwefelwuker Troll Kristallbrut Lavabrut\} -owner enemy -boundingbox \{-0.5 -1.0 0.0 0.5 1.0 5.0\}"]]
			//log "olist: $attack_item_list"
			
			set last_victims [land $last_victims $attack_item_list]
			set attack_item_list [lnand $last_victims $attack_item_list]
			
			foreach item $last_victims {
				if {![obj_valid $item]} {
					set last_victims [lnand $item $last_victims]
				} elseif {[state_get $item]=="fight_dispatch"} {
					if {[call_method $item get_attack_item]==$myref} {
						set last_victims [lnand $item $last_victims]
					}
				}
			}
			
			set attack_item 0
			foreach item $attack_item_list {
				if {[state_get $item] != "trapped"  &&  [get_attrib $item atr_Hitpoints] >= 0.01} {
					set attack_item $item
					break
				}
			}
			
			if {$attack_item} {
				snap_victim
			}
		}

		proc snap_victim {} {
			global attack_item last_victims last_snap myowner
			set ctime [gettime]
			if {$attack_item} {
				log "Trap - Attack $attack_item -[get_attrib this attackpoints_sr]"
				call_method $attack_item cause_damage -[get_attrib this attackpoints_sr]
				call_method $attack_item get_trapped petrify
				create_particlesource 28 [get_pos this] {0 0 0.2} 2 3 
				lappend last_victims $attack_item
				set item_owner [get_owner $attack_item]
				if {$item_owner>-1} {
					set_diplomacy $item_owner $myowner "enemy"
				}
			} else {log "Trap : Target lost"}
			set last_snap $ctime
		}

	}
}
