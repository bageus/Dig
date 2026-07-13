// Trigger für die Lavawelt

def_class Trigger_Lava_170 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
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
			set_pos $FR [vector_add $pos {-10.0 0.0 0.0}]
			set_posz $FR 14
			call_method $FR fog_remove 0 20 12
			call_method $FR timer_delete 20
		}

		proc get_sequencescript {} {
			set baby_ei [obj_query 0 -class {Drachenbaby Drachen_Ei}]

			if { [sm_get_event Drache_angegriffen] || $baby_ei == 0} {
				return "lav_170b"
			}
			return "lav_170"
		}
	}

	handle_event evt_timer0 {

		set sequencescript [get_sequencescript]
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 8
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
	}
}


def_class Trigger_Walhalla none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+2]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {

		set_fow_begin 2000
		set_light_begin 2000
		set_ground_begin 0
		set sequencescript "unq_ende"
		sequencer_activate
	}

	obj_exit {
		log "Abspann gestartet ([get_ref this])!"

		set ofsx [lindex [map getoffset] 0]
		set ofsy [lindex [map getoffset] 1]

       	new Trigger_Abspann "" "[expr $ofsx + 26.0] [expr $ofsy + 180.5] 11.0" "0 0 0"
	}
}

def_class Trigger_Lava_175c none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+20]
		call scripts/classes/story/sequencer.tcl
		proc check {} {
			if {[obj_query this -class Zwerg -owner 0 -range 3  -limit 1 -cloaked 1]==0} {
			return 0
			}
			if {[obj_query this -class Drache -range 100 -limit 1]==0} {
				log "Activitated because of Mama abgemetzelt"
				return 1
			}
			return 0
		}
	}

	handle_event evt_timer0 {
		set sequencescript "lav_175c"
		trigger create this callback "sequencer_activate"
		trigger set_callback this "check"
		trigger set_timer this 3

	}
}




def_class Trigger_Lava_175_220_Drachenbaby none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		proc check {} {
			global sequencescript

			set gnomes [obj_query this "-class Zwerg -owner 0 -range 5 -cloaked 1"]
			if {$gnomes == 0} {
				return 0
			}

			set baby [obj_query this "-class Drachenbaby -owner 0"]
			if {$baby != 0} {
				foreach obj $baby {
					set_selectable $obj 0
					set_owner $obj 7
					if {[get_selectedobject] == $baby} {
						set_selectedobject 0
					}
				}
			}

			if {$baby != 0} {
				log "Drachenbaby abgeliefert!"
				set mama [obj_query this "-class Drache -range 15 -limit 1"]
				if {$mama != 0} {
					call_method $mama delete_collisionboxes
					set_diplomacy 0 7 ally
					set_diplomacy 7 0 ally
				}
				set sequencescript "lav_175"
				return 1
			}


			foreach gnome $gnomes {
				if {[inv_find $gnome Drachen_Ei] >= 0} {
					log "Drachenei abgeliefert!"
					set idx [inv_find $gnome Drachen_Ei]
					set obj [inv_get $gnome $idx]
					inv_rem $gnome $idx
					del $obj

					set mama [obj_query this "-class Drache -range 15 -limit 1"]
					if {$mama != 0} {
						call_method $mama delete_collisionboxes
						set_diplomacy 0 7 ally
						set_diplomacy 7 0 ally
					}

					set sequencescript "lav_175b"
					return 1
				}
			}

			set drachenei [obj_query 0 "-class Drachen_Ei"]
			if { [sm_get_event Drachenbaby_tot]  ||
				 [sm_get_event Drache_angegriffen] ||
				 $drachenei == 0} {
				//Wenn Drachenbaby tot ist oder Kein Ei existiert, .. muss man kämpfen
				set_diplomacy 0 7 enemy
				set_diplomacy 7 0 enemy
				destroy 1
			}
            return 0
		}
	}

	handle_event evt_timer0 {
		set sequencescript ""
		trigger create this callback "sequencer_activate"
		trigger set_timer this 4
		trigger set_callback this "check"
	}
}

def_class Trigger_lav_177 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0

	method destroy_overload {} {
		set gender "male"
		foreach pz [lnand 0 [obj_query this -class PseudoZwerg -owner 4]] {
			call_method $pz set_gender $gender
			//log "Trigger activates pz $pz"
			call_method $pz activate
			set gender [string map {"fem" "m" "m" "fem"} $gender]
		}
		foreach dr [lnand 0 [obj_query this -class Tuer_kaserne -pos [vector_add [get_pos this] {50 100 0}] -range 100 -limit 3]] {
			set sw [obj_query $dr -class Feuerstelle -owner 4 -limit 1]
			call_method $dr oeffnen $sw -1
		}
		foreach lb [lnand 0 [obj_query this -class [string map {ix ab} Lixor] -owner 4]] {
			sel /obj
			set objhdl [new Farm "" [get_pos $lb] {0 0 0}]
			set_owner $objhdl 4
			set_physic $objhdl 1
			del $lb
		}
		set info_fogs [obj_query this -class Info_Fog_Aufdecker -range 200]
		if {$info_fogs != 0} {
			foreach item $info_fogs {
				call_method $item activate
			}
		}
		ai init 4 data/scripts/ai/std_ai.tcl
		ai enable 4
		set_diplomacy 0 4 enemy
		set_diplomacy 4 0 enemy
    	destroy_permanently
	}
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
		proc elfenarbeit1 {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {10 -7 3}]
			call_method $FR fog_remove 0 10 10
			call_method $FR timer_delete 2
        	}
		proc elfenarbeit2 {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {16 -1 0}]
			call_method $FR fog_remove 0 15 15
			call_method $FR timer_delete 2
        	}

	}

	handle_event evt_timer0 {
		set sequencescript "lav_177"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 4
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1

	}
}

def_class Trigger_lav_183 none dummy 0 {} {
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
			set FR [new FogRemover]; #eine Stelle wo der Nebel weggeht
			set_pos $FR [vector_add $pos {36 -35 -5}]
			call_method $FR fog_remove 0 30 30
			set_pos $FR [vector_add $pos {38 -15 0}]
			call_method $FR fog_remove 0 30 30
			call_method $FR timer_delete 25
		}

	}

	handle_event evt_timer0 {
		set sequencescript "lav_183"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 3
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
	}
}

def_class Trigger_Lava_250 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0

	method destroy_overload {} {
		global warn_count found
		if {$found == 1} {
			destroy 1
		} else {
			incr warn_count
		    timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
		  }



	}
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		set warn_count 1
 		set hint 0
		set found 0

		proc generate_pos {pos} {
			set place [get_place -center $pos -circle 3 -random 8]
			if {[lindex $place 0]>0} {
				return $place
			}
			return $pos
		}


        proc check_gleipnir {} {
	        global found sequencescript;
	        if {[obj_query this -class Zwerg -range 6 -cloaked 1] != 0} {
	        	log "Dwarf found"
	            set gl [obj_query this -class Zwerg -range 12 -cloaked 1]
	        	foreach g $gl {
	        		if {[inv_find $g Gleipnir] != -1 } {
	        			log "Gleipnir found"
	        			set found 1; log "Found set to 1"
	        			set warn_count 0
		                set sequencescript "lav_253"; log "Sequence set to 253"
						return 1

	        	    } else {
	        	    	// Kein Gleipnir -> Zwerge zurückbeamen !
	        	    	set zl [lnand 0 [obj_query this -class Zwerg -range 10 -cloaked 1]]
	        	    	set bFirst 1
	        	    	set vDest [vector_add [get_pos this] {-11 0 +4}]
	        	    	foreach item $zl {
	        	    		if { !$bFirst } {
	        	    			// Actions breaken
	        	    			action $item wait 2 "state_enable $item"
	        	    			set_pos $item [generate_pos $vDest]
	        	    		}
	        	    		set bFirst 0
	        	    	}
	        	    	return 1
	        	    }
	        	}
        	} else {
        		return 0
        	}
        }
	}

	handle_event evt_timer0 {
		global found sequencescript
    	if {$found == 0} {
    		if {$warn_count == 1} {
    			set sequencescript "lav_250"; log "Sequnce set to 250"
    		} elseif {$warn_count == 2} {
	    		set sequencescript "lav_251"; log "Sequnce set to 251"
    		} elseif {$warn_count > 2} {
	    		set sequencescript "lav_252"; log "Sequnce set to 252"
    		}
		}
		if {$found == 1} {
			set sequencescript "lav_253"; log "Sequnce set to 253"
		}
		trigger create this callback "sequencer_activate"
		trigger set_callback this "check_gleipnir"
		trigger set_checktimer this 1

		}
}


def_class Trigger_Lava_300_Feuerring none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	method destroy_overload {} {
		set the_other [obj_query this -class Trigger_Fenris_300a -limit 1]
		if {$the_other!=0 } {
			call_method $the_other activate_fenris_300a
			destroy_permanently
		} else {
			log "Kein Fenristrigger 300a weit und breit... ich wette, Christoph ist schuld..."
			cancel_fade
			destroy_permanently
		}
	}

	obj_init {
		set_anim this trigger_fahne3.standard 0 0
		set_selectable this 0
		set_hoverable this 0
		set firering -1

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		proc check {} {
			global sequencescript firering mypos

			if {$firering <= 0} {
				return 0
			}

			if {[vector_dist [get_pos $firering] $mypos] > 5.0} {
				return 1
			}

			return 0
		}
	}

	handle_event evt_timer0 {
		set sequencescript "lav_300"
		set mypos [get_pos this]
		set firering [obj_query this -range 10 -class Ring_Des_Feuers]
		trigger create this callback "sequencer_activate"
		trigger set_timer this 4
		trigger set_callback this "check"
	}
}

def_class Trigger_Fenris_300a none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {0 -3 0}]
			call_method $FR fog_remove 0 30 30
			call_method $FR timer_delete -1
		}

		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	method activate_fenris_300a {} {
		set sequencescript "fenris_300a"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 200.0
		trigger set_target_class this Fenris
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}

	handle_event evt_timer0 {
	}
}


// Trigger: FoW-Remover für Kathedrale
def_class Trigger_Fog_Remover_Kathedrale none dummy 0 {} {
	obj_init {
		set_anim this trigger_fahne3.standard 0 0
		set_selectable this 0
		set_hoverable this 0
		if {![get_mapedit]} {
			set_visibility this 0
		} else {
			set_visibility this 1
		}
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		proc remove_fow {} {
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {-20 -12 0}]
			call_method $FR fog_remove 0 -20 -15
			del this
		}
    }

	def_event evt_timer0
	handle_event evt_timer0 {
		trigger create this any_object "remove_fow"
		trigger set_target_range this 6
		trigger set_target_class this "Zwerg"

	}
}




// Trigger: FoW-Remover für die Brücke vor der Kathedrale
def_class Trigger_Fog_Remover_KathedraleBruecke none dummy 0 {} {
	obj_init {
		set_anim this trigger_fahne3.standard 0 0
		set_selectable this 0
		set_hoverable this 0
		if {![get_mapedit]} {
			set_visibility this 0
		} else {
			set_visibility this 1
		}
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		proc remove_fow {} {
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add [get_pos this] {-23 0 0}]
			call_method $FR fog_remove 0 30 30
//			call_method $FR timer_delete 180
			del this
		}
    }

	def_event evt_timer0
	handle_event evt_timer0 {
		trigger create this any_object "remove_fow"
		trigger set_target_range this 6
		trigger set_target_class this "Zwerg"

	}
}



def_class Trigger_Lava_450 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {

		set sequencescript "lav_450"
		sequencer_activate
	}
}


def_class Trigger_Lava_340 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			set stuhl [obj_query this -class Fenris_Stuhl -limit 1]
			if {$stuhl > 0} {
				set pos [get_pos $stuhl]
				sel /obj
				set FR [new FogRemover]
				set_pos $FR [vector_add $pos {0 -2 0}]
				call_method $FR fog_remove 0 10 10
				call_method $FR timer_delete 10
			}
		}


	}

	handle_event evt_timer0 {
		set sequencescript "fenris_340"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 100.0
		trigger set_target_class this Fenris
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}
}

def_class Trigger_Lava_350 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			set stuhl [obj_query this -class Fenris_Stuhl -limit 1]
			if {$stuhl > 0} {
				set pos [get_pos $stuhl]
				sel /obj
				set FR [new FogRemover]
				set_pos $FR [vector_add $pos {0 -2 0}]
				call_method $FR fog_remove 0 10 10
				call_method $FR timer_delete 10
			}
		}

	}


	handle_event evt_timer0 {
		set sequencescript "fenris_350"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 100.0
		trigger set_target_class this Fenris
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}
}

def_class Trigger_Lava_360 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			set stuhl [obj_query this -class Fenris_Stuhl -limit 1]
			if {$stuhl > 0} {
				set pos [get_pos $stuhl]
				sel /obj
				set FR [new FogRemover]
				set_pos $FR [vector_add $pos {0 -2 0}]
				call_method $FR fog_remove 0 10 10
				call_method $FR timer_delete 10
			}
		}

	}

	handle_event evt_timer0 {
		set sequencescript "fenris_360"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 100.0
		trigger set_target_class this Fenris
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}
}

def_class Trigger_Lava_370 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			set stuhl [obj_query this -class Fenris_Stuhl -limit 1]
			if {$stuhl > 0} {
				set pos [get_pos $stuhl]
				sel /obj
				set FR [new FogRemover]
				set_pos $FR [vector_add $pos {0 -2 0}]
				call_method $FR fog_remove 0 10 10
				call_method $FR timer_delete 10

			}
		}

	}

	handle_event evt_timer0 {
		set sequencescript "fenris_370"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 100.0
		trigger set_target_class this Fenris
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}
}


def_class Trigger_Lava_400a none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			set stuhl [obj_query this -class Fenris_Stuhl -limit 1]
			if {$stuhl > 0} {
				set pos [get_pos $stuhl]
				sel /obj
				set FR [new FogRemover]
				set_pos $FR [vector_add $pos {0 -2 0}]
				call_method $FR fog_remove 0 20 20
				call_method $FR timer_delete 10

			}
		}

	}

	handle_event evt_timer0 {
		set sequencescript "fenris_400a"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 10.0
		trigger set_target_class this Fenris
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}

	obj_exit {
		set i [new Trigger_Lava_450]
		set_pos $i [get_pos this]

		set trolllist [obj_query this -class Troll]
		if {$trolllist != 0} {
			foreach obj $trolllist {
				call_method $obj destroy
			}
		}
		set gleipnir [obj_query this -class Gleipnir]
		if {$gleipnir != 0} {
			foreach obj $gleipnir {
				del $obj
			}
		}
	}
}


def_class Trigger_Lava_400b none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			set stuhl [obj_query this -class Fenris_Stuhl -limit 1]
			if {$stuhl > 0} {
				set pos [get_pos $stuhl]
				sel /obj
				set FR [new FogRemover]
				set_pos $FR [vector_add $pos {0 -2 0}]
				call_method $FR fog_remove 0 20 20
				call_method $FR timer_delete 10

			}
		}

	}

	handle_event evt_timer0 {
		set sequencescript "fenris_400b"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 10.0
		trigger set_target_class this Fenris
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}

	obj_exit {
		set i [new Trigger_Lava_450]
		set_pos $i [get_pos this]

		set trolllist [obj_query this -class Troll]
		if {$trolllist != 0} {
			foreach obj $trolllist {
				call_method $obj destroy
			}
		}
		set gleipnir [obj_query this -class Gleipnir]
		if {$gleipnir != 0} {
			foreach obj $gleipnir {
				del $obj
			}
		}		
	}
}


// Trigger für die Sequenz "Superelfe ist besiegt"
// Dieser Trigger löscht die Riesenelfe, falls es noch eine gibt

def_class Trigger_Extro_470 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		log "Trigger_Extro_470 starting ([get_ref this])"
		set sequencescript "extro_470"
		set elfe [obj_query this "-class Riesenelfe -limit 1"]
		if {$elfe > 0} {
			call_method $elfe destroy
		}
		catch {sequencer_activate}
	}

	obj_exit {
		// GameOver darf ab jetzt nicht mehr passieren... schliesslich haben wir gleich keine Zwerge mehr
	   	sm_set_event GameOverCheck 0

		log "setting templates... ([get_ref this])"
       	start_fade 1 0

		set ofsx [lindex [map getoffset] 0]
		set ofsy [lindex [map getoffset] 1]

       	reset_map [expr $ofsx + 0] [expr $ofsy + 0] [expr $ofsx + 10000] [expr $ofsy + 10000]

		call templates/unq_ende_seq.tcl
		MapTemplateSet [expr $ofsx + 16] [expr $ofsy + 16]
		start_fade 0.1 0
		call templates/unq_menue.tcl
		MapTemplateSet [expr $ofsx + 16] [expr $ofsy + 166]

       	set_fow_begin [expr $ofsy + 200]
		start_fade 0.1 0
		//set_view
		log "setting templates finished ([get_ref this])"
 	}
}


// Amboss: noch nicht alle Ringe da!
def_class Trigger_Lava_Amboss_190 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

	}

	handle_event evt_timer0 {
		set sequencescript "lav_190"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 4
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
		trigger set_target_owner this 0
	}
}


// Amboss: keine Lava da!
def_class Trigger_Lava_Amboss_195 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

	}

	handle_event evt_timer0 {
		set sequencescript "lav_195"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 4
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
		trigger set_target_owner this 0
	}
}


// Amboss: alles ist OK, die Zwerge beginnen zu schmieden
def_class Trigger_Lava_Amboss_200 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl


		proc run_seqs {} {
			global sequencescript

			set sequencescript "lav_200"
			catch {sequencer_activate}
		}
	}

	handle_event evt_timer0 {
		trigger create this any_object "run_seqs"
		trigger set_target_range this 20
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
		trigger set_target_owner this 0
	}

	method destroy_overload {} {
		set dr [obj_query this -class Tuer_kaserne -range 100 -limit 1]
		if {$dr} {
			call_method $dr oeffnen [get_ref this] -1
		}
		set i [obj_query this "-class Trigger_Fenris_200a -limit 1"]
		if {$i > 0} {
			call_method $i activate_fenris_200a
		} else {
		log "Kein Fenristrigger 200a weit und breit... ich wette, Christoph ist schuld..."
			cancel_fade
		}
    	destroy_permanently
   	}

	obj_exit {
		set gleipnir [new Gleipnir]
		set_owner $gleipnir 0
		set zwerg [obj_query this "-class Zwerg -owner 0 -range 20 -limit 1"]
		if {$zwerg} {
			if {[inv_check $zwerg $gleipnir]} {
				inv_add $zwerg $gleipnir
			} else {
				set_posbottom $gleipnir [get_pos $zwerg]
			}
		} else {
			set_posbottom $gleipnir [get_pos this]
		}
	}
}


// Fenris_Seq, nachdem Gleipnir geschmiedet
def_class Trigger_Fenris_200a none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {0 -3 0}]
			call_method $FR fog_remove 0 30 30
			call_method $FR timer_delete -1
		}

		set_selectable this 0
		set_hoverable this 0
		call scripts/classes/story/sequencer.tcl
	}

	method activate_fenris_200a {} {
		set sequencescript "fenris_200a"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 200.0
		trigger set_target_class this Fenris
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}
}
