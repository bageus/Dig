def_class Drachen_Ei none material 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim swf_ei.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this swf_ei.standard 0 $ANIM_STILL
        set_storable this 1
		set_physic this 1
        set_hoverable this 1

	}
}


def_class Drache none monster 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members

	class_fightdist 3.5

	def_event evt_timer0
	def_event evt_burnstart
	def_event evt_faffect
	def_event evt_burnstop
	def_event evt_task_defend
	def_event evt_drache_die


	handle_event evt_timer0 {
		set ip [obj_query this "-class Info_Pos_ZwergTmp -range 20 -limit 1"]
		if { $ip != 0 } {
			set scanpos [get_pos $ip]
		}

       	if {[get_info Drachenmama] == "true"} {
       		log "[get_objname this] : Ich bin die Drachenmama..."
       		set_texturevariation this 1
			state_triggerfresh this idle

       		set is_drachenmama 1
       		set cboxes [list [new Info_Coll_d] [new Info_Coll_d] [new Info_Coll_d] [new Info_Coll_d]]
			set i 13
			foreach obj $cboxes {
				set_pos $obj "[expr [get_posx this]] [get_posy this] $i"
				set i [expr {$i - 3.6}]
			}
       	}
	}

	handle_event evt_task_defend {

		if { [state_get this] ==  "fight_dispatch" } { return }
		if { $is_turning == 1 } { return }

		log "Drache defend"

		set evtitem [event_get this -subject1]
		if { [get_objclass $evtitem] == "Zwerg" && [get_owner $evtitem] == 0 } {
			if {$is_drachenmama} {
				catch { sm_set_event Drachenmama_angegriffen }
			} else {
				catch { sm_set_event Drache_angegriffen }
			}
		}
		set ry [get_roty this]
		if { $ry > 4.6 && $ry < 4.8 } {

			// break current action
			action this wait 0 {} {}

			state_disable this
			set_attackinprogress this 1
			set is_turning 1
			action this anim drache.sitzen_umdrehen {state_enable this;set_roty this 1.57;set_attackinprogress this 0;set is_turning 0} {set is_turning 0}
			state_trigger this fight_dispatch
			return
		}

		state_triggerfresh this	fight_dispatch
	}

	handle_event evt_burnstart {
		burnstart
	}

	handle_event evt_burnstop {
		set_attackinprogress this 0
		burnend
	}

	handle_event evt_faffect {
		affect_enemies
	}


	handle_event evt_drache_die {
		call_method this delete_collisionboxes
		if {$is_drachenmama} {
			catch { sm_set_event Drachenmama_tot }
		} else {
			catch { sm_set_event Drache_tot }
		}
		state_disable this
   		action this wait 1 "call_method [get_ref this] destroy" "call_method [get_ref this] destroy"
	}


    set_class_anim idlea drache.sitzen_warten_a
    set_class_anim idleb drache.sitzen_warten_b
    set_class_anim idlec drache.sitzen_warten_c

    set_class_anim sleeping1 drache.liegen_schlafen
    set_class_anim dead1 drache.toetlich_d_stumm
    set_class_anim deadtalk1 drache.toetlich_d_stumm

    set_class_anim burnas  drache.sitzen_zu_speien_a
    set_class_anim burnal  drache.speien_a_loop
    set_class_anim burnae  drache.speien_a_zu_sitzen
    set_class_anim burna2b drache.speien_a_zu_b
    set_class_anim burna2c drache.speien_a_zu_c

    set_class_anim burnbs  drache.sitzen_zu_speien_b
    set_class_anim burnbl  drache.speien_b_loop
    set_class_anim burnbe  drache.speien_b_zu_sitzen
    set_class_anim burnb2a drache.speien_b_zu_a
    set_class_anim burnb2c drache.speien_b_zu_c

    set_class_anim burncs  drache.sitzen_zu_speien_c
    set_class_anim burncl  drache.speien_c_loop
    set_class_anim burnce  drache.speien_c_zu_sitzen
    set_class_anim burnc2a drache.speien_c_zu_a
    set_class_anim burnc2b drache.speien_c_zu_b

    set_class_anim talka drache.sitzen_dialog_d
    set_class_anim talkd drache.sitzen_dialog_f
    set_class_anim talkc drache.sitzen_dialog_g
    set_class_anim talke drache.sitzen_dialog_d

	set_class_anim attack drache.sitzen_vorbeugen

   	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members

		set_anim this drache.sitzen_warten_a 0 $ANIM_LOOP		;# set standard anim
		set_collision this 1
		set_attrib this hitpoints 1
		set_hoverable this 1
		set_selectable this 0
		set_visibility this 1
		set_collision this 1
		state_triggerfresh this sleeping

		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 1]
		set_objname this "Drache"

		set_attrib this atr_Hitpoints 4.0
		set enemy 0
		set enmoldpos 0

		set burnstate 0
		set is_turning 0

		change_particlesource this 0 2 {0 0 0} {0.4 0.05 0} 255 8 0 3
		//set_particlesource this 0 1

       	set fdir {0 0.05 0.4}
       	set scanpos {0 0 0}
       	set info_string ""
   		set is_drachenmama 0
		set cboxes ""

		catch { sm_add_event Drachenmama_tot }			;// falls nicht in der Kampagne
		catch { sm_add_event Drache_tot }
		catch { sm_add_event Drache_angegriffen }
		catch { sm_add_event Drachenmama_angegriffen }


		// --- Procs ------------------------------------------

		proc get_info {name} {
			global info_string
			foreach item $info_string {
				set inam [lindex $item 0]
				set ival [lindex $item 1]
				if { $name == $inam } {
					return $ival
				}
			}
			return undefined
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

		proc play_anim_s {anim sta {start 0} {stop 0}} {
			global burnstate
			log "--> $anim $sta $start  $stop : $burnstate"
			set_attackinprogress this 1
			state_disable this
			action this anim $anim "state_enable this ; set burnstate $sta"
			if { $start != 0 } {
				timer_event this evt_burnstart -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + $start]
			}
			if { $stop != 0 } {
    			timer_event this evt_burnstop -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + $stop]
    		}
		}

		proc burn_to {id} {
			global burnstate
			//log "burn_to: id = $id, burnstate = $burnstate"
			switch $burnstate {
				"0"	{
						switch $id {
							"0"	{play_anim_s idlea 	0}
							"a" {play_anim_s burnas a}
							"b" {play_anim_s burnbs b}
							"c" {play_anim_s burncs c}
						}
					}
				"a" {
						switch $id {
							"0"	{play_anim_s burnae  0}
							"a" {play_anim_s burnal  a 0.1 1}
							"b" {play_anim_s burna2b b}
							"c" {play_anim_s burna2c c}
						}
					}
				"b" {
						switch $id {
							"0"	{play_anim_s burnbe  0}
							"a" {play_anim_s burnb2a a}
							"b" {play_anim_s burnbl  b 0.1 1}
							"c" {play_anim_s burnb2c c}
						}
					}
				"c" {
						switch $id {
							"0"	{play_anim_s burnce  0}
							"a" {play_anim_s burnc2a a}
							"b" {play_anim_s burnc2b b}
							"c" {play_anim_s burncl  c 0.1 1}
						}
					}
			}
		}

       	proc getfdir {} {
       		global enemy
    		set ownpos [get_pos this]
    		set dpos [get_linkpos this 3]
    		set dpos [vector_add $ownpos $dpos]
            set epos [get_pos $enemy]
            set vc [vector_mul [get_velcomp $enemy] 8]
            set epos [vector_add $epos $vc]

            set dif [vector_sub $epos $dpos]
            set dir [vector_mul $dif 0.03125]
            #log "fd: $dir"
			return $dir
       	}

		proc get_random_of {str} {
        	set rlist [split $str ""]
        	set which [irandom [llength $rlist]]
        	return [lindex $rlist $which]
        }


        proc burna {id} {
        	global fdir burning attime
        	set burning 1
			state_disable this
    		action this anim burna "set burning 0;state_enable [get_ref this]" "set burning 0;state_enable [get_ref this]"
    		timer_event this evt_burnstart -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 1.1]
    		timer_event this evt_burnstop -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 2.4]
    		#set attime 11
        }

        proc burna {} {
        	global fdir burning attime
        	set burning 1
			state_disable this
    		action this anim burna "set burning 0;state_enable [get_ref this]" "set burning 0;state_enable [get_ref this]"
    		timer_event this evt_burnstart -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 1.1]
    		timer_event this evt_burnstop -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 2.4]
    		#set attime 11
        }
        proc burnb {} {
        	global fdir burning
        	set burning 1
			state_disable this
    		action this anim burnb "set burning 0;state_enable [get_ref this]" "set burning 0;state_enable [get_ref this]"
    		timer_event this evt_burnstart -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 0.8]
    		timer_event this evt_burnstop -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 1.7]
        }
        proc burnc {} {
        	global fdir burning
        	set burning 1
			state_disable this
    		action this anim burnc "set burning 0;state_enable [get_ref this]" "set burning 0;state_enable [get_ref this]"
    		timer_event this evt_burnstart -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 0.4]
    		timer_event this evt_burnstop -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 1.3]
        }

        proc burnstart {} {
        	if { [check_enemy] } {
    			change_particlesource this 0 27 {0 0 0} [getfdir] 511 16 0 3
    		}
    		set_particlesource this 0 1
        }
        proc burn2start {} {
        	if { [check_enemy] } {
    			change_particlesource this 0 27 {0 0 0} [getfdir] 511 16 0 3
    		}
    		set_particlesource this 0 1
    	}
    	proc burn3start {} {
        	if { [check_enemy] } {
    			change_particlesource this 0 34 {0 0 0} [getfdir] 255 16 0 3
    		}
    		set_particlesource this 0 1
        }
        proc burnend {} {
        	if { [check_enemy] } {
        		change_particlesource this 0 27 {0 0 0} [getfdir] 511 16 0 -1
				affect_enemies
        	}
    		set_particlesource this 0 0
        }

        proc affect_enemies {} {
        	global enmoldpos
  			set el [lnand 0 [obj_query this "-class {Zwerg Troll} -pos \{$enmoldpos\} -range 1.1"]]
 			//log "Objq: $el"
 			foreach item $el {
 				call_method $item burn
 			}
        }

        proc get_enemy_classes {} {
        	global is_drachenmama

        	set classes "Troll Zwerg"
			catch {
				if  {$is_drachenmama} {
					if  { [sm_get_event Drachenmama_angegriffen]  ||
						  [sm_get_event Drachenbaby_tot]  ||
						  [sm_get_event Drache_angegriffen] } {

						set classes "Troll Zwerg"
					} else {
						set classes "Troll"
					}
				} else {
					if  { [sm_get_event Drache_angegriffen] } {
						set classes "Troll Zwerg"
					} else {
						set classes "Troll"
					}
				}
			}
			return $classes
        }

	}

	method Editor_Set_Info {ifo} {
		global info_string
		set info_string $ifo
	}

	method burn {ani} {
		burn$ani
	}

	method im_attacking_you {} {
		//super
		return 0
	}

	method check_first_strike {caller} {
		return 1
	}

	method get_burning {} {
		return { [expr $burnstate != 0] }
	}

	method idle_anim {} {
		set anim "idle[get_random_of ab]"
		set_anim this $anim 0 2
	}

	method set_enemy {enm} {
		set enemy $enm
	}

	method fire_start {} {
		burnstart
	}
	method fire2_start {} {
		burn2start
	}
    method fire3_start {} {
		burn3start
	}

	method fire_stop {} {
		burnend
	}

	method destroy {} {
		set pos [get_pos this]
		sel /obj
		set drachenschuppe [new Drachenschuppe]
		set_pos $drachenschuppe $pos
		destruct this
		del this
	}

	method delete_collisionboxes {} {
		global cboxes
		foreach obj $cboxes {
			del $obj
		}
	}

	state idle {
		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			set_event this evt_drache_die -target this
    		return
		}

		set el [obj_query this "-class \{ [get_enemy_classes] \} -range 20"]
		if { $el != 0 } {
			state_trigger this fight_dispatch
			return
		}

		set anim "idle[get_random_of ab]"
		state_disable this
		action this anim $anim "state_enable [get_ref this]"

	}
	state sleeping {
		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			set_event this evt_drache_die -target this
    		return
		}

		set el [obj_query this "-class \{ [get_enemy_classes] \} -range 20"]
		if { $el != 0 } {
			state_trigger this fight_dispatch
			return
		}

		set anim "sleeping1"
		state_disable this
		action this anim $anim "state_enable [get_ref this]"

	}
    state dead {
		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			set_event this evt_drache_die -target this
   	 		return
		}

		set anim "dead1"
		state_disable this
		action this anim $anim "state_enable [get_ref this]"

	}
    state deadtalk {
		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			set_event this evt_drache_die -target this
   	 		return
		}

		set anim "deadtalk1"
		state_disable this
		action this anim $anim "state_enable [get_ref this]"

	}

	state fight_dispatch {
		if { [get_attrib this atr_Hitpoints] < 0.01 } {
			set_event this evt_drache_die -target this
   	 		return
		}
		set ownpos [get_pos this]
		set el [obj_query this "-class \{ [get_enemy_classes] \} -range 20"]
		if { $el == 0 } {
			state_trigger this idle
			burn_to 0
			return
		}
		//log "el = $el"
		foreach item $el {
			set epos [get_pos $item]
			set dist [vector_abs [vector_sub $ownpos $epos]]
			set enemy $item
			set enmoldpos $epos

			set ldir [expr sin([get_roty this])]
			set bBlow 1
			if { $ldir > 0 && [expr [lindex $ownpos 0] < [lindex $epos 0]]} {set bBlow 0}
			if { $ldir < 0 && [expr [lindex $ownpos 0] > [lindex $epos 0]]} {set bBlow 0}

			set hp [get_attrib $item atr_Hitpoints]
			if { $hp < 0.01 } {
				continue
			}
			log "dist = $dist, bBlow = $bBlow"
			if { $dist < 7.6 } {
				#state_disable this
				set fresult [fight_setactions_strikeback this 7.5] ;#4.5] ;#3.2]
				log "Drache-fres: $fresult ([state_getenablecnt this])"
				foreach item $fresult {
					if { $item == "attack" } {
						state_disable this
    					action this anim attack {state_enable this} {state_enable this}
    					return
					}
				}
			}
			if { [check_enemy] && $bBlow } {
				set ez [lindex $epos 2]
				set oz [lindex $ownpos 2]
    			if { $ez > $oz + 2 } {
                     burn_to c
                     return
    			} elseif { $ez < $oz - 2 } {
                     burn_to a
                     return
    			} elseif { 1 } {
                     burn_to b
                     return
    			}
    		}
    		set anim "idle[get_random_of abc]"
			state_disable this
			action this anim $anim "state_enable [get_ref this]"
			return
		}
	}
}
