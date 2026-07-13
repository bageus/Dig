//# STOPIFNOT FULL
//fenris.tcl

def_class Fenris_greifen none tool 0 {} {

	method objaction {user} {
		log "obj_action Fenris_greifen"
		tasklist_add $user "walk_near_item $myobj 2.0"
		tasklist_add $user "if {\[vector_dist \[get_pos $myobj\] \[get_pos this\]\] > 4.0} {log \"tasklist clear\"; tasklist_clear this}"
		tasklist_add $user "play_anim fenrisjump"
		tasklist_add $user "del_current_muetze; change_shield_finish_in; change_weapon_finish_in; call_method [get_ref this] add_guest $user"
	}


	method get_standoff_dist {} {
		return -1;
	}

	method add_guest {g} {
		if {$g <= 0} {
			set_hoverable this 1
			return
		}

		if {$myguest > 0} {
			return
		}

		set myguest $g
		set_hoverable this 0
		link_obj $g $myobj $mydummy
		set_anim $g fenrishold 0 2
		set_hoverable $g 0
		set_selectable $g 0
		set_activegameplay $g 0
	}

	method get_guest {} {
		return $myguest
	}

	method set_position {obj dummy} {
		set mydummy $dummy
		set myobj $obj
		link_obj this $obj $dummy
		log "[get_objname this] linked to $obj, dummy $dummy"
	}

	obj_init {
		set_anim this fenris_dummy.standard 0 2
		set_physic this 0
		set_hoverable this 1
		set_visibility this 1

		set mydummy -1
		set myobj -1
		set myguest -1
	}
}


def_class FenrisFuss none monster 2 {} {
	call scripts/misc/animclassinit.tcl

	set_class_anim  kungfustillani		fenris_dummy.standard
	class_fightdist 1.5

	def_event evt_timer_check
	handle_event evt_timer_check {
		if {[get_attrib this atr_Hitpoints] < 0.01} {
			del this
		}
	}

	// virtual
	method im_attacking_you {} {}
	
	method check_first_strike {caller} {
		return 1
	}	

	state idle {
		log "[get_objname this] : I have [get_attrib this atr_Hitpoints] left."
		if {[get_attrib this atr_Hitpoints] < 0.01} {
			del this
		}
		state_disable this
	}

	obj_init {
		call scripts/misc/animclassinit.tcl

		set_anim this fenris_dummy.standard 0 2
		set_physic this 0
		set_hoverable this 1
		set_visibility this 1
		set_attrib this atr_Hitpoints 1.0
		state_triggerfresh this idle

		timer_event this evt_timer_check -repeat -1 -interval 1 -userid 0
	}
}


def_class Fenris none tool 2 {} {
	call scripts/misc/animclassinit.tcl

	class_fightdist 2.0

	set_class_anim	standloop			fenrir.stand_anim
	set_class_anim	walkstart			fenrir.gehen_start
	set_class_anim	walkloop			fenrir.gehen_loop
	set_class_anim	walkstop			fenrir.gehen_end
	set_class_anim	shocka				fenrir.schreck_a
	set_class_anim	shockb				fenrir.schreck_b

	set_class_anim	standup				fenrir.hinstellen_sitzen_start
	set_class_anim 	sitdown				fenrir.hinstellen_sitzen_end
	set_class_anim	sitting				fenrir.sitzen_standanim

	set_class_anim	beat1start			fenrir.kampf_schlag_a_start
	set_class_anim	beat1end			fenrir.kampf_schlag_a_end
	set_class_anim	beat2start			fenrir.kampf_schlag_b_start
	set_class_anim	beat2end			fenrir.kampf_schlag_b_end
	set_class_anim  standtofight		fenrir.stand_zu_kampf
	set_class_anim	fightstand			fenrir.kampf_stand
	set_class_anim	fightstandanim		fenrir.kampf_standanim
	set_class_anim	kick1start			fenrir.kampf_treten_a_start
	set_class_anim	kick1end			fenrir.kampf_treten_a_end
	set_class_anim	fall				fenrir.kampf_zu_knie
	set_class_anim  kungfustillani		fenrir.kampf_standanim

	set_class_anim	kneeshake			fenrir.knie_schuetteln
	set_class_anim	kneefall			fenrir.knie_umfallen
	set_class_anim	kneebeat1start		fenrir.knie_schlag_a_start
	set_class_anim	kneebeat1end		fenrir.knie_schlag_a_end
	set_class_anim	kneeroar			fenrir.knie_bruellen
	set_class_anim	kneestandanim		fenrir.knie_standanim

	set_class_anim	drink1				fenrir.trinken_start_ohne
	set_class_anim	drink2				fenrir.trinken_start_mit
	set_class_anim	drink3				fenrir.trinken_loop
	set_class_anim	drink4				fenrir.trinken_end_mit
	set_class_anim	drink5				fenrir.trinken_end_ohne

	set_class_anim	drunkfall			fenrir.betr_umfallen
	set_class_anim	drunkstand			fenrir.stand_betrunken

	class_defaultanim fenrir.bad_stand


	// Walk
	set_class_animset 0 {
		{standard			fenrir.stand_anim			}
		{walk_start			fenrir.gehen_start			}
		{walk_loop			fenrir.gehen_loop			}
		{walk_stop			fenrir.gehen_zu_stand		}

		{turn_left_90		fenrir.drehen_links			}
		{turn_right_90		fenrir.drehen_rechts		}
		{turn_left_180		fenrir.drehen_ganz			}
		{turn_right_180		fenrir.drehen_ganz			}

	}

	method destroy {} {
		destruct this
		del this
	}


	// virtual
	method im_attacking_you {} {}

	def_event evt_timer0
	handle_event evt_timer0 {
		set chairsitpos 	[get_pos this]
		set chairstandpos 	[get_pos this]
		set bathpos  		[get_pos this]
		set walkarea 		[list [get_pos this]]

		set leftfoot  [new FenrisFuss]
		link_obj $leftfoot this 15
		set rightfoot [new FenrisFuss]
		link_obj $rightfoot this 14

		set cup [obj_query this "-class Fenris_Krug -range 80 -limit 1"]

		set infoobjlist [obj_query this "-class Info_Fenris -range 200"]
		if {$infoobjlist != 0} {
			foreach obj $infoobjlist {
				set type [call_method $obj get_info type]
				if {$type == "chairstanding"} {
					set chairstandpos [get_pos $obj]
					log "Fenris: chairstandpos is $chairstandpos"
				}
				if {$type == "chairsitting"} {
					set chairsitpos [get_pos $obj]
					log "Fenris: chairsitpos is $chairsitpos"
				}
				if {$type == "bath"} {
					set bathpos [get_pos $obj]
					log "Fenris: bathpos is $bathpos"
				}
				if {$type == "walkarea"} {
					lappend walkarea [get_pos $obj]
					log "Fenris: walkarea is now $walkarea"
				}
			}
		}

		sitdown
		state_triggerfresh this idle
	}


	state_enter idle {
		if {$is_sitting} {
			set_anim this fenrir.sitzen_standanim 0 $ANIM_LOOP
		} else {
			set_anim this fenrir.stand_anim 0 $ANIM_LOOP
		}
	}

	state idle {
		global is_bathout poisoncount
		
		if {[check_kneefall]} {
			call_method this fall
			return
		}

		set attack_item [find_nearest_gnome]
		if {$attack_item > 0} {
			set enemypos [get_pos $attack_item]
			set dist [vector_dist [get_pos this] $enemypos]
			if {$dist > 7.0} {´
				log "[get_objname this] : approaching [get_objname $attack_item]"
				if {$is_sitting} {
					standupanim
				}
				walk_pos $enemypos
				return
			} else {
				log "[get_objname this] : attacking because of [get_objname $attack_item]"
				set attack_behaviour "offensive"
				set approach 0
				tasklist_clear this
				state_triggerfresh this fight_standing
				return
			}
			return
		}

		// Tasklist nur, wenn ich gerade keinen Kampf vorhabe :-)

		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
//			log "Fenris: Task to do:'$command'"
			eval $command
			return
		}

		// keine Tasklist, kein Kampf --- mal sehen, ob das Bad ausgelassen ist

		if {$is_bathout && $poisoncount < 2} {
			call_method this bathempty
			return
		}


		// und falls sogar das Bad eingelassen ist, können wir uns wieder hinsetzen

		if {$is_sitting == 0} {
			sitdown
			drink
		}

		wait_time_fenris 2
	}

	state_enter fight_standing {
		if {$is_sitting} {
			standupanim
		}
		state_disable this
		action this anim standtofight {
			set_anim this fightstandanim 0 2;
			state_enable this
		}
//		set last_beat_time [gettime]
	}

	state fight_standing {
		if {[check_kneefall]} {
			call_method this fall
			return
		}

		if {$attack_item == 0  ||  ![obj_valid $attack_item]} {
			state_trigger this idle
			return
		}

		set gnomelist [obj_query this "-range 10 -class Zwerg"]
		if {$gnomelist == 0} {
			state_trigger this idle
			return
		}

		if {[expr [gettime] - $last_beat_time] < 10.0} {
			log "[get_objname this] : not time yet..."
			wait_time_fenris 1
			return
		} else {
			state_disable this
			set_attackinprogress this 1
			set last_beat_time [gettime]
			set anim [lindex "{beat1start beat1end} {beat2start beat2end} {kick1start kick2end}" [irandom 3]]
			action this anim [lindex $anim 0] {
				strike;
				action this anim [lindex $anim 1] {
					state_enable this;
					set_attackinprogress this 0;
					set_anim this fightstandanim 0 $ANIM_LOOP
				}
			}
		}
	}	;# state fight_standing


	state_enter fight_kneeing {
		set_anim this kneestandanim 0 $ANIM_LOOP
		set last_beat_time [gettime]
	}

	state fight_kneeing {
		if {[is_defeated]} {
			call_method this finished
			return
		}

		set gnomelist [obj_query this "-range 10 -class Zwerg"]
		if {$gnomelist == 0} {
			wait_time_fenris 10
			return
		}

		if {[expr [gettime] - $last_beat_time] < 12.0} {
			log "[get_objname this] : not time yet..."
			wait_time_fenris 1
			return
		} else {
			state_disable this
			set_attackinprogress this 1
			set last_beat_time [gettime]
			set anim [lindex "{kneebeat1start kneebeat1end}" [irandom 1]]
			action this anim [lindex $anim 0] {
				strike;
				action this anim [lindex $anim 1] {
					state_enable this;
					set_attackinprogress this 0;
					set_anim this kneestandanim 0 $ANIM_LOOP
				}
			}
		}
	}	;# state fight_kneeing


	state dead {
		state_disable this
	}


	// schickt Fenris auf seinen Sessel

	method sitdown {} {
		tasklist_clear this
		sitdown
	}


	method standup {} {
		tasklist_clear this
		standup
	}

	method fall {} {
		global gnome_dummys

		set gnome_dummys "[new Fenris_greifen] [new Fenris_greifen] [new Fenris_greifen] [new Fenris_greifen] [new Fenris_greifen] [new Fenris_greifen]"
		set myref [get_ref this]
		call_method [lindex $gnome_dummys 0] set_position $myref 7
		call_method [lindex $gnome_dummys 1] set_position $myref 8
		call_method [lindex $gnome_dummys 2] set_position $myref 9
		call_method [lindex $gnome_dummys 3] set_position $myref 11
		call_method [lindex $gnome_dummys 4] set_position $myref 12
		call_method [lindex $gnome_dummys 5] set_position $myref 13
		log "fenris on his knees"

		state_disable this
		action this rotate 0 {
			action this anim fall {
				action this anim kneeroar {
					state_enable this;
					state_triggerfresh this fight_kneeing
				}
			}
		}
	}


	method finished {} {
		global gnome_dummys

		log "Fenris is finished!"
		state_triggerfresh this dead

		foreach obj $gnome_dummys {
			set zwerg [call_method $obj get_guest]
			link_obj $zwerg
			set_pos $zwerg "[expr {[get_posx this] + 3 + [random 6.0]}] [get_posy this] [expr {15 - [random 6.0]}]"
			set_hoverable $zwerg 1
			set_selectable $zwerg 1
			set_activegameplay $zwerg 1
			del $obj
		}

		catch { sm_set_event Fenris_besiegt }
		set i [new Trigger_Lava_400a]
		log "SEQUENCE 400a"
		set_pos $i [get_pos this]
	}


	// wird vom Stoepsel im Badezimmer aufgerufen, wenn er rausgezogen worden ist!

	method bathempty {} {
		global bathpos poisoncount seqcount is_sitting is_bathout

		set is_bathout 1

		if {[state_get this] != "idle"} {
			// es genügt, die Variable gesetzt zu haben, wir kümmern uns später darum
			return
		}

		if {$poisoncount == 0   &&  $seqcount == 0} {
			// Das Wasser wurde zum ersten mal rausgelassen --> Sequenz

			set i [new Trigger_Lava_340]
			set_pos $i [get_pos this]
			log "SEQUENCE 340!"
			wait_time_fenris 3
			incr seqcount

			tasklist_clear this
			set is_sitting 0
			tasklist_add this "set_pos this $bathpos; set_roty this 4.71; set_anim this standloop 0 2"
			tasklist_add this "wait_time_fenris 4"
			tasklist_add this "fillbath"
			sitdown
			drink

			return
		}


		if {$poisoncount == 1   &&  $seqcount == 1} {
			// Das Wasser wurde zum herausgelassen und Fenris ist beim letzen Mal vergiftet worden --> Sequenz

			set i [new Trigger_Lava_350]
			set_pos $i [get_pos this]
			log "SEQUENCE 350!"
			wait_time_fenris 3
			incr seqcount

			tasklist_clear this
			set is_sitting 0
			tasklist_add this "set_pos this $bathpos; set_roty this 4.71; set_anim this standloop 0 2"
			tasklist_add this "wait_time_fenris 4"
			tasklist_add this "fillbath"
			sitdown
			drink

			return
		}

		if {$poisoncount >= 2   &&  $seqcount == 2} {
			// Das Wasser wurde mehr als zweimal herausgelassen und Fenris ist schon zwei mal vergiftet worden --> Sequenz

			set i [new Trigger_Lava_360]
			set_pos $i [get_pos this]
			log "SEQUENCE 360!"
			incr seqcount

			// Quitschewiggle läßt sich ab jetzt drücken
			// ACHTUNG: evtl. gibt es mehrere davon, weil die ****-Sequenzhöhle noch drin ist!
			set wiggle [obj_query this "-class Quietschewiggle"]
			if {$wiggle != 0} {
				foreach obj $wiggle {
					call_method $obj activate
				}
			}

			return
		}


		// andernfalls: ohne Sequenz ins Bad gehen (wenn nicht schon 2x vergiftet

		if {$poisoncount < 2} {
			tasklist_clear this
			standup
			tasklist_add this "walk_pos \{ $bathpos \}"
			tasklist_add this "fillbath"
			sitdown
			drink
		}
	}


	// wird von Quietschewiggle im Badezimme aufgerufen, wenn er gedrückt wird

	method squeaksound {} {
		global bathpos poisoncount

		if {[state_get this] != "idle"} {
			return
		}

		if {$seqcount == 3} {
			// Das Wasser wurde mehr als zweimal herausgelassen und Fenris interessiert das inzwischen nicht mehr

			set i [new Trigger_Lava_370]
			set_pos $i [get_pos this]
			log "SEQUENCE 370!"
			incr seqcount

			tasklist_clear this
			set is_sitting 0
			tasklist_add this "set_pos this $bathpos; set_roty this 4.71; set_anim this standloop 0 2"
			tasklist_add this "wait_time_fenris 4"
			sitdown
			drink

			return
		}

		tasklist_clear this
		standup
		tasklist_add this "walk_pos \{ $bathpos \}"
		sitdown
		drink
	}


	obj_init {
		set_anim this fenrir.stand_anim 0 $ANIM_LOOP

		set_viewinfog this 0
		set_hoverable this 0
		set_selectable this 0
		set_visibility this 1
		set_collision this 1

		set is_sitting			0
		set last_beat_time		0
		set strike_range 		10.0

		set gnome_dummys		""				;# Liste von Dummy-Objekten, an denen sich die Zwerge festhalten
		set leftfoot			-1				;# linker Fuss (Kampfobjekt)
		set rightfoot			-1				;# rechter Fuss
		set cup					-1				;# mein Trinkgefäß

		set chairsitpos {0 0 0}					;# Position des Stuhles zum Hinsetzen (sitzen-Anim)
		set chairstandpos {0 0 0}				;# Position des Stuhles zum Davorstehen (hinsetzen-Anim)
		set bathpos  {0 0 0}					;# Position des Bades zum Hinlaufen und Nachsehen
		set walkarea ""							;# Liste von Punkten, in deren Nähe ich mich Aufhalten kann

		set poisoncount 0						;# ich bin schon x mal vergiftet worden!
		set seqcount	0						;# zählt die Zwischensequenzen der Vergiftungs-Aktion mit
		set is_bathout	0						;# Bad ist aus ja/nein (Falls ich im Kampf bin und es später füllen muss)

		catch { sm_add_event Fenris_besiegt }
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]


		// liefert 1, wenn Fenris von 6 Zwergen besetzt ist, sonst 0

		proc is_defeated {} {
			global gnome_dummys

			foreach obj $gnome_dummys {
				if {[call_method $obj get_guest] == -1} {
					return 0
				}
			}
			return 1
		}


		// überprüft, ob die beiden Füsse besiegt worden sind und Fenris auf die Knie gegen muss

		proc check_kneefall {} {
			global leftfoot rightfoot

			if { (![obj_valid $leftfoot]   ||  [get_objclass $leftfoot]  != "FenrisFuss")   &&
				 (![obj_valid $rightfoot]  ||  [get_objclass $rightfoot] != "FenrisFuss") } {
				return 1
			}
			return 0
		}


		// sucht nach nächstgelegenem Zwergen

		proc find_nearest_gnome {} {
			set fzwerg_list [obj_query this "-boundingbox {-15 -2 -9 15 0.5 9} -class Zwerg"]
//			log "fzwerg_list : $fzwerg_list"
			if { $fzwerg_list == 0 } {
				return -1
			}

			set ownpos [get_pos this]
			set gnome -1
			set mindist 10000
			foreach fzwerg $fzwerg_list {
				log "[get_objname this]: [get_objname $fzwerg] has [get_attrib $fzwerg atr_Hitpoints] HP"
				if { [get_attrib $fzwerg atr_Hitpoints] >= 0.01 } {
					set dist [vector_dist $ownpos [get_pos $fzwerg]]
					if { $dist < $mindist } {
						set mindist $dist					;// nahen zwerg suchen
						set gnome $fzwerg
					}
				}
			}

			log "nearest gnome is [get_objname $gnome]"
			return $gnome
		}	;// find_nearest_gnome



		// jetzt kriegen die Zwerge ordentlich eins auf die Rübe

		proc strike {} {
			global strike_range

			screenvibe 0.05 1 0.5 0.1 103 0.1 200;
			set gnomelist [obj_query this -class Zwerg -range $strike_range]
			if {$gnomelist == 0} {
				return
			}

			foreach obj $gnomelist {
				if {[expr {abs ([get_posy this] - [get_posy $obj])}] < 0.2} {
					call_method $obj fall
					add_attrib $obj atr_Hitpoints -[random 0.10]
				}
			}
		}

		proc sitdown {} {
			global chairstandpos

			tasklist_add this "walk_pos \{$chairstandpos\}"
			tasklist_add this "rotate_toangle 1.64"
			tasklist_add this "sitdownanim"
			tasklist_add this "wait_time_fenris 2"
		}

		proc sitdownanim {} {
			global is_sitting chairsitpos chairstandpos leftfoot rightfoot

			if {$is_sitting} {
				return
			}

			if {[obj_valid $leftfoot]} {
				set_hoverable $leftfoot 0
			}
			if {[obj_valid $rightfoot]} {
				set_hoverable $rightfoot 0
			}

			state_disable this
			set_pos this $chairsitpos
			set is_sitting 1
			action this anim sitdown {
				set_pos this "$chairsitpos";
				set_anim this fenrir.sitzen_standanim 0 2;
				state_enable this
			}
		}


		proc standup {} {
			global chairstandpos

			tasklist_add this "wait_time_fenris 2"
			tasklist_add this "standupanim"
			tasklist_add this "walk_pos \{ [vector_add [get_pos this] {0 0 4}] \}"
		}


		proc standupanim {} {
			global is_sitting chairsitpos chairstandpos leftfoot rightfoot

			if {$is_sitting == 0} {
				return
			}

			if {[obj_valid $leftfoot]} {
				set_hoverable $leftfoot 1
			}
			if {[obj_valid $rightfoot]} {
				set_hoverable $rightfoot 1
			}

			state_disable this
			set_pos this $chairsitpos;
			set is_sitting 0
			action this anim standup {
				set_pos this "$chairstandpos";
				set_anim this fenrir.stand_anim 0 2;
				state_enable this
			}
		}


		// trinken-Anims (geht nur im Sitzen)

		proc drink {} {
			global cup

			tasklist_add this "play_anim drink1"
			tasklist_add this "play_anim drink2; call_method $cup hide"
			tasklist_add this "play_anim drink3"
			tasklist_add this "play_anim drink3; poisoncheck"
			tasklist_add this "play_anim drink3"
			tasklist_add this "play_anim drink3"
			tasklist_add this "play_anim drink4"
			tasklist_add this "play_anim drink5; call_method $cup show"
			tasklist_add this "set_anim this sitting 0 2"
		}


		// füllt die Badewanne in Fenris Bad nach

		proc fillbath {} {
			global is_bathout

			set plug [obj_query this "-class Fenris_Stoepsel -range 80 -limit 1"]

			if {$plug > 0} {
				call_method $plug putin
				set is_bathout 0
			}
		}


		// überprüft das Trinkgefäß auf Gift
		// reagiert entsprechend

		proc poisoncheck {} {
			global poisoncount cup

			if {![obj_valid $cup]} {
				return
			}

			set result [call_method $cup drink]
			if {$result > 0} {
				incr poisoncount
			}

			if {$poisoncount >= 3} {
				set i [new Trigger_Lava_400b]
				set_pos $i [get_pos this]
				log "SEQUENCE 400b!"
				catch { sm_set_event Fenris_besiegt }
			}
		}


		// laufe an Position

		proc walk_pos {pos} {
			global walkarea

			if {[lindex $pos 2] > 10} {
				set pos "[lindex $pos 0] [lindex $pos 1] 10"
			}

			set mypos [get_pos this]
			set ok 0
			foreach wpos $walkarea {
				if {[vector_dist $wpos $mypos] <= 20} {
					set ok 1
				}
			}

			if {$ok == 0} {
				log "Fenris: Zielposition nicht erlaubt! ($pos)"
				return
			}

			state_disable this
			set pos [vector_fix $pos]
			action this walk "-target \{$pos\} -canclimb 0 -animsets 0" {state_enable this}
			return true
		}


		proc wait_time_fenris {time} {
			state_disable this
			action this wait $time { state_enable this }
		}

		proc play_anim {anim} {
			state_disable this
			action this anim $anim {state_enable this; set_idle_anim}
			return true
		}


		proc rotate_toright {}  {state_disable this;action this rotate 4.71 	{state_enable this}}
		proc rotate_toleft {}   {state_disable this;action this rotate 1.57 	{state_enable this}}
		proc rotate_toback {}   {state_disable this;action this rotate 3.14 	{state_enable this}}
		proc rotate_tofront {}  {state_disable this;action this rotate 0 		{state_enable this}}
		proc rotate_toangle {a} {state_disable this;action this rotate $a 		{state_enable this}}



		proc set_idle_anim {} {
			global ANIM_LOOP
			set_anim this fenrir.stand_anim 0 $ANIM_LOOP
		}

	}  	;// obj_init
} 	;// class Fenris



def_class Fenris_001 none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	set_class_anim	standloop			fenrir.stand_anim
	set_class_anim	walkstart			fenrir.gehen_start
	set_class_anim	walkloop			fenrir.gehen_loop
	set_class_anim	walkstop			fenrir.gehen_end
	set_class_anim	boardtalkstart		fenrir.reden_tafel_start
	set_class_anim	boardtalkloop		fenrir.reden_tafel_loop
	set_class_anim	boardturnastart		fenrir.umdrehen_tafel_a_start
	set_class_anim	boardturnaloop		fenrir.umdrehen_tafel_a_loop
	set_class_anim	boardturnbstart		fenrir.umdrehen_tafel_b_start
	set_class_anim	boardturnbloop		fenrir.umdrehen_tafel_b_loop
	set_class_anim	shocka				fenrir.schreck_a
	set_class_anim	shockb				fenrir.schreck_b
	set_class_anim	boardloop			fenrir.tafel_a_loop
	set_class_anim	tvtalkastart		fenrir.reden_fernseher_a_start
	set_class_anim	tvtalkaloop			fenrir.reden_fernseher_a_loop
	set_class_anim	tvaloop				fenrir.fernseher_a_loop
	set_class_anim	tvtalkatob			fenrir.reden_fernseher_a_zu_b
	set_class_anim	tvtalkbstart		fenrir.reden_fernseher_b_start
	set_class_anim	tvtalkbloop			fenrir.reden_fernseher_b_loop
	set_class_anim	tvtalkbstop			fenrir.reden_fernseher_b_end
	set_class_anim	lolli				fenrir.lolli

	set_class_anim	sitfist				fenrir.sitzen_fausttisch
	set_class_anim	sitdown				fenrir.hinsetzen
	set_class_anim	sithairstart		fenrir.sitzen_haare_start
	set_class_anim	sithairloop			fenrir.sitzen_haare_loop
	set_class_anim	sithairstop			fenrir.sitzen_haare_end
	set_class_anim	sithairstoploop		fenrir.sitzen_haare_end_loop
	set_class_anim	sitplantstart		fenrir.sitzen_pflanzen_start
	set_class_anim	sitplantloop		fenrir.sitzen_pflanzen_loop
	set_class_anim	sittalkb			fenrir.sitzen_reden_b
	set_class_anim	sittalkc			fenrir.sitzen_reden_c
	set_class_anim	sittalkd			fenrir.sitzen_reden_d
	set_class_anim	sithit				fenrir.sitzen_schlagen
	set_class_anim	sitstandloop		fenrir.sitzen_standanim

	// Walk
	set_class_animset 0 {
		{standard			fenrir.stand_anim			}
		{walk_start			fenrir.gehen_start			}
		{walk_loop			fenrir.gehen_loop			}
		{walk_stop			fenrir.gehen_end			}

		{turn_left_90		fenrir.drehen_links			}
		{turn_right_90		fenrir.drehen_rechts		}
		{turn_left_180		fenrir.drehen_links			}
		{turn_right_180		fenrir.drehen_rechts		}
//		{turn_left_180		fenrir.drehen_ganz			}
//		{turn_right_180		fenrir.drehen_ganz			}

	}


	class_defaultanim fenrir.bad_stand

	method idle_anim {} {
		// absichtlich leer!
	}

	method burnbaby {} {
		change_particlesource this 20 34 {0 0 0} {0 0 0} 4096 16 0 4 2.4 1 0
		set_particlesource this 20 1
	}

	obj_init {

		set_anim this fenrir.stand_anim 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0

	}

}


def_class Fenris_Drunk none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	set_class_anim	getup				fenrir.hinstellen_sitzen_start
	set_class_anim	standtalk			fenrir.hinstellen_reden_loop
	set_class_anim	sitdown				fenrir.hinstellen_sitzen_end
	set_class_anim	drinkstarta			fenrir.trinken_start_ohne
	set_class_anim	drinkstartb			fenrir.trinken_start_mit
	set_class_anim	drinkloop			fenrir.trinken_loop
	set_class_anim	drinkstopb			fenrir.trinken_end_mit
 	set_class_anim	drinkstopa			fenrir.trinken_end_ohne
 	set_class_anim	drunkstand			fenrir.stand_betrunken
 	set_class_anim	drunkfall			fenrir.betr_umfallen
	set_class_anim	sittalkb			fenrir.sitzen_reden_b
	set_class_anim	sittalkc			fenrir.sitzen_reden_c
	set_class_anim	getupfast			fenrir.sitzen_zu_gehen
	set_class_anim	fenris2fifi			fenrir.fenris_zu_fifi
	set_class_anim	sitloop				fenrir.sitzen_standanim


	class_defaultanim fenrir.bad_stand

	method idle_anim {} {
		// absichtlich leer!
	}

	method burnbaby {} {
		change_particlesource this 20 34 {0 0 0} {0 0 0} 4096 16 0 4 2.4 1 0
		set_particlesource this 20 1
	}

	obj_init {

		set_anim this fenrir.bad_stand_anim 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0

	}

}






def_class Fenris_003 none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	set_class_anim	bathwhistlea		fenrir.bad_floeten_a
	set_class_anim	bathtalknothing		fenrir.bad_nix_reden
	set_class_anim	bathtalkbloop		fenrir.bad_reden_a
	set_class_anim	bathtalkastart		fenrir.bad_reden_b_start
	set_class_anim	bathtalkaloop		fenrir.bad_reden_b
	set_class_anim	bathtalkastop		fenrir.bad_reden_b_end
	set_class_anim	bathtalkend			fenrir.bad_reden_end
	set_class_anim	bathtalkstart		fenrir.bad_reden_start
	set_class_anim	bathstand			fenrir.bad_stand
	set_class_anim	bathstandloop		fenrir.bad_stand_anim
	set_class_anim	bathwash			fenrir.bad_waschen
	set_class_anim	bathwigglestart		fenrir.quietschewiggle_start
	set_class_anim	bathwiggleloop		fenrir.quietschewiggle_loop
	set_class_anim	bathwigglestop		fenrir.quietschewiggle_end

	class_defaultanim fenrir.bad_stand

	method idle_anim {} {
		// absichtlich leer!
	}

	method burnbaby {} {
		change_particlesource this 20 34 {0 0 0} {0 0 0} 4096 16 0 4 2.4 1 0
		set_particlesource this 20 1
	}

	obj_init {

		set_anim this fenrir.bad_stand_anim 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0
	}

}

def_class Fenris_400 none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	set_class_anim	knee_roar			fenrir.knie_bruellen
	set_class_anim	knee_shake			fenrir.knie_schuetteln
	set_class_anim	knee_fall			fenrir.knie_umfallen
	set_class_anim	unconscious			fenrir.bewusstlos
	set_class_anim	fenris2fifi			fenrir.fenris_zu_fifi

	class_defaultanim fenrir.bewusstlos

 	method idle_anim {} {
		// absichtlich leer!
	}

	method burnbaby {} {
		change_particlesource this 20 34 {0 0 0} {0 0 0} 4096 16 0 4 2.4 1 0
		set_particlesource this 20 1
	}

	obj_init {

		set_anim this fenrir.bewusstlos 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0

	}

}

def_class Fenris_002 none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	set_class_anim	throntalka			fenrir.thron_reden_a
	set_class_anim	throntalkbstart		fenrir.thron_bruellen_start
	set_class_anim	throntalkbloop		fenrir.thron_bruellen_loop
	set_class_anim	throntalkbstop		fenrir.thron_bruellen_end
	set_class_anim	thronsit			fenrir.thron_sitzen
	set_class_anim	throndespairstart	fenrir.thron_verzweifeln_start
	set_class_anim	throndespairloop	fenrir.thron_verzweifeln_loop_a
	set_class_anim	throndespairend		fenrir.thron_verzweifeln_end
	set_class_anim	throndespairloopb	fenrir.thron_verzweifeln_loop_b
	set_class_anim	thronnanana			fenrir.thron_nanana

	class_defaultanim fenrir.thron_sitzen

 	method idle_anim {} {
		// absichtlich leer!
	}

	method burnbaby {} {
		change_particlesource this 20 34 {0 0 0} {0 0 0} 4096 16 0 4 2.4 1 0
		set_particlesource this 20 1
	}

	obj_init {

		set_anim this fenrir.thron_sitzen 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0

	}

}

def_class Fifi_mit_Gleipnir none monster 0 {} {
	call scripts/misc/animclassinit.tcl

	set_class_anim	newfifi			fifi.verwandeln_b

	class_defaultanim fifi.stand_g

 	method idle_anim {}	{
		set_anim this fifi.standanim_g 0 $ANIM_LOOP
 	}

	obj_init {
		set_anim this fifi.standanim_g 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0
	}

}

def_class Fifi none monster 0 {} {
	call scripts/misc/animclassinit.tcl

	set_class_anim	bark			fifi.klaeffen
	set_class_anim	oldfifi			fifi.verwandeln_a
	set_class_anim	jump			fifi.hopsen
	set_class_anim	standanim		fifi.standanim
	set_class_anim	beinheben		fifi.bein_heben


	class_defaultanim fifi.stand

	// Walk
	set_class_animset 0 {
		{standard			fifi.standanim				}
		{walk_start			fifi.gehen_start			}
		{walk_loop			fifi.gehen_loop				}
		{walk_stop			fifi.gehen_end				}

		{turn_left_90		fifi.traben_start			}
		{turn_right_90		fifi.traben_start           }
		{turn_left_180		fifi.traben_start			}
		{turn_right_180		fifi.traben_start			}

	}


 	method idle_anim {}	{
		set_anim this fifi.standanim 0 $ANIM_LOOP
 	}

	obj_init {
		set_anim this fifi.standanim 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0
	}

}

