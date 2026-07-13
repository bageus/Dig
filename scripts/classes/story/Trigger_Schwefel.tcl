// Trigger für die Schwefelwelt

Create_Instant_Trigger_Class Trigger_swf_120_Ex "swf_120"
Create_Instant_Trigger_Class Trigger_swf_121_Ex "swf_121" "Krake"

def_class Trigger_swf_121 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {6 -3 3}]
			call_method $FR fog_remove 0 20 20
			set_pos $FR [vector_add $pos {1 -4 2}]
			call_method $FR fog_remove 0 20 20
			call_method $FR timer_delete 5
        	}
	}

	handle_event evt_timer0 {
		set sequencescript "swf_121"
		remove_fow
		sequencer_activate
	}
}

def_class Trigger_swf_131_exkrake none dummy 0 {} {

// CheckTrigger / checkt ob sich ein Zwerg einem Gegenstand nähert mit dem Ziel es aufzuheben/zu verarbeiten (current_lock_obj) / startet "Voodoo_Lager"

	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
		set andres_actuator 0
		set newring 0

		proc ringuebergeben {obj_ref} {
			global newring
			create_ring
			set_inventoryslotuse this 1
			if {[inv_check $obj_ref this] == 1}	{
				set_owner $newring 0
				inv_add $obj_ref $newring
			} else {
				set pos [vector_add [get_pos this] {0 -0.5 1}]
				set_pos $newring $pos
			}

        }

		proc create_ring {} {
			global newring
			set newring [obj_query this -class Ring_Des_Wassers -range 5 -limit 1]
			if {$newring == 0} {
			set newring [new Ring_Des_Wassers "" [ get_pos this ] {0 0 0} ]
			}
		}

	}

	handle_event evt_timer0 {
		set kistenref [obj_query this -class Schatzkiste -range 2]
		set sequencescript "swf_131"
//		trigger create this callback "sequencer_activate"
//		trigger set_timer this 3
//		trigger set_callback this "check_gnomes"

		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 8
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1

	}
}


def_class Trigger_Fenris_131a none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0

	method activate_fenris_131a {} {
		set sequencescript "fenris_131a"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 10.0
		trigger set_target_class this Fenris_002
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}

	obj_init {

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {0 -6 8}]
			call_method $FR fog_remove 0 100 100
			call_method $FR timer_delete -1
		}

		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
	}
}


def_class Trigger_Troll_gotoDrache none dummy 0 {} {
	def_event evt_timer0

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

	  	proc gotoDrache {} {
           	set troll [obj_query this "-class Troll -limit 1 -range 50"]
           	log "TROLL = $troll"
            set way_point [obj_query this "-class Info_Drache_Waypoint -limit 1"]
            log "WAY_POINT = $way_point"
            if {$way_point == 0 } {
            	set troll_da [obj_query $way_point "-class Troll -range 10"]
            	log "Troll_da = $troll_da"
            	if {$troll_da != 0} {
            		set way_pos [get_pos $way_point]
            		tasklist_add $troll "walk_pos \{$way_pos\}; call_method $troll set_occupation random_guard"
            	}
            }
            timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
        }
	}
	handle_event evt_timer0 {
		trigger create this single_timer "gotoDrache"
		trigger set_timer this [expr 5*60]  ;# 300
	}
}

def_class Trigger_Swf_065 none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl


	obj_init {

		proc start_seq {} {
			global sequence_a sequencescript
			sequencer_activate
		}

		set_selectable this 0
		set_hoverable this 0
		set sequence_a 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "swf_065"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 0.1
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}
def_class Trigger_Swf_073 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+20]
		call scripts/classes/story/sequencer.tcl

		proc check {} {

			if { [DracheAutoDestroy] } { return 0 }

			if {[obj_query this -class Zwerg -owner 0 -range 5 -limit 1 -cloaked 1]==0} {
			return 0
			}
			if {[obj_query this -class Trigger_swf_unq_drache_graben -range 100 -limit 1]==0} {
				log "Activitated because of Trigger_swf_unq_drache_graben"
				return 1
			}
			return 0
		}
	}

	handle_event evt_timer0 {
		set sequencescript "swf_073"
		trigger create this callback "sequencer_activate"
		trigger set_callback this "check"
		trigger set_timer this 3

	}
}


//def_class Trigger_Swf_073 none dummy 0 {} {
//	# &085 #
//	def_event evt_timer0
//	call scripts/classes/story/sequencer.tcl

//	obj_init {

//		proc start_seq {} {
//			global sequence_a sequencescript
//			sequencer_activate
//		}

//		proc remove_fow {} {
//			 global FR
//			 set pos [get_pos this]
//			 sel /obj
//			 set FR [new FogRemover]
//			 set titanicpumpe [obj_query this -class TitanicPumpe -range 100 -limit 1]
//			 if { $titanicpumpe==0 } {
//				log "warning: Trigger_Swf_073 Titanicpumpe not found !"
//				return
//			 }
//			 set_pos $FR [vector_add [get_pos $titanicpumpe] { 0 -3 0 }]
//			 call_method $FR fog_remove 0 50 10
//			 call_method $FR timer_delete 60
//		}

//		set_selectable this 0
//		set_hoverable this 0
//		set sequence_a 0
//		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
//		call scripts/classes/story/sequencer.tcl
//	}

//	handle_event evt_timer0 {
//		set sequencescript "swf_073"
//		trigger create this any_object "remove_fow; sequencer_activate"
//		trigger set_target_range this 5
//		trigger set_target_class this Zwerg
//		trigger set_target_owner this 0
//		trigger set_target_count this 1
//	}
//}

def_class Trigger_Abspann none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {

		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+5]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		cancel_fade
		set sequencescript "st019_end_2"
		sequencer_activate
	}

	obj_exit {
		set_sequence 0
		set_view 96 56 1.31 -0.2 0.0
		scroller stop
		gametime stop
		gui_new_game 1
	}
}

def_class Trigger_Swf_095 none dummy 0 {} {
	# &085 #
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	method destroy_overload {} {
		set gender "male"
		foreach pz [lnand 0 [obj_query this -class PseudoZwerg -owner 2]] {
			call_method $pz set_gender $gender
			//log "Trigger activates pz $pz"
			call_method $pz activate
			set gender [string map {"fem" "m" "m" "fem"} $gender]
		}
		foreach dr [lnand 0 [obj_query this -class Tuer_kaserne -pos [vector_add [get_pos this] {21 48 0}] -range 60]] {
			set sw [obj_query $dr -class Hauklotz -owner 2 -limit 1]
			call_method $dr oeffnen $sw -1
		}
		ai init 2 data/scripts/ai/std_ai.tcl
		ai enable 2
		destroy 1
	}

	obj_init {

		proc start_seq {} {
			global sequence_a sequencescript
			sequencer_activate
		}

		set_selectable this 0
		set_hoverable this 0
		set sequence_a 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "swf_095"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 13
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


// CheckTrigger / checkt ob sich ein Zwerg einem Gegenstand (Kohle)nähert mit dem Ziel es aufzuheben oder
// zu verarbeiten (current_lock_obj) / startet "Swf_118"

def_class Trigger_Swf_118 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+5]
		call scripts/classes/story/sequencer.tcl

		set phase 0

		proc call_kohle {} {
			global kohleliste
			set kohleliste [obj_query this -class Kohle -range 10]
		}

		proc check_kohle {} {
			global kohleliste kohlephase andres_actuator

			// falls Knocker schon feindlich sind, ist die Sache erledigt
			if {[get_diplomacy 2 0] == "enemy"} {
				destroy_permanently
				return 0
			}

			set gnomes [obj_query this "-class Zwerg -owner 0 -range 5 -cloaked 1"]
			if {$gnomes==0} {
				return 0
			}

   			foreach g $gnomes {
   				if {[dist_between this [ref_get $g current_lock_obj]]<5} {
   					if {[get_objclass [ref_get $g current_lock_obj]] == "Kohle"} {
   						return 1
   					}
   				} elseif { [land [inv_list $g] $kohleliste ] != "" } {
   						return 1
   				}
   			}
   			return 0
		}

		proc new_trigger_enemy {} {
			trigger create this callback "change_to_enemy"
			trigger set_checktimer this 1
			trigger set_callback this "check_kohle"
		}

		proc change_to_enemy {} {
			log "Knockers are now hostile!"
			set_diplomacy 2 0 enemy
			set_diplomacy 0 2 enemy
			set nttext [lmsg Clantoenemy]
			set nttext [string map "-clanname- [lmsg clanname2]" $nttext]
			newsticker new 0 -category fight -color {255 0 0} -text $nttext -priority 5.0 -time 50
			destroy_permanently
		}

		proc new_trigger_sequence {} {
			global sequencescript

			set sequencescript "swf_118"
			trigger create this any_object "sequencer_activate"
			trigger set_target_range this 7
			trigger set_target_class this "Zwerg"
			trigger set_target_count this 1
			trigger set_target_owner this 0

		}
	}

	method destroy_overload {} {
		global phase

		log "Trigger_Swf_118 phase: $phase"
		if {$phase == 0} {
			foreach obj [obj_query this -class Zwerg -range 10 -owner 0] {
				set_event $obj evt_zwerg_break -target $obj
			}
			action this wait 1.5 {new_trigger_enemy}
		}
		incr phase
	}

	def_event evt_timer0
	handle_event evt_timer0 {
		call_kohle
		trigger create this callback "new_trigger_sequence"
		trigger set_checktimer this 1
		trigger set_callback this "check_kohle"
	}
}



def_class Trigger_Swf_119 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Swf_119"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 5
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1

	}
}


def_class Trigger_Swf_121 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Swf_121"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 5
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1

	}
}


def_class Trigger_Swf_124 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Swf_124"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 5
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1

	}
}



def_class Trigger_Swf_131 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Swf_131"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 5
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1

	}
}

def_class Trigger_swf_unq_drache_auftrag none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+20]
		call scripts/classes/story/sequencer.tcl

		proc activate_digcheck {} {
			set the_other [obj_query this -class Trigger_swf_unq_drache_graben -range 50]
			if { $the_other == 0 } {
				log "Trigger_swf_unq_drache_auftrag: Trigger_swf_unq_drache_graben isn't here anymore..."
			} else {
				call_method $the_other activate_this
				log "Trigger_swf_unq_drache_auftrag: activated: $the_other"
			}
		}
		proc check {} {

			if { [DracheAutoDestroy] } { return 0 }

			if {[obj_query this -class Zwerg -owner 0 -range 5 -limit 1 -cloaked 1]==0} {
			return 0
			}
			if {[obj_query this -class Trigger_swf_unq_drache_maschine -range 100 -limit 1]==0} {
				log "Activitated because of Trigger_swf_unq_drache_maschine"
				return 1
			}
			return 0
		}
	}

	handle_event evt_timer0 {
		set sequencescript "swf_085"
		trigger create this callback "sequencer_activate"
		trigger set_callback this "check"
		trigger set_timer this 3

	}
}


def_class Trigger_swf_unq_drache_schlafen none dummy 0 {} {
	# &085 #
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl


	obj_init {

		proc start_seq {} {
			global sequence_a sequencescript
			sequencer_activate
		}

		proc remove_fow {} {
			 global FR
			 set pos [get_pos this]
			 sel /obj
			 set FR [new FogRemover]
			 set drache [obj_query this -class Drache -range 100 -limit 1]
			 if { $drache==0 } {
				log "warning: Trigger_swf_unq_drache_schlafen Drache not found !"
				return
			 }
			 set_pos $FR [get_pos $drache]
			 call_method $FR fog_remove 0 20 10
			 call_method $FR timer_delete -1
	   }

		set_selectable this 0
		set_hoverable this 0
		set sequence_a 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}
   	handle_event evt_timer0 {
		set sequencescript "swf_092"
		trigger create this any_object "remove_fow; start_seq"
		trigger set_target_range this 3
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


def_class Trigger_swf_unq_drache_graben none dummy 0 {} {
	# &085 #
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	method destroy_overload {} {
		global digphase
		log "Trigger_swf_unq_drache_graben: old digphase: $digphase"
		if { $digphase < 2 } {
			incr digphase
			log "Trigger_swf_unq_drache_graben: new digphase: $digphase  I'll kill myself at 2"
			call_method this activate_this
		} else {
			log "Trigger_swf_unq_drache_graben: new digphase: $digphase -> byebye!"
			destroy_permanently
		}
	}

	method rausbuddel {} {
		rausbuddel
	}

	method activate_this {} {
		trigger create this callback "sequencer_activate"
		trigger set_callback this "callbackcheck"
		trigger set_checktimer this 3
		log "Trigger_swf_unq_drache_graben: have been activated!"
	}

	catch { sm_add_event Drache_angegriffen }

	obj_init {

        proc decrease_digcount {} {
        	global digcount sequencescript
 	      	if { $digcount == 10.1 } {
 	      		set sequencescript "swf_071"
 	      	} elseif { $digcount == 12 } {
         		set digcount 10.1
         		set sequencescript "swf_072"
        	} elseif { $digcount == 14.0 } {
        		set digcount 12
        		set sequencescript "swf_090"
        	}
        }


        proc rausbuddel {} {
			set dig_pos [obj_query this -class Info_Pos_Zwerg -range 50 -limit 2]
			set dig_gnome [obj_query this -class Zwerg -owner 0 -limit 1 -cloaked 1]
			if {$dig_gnome == 0} {
				log "kein zwerg auf der ganzen map, also auch kein automatisches buddeln!!!"
				return
			}
			if { [llength $dig_pos] > 1 } {
				set center {0 0 0}
				set pcnt 0
				foreach item $dig_pos {
					set pos [get_pos $item]
					set center [vector_add $center $pos]
					set x [lindex $pos 0]
					set y [lindex $pos 1]
					incr pcnt
		        	for {set i -4.4} {$i < 5} {fincr i} {
		        		set depth 1
        				for {set j 0.4} {$j < 7} {fincr j} {
        					if {$j>4} {set depth 1}
        					dig_mark 0 [expr {int($x+$i)}] [expr {int($y-$j)}] 0 $depth
        					log "DigPunkt: $item / Markierung: [expr $x+$i] / [expr $y-$j] / Tiefe: $depth"
        				}
        			}
				}
				set center [vector_mul $center 0.5]
				log "Center: $center"
				dig_resetid $dig_gnome
				set dp [dig_next $center $dig_gnome]
				log "first digpoint ($dp) $dig_gnome ($center)"
				while {[lindex $dp 0]>0} {
					log " vorher DP: $dp / DigGnome: $dig_gnome"
					dig_apply $dp $dig_gnome
					dig_resetid $dig_gnome
					set last_dp $dp
					set dp [dig_next $center $dig_gnome]
					if {$last_dp==$dp} {break}
					log " hinterher DP: $dp / DigGnome: $dig_gnome"
				}
   				dig_resetid $dig_gnome
				foreach item $dig_pos {
					set pos [get_pos $item]
					set center [vector_add $center $pos]
					set x [lindex $pos 0]
					set y [lindex $pos 1]
					incr pcnt
		        	for {set i -3.4} {$i < 4} {fincr i} {
		        		set depth 0
        				for {set j 0.4} {$j < 8} {fincr j} {
        					if {$j>4} {set depth 1}
        					dig_mark 0 [expr {int($x+$i)}] [expr {int($y-$j)}] 2 $depth
        					log "DigPunkt: $item / Markierung: [expr $x+$i] / [expr $y-$j] / Tiefe: $depth"
        				}
        			}
				}
        	} else {
				log "Trigger_swf_unq_drache_graben: warning no 'Info_Drache_Digpoint' found !"
	       	}
        }

		proc callbackcheck {} {
			global digpoints xn xp yn yp digready digcount sequencescript digmarked caveskin
			if { [sm_get_event Drache_angegriffen] } {
				sm_send_message this "kill"
				trigger delete this
				destroy_permanently
				return
			}
			if { $digmarked==0 } {
				set dp [obj_query this "-class Info_Drache_Digpoint -range 35"]
				if { $dp == 0 } {
					log "Trigger_swf_unq_drache_graben: warning no 'Info_Drache_Digpoint' found !"
					destroy_permanently
					return
				}
				set digpoints 1
				foreach item $dp {
					set pos [get_pos $item]
					set x [lindex $pos 0]
					set y [lindex $pos 1]
					set xp [hmax $x $xp]
					set yp [hmax $y $yp]
					set xn [hmin $x $xn]
					set yn [hmin $y $yn]
				}
				if {[is_dig_marked [expr $xn - 4] [expr $yn - 4] [expr $xp + 4] [expr $yp + 4]]} {
					set digmarked 1
					log "=====> digmarked gesetzt auf 1"
				} else {
					log "=====> noch keine digmarkierung gesetzt..."
					return 0
				}
			}

			if { $digmarked==1 } {
				set zl [obj_query this "-class Zwerg -range 20 -limit 1 -cloaked 1"]
				if { $zl == 0 } {
					return
				}
				if { $digready == 0 } {
					if { $digpoints == 0 } {
						set dp [obj_query this "-class Info_Drache_Digpoint -range 35"]
						if { $dp == 0 } {
							log "Trigger_swf_unq_drache_graben: warning no 'Info_Drache_Digpoint' found !"
							destroy_permanently
							return
						}
						set digpoints 1
						foreach item $dp {
							set pos [get_pos $item]
							set x [lindex $pos 0]
							set y [lindex $pos 1]
							set xp [hmax $x $xp]
							set yp [hmax $y $yp]
							set xn [hmin $x $xn]
							set yn [hmin $y $yn]
							del $item
						}
					}
					set volume 0
					set cnt 0
					for {set i [hf2i $xn]} {$i < [hf2i $xp]} {incr i} {
						for {set j [hf2i $yn]} {$j < [hf2i $yp]} {incr j} {
							set h [get_hmap $i $j]
							incr volume $h
							incr cnt
						}
					}
					if { [expr $volume / $cnt.0001] < $digcount } {
						if { $digcount == 10.1 } {
							set digready 1
						}
						log "digready!!!!!! -----------------> digcount: $digcount"
						decrease_digcount
						if { $caveskin==0} {
							set dig_pos [obj_query this -class Info_Pos_Zwerg -range 35 -limit 2]
							if { [llength $dig_pos] > 1 } {
								set center {0 0 0}
								foreach item $dig_pos {
									set pos [get_pos $item]
									set center [vector_add $center $pos]
								}
								set caveskin 1
								set center [vector_mul $center 0.5]
								set xcs [lindex $center 0]
								set ycs [lindex $center 1]
								cave_skin rem $xcs $ycs
							}
						}
						log "--------------------------------> neuer digcount: $digcount <------------------------------------> neues sequenzscript: $sequencescript"
						if { $sequencescript == "swf_090" || $sequencescript == "swf_072" } {
							return 1
						}
					} else {
						log "Vol: [expr $volume / $cnt.0]"
					}
				} else {
					if { [is_dig_marked [expr $xn - 4] [expr $yn - 4] [expr $xp + 4] [expr $yp + 4]] } {
						log "digmark"
					} else {
						log "no digmark"
						set sequencescript "swf_071"
						return 1
					}
				}
				return 0
			} else {
				return 0
			}
		}

		set_selectable this 0
		set_hoverable this 0
		set sequence_a 0
		set digcount 14.0
		set digphase 0
		set digpoints 0
		set caveskin 0
		set xn 1111111111
		set xp 0
		set yn 1111111111
		set yp 0
		set digmarked 0
		set sequencescript "swf_090"
		set digready 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
	}
}


def_class Trigger_urw_unq_schwefel none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Urw_045"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 7
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1

	}
}

def_class Trigger_swf_unq_drache_maschine none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		proc activate_seq {} {
			//if { [DracheAutoDestroy] } { return 0 }
			sequencer_activate
		}
	}

	handle_event evt_timer0 {
		set sequencescript "swf_090b"
		trigger create this any_object "activate_seq"
		trigger set_target_range this 8
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1

	}
}


def_class Trigger_Swf_unq_titanic_pumpe none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {25.0 0.0 0.0}]
			set_posz $FR 14
			call_method $FR fog_remove 0 20 5
			call_method $FR timer_delete 20
			foreach t [obj_query this -class WasserabsaugTitanic -range 2000] {call_method $t start -1}

	   }
	}
	handle_event evt_timer0 {
		trigger create this any_object "remove_fow; sequencer_activate"
		set sequencescript "Swf_Unq_Tit_PumpeD"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}



def_class Trigger_swf_unq_Pumpitup none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+20]
		call scripts/classes/story/sequencer.tcl
		proc check {} {

			if { [DracheAutoDestroy] } { return 0 }

			if {[obj_query this -class Zwerg -owner 0 -range 7 -limit 1 -cloaked 1]==0} {
			return 0
			}
			if {[obj_query this -class Trigger_Swf_073 -range 100 -limit 1]==0} {
			log "Activitated because of Trigger_Swf_073"
				return 1
			}
			return 0
		}

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {25.0 0.0 0.0}]
			set_posz $FR 14
			call_method $FR fog_remove 0 20 5
			call_method $FR timer_delete 20
	 		foreach t [obj_query this -class WasserabsaugTitanic -range 2000] {call_method $t start -1}
			set_owner [obj_query 0 -class TitanicPumpe] -1
	   }

	}

	handle_event evt_timer0 {
		set sequencescript "swf_068"
		trigger create this callback "remove_fow; sequencer_activate"
		trigger set_callback this "check"
		trigger set_timer this 3

	}
}
def_class Trigger_Pumpentest none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "swf_068"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 7
		trigger set_target_class this "Zwerg"

	}
}



def_class Trigger_Swf_unq_bruecke none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	method destroy_overload {} {
		global phase

		if { $phase < 3} {
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
		} else {
			destroy 1
		}
	}

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		set phase 0
		set metlist [list]
		set woodlist [list]
		set met 0
		set wood 0

		proc remove_fow {} {
			 global FR
			 set pos [get_pos this]
			 sel /obj
			 set FR [new FogRemover]
			 set_pos $FR [vector_add $pos {20 -5 0}]
			 call_method $FR fog_remove 0 23 80
			 set FR2 [new FogRemover]
			 set_pos $FR2 [vector_add $pos {19 -5 0}]
			 call_method $FR2 fog_remove 0 23 80
			 #set_pos $FR [vector_add $pos {30 -20 0}]
			 #call_method $FR fog_remove 0 40 60
			 call_method $FR timer_delete 60
		}

		proc find_all {classname count} {
			set zl 		[lnand 0 [obj_query this -class Zwerg -range 12 -cloaked 1]]
			set olist 	[lnand 0 [obj_query this -class $classname -range 12 -cloaked 1]]

			while { [llength $olist] > $count } {
				lrem olist 0
			}
			set objcnt [llength $olist]

			foreach item $zl {
				set invlist [inv_list $item]
				foreach invitem $invlist {
					if { [llength $olist] >= $count } { break }
					if { [get_objclass $invitem] == $classname } {
						set olist [lor $olist [lnand $olist $invitem]]
					}
				}
			}
			return $olist
		}

		// sorgt dafür, das diese items liegenbleiben und nicht mehr mitgenommen werden können
		// falls die items im Inventory eines Zwerges sind, werden sie auf den Boden gelegt
		proc lock_all {itemlist} {
			set zl 		[lnand 0 [obj_query this -class Zwerg -range 12 -cloaked 1]]

			// zuerst alles fallen lassen
			foreach gnome $zl {
				set invlist [inv_list $gnome]
				foreach invitem $invlist {
					if {[lsearch $itemlist $invitem] >= 0} {
						inv_rem $gnome $invitem
						set_posbottom $invitem [vector_add [get_pos $gnome] "[random -0.3 0.3] 0 [random -1 1]"]
					}
				}
			}

			foreach item $itemlist {
				set_hoverable $item 0
				set_lock $item 1
				set_prodalloclock $item 1
			}
		}

		proc check {} {
			global metlist met woodlist wood phase
			set zl [lnand 0 [obj_query this -class Zwerg -range 12 -cloaked 1]]
			if { [llength $zl] < 1 } {
				 return 0
			} else {
				set woodlist [find_all "Pilzstamm" 15]
				set wood [llength $woodlist]

				set metlist [find_all "Eisen" 10]
				set met [llength $metlist]

				log "checking... phase is $phase"

				if { $met >= 10  &&  $phase > 1} {
					lock_all $metlist
					return 3
				} elseif { $wood >= 15  &&  $phase > 0} {
					lock_all $woodlist
					return 2
				} else {
					return 1
				}
			}
		}

		proc restart {} {
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+6]
		}

		proc start_seq {} {
			global sequencescript metlist met phase woodlist wood phase
			set ch [check]

			if { $ch > $phase } {
				set phase $ch

				remove_fow
				set sequencescript "swf_70$ch"
				sequencer_activate
				if { $ch == 1 } {
					sm_send_message this "Pilzstaemme"
				} elseif { $ch == 2 } {
					sm_send_message this "Eisen"
				} elseif { $ch == 3 } {
					sm_send_message this "Umzug"
				}

			} else {
				restart
			}
		}


		proc rem_eisen {} {
			global metlist
			foreach item $metlist {
				del $item
			}
		}

		proc rem_material {} {
			global metlist
			global woodlist
			foreach item $metlist {
				del $item
			}
			foreach item $woodlist {
				del $item
			}
		}
	}

	handle_event evt_timer0 {
		trigger create this any_object "start_seq"
		set sequencescript "swf_701"
		trigger set_target_range this 7
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


#Zeittrigger, wird ausgelöst wenn alle Wiggles über Schwefelbruecke sind
def_class Trigger_swf_704_Bruecke_Einsturz none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		call scripts/classes/story/sequencer.tcl
		trigger create this any_object "sequencer_activate"
		set sequencescript "swf_704"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
    }

	method destroy_overload {} {
		set bruecke [obj_query this -class Schwefelbruecke]
		if {$bruecke > 0} {
			call_method $bruecke set_destroyed
		}
		destroy 1
	}
}


def_class Trigger_swf_8102_elfe_warnt_a none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+5] ;#Durch echten Countdown wert ersetzen
		call scripts/classes/story/sequencer.tcl
    }

	handle_event evt_timer0 {
		trigger create this any_object "sequencer_activate"
		set sequencescript "swf_8102"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


def_class Trigger_swf_8103_elfe_warnt_b none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+5] ;#Durch echten Countdown wert ersetzen
		call scripts/classes/story/sequencer.tcl
    }

	handle_event evt_timer0 {
		trigger create this any_object "sequencer_activate"
		set sequencescript "swf_8103"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}

def_class Trigger_Swf_116 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+20]
		call scripts/classes/story/sequencer.tcl
		proc check {} {

			if { [DracheAutoDestroy] } { return 0 }

			if {[obj_query this -class Zwerg -owner 0 -range 8  -limit 1 -cloaked 1]==0} {
			return 0
			}
			if {[obj_query this -class Trigger_Swf_065 -range 100 -limit 1]==0} {
				log "Activitated because of Trigger_Swf_065"
				return 1
			}
			if {[obj_query this -class Trigger_swf_unq_Pumpitup -range 100 -limit 1]==0} {
				log "Activitated because of Trigger_swf_unq_Pumpitup"
				return 1
			}
			return 0
		}
	}

	handle_event evt_timer0 {
		set sequencescript "swf_116"
		trigger create this callback "sequencer_activate"
		trigger set_callback this "check"
		trigger set_timer this 3

	}
}

def_class Trigger_swf_unq_eierbecher none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+20]
		call scripts/classes/story/sequencer.tcl

		set ei ""

		proc find_all {classname count} {;#range-Werte geändert von 12 auf 5 - Sequenzerdaniel wars
			set zl [lnand 0 [obj_query this -class Zwerg -range 5 -cloaked 1]]
			set olist [lnand 0 [obj_query this -class $classname -range 5]]
			while { [llength $olist] > $count } { lrem olist 0 }
			set objcnt [llength $olist]
			foreach item $zl {
				set invlist [inv_list $item]
				foreach invitem $invlist {
					if { [llength $olist] >= $count } { break }
					if { [get_objclass $invitem] == $classname } {
						set olist [lor $olist [lnand $olist $invitem]]
					}
				}
			}
			return $olist
		}

		proc check {} {
			global ei
			set ei [find_all Drachen_Ei 1]
			set zw [find_all Zwerg 1]
			return [expr ([llength $ei] & [llength $zw])]
		}

		proc del_ei {} {
			global ei
			log "del $ei"
			del $ei
		}

	}

	handle_event evt_timer0 {
		set_owner this 0
		set sequencescript "swf_050"
		trigger create this callback "del_ei;sequencer_activate"
		trigger set_callback this "check"
		trigger set_timer this 3

	}
}


