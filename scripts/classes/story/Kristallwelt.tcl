// Klassen für das Bilderrätsel in der Kristallwelt

// virtueller Bilderhaken für das Bilderrätsel
// ACHTUNG: Diese Klasse muss VOR den Bildern definiert sein!!!

def_class Bild_Positionsmarke wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_dummy.standard

	method snapping_action {user item} {
		global snapped_obj

		tasklist_add $user "walk_pos \{[vector_add [get_pos this] {0 0 -1}]\}"
		tasklist_add $user "rotate_toback"
		tasklist_add $user "play_anim putjump"
		tasklist_add $user "inv_rem $user [inv_find_obj $user $item]; call_method [get_ref this] snap_obj $item"
	}


	method snap_obj {obj} {
		global snapped_obj

		if {[obj_valid $obj]} {
			set snapped_obj $obj
			link_obj $obj [get_ref this] 0;
			set_pos $obj [vector_add [get_pos this] [get_linkpos this 0]]
		} else {
			set snapped_obj -1
		}
	}


	method get_snapped_obj {} {
		global snapped_obj

		// genau überprüfen, da Objekt inzwischen evtl. entfernt worden ist!
		if {$snapped_obj != -1} {
			if {![obj_valid $snapped_obj]  ||  [get_linked_to $snapped_obj] != [get_ref this]} {
				set snapped_obj -1
			}
		}
		return $snapped_obj
	}


	// Init-Timer: Bilder in der Nähe einfangen und anhängen
	def_event evt_timer0
	handle_event evt_timer0 {
		set pic [obj_query this "-class {Bild1 Bild2 Bild3 Bild4 Bild5 Bild6 Bild7 Bild8} -range 1.0 -limit 1"]
		if {$pic != 0  &&  [obj_valid $pic]} {
			call_method this snap_obj $pic
		}
	}


	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_dummy.standard 0 $ANIM_LOOP
		set_physic this 0
		set_visibility this 1
		set_hoverable this 0

		set snapped_obj -1
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
	}
}


def_class Bild1 wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_a.standard
	class_objsnapclass Bild_Positionsmarke 1.5

	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_a.standard 0 $ANIM_LOOP

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Bild2 wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_b.standard
	class_objsnapclass Bild_Positionsmarke 1.5

	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_b.standard 0 $ANIM_LOOP

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Bild3 wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_c.standard
	class_objsnapclass Bild_Positionsmarke 1.5

	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_c.standard 0 $ANIM_LOOP

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Bild4 wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_d.standard
	class_objsnapclass Bild_Positionsmarke 1.5

	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_d.standard 0 $ANIM_LOOP

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Bild5 wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_e.standard
	class_objsnapclass Bild_Positionsmarke 1.5

	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_e.standard 0 $ANIM_LOOP

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Bild6 wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_f.standard
	class_objsnapclass Bild_Positionsmarke 1.5

	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_f.standard 0 $ANIM_LOOP

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Bild7 wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_g.standard
	class_objsnapclass Bild_Positionsmarke 1.5

	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_g.standard 0 $ANIM_LOOP

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Bild8 wood tool 1 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim kris_bilderrahmen_h.standard
	class_objsnapclass Bild_Positionsmarke 1.5

	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_bilderrahmen_h.standard 0 $ANIM_LOOP

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}

// Klassen für das Labor der Brains in der Kristallwelt

def_class Riesenlaufrad wood monster 2 {} {
	call scripts/misc/autodef.tcl

	def_event	 evt_timer_init
	handle_event evt_timer_init {
		set rh [obj_query this "-class Riesenhamster -limit 1"]
		if {$rh == 0} {
			log "Class Riesenlaufrad: Riesenhamster nicht gefunden... versuche in 1 sek. nochmal"
			timer_event this evt_timer_init -repeat 1 -interval 1 -userid 0 -attime [expr {[gettime] +1}]
			return
		}

		call_method $rh put_in_wheel [get_ref this]
		set_anim this kris_riesenlaufrad.drehen 0 $ANIM_LOOP

		set_attrib this atr_Hitpoints 1.0
		state_triggerfresh this idle
	}


	method destroy {} {
		set_anim this kris_riesenlaufrad.zerstoert 0 $ANIM_LOOP
		set_attrib this atr_Hitpoints 0.0
		set_hoverable this 0
		set_collision this 0
		set_particlesource this 0 1
		set_particlesource this 1 1
		set_particlesource this 2 1

		set rh [obj_query this "-class Riesenhamster -limit 1 -range 3.0"]
		if {$rh != 0} {
			call_method $rh free [get_ref this]
		}
	}

	// virtual
	method im_attacking_you {} {}

	method check_first_strike {caller} {
		return 1
	}

	state idle {
//		set fresult [fight_setactions_strikeback this 2.2]
		log "Riesenlaufrad: I have [get_attrib this atr_Hitpoints] Hitpoints left"
		if {[get_attrib this atr_Hitpoints] <= 0.05} {
			log "Riesenlaufrad has been destroyed"
			call_method this destroy
		}
		state_disable this
	}


	set_class_anim 		still		kris_riesenlaufrad.standard
	set_class_anim		turn		kris_riesenlaufrad.drehen
	set_class_anim		destroyed	kris_riesenlaufrad.zerstoert
	class_defaultanim	kris_riesenlaufrad.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this kris_riesenlaufrad.standard 0 $ANIM_STILL

		change_particlesource this 0 6 { -1 0.5 3 } { 0 0 0 } 100 2 0 0 5
		change_particlesource this 1 6 { 0 0.5 3 } { 0 0 0 } 100 10 0 0 5
		change_particlesource this 2 3 { 0 0.5 2 } { 0 0 0 } 100 1 0 0 5

		set_hoverable this 0		;// darf nicht angreifbar sein, weil man sonst Seq durcheinanderbringen kann :-(
		set_collision this 1

		timer_event this evt_timer_init -repeat 1 -interval 1 -userid 0 -attime [expr {[gettime] +1}]
	}
}



def_class Brainmaschine_Schalter metal tool 1 {} {
	call scripts/misc/autodef.tcl
	call scripts/classes/story/sequencer.tcl

	class_defaultanim kris_maschineschalter.an

	method destroy_overload {} {
	}

	method objaction {user} {
		global status sequencescript
		tasklist_add $user "walk_pos \{[vector_fix [vector_add [get_pos this] {0.7 1.0 1.5}]]\}"

		if  { [sm_get_event Brains_Disko_aufgebaut]  ||  [get_diplomacy 3 0] == "enemy"} {
			// Brains sind abgelenkt
			tasklist_add $user "rotate_toleft"
			if {$status == "on"} {
				tasklist_add $user "play_anim bend; call_method [get_ref this] switch"
			}
		} elseif { [sm_get_event Brains_Ring_gestohlen] } {
			// der Ring ist gar nicht mehr da
			tasklist_add this "playanim dontknow"

		} else {

			set sequencescript "brains_hebel_a_147"
			catch {sequencer_activate}
		}
	}

	method get_standoff_dist {} {
		return 4.0
	}

	method switch {} {
		global status
		set status "off"
		set_hoverable this 0
		set_anim this kris_maschineschalter.aus 0 $ANIM_STILL
		catch { sm_add_event Brains_Maschine_abgeschaltet }
		catch { sm_set_event Brains_Maschine_abgeschaltet }
		set maschine [obj_query this "-class Brainmaschine"]
		if {$maschine > 0} {
			foreach obj $maschine {
				set_anim $obj kris_maschine.standard 0 $ANIM_STILL
			}
		}

		log "Brainsmaschine ausgeschaltet!"
	}

	method get_status {} {
		global status
		return $status
	}

	obj_init {
		call scripts/misc/autodef.tcl
		call scripts/classes/story/sequencer.tcl

		set_anim this kris_maschineschalter.an 0 $ANIM_STILL

		set_collision this 0
		set_hoverable this 1
		set_visibility this 1

		set status "on"
		catch { sm_add_event Brains_Disko_aufgebaut }
	}
}

def_class Brainmaschine metal tool 1 {} {
	call scripts/misc/autodef.tcl
	call scripts/classes/story/sequencer.tcl

	class_defaultanim kris_maschine.betrieb

	// Init-Timer: Ring in der Nähe einfangen und anhängen
	def_event evt_timer0
	handle_event evt_timer0 {
		set ring [obj_query this "-class Ring_Der_Magie -range 10.0 -limit 1"]
		if {$ring != 0  &&  [obj_valid $ring]} {
			link_obj $ring this 0
			log "Brainmaschine: Ring found & captured!"
		} else {
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
			log "Brainmaschine: Ring not found, will try again"
			set ring -1
		}
	}

	// Update-Timer: feststellen, ob ein Kristall der eben noch drin war entfernt wurde
	//               in diesem Fall muss das ganze Rätsel geupdated werden!
	def_event evt_timer_update
	handle_event evt_timer_update {
		if {$ring == -1} {
			return
		}

		if {![obj_valid $ring]  ||  [get_linked_to $ring] != [get_ref this]} {
			timer_unset this 0

			catch { sm_set_event Brains_Ring_gestohlen }
			
			// evtl. Unsichtbarkeitstränke aufheben
			foreach obj [obj_query this "-class Zwerg -range 20 -cloaked 1"] {
				 call_method $obj set_invisibility 0 0
			}
			
			if {![sm_get_event Brains_Maschine_abgeschaltet]} {
				// Maschine ist noch an
				if {[sm_get_event Brains_Disko_aufgebaut]} {
					// ... aber die Brains sind weg --> Hamster bricht aus
					set sequencescript "brains_hebel_b_148"
					catch {sequencer_activate}
					loese_fenris_aus
				} else {
					// ... und die Brains sind noch da!
					set btrigger [obj_query this "-class Trigger_Crystal_Brains_Erfindung -range 20"]
					if {$btrigger != 0} {
						foreach item $btrigger {
							call_method $item destroy
						}
					}

					get_angry

					set sequencescript "brains_hebel_b_148"
					set erfindung [obj_query this -class Trigger_Crystal_Brains_Erfindung]
					if {$erfindung != 0} {
						call_method $erfindung destroy
					} else {
						log "Disco was built already..."
					}
					remove_fow
					catch {sequencer_activate}
					loese_fenris_aus
				}
			}
			loese_fenris_aus
		}
	}

	method destroy_overload {} {
	}

	obj_init {
		call scripts/misc/autodef.tcl
		call scripts/classes/story/sequencer.tcl

		set_anim this kris_maschine.betrieb 0 $ANIM_LOOP

		set_collision this 1
		set_hoverable this 0
		set_visibility this 1
		set ring -1
		set newring 0

		catch { sm_add_event Brains_Ring_gestohlen }
		catch { sm_add_event Brains_Disko_aufgebaut }
		catch { sm_add_event Brains_Maschine_abgeschaltet }

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0

		proc get_angry {} {
			set_diplomacy 3 0 enemy
			set_diplomacy 0 3 enemy
            set brains [obj_query this -class Zwerg -owner 3 -limit 5]
            foreach g $brains {
               	set_event $g evt_zwerg_break -target $g
           	}
        }

		proc loese_fenris_aus {} {
			set the_other [obj_query this -class Trigger_Fenris_144a -limit 1]
			if {$the_other!=0 } {
				call_method $the_other activate_fenris_144a
			} else {
				log "Kein Fenristrigger 144a weit und breit... ich wette, Christoph ist schuld..."
				cancel_fade
			}

        }

		proc free_hamster {} {
			set rl [obj_query this "-class Riesenlaufrad -range 20"]
			if {$rl != 0} {
				call_method $rl destroy
			}
		}

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {-20 -4 4}]
			call_method $FR fog_remove 0 50 20
			call_method $FR timer_delete 5
		}

		proc ringplatzieren {obj_pos} {
			global newring
			takeringaway
			set_pos $newring $obj_pos

        }

		proc takeringaway {} {
			global newring
			set ring_da 0
			set gnomes [obj_query this -class Zwerg -range 5 -owner 0]
			if { $gnomes != 0 } {
				foreach item $gnomes {
			        set inv_liste [inv_list $item]
			        foreach invitem $inv_liste {
						if { [get_objclass $invitem] == "Ring_Der_Magie" } {
							inv_rem $item $invitem
							del $invitem
							set ring_da 1
						}

					}
			    }
			}
			if { $ring_da != 1 } {
				set ring_woanders 0
				set ring_woanders [obj_query this -class Ring_Der_Magie]
				if { $ring_woanders != 0 } {
					del $ring_woanders
				}
			}
			sel /obj
			set newring [new Ring_Der_Magie "" [ get_pos this ] {0 0 0} ]
		}


	}
}


def_class Kristalltor none dummy 0 {} {
	method set_open {} {
		set_pf_influence this 0 0 0 0 0 0
	}

	method set_closed {} {
		set_pf_influence this -8 -60 +8 +16 INT_MAX 0
	}

	def_event evt_timer_init
	handle_event evt_timer_init {
		call_method this set_closed
	}

	class_defaultanim kris_tor.standard

	obj_init {
		set_anim this kris_tor.standard 0 0
		set_collision this 1
		set_physic this 1
		set_visibility this 1
		set_viewinfog this 1
		set_buildupstate this 1
		timer_event this evt_timer_init -repeat 0 -userid 0 -attime [expr [gettime] + 0.1]
	}
}
