// Trigger für die Urwaldwelt


def_class Trigger_StartScreen none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0

	method destroy_overload {} {
		global seq_was_breaked
		if { $seq_was_breaked } {
			destroy_permanently
		} else {
			run
		}
	}

	method validate {} {
		set valid 1
	}

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		set valid 0
		set last_script -1
		set sequences_path data/scripts/sequences/Menue/start_
		set filelist [glob ${sequences_path}*.tcl]
		set script_list {}
		foreach f $filelist {
			lappend script_list [string trimright [string trimleft $f $sequences_path] ".tcl"]
		}
		// Die folgende Zeile aktivieren, um eine spezielle Sequenz zu erzwingen:
		// set script_list "001"
		set sc_length [llength $script_list]

		proc run {} {
			global sequencescript script_list sc_length last_script
			global followaction_list eyefocus_list

			set followaction_list [list]
			set eyefocus_list [list]
			set scidx [irandom 0 $sc_length]
			if { $scidx == $last_script } {
				incr scidx
			}
			if { $scidx >= $sc_length } {
				set scidx 0
			}
			set script [lindex $script_list $scidx]
			set sequencescript "start_$script"
			log "startsequence [string trimleft $script 0]"
			set last_script $scidx
			gametime factor 1
			sequencer_activate
		}
	}

	handle_event evt_timer0 {
		if { $valid == 0 } {
			log "WARNING don't use Trigger_StartScreen in any template !!!!"
			destroy_permanently
		} else {
			run
		}
	}
}


def_class Trigger_wusch none dummy 0 {} {

// Annäherungstrigger  / setzt FogRemover-Mittelpunkt auf Schatztonne / startet "wusch"

	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			global FR
			sel /obj
			set FR [new FogRemover]
			set schatz [obj_query this -class Schatztonne -range 50 -limit 1 -owner 6]
			if { $schatz != 0 } {
				set pos [get_pos $schatz]
			} else {
				log "Kein Schatz in der Nähe, den es sich lohnt anzugucken"
				destroy_permanently
			}

			set_pos $FR [vector_add $pos {0 -1 0}]
			call_method $FR fog_remove 0 10 10
			call_method $FR timer_delete -1
		}

	}

	handle_event evt_timer0 {
		set sequencescript "wusch"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 2
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
	}
}


def_class Trigger_wusch2 none dummy 0 {} {

// Checkt ob der Trigger unter Wasser ist, wenn ja, FogRemover auf Tuer_Kaserne und startet "wusch2"

	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			global FR
			sel /obj
			set FR [new FogRemover]
			set pos [get_pos this]

			set_pos $FR [vector_add $pos {0 0 0}]
			call_method $FR fog_remove 0 4 4
			call_method $FR timer_delete -1
		}

		proc remove_fow2 {} {
			global FR
			sel /obj
			set FR [new FogRemover]
			set pos [get_pos this]

			set_pos $FR [vector_add $pos {4 0 0}]
			call_method $FR fog_remove 0 4 4
			call_method $FR timer_delete -1
		}

	}

	handle_event evt_timer0 {
		set sequencescript "wusch2"
		trigger create this callback "remove_fow; remove_fow2; sequencer_activate"
		trigger set_callback this {isunderwater [get_pos this]}
		trigger set_target_count this 1
	}
}


def_class Trigger_SchwertGenHimmel none dummy 0 {} {

// Annäherungstrigger / Umkreis standard (= 5 Zm) / startet "SchwertGenHimmel

	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "SchwertGenHimmel"
		trigger create this any_object "sequencer_activate"
		trigger set_target_class this "Zwerg"
		trigger set_target_count this 1
	}
}


def_class Trigger_FirstContact none dummy 0 {} {

// AnnäherungsTrigger / ausgelöst durch Zwerg Owner 0 (Wiggle) / FogRemover 14 Zm nach links / setzt Produktion der Feuerstelle auf 10 Futter

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
			set_pos $FR [vector_add $pos {-14 0 0}]
			call_method $FR fog_remove 0 12 2
			call_method $FR timer_delete 5
		}
	}
	
	method destroy_overload {} {
		catch {
			ai exec 1 {
				set bIsDigging 0
				set bIsBuilding 0
				set bIgnoreThieves 1
			}
		}
		set fire [obj_query this -class Feuerstelle -owner 1 -limit 1]
		if {$fire} {
			set mats [lnand 0 [obj_query $fire -class {Pilz Hamster} -range 10]]
			foreach mat $mats {
				set_owner $mat 1
			}
		}
		destroy 1
	}

	handle_event evt_timer0 {
		set wigglegender ""
		lappend known_vars wigglegender
		set sequencescript "ErsterKontakt"
		trigger create this any_object "remove_fow;sequencer_activate"
		trigger set_target_range this 2.0
		trigger set_target_type this gnome
		trigger set_target_owner this 0
		trigger set_target_count this 1

		set fs [obj_query this "-class Feuerstelle -range 10 -limit 1"]
		log "Voodoo Feuerstelle : $fs"
		if { $fs != 0 } {
			set_prod_slot_cnt $fs Grillpilz 10
		}
	}
}


def_class Trigger_Einsiedler_1 none dummy 0 {} {
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
			set_pos $FR [vector_add $pos {-10 0 0}]
			call_method $FR fog_remove 0 10 2
			call_method $FR timer_delete 5
		}
		proc Hamstersneeded {} {
			return 6
		}


	}

	handle_event evt_timer0 {
		set sequencescript "urw_22"
		trigger create this any_object "remove_fow;sequencer_activate"
		trigger set_target_range this 5.0
		trigger set_target_type this gnome
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}

def_class Trigger_SecondContact none dummy 0 {} {

// AnnäherungsTrigger / Startet "ZweiterKontakt"

	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}
	method destroy_overload {} {
		catch {
			ai exec 1 {
				set bIsDigging 1
				set bIsBuilding 1
			}
		}
		set myposy [get_posy this]
		fincr myposy -20.0
		set fires [lnand 0 [obj_query this -class Feuerstelle -owner 1]]
		foreach fire $fires {
			if {[get_posy $fire]<$myposy} {
				set_prodautoschedule $fire 0
				set firex [hf2i [get_posx $fire]]
				set firey [hf2i [get_posy $fire]]
				set myx [hf2i [get_posx this]]
				set myy [hf2i [get_posy this]]
				incr myx 20
				if {$firex<$myx} {
					set xn $firex
					set xp $myx
				} else {
					set xn $myx
					set xp $firex
				}
				if {$xp-$xn<30} {
					incr xn [expr {-int(($xp-$xn)*0.5)}]
					incr xp [expr {int(($xp-$xn)*0.5)}]
				}
				incr xn -10
				incr xp 10
				remove_black_fog 1 $xn $firey $xp $myy
				break
			}
		}
		set tents [lnand 0 [obj_query this -class Zelt -owner 1]]
		foreach tent $tents {
			if {[get_posy $tent]<$myposy} {
				set_prodautoschedule $tent 0
				break
			}
		}
		destroy 1
	}
	handle_event evt_timer0 {
		trigger create this any_object "sequencer_activate"
		set sequencescript "ZweiterKontakt"
		trigger set_target_range this 5
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


def_class Trigger_Einsiedler_2 none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0

	method destroy_overload {} {
		global status hamstersneeded Rem_H Rem_Z

		foreach item $Rem_H {
			if { $Rem_Z != 0 } {
				inv_rem $Rem_Z $item
			}
			del $item
		}

		if { $Rem_Z != 0 } {
			sel /obj
			set imap [new Karte "" [get_pos this] {0 0 0}]
			call_method $imap change_owner 0
			inv_add $Rem_Z $imap
		}

		if {$hamstersneeded > 0} {
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
		} else {
			destroy_permanently
		}
	}

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl

		set status check
		set hamstersfound 0
		set hamstersneeded 6
		set newhamsters 0

		set Rem_Z 0
		set Rem_H [list]

		proc check_activation {} {
			global status hamstersfound hamstersneeded newhamsters sequencescript Rem_Z Rem_H
			#log "check"
			set newhamsters 0
			set Rem_H [list]
			set zl [obj_query this "-class Zwerg -range 5 -cloaked 1"]
			#log "---> $zl"
			if { $zl != 0 } {
				foreach item $zl {
					if {$hamstersneeded <= 0} {break}
					set invlist [inv_list $item]
					foreach invitem $invlist {
						if {$hamstersneeded <= 0} {break}
						if { [get_objclass $invitem] == "Hamster" } {
							//inv_rem $item $invitem
							//del $invitem
							incr hamstersfound
							incr hamstersneeded -1
							incr newhamsters 1
							lappend Rem_H $invitem
						}
					}
				}
				#log "_-_ $newhamsters : $hamstersfound < $hamstersneeded"
				if { $newhamsters > 0 } {
					if { $hamstersneeded > 0} {
						init_trigger "sequencer_activate" "urw_23a"
					} else {
						set Rem_Z [lindex $zl 0]
						set sequencescript "urw_23b"
						sequencer_activate
					}
				} else {
					timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
				}
			}
		}

		proc init_trigger {{pr "check_activation"} {scr "none"}} {
			global sequencescript hamstersneeded
			set known_vars {hamstersneeded}
			trigger create this any_object $pr
			trigger set_target_range this 3.0
			trigger set_target_type this gnome
			trigger set_target_owner this 0
			trigger set_target_count this 1
			if { $scr != "none" } {
				set sequencescript $scr
			}
		}

		proc Hamstersneeded {} {
			global hamstersneeded
			return $hamstersneeded
		}

		proc Newhamsters {} {
			global newhamsters
			return $newhamsters
		}

		proc get_allhamsters {} {
			global status hamstersfound
			return $hamstersfound
		}

		proc get_newhamsters {} {
			global status hamstersfound
			return $hamstersfound
		}

	}

	handle_event evt_timer0 {
		init_trigger
	}
}



def_class Trigger_Voodoo_Lager none dummy 0 {} {

// CheckTrigger / checkt ob sich ein Zwerg einem Gegenstand nähert mit dem Ziel es aufzuheben/zu verarbeiten (current_lock_obj) / startet "Voodoo_Lager"

	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
		set andres_actuator 0
		proc check_gnomes {} {
			set gnomes [obj_query this "-class Zwerg -owner 0 -range 5 -cloaked 1"]
			if {$gnomes==0} {return 0}
			foreach g $gnomes {
				if {[dist_between this [ref_get $g current_lock_obj]]<5} {
					global andres_actuator
					set andres_actuator $g
					return 1
				}
			}
			return 0
		}
	}

	handle_event evt_timer0 {
		set sequencescript "Voodoo_Lager"
		trigger create this callback "sequencer_activate"
		trigger set_timer this 3
		trigger set_callback this "check_gnomes"
		trigger set_target_count this 1
	}
}



def_class Trigger_Urw_040 none dummy 0 {} {

// ???????????????????

	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl
	method activate {ref} {
		set sequencescript "Urw_040"
		trigger create this unique_object "sequencer_activate"
		trigger set_target_count this 1
		trigger set_target_range this 15
		trigger set_target_ref this $ref

	}
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}
	handle_event evt_timer0 {
	}
}


def_class Trigger_urw_unq_troll_005_Betreten none dummy 0 {} {

// ???????????????

	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0

	method destroy_overload {} {
		if { ![sm_get_event Einsiedler_Karte_erhalten]} {
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
		} else {
			destroy 1
		}
	}

	obj_init {

		catch { sm_add_event Einsiedler_Karte_erhalten }

		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
		set guardessactive 0
		set started 0

		proc start_seq {} {
			global guardessactive sequencescript started
			if { $guardessactive == 0 } {

				set gateguardess [obj_query this "-class Torwaechterin -limit 1 -range 50"]
				set jailswitcher [obj_query this "-class Schalter_hebel_holz_up -limit 2 -range 50"]
				if { $jailswitcher != 0 } {
					foreach item $jailswitcher {
						log "+++++++++++  [call_method $item get_uniquename]"
						if { [call_method $item get_uniquename] == "T005_SchalterTorwaechterin" } {

							trigger create $item callback "trigger delete this ;call_method \[obj_query this \"-class Trigger_urw_unq_troll_005_Befreien -limit 1\"\] activate"
							trigger set_target_count this 1
							trigger set_execution $item on_activate
							trigger set_checktimer $item 1
							trigger set_callback $item { is_on }
							break
						}
					}
				}
				set guardessactive 1
			}

			if { [sm_get_event Einsiedler_Karte_erhalten] } {
				remove_fow
				set sequencescript "troll_005_Karte_erhalten"
				sequencer_activate
			} else {
				if { $started == 0 } {
					remove_fow
					set sequencescript "troll_005_Karte_n_erhalten"
					sequencer_activate
					set started 1
				} else {
					call_method this destroy_overload
				}
			}
			#destroy
		}

		proc remove_fow {} {
			global FR
			set pos [get_pos this]
			sel /obj
			set FR [new FogRemover]
			set_pos $FR [vector_add $pos {10 0 0}]
			call_method $FR fog_remove 0 15 3
			call_method $FR timer_delete 5
		}
	}

	handle_event evt_timer0 {
		trigger create this any_object "start_seq"
		trigger set_target_range this 3.0
		trigger set_target_type this gnome
		trigger set_target_owner this 0
		trigger set_target_count this 1

		set_owner this 0
	}
}


def_class Trigger_urw_unq_troll_005_Befreien none dummy 0 {} {

// Annäherungstrigger / löst aus durch Torwächterin Owner 1 (Voodoos)

	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	method activate {} {
		catch { sm_add_event Ring_Der_Natur_erhalten }
		trigger create this any_object "start_seq"
		trigger set_target_range this 5
		trigger set_target_class this Torwaechterin
		trigger set_target_owner this 6
		trigger set_target_count this 1
	}

	obj_init {

		proc start_seq {} {
			global sequencescript
			set guardess [obj_query this "-class Torwaechterin -limit 1 -range 10"]

			set ringda [sm_get_event Ring_Der_Natur_erhalten]

    		if { $ringda == 1 } {
				set sequencescript "Urw_025b"
			} else {
				set sequencescript "Urw_025"
			}

			sequencer_activate
		}


		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}
	handle_event evt_timer0 {
	}
}


def_class Trigger_urw_unq_metalltor_oeffnen none dummy 0 {} {
	# &040 #
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl


	method destroy_overload {} {
		global sequencescript
		if { $sequencescript == "Urw_040_a" } {
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+3]
		} else {
			destroy 1
		}
	}

	obj_init {


		catch { sm_add_event Torwaechterin_befreit }

		proc ringcheck {} {
			set zw [lnand 0 [obj_query this -class Zwerg -range 6 -cloaked 1]]
			foreach item $zw {
				set invidx [inv_find $item "Ring_Des_Lebens"]
				if {$invidx!=-1} {
					return 1
				}
			}
			return 0
		}

		proc start_seq {} {
			global sequence_a sequencescript
			if { [sm_get_event Torwaechterin_befreit] == 0 } {
				return 0
			}
			if { [obj_query this -class Zwerg -owner 0 -range 5 -cloaked 1] == 0 } {
				return 0
			}
			if { ![ringcheck] } {
				if { $sequence_a == 0 } {
					set sequencescript "Urw_040_a"  ;#kein Ring dabei
					set sequence_a 1
					return 1
				} else {
					call_method this destroy_overload
				}
			} else {
				set sequencescript "Urw_040_b"  ;#Ring dabei
				return 1
			}
			return 0
		}

		set_selectable this 0
		set_hoverable this 0
		set sequence_a 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		trigger create this callback "sequencer_activate"
		trigger set_callback this "start_seq"
		trigger set_timer this 3
		trigger set_target_count this 1

	}
}


def_class Trigger_Urw_unq_feen_Start none dummy 0 {} {
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
			 set_pos $FR [vector_add $pos "10 -1 0"]
			 call_method $FR fog_remove 0 55 5
			 call_method $FR timer_delete 30
	   }
	}
	handle_event evt_timer0 {
		trigger create this any_object "remove_fow; sequencer_activate"
		set sequencescript "clip_008"
		trigger set_target_range this 3
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


def_class Trigger_Urw_unq_feen_Tuer none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	method destroy_overload {} {
		global sequencescript

		if { $sequencescript == "AnimTest" } {
			#set myswitcher 0
			create_trigger
		} else {
			destroy 1
		}
	}

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
		set sequence_ran 0
		set myswitcher 0

		proc remove_fow {} {
			 global FR
			 set pos [get_pos this]
			 sel /obj
			 set FR [new FogRemover]
			 set_pos $FR [vector_add $pos "10 -1 0"]
			 call_method $FR fog_remove 0 55 5
			 call_method $FR timer_delete 30
		}

		proc find_switcher {} {
			set switcherlist [obj_query this -class Schalter_knopf_stein -range 5]
			set found 0
			foreach switcher $switcherlist {
				if {[call_method $switcher get_uniquename]=="SFeen_Schalter"} {set found 1;break}
			}
			if {!$found} {return 0}
			return $switcher
		}

		proc check_switcher_state {} {
			global myswitcher
			return [call_method $myswitcher is_on]
		}

		proc CheckBuddel {wflag} {

			set BuddelFlag [obj_query this "-class Info_Pos_Spinne -range 100 -limit 1"]
			set HMapWert [get_hmap [get_posx $BuddelFlag] [get_posy $BuddelFlag]]
        	if { $HMapWert < 15 } {
        		return 1
        	}
		    return 0
		 }

		proc check_switcher_state_and_water_state {} {
		 	global sequencescript sequence_ran
		 	set WasserFlag [obj_query this "-class WasserTrans -range 20 -limit 1"]
		 	set WasserLaeuft [CheckBuddel $WasserFlag]
		 	set SchalterGedrueckt [check_switcher_state]

			if { $SchalterGedrueckt == 0 && $sequence_ran == 1 } {
				set sequence_ran 0
			}

			slog "WasserLaeuft = $WasserLaeuft und SchalterGedrueckt = $SchalterGedrueckt"
			slog "Sequence_ran $sequence_ran"
			if { $WasserLaeuft == 1 && $SchalterGedrueckt == 1 && $sequence_ran == 0} {
				set sequencescript "clip_009"
				return 1
			}

			if { $WasserLaeuft != 1 && $SchalterGedrueckt == 1 && $sequence_ran == 0} {
				set sequencescript "AnimTest"
				set sequence_ran 1
				return 1
			}
			return 0
		 }

		 proc remove_fow {} {
				global FR
				set pos [get_pos this]
				sel /obj
				set FR [new FogRemover]
				set_pos $FR [vector_add $pos "-10 20 0"]
				call_method $FR fog_remove 0 30 40
				call_method $FR timer_delete 30
		}

		 proc create_trigger {} {
		 	slog "Trigger Feen_Tuer created"
		 	global myswitcher

		 	if {[set myswitcher [find_switcher]]==0} {log "WARNING: Schalter SFeen_Schalter nicht gefunden!";return}
		 	trigger create this callback "remove_fow; sequencer_activate"
			set sequencescript "AnimTest"
			#slog "Sequencescript1 $sequencescript"
			trigger set_target_count this 1
			trigger set_checktimer this 1
			trigger set_callback this "check_switcher_state_and_water_state"
		 }
	}

	handle_event evt_timer0 {
		create_trigger
	}
}



def_class Trigger_Urw_unq_feen_End none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1.5]
		call scripts/classes/story/sequencer.tcl

				proc remove_fow {} {
						global FR
						set pos [get_pos this]
						sel /obj
						set FR [new FogRemover]
						set_pos $FR [vector_add $pos "10 -1 0"]
						call_method $FR fog_remove 0 55 5
						call_method $FR timer_delete 30
				}

				proc CheckWasser  {wflag} {
					set zwerg [obj_query this "-class Zwerg -range 5 -cloaked 1"]
					if {$zwerg == 0} {
						return 0
					}
#         log "Pos von WasserFlag = [get_pos $wflag], get_fsource = [get_fsource $wflag -volume], ref = $wflag, this = [get_ref this], Zwerg = $zwerg"
				   	if {[get_fsource $wflag -volume] > 50} {
						return 0
					}
		    		return 1
				}

	}
	handle_event evt_timer0 {
				trigger create this callback "remove_fow; trigger delete this; sequencer_activate"
				trigger set_target_count this 1
		set sequencescript "clip_010"
				trigger set_execution this on_activate
				trigger set_checktimer this 1
				set WasserFlag [obj_query this "-class WasserTrans -range 20 -limit 1"]
				trigger set_callback this "CheckWasser $WasserFlag"
	}
}


def_class Trigger_Urw_006 none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+5]
		call scripts/classes/story/sequencer.tcl

	}

	handle_event evt_timer0 {
		trigger create this any_object "sequencer_activate"
		set sequencescript "urw_3"
		trigger set_target_range this 40
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


def_class Trigger_Gleipnir none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+900]
		call scripts/classes/story/sequencer.tcl

		proc remove_fow {} {
			 global FR
			 set pos [get_pos [obj_query this -class Info_Pos_Spinne]]
			 sel /obj
			 set FR [new FogRemover]
			 set_pos $FR [vector_add $pos "10 -1 0"]
			 call_method $FR fog_remove 0 -20 -20
			 call_method $FR timer_delete 30
		}
	}



	handle_event evt_timer0 {
		trigger create this any_object "remove_fow; sequencer_activate"
		set sequencescript "urw_6a"
		trigger set_target_range this 400
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


def_class Trigger_urw_8100_elfe_warnt_a none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1] ;#Wird instant erzeugt
		call scripts/classes/story/sequencer.tcl

    }

	handle_event evt_timer0 {
		trigger create this any_object "sequencer_activate"
		set sequencescript "urw_8100"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}


def_class Trigger_urw_8101_elfe_warnt_b none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1] ;#Wird instant erzeugt
		call scripts/classes/story/sequencer.tcl

    }

	handle_event evt_timer0 {
		trigger create this any_object "sequencer_activate"
		set sequencescript "urw_8101"
		trigger set_target_range this 10
		trigger set_target_class this Zwerg
		trigger set_target_owner this 0
		trigger set_target_count this 1
	}
}

def_class Trigger_urw_040_c none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl

	}

	handle_event evt_timer0 {
		set sequencescript "urw_040_c"
		sequencer_activate
	}
}

def_class Trigger_RingCheck none dummy 0 {} {
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]

		set kiste -1

		proc activate_the_other {} {
		    catch { sm_set_event Ring_Des_Lebens_Gefunden }
			set the_other [obj_query this -class Trigger_Fenris_43a -limit 1]
			if {$the_other!=0 } {
				call_method $the_other activate_fenris_43a
				del this
			} else {
				log "Kein Fenristrigger 43a weit und breit... ich wette, Christoph ist schuld..."
				del this
			}
		}

		proc check_ring {} {
			global kiste

			if { $kiste == -1 } {
				set kiste [obj_query this -class Schatzkiste -range 4 -limit 1]
			}

			if {$kiste != 0} {
				if { [inv_find $kiste "Ring_Des_Lebens"] == -1 } {
					log "Der Ring des Lebens wurde gefunden!!!"
					sm_send_message this "RdL"
					return 1
				} else {
					log "Der Ring wurde noch nicht gefunden!!!"
					return 0
				}

			} else {
				log "Die Kiste mit den Ring des Lebens wurde gestohlen!!!"
				del this
			}
 		}

	}

	handle_event evt_timer0 {
	    catch { sm_add_event Ring_Des_Lebens_Gefunden }
		trigger create this callback "activate_the_other"
		trigger set_target_count this 1
		trigger set_timer this 3
		trigger set_callback this "check_ring"
	}


}

def_class Trigger_Fenris_43a none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0

	method activate_fenris_43a {} {
		set sequencescript "fenris_43a"
		trigger create this any_object "remove_fow; sequencer_activate"
		trigger set_target_range this 10.0
		trigger set_target_class this Fenris_001
		trigger set_target_count this 1
		trigger set_target_owner this -1
	}

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

	handle_event evt_timer0 {
	}
}

def_class Trigger_GameOver none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "gameover"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 6.0
		trigger set_target_class this Zwerg
		trigger set_target_count this 1
	}
}


def_class Trigger_Riesenelfe_460a none dummy 0 {} {
	def_event evt_timer0
	call scripts/classes/story/sequencer.tcl

	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+0.1]
		call scripts/classes/story/sequencer.tcl
    }

	handle_event evt_timer0 {
		set tor [obj_query this "-class Dimensionstor"]
		if {$tor <= 0} {
			log "Error: Dimensionstor not found ... no gnomes in final battle..."
			return
		}

		set marker [obj_query this "-class Info_Pos_Uebergang_Endkampf -limit 1"]
		if {$marker <= 0} {
			log "Error: Marker not found in Template unq_ende!"
			return
		}

		log "setting $tor to [get_pos $marker]"
		set_pos $tor [get_pos $marker]

		cancel_fade
		
		set sequencescript "riesenelfe_460a"
		sequencer_activate
	}
	
	obj_exit {
		log "Trigger_Riesenelfe_460a starting ([get_ref this])"
				
		set tor [obj_query this "-class Dimensionstor"]
		if {$tor <= 0} {
			log "Error: Dimensionstor not found ... no gnomes in final battle..."
			return
		}

		set marker [obj_query this "-class Info_Pos_Uebergang_Endkampf -limit 1"]
		if {$marker <= 0} {
			log "Error: Marker not found in Template unq_ende!"
			return
		}

		log "setting $tor to [get_pos $marker]"
		set_pos $tor [get_pos $marker]
		
		set gnomeslist [call_method $tor get_gnomes]
		set mypos [get_pos this]
		set gatepos [get_pos $tor]
		log "mypos is $mypos"
		log "gatepos is $gatepos"
		
		for {set i 0} {$i < [llength $gnomeslist]} {incr i} {
			if {$i < 5} {
				set pos [get_place -center $mypos -rect -4 -4 4 4 -nearpos $mypos]
			} else {
				set pos [get_place -center $gatepos -rect -4 -4 4 4 -nearpos $gatepos]
			}
			log "pos after getplace is $pos"
			if {[lindex $pos 0] <= 0} {
				set pos $gatepos 
			}
			log "corrected pos is $pos"
			set gnome [lindex $gnomeslist $i]
			log "gnome is $gnome"
			inv_rem $tor [inv_find_obj $tor $gnome]
			set_pos $gnome $pos
			set_owner $gnome 0
			set_visibility $gnome 1
			set_activegameplay $gnome 1
		}
		
		set elfe [new Riesenelfe]
		set_pos $elfe [vector_add $mypos {0 -10 0}]
		
		set_view [lindex $mypos 0] [lindex $mypos 1] 1
		
		// Zwerge sind wieder da - GameOver wieder erlaubt
	   	sm_set_event GameOverCheck
	}


}

