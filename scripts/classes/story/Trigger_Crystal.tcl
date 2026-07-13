// Trigger für die Kristallwelt


// Trigger: FoW-Remover 6 zm unterhalb des Triggers
def_class Trigger_soundmarker_brains none dummy 0 {} {
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		proc set_music {} {
			set pos [get_pos this]
			adaptive_sound marker brains $pos 20
			adaptive_sound marker brains [vector_add $pos {-10 -24.875 0}] 50
			adaptive_sound marker brains [vector_add $pos {-30 -52.875 0}] 90
			adaptive_sound marker brains [vector_add $pos {-66.5 -12.875 0}] 90
		}
    }

	handle_event evt_timer0 {
		trigger create this any_object "set_music"
		trigger set_target_range this 5
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
		trigger set_target_owner this 0

	}
}

def_class Trigger_soundmarker none dummy 0 {} {
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		proc set_music {} {
			set pos [get_pos this]
			adaptive_sound marker trollfestung $pos 30
			adaptive_sound marker trollfestung [vector_add $pos {20 -12.5 0}] 70
			adaptive_sound marker trollfestung [vector_add $pos {45 -12.5 0}] 70
		}
    }

	handle_event evt_timer0 {
		trigger create this any_object "set_music"
		trigger set_target_range this 20
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
		trigger set_target_owner this 0

	}
}

def_class Trigger_fog_remover_brains none dummy 0 {} {
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		proc remove_fow {} {
			set pos [get_pos this]
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {0 5 0}]
			call_method $FR fog_remove 0 10 5
			call_method $FR timer_delete 20
			del this
		}
    }

	handle_event evt_timer0 {
		trigger create this any_object "remove_fow"
		trigger set_target_range this 6
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1

	}
}

// Trigger: Erstes Betreten des Maschineraums
def_class Trigger_Brains_139_Betreten none dummy 0 {} {
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
			 set_pos $FR [vector_add $pos "-10 -3 0"]
			 call_method $FR fog_remove 0 7 4
			 call_method $FR timer_delete 30
	   	}
	   
	   
		proc runit {} {
			if {[get_diplomacy 3 0] != "enemy"} {
		   		remove_fow
		   		sequencer_activate
		   	}
		}
	}
	
	method destroy_overload {} {
		set mypos [get_pos this]
		set icos [lnand 0 [obj_query this -class Info_Coll_o -pos [vector_add $mypos {-34 0 0}] -range 10]]
		foreach ico $icos {
			del $ico
		}
		set myx [lindex $mypos 0]
		set myy [lindex $mypos 1]
		set xn [expr {$myx-40}]
		set yn [expr {$myy-5}]
		set xp [expr {$myx+5}]
		set yp [expr {$myy+5}]
		catch {
			ai exec 3 "set DontPlaceArea \{$xn $yn $xp $yp\}"
			log "Ai3 forbidden Area: \{$xn $yn $xp $yp\}"
		}
		destroy_permanently
	}
	
	handle_event evt_timer0 {
		trigger create this any_object "runit"
		set sequencescript "Brains_139_Betreten"
		trigger set_target_range this 10.5
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


// Trigger: die alte Zwergenburg wird entdeckt, Kamerafahrt

def_class Trigger_Crystal_Burg_entdeckt none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0

	method destroy_overload {} {
		set TrollSpeier [obj_query this -class Info_Pos_TrollSpeier -limit 1]
		if {$TrollSpeier} {
			call_method $TrollSpeier activate_spaw
		}
		set the_other [obj_query this -class Trigger_Fenris_135a -limit 1]
		if {$the_other!=0 } {
			call_method $the_other activate_fenris_135a
			destroy_permanently
		} else {
			log "Kein Fenristrigger 135a weit und breit... ich wette, Christoph ist schuld..."
			cancel_fade
			destroy_permanently
		}
	}

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]; #eine Stelle wo der Nebel weggeht
			set_pos $FR [vector_add $pos {-50 -30 0}]
			call_method $FR fog_remove 0 100 100
			set_pos $FR [vector_add $pos {-40 -20 0}]
			call_method $FR fog_remove 0 100 100
			set_pos $FR [vector_add $pos {-10 -10 0}]
			call_method $FR fog_remove 0 100 100
			call_method $FR timer_delete 5
		}
	}

	handle_event evt_timer0 {
		set sequencescript "kri_135"
		trigger create this any_object "remove_fow;sequencer_activate"
		trigger set_target_range this 5.0
		trigger set_target_type this gnome
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}

def_class Trigger_Fenris_135a none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {0 -3 0}]
			call_method $FR fog_remove 0 50 50
			call_method $FR timer_delete -1
		}

		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	method activate_fenris_135a {} {
		set sequencescript "fenris_135a"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 10.0
		trigger set_target_class this Fenris_003
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}

	handle_event evt_timer0 {
	}
}

def_class Trigger_Fenris_144a none dummy 0 {} {
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

	method activate_fenris_144a {} {
		set sequencescript "fenris_144a"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 10.0
		trigger set_target_class this Fenris_001
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}

	handle_event evt_timer0 {
	}
}


// Trigger: Bilderraetsel mit den 7 Zwergen ist gelöst worden; Geheimtüren werden geöffnet

def_class Trigger_Crystal_Bilderraetsel none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		set hooklist [list]

		proc check_paintings {} {
			global hooklist

			set paintingorder [list]
			foreach hook $hooklist {
				set painting [call_method $hook get_snapped_obj]
				if {$painting != -1} {
					lappend paintingorder [get_objclass $painting]
				}
			}

//			log "Trigger_Crystal_Bilderraetsel: Paintingorder: $paintingorder"
			if {$paintingorder == "Bild1 Bild2 Bild3 Bild4 Bild5 Bild6 Bild7 Bild8"} {
				return 1
			}

			return 0
		}


		proc opendoor {side} {
			set doorlist [obj_query this "-class Tuer_kristall -range 50.0"]
			foreach door $doorlist {
				if {[call_method $door get_uniquename] == "Bilderraetseltuer"} {
					if {$side == "left"  &&  [get_posx $door] < [get_posx this]} {
						call_method $door oeffnen [get_ref this] -1
					}
					if {$side == "right"  &&  [get_posx $door] > [get_posx this]} {
						call_method $door oeffnen [get_ref this] -1
					}

				}
			}

			foreach obj [obj_query this "-class {Bild1 Bild2 Bild3 Bild4 Bild5 Bild6 Bild7 Bild8} -range 50.0"] {
				set pos [get_pos $obj]
				set_physic $obj 0
				set_hoverable $obj 0
				link_obj $obj
				set_pos $obj $pos
			}

			set markerlist [obj_query this "-class Bild_Positionsmarke -range 50.0"]
			if {$markerlist != 0} {
				foreach obj $markerlist {
					del $obj
					log "deleted $obj"
				}
			}
		}
	}


	def_event evt_timer0
	handle_event evt_timer0 {

		set hooklist [obj_query this "-class Bild_Positionsmarke -range 30.0"]
		if {[llength $hooklist] != 8} {
			log "Trigger_Crystal_Bilderraetsel: could not find all hooks, will try again later"
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
			return
		}

		trigger create this callback "log \"--- Bilderraetsel gelöst! --- \"; sequencer_activate"
		set sequencescript "kri_137"
		trigger set_target_count this 1
		trigger set_checktimer this 3
		trigger set_callback this "check_paintings"
	}
}

def_class Trigger_kri_8104_elfe_warnt_a none dummy 0 {} {
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
		set sequencescript "kri_8104"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


def_class Trigger_kri_8105_elfe_warnt_b none dummy 0 {} {
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
		set sequencescript "kri_8105"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


// Trigger: Erfindung in Brainshöhle aufgebaut

def_class Trigger_Crystal_Brains_Erfindung none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl

	obj_init {
		call scripts/classes/story/sequencer.tcl

		set_selectable this 0
		set_hoverable this 0
		set is_disco 0
		set old_inventions [list]
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		proc start_seq {} {
			global sequencescript is_disco
			
			if {[get_diplomacy 3 0] == "enemy"} {
				return
			}
			
			if {$is_disco == 0} {
				// falsche Erfindung aufgebaut
				set sequencescript "brains_erfindung_a_145"
				log "falsche Erfindung!!!"
				remove_fow
			} else {
				// Disko aufgebaut
				set sequencescript "brains_erfindung_b_146"
				log "Disco!!!"
				remove_fow
			}
			sequencer_activate
		}


		proc remove_fow {} {
			 global FR
			 set pos [get_pos this]
			 sel /obj
			 set FR [new FogRemover]
			 set_pos $FR [vector_add $pos "22 -3 0"]
			 call_method $FR fog_remove 0 20 6
			 call_method $FR timer_delete 30
	   }


		proc test_invention {} {
			global P_obj is_disco old_inventions

			if {[get_diplomacy 3 0] == "enemy"} {
				// Brains sind feindlich - vergeigt!
				destroy_permanently
				return 0
			}

			set ilist [obj_query this "-type production -owner 0 -range 7.0 -flagpos build"]
			if {$ilist == 0} {
				return 0
			}

			set is_disco 0
			set newitem  0
			foreach item $ilist {
				if {[lsearch $old_inventions $item] < 0} {
					set P_obj [ get_pos $item]
					if {[get_objclass $item] == "Disco"} {
						set is_disco 1
						catch { sm_add_event Brains_Disko_aufgebaut }
						catch { sm_set_event Brains_Disko_aufgebaut }
						add_owner_attrib 0 [get_objclass $item] -1
						set_owner $item 3
						add_owner_attrib 0 [get_objclass $item] 1
						return 1
					}
					lappend old_inventions $item
					set newitem 1
				}
			}
			log "old_inventions: $old_inventions"
			if {$newitem} {
				return 1
			} else {
				return 0
			}
		}

	}


	method destroy_overload {} {
		global is_disco
		
		if {[get_diplomacy 3 0] == "enemy"} {
			return 1
		}
		
		if {$is_disco} {
			// alle Brains zu Discogängern mutieren!
			set_diplomacy 0 3 friend
			set_diplomacy 3 0 friend
			set brainslist [obj_query this "-class Zwerg -owner 3 -range 15.0 -cloaked 1"]
			if {$brainslist != 0} {
				foreach brain $brainslist {
					tasklist_clear $brain
					state_triggerfresh $brain mad_scientist_dance
				}
			}
			destroy 1
		} else {
			trigger create this callback "start_seq"
			trigger set_target_count this 1
			trigger set_checktimer this 5
			trigger set_callback this test_invention
		}
	}


	def_event evt_timer0
	handle_event evt_timer0 {
		trigger create this callback "start_seq"
		trigger set_target_count this 1
		trigger set_checktimer this 5
		trigger set_callback this test_invention
	}
}



//Trigger: Männer horchen auf Lorelei und werden Zombies
def_class Trigger_Crystal_Lorelei_Entfuehrung none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		
		proc run_it {} {
			global sequencescript

			set FR [new FogRemover]
			set_pos $FR [vector_add [get_pos this] {57 -24 3}]
			call_method $FR fog_remove 0 20 15
			call_method $FR timer_delete 30

			set sequencescript "kri_140"
			sequencer_activate
		}
	}

	handle_event evt_timer0 {
		trigger create this any_object "run_it"
		trigger set_target_range this 4
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
	}
}


//Trigger: Lorelei ist zerstört worden
def_class Trigger_Crystal_Lorelei_Vernichtung none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc run_it {} {
			global sequencescript

			set FR [new FogRemover]
			set_pos $FR [vector_add [get_pos this] {0 -5 0}]
			call_method $FR fog_remove 0 15 15
			call_method $FR timer_delete 30

			set sequencescript "kri_144"
			sequencer_activate

			// jetzt das Tor öffnen (niemals der Sequenz vertrauen!)

			set lorelei [obj_query this "-class Lorelei -limit 1"]
			if {$lorelei != 0} {
				set_anim $lorelei kris_lorelei_kris.durchbruch 0 0
				call_method $lorelei free_males
			}
			set tor [obj_query this "-class Kristalltor -limit 1"]
			if {$tor != 0} {
				set_anim $tor kris_tor.durchbruch 0 0
				call_method $tor set_open
			}
		}
	}


	handle_event evt_timer0 {
		trigger create this any_object "run_it"
		trigger set_target_range this 4
		trigger set_target_class this "Lorelei"
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}
}


def_class Trigger_Circus_breakthru none dummy 0 {} {
	call scripts/misc/info_obj.tcl
	def_event evt_timer0
	def_event evt_checkdig
	method activate {ref} {
		log "$ref gedrueckt"
		call_method $ref set_actiononrelease "call_method $myref deactivate $ref"
		set pressed [lor $pressed $ref]
		if {[llength $pressed]>1} {
			activate
		}
	}
	method deactivate {ref} {
		log "$ref entrastet"
		set pressed [lnand $ref $pressed]
	}

	obj_init {
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		timer_event this evt_checkdig -repeat -1 -interval 1 -userid 1 -attime [expr [gettime]+2]
		call scripts/misc/info_obj.tcl

		set myref [get_ref this]
		set pressed {}

		proc activate {} {
			global Haengelicht myy Holztisch
			if {[obj_valid $Haengelicht]&&[set hy [get_posy $Haengelicht]]<$myy-2.7} {
				set_posy $Haengelicht [expr {$hy+0.1}]
				set_physic $Haengelicht 1
				log "act on Haengelicht"
				action this wait 0.1 {activate}
			} else {
				destruct $Holztisch
				action this wait 0.2 {lastact}
			}
		}
		proc lastact {} {
			global myx myy mypos Boden_verlies_a
			set gnome [obj_query this -class Zwerg -owner 0 -range 20 -limit 1 -cloaked 1]
			for {set j 0} {$j<3} {incr j} {
				for {set i 0} {$i<2} {incr i} {
					dig_mark 0 [expr {$myx+$i}] [expr {$myy+$j}] 1
					set dp [dig_next $mypos $gnome]
					if {[vector_dist $dp $mypos]>5} {continue}
					dig_apply $dp $gnome
				}
			}
			destruct $Boden_verlies_a
			action this wait 1 {destroy}
		}

		proc destroy {} {
			global Holztisch Boden_verlies_a
			catch {del $Holztisch}
			catch {del $Boden_verlies_a}
			del this
		}
	}

	handle_event evt_timer0 {
		set_objname this Trigger_Circus_breakthru
		set myy [expr {int([get_posy this])+1}]
		set myx [expr {int([get_posx this])}]
		set mypos [get_pos this]
		foreach cn {Haengelicht Holztisch Boden_verlies_a} {
			set $cn [obj_query this -class Dummy_$cn -range 10 -limit 1]
			set_collision [subst $$cn] 1
		}
	}
	handle_event evt_checkdig {
		for {set i 0} {$i<2} {incr i} {
			for {set j 0} {$j<4} {incr j} {
				dig_mark 0 [expr {$myx+$i}] [expr {$myy+$j}] 2
			}
		}
	}
}

def_class Trigger_Circus_schalter none dummy 0 {} {
	call scripts/misc/info_obj.tcl
	def_event evt_timer0
	method activate {mode dir} {
		if {$mode} {
			change_doors
		} else {
			both_doors $dir
		}
	}

	obj_init {
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/misc/info_obj.tcl

		set myref [get_ref this]

		set s0 0;set s1 0;set s2 0;set s4 0;set s5 0;set s6 0
		set d0 0;set d1 0

		proc change_doors {} {
			global d0 d1 myref
			if {[get_attrib $d0 Schaltstatus]} {
				call_method $d0 oeffnen $myref -1
				call_method $d1 schliessen
			} else {
				call_method $d0 schliessen
				call_method $d1 oeffnen $myref -1
			}
		}

		proc both_doors {dir} {
			if {$dir} {
				global s0 s1 s2 s3 s4 s5
				for {set i 0} {$i<6} {incr i} {
					call_method [subst \$s$i] entrasten
				}
				action this wait 1 {open_doors}
			} else {
				global d0 d1 myref
				call_method $d0 schliessen $myref -1
				call_method $d1 schliessen $myref -1
			}
		}

		proc open_doors {} {
			global d0 d1 myref
			call_method $d0 oeffnen $myref -1
			call_method $d1 oeffnen $myref -1
		}
	}

	handle_event evt_timer0 {
		set_objname this Trigger_Circus_Schalter
		set slist [obj_query this -type tool -range 15]
		if {$slist==0} {set slist ""}
		foreach s $slist {
			set name [call_method $s get_uniquename]
			if {[string range $name 0 8]=="Sch_Circ_"} {
				set s[string index $name 9] $s
			}
		}
		set dlist [obj_query this -class Tuer_kaserne -range 15 -limit 2]
		if {$dlist==0} {log "NO doors found!!!";return}
		set d0 [lindex $dlist 0]
		set d1 [lindex $dlist 1]
		if {$d1==""} {log "NO 2nd door found!!!";return}
		if {[get_posx $d1]<[get_posx $d0]} {
			set d2 $d0;set d0 $d1;set d1 $d2
		}
	}
}


// Einsturz der Loreleihöhle

def_class Trigger_Crystal_160_Hol_Einsturz none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

		proc run_it {} {
			global sequencescript

			set FR [new FogRemover]
			set_pos $FR [vector_add [get_pos this] {0 -5 0}]
			call_method $FR fog_remove 0 15 15
			call_method $FR timer_delete 30

			set sequencescript "kri_160"
			sequencer_activate

			// jetzt das Tor versperren (niemals der Sequenz vertrauen!)

			set lorelei [obj_query this "-class Lorelei -limit 1"]
			if {$lorelei != 0} {
				set_anim $lorelei kris_lorelei_kris.versperrt 0 0
			}
			set tor [obj_query this "-class Kristalltor -limit 1"]
			if {$tor != 0} {
				set_anim $tor kris_tor.versperrt 0 0
				call_method $tor set_closed
			}
		}

    }

	handle_event evt_timer0 {
		trigger create this any_object "run_it"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}
