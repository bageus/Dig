def_class Zuschauer none none 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members

	set_class_anim turn180right		mann.drehen_ganz
	set_class_anim turn180left		mann.drehen_ganz_links
	set_class_anim turnright		mann.drehen_rechts
	set_class_anim turnleft			mann.drehen_links
	set_class_anim rotateright		mann.drehen_rechts
	set_class_anim rotateleft		mann.drehen_links

	set_class_anim walkstart		mann.gehen_start
	set_class_anim walkloop			mann.gehen_loop
	set_class_anim walkwaveloop		mann.gehen_gruessen
	set_class_anim walkstop			mann.gehen_end
	set_class_anim fleestart		mann.fliehen_start
	set_class_anim fleeloop			mann.fliehen_loop
	set_class_anim fleestop			mann.fliehen_end
	set_class_anim walkfaststart	mann.laufen_schnell_start
	set_class_anim walkfastloop		mann.laufen_schnell_loop
	set_class_anim walkfastwaveloop	mann.gehen_fit_gruessen
	set_class_anim walkfaststop		mann.laufen_schnell_end

	set_class_anim tooltakeout_a	mann.werkzeug_raus_a
	set_class_anim tooltakeout_b	mann.werkzeug_raus_b
	set_class_anim toolputaway_a	mann.werkzeug_weg_a
	set_class_anim toolputaway_b	mann.werkzeug_weg_b

	set_class_anim standstill		mann.standard
	set_class_anim standloopa		mann.stand_anim_a
	set_class_anim standloopb		mann.stand_anim_b
	set_class_anim standloopc		mann.stand_anim_c
	set_class_anim standloopd		mann.stand_anim_d
	set_class_anim stopmove_a		mann.stand_anim_a
	set_class_anim stopmove_b		mann.stand_anim_b
	set_class_anim wait				mann.warten

	set_class_anim jumpa			mann.hopsen_a
	set_class_anim jumpb			mann.hopsen_b
	set_class_anim teeter_t			mann.verlegen
	set_class_anim scratch			mann.kratzen
	set_class_anim breathe			mann.aufatmen
	set_class_anim stretch			mann.recken
	set_class_anim knitstart		mann.stricken_start
	set_class_anim knitloop			mann.stricken_loop
	set_class_anim knitstop			mann.stricken_end
	set_class_anim carvestart		mann.schnitzen_start
	set_class_anim carveloop		mann.schnitzen_loop
	set_class_anim carvestop		mann.schnitzen_end
	set_class_anim cough			mann.raeuspern
	set_class_anim teeter_w			mann.wippen
	set_class_anim wipenose			mann.naseabstreifen
	set_class_anim washface			mann.gesicht_reiben
	set_class_anim kneebend			mann.kniebeuge
	set_class_anim leanstart		mann.anlehnen_start
	set_class_anim leanloop			mann.anlehnen_loop
	set_class_anim leanstop			mann.anlehnen_end
	set_class_anim smokepipestart	mann.pfeife_rauchen_start
	set_class_anim smokepipeloop	mann.pfeife_rauchen_loop
	set_class_anim smokepipestop	mann.pfeife_rauchen_end

	set_class_anim die				mann.sterben
	set_class_anim scout			mann.spaehen
	set_class_anim leftright		mann.blicken_rechts_links
	set_class_anim impatient		mann.ungeduldig

	set_class_anim bowlwin			mann.bowl_gewinnen
	set_class_anim cheer			mann.jubeln
	set_class_anim applaud			mann.applaudieren

	set_class_anim boo				mann.ausbuhen
	set_class_anim tired			mann.erschoepft
	set_class_anim shock			mann.schreck
	set_class_anim bowllose			mann.bowl_verlieren
	set_class_anim warmbutt			mann.popo_waermen

	set_class_anim talka			mann.unterhalten_a
	set_class_anim talkb			mann.unterhalten_b
	set_class_anim talkc			mann.unterhalten_c
	set_class_anim talkd			mann.unterhalten_d
	set_class_anim talke			mann.unterhalten_e
	set_class_anim talkf			mann.unterhalten_f
	set_class_anim talkg			mann.unterhalten_g
	set_class_anim talkh			mann.unterhalten_h
	set_class_anim talki			mann.unterhalten_i
	set_class_anim talkk			mann.unterhalten_k
	set_class_anim talkm			mann.unterhalten_m
	set_class_anim talkn			mann.unterhalten_n
	set_class_anim talko			mann.unterhalten_o
	set_class_anim talkp			mann.unterhalten_p
	set_class_anim talkq			mann.unterhalten_q
	set_class_anim talkr			mann.unterhalten_r
	set_class_anim talks			mann.unterhalten_s

	set_class_anim sitdown			mann.hinsetzen
	set_class_anim sitfloorstill	mann.sitzen_boden_stand
	set_class_anim standup			mann.aufstehen
	set_class_anim sitdown_edge		mann.hinsetzen_rand
	set_class_anim sitedgeloop		mann.sitzen_rand_loop_a
	set_class_anim sitedgestill		mann.sitzen_rand_stand
	set_class_anim standup_edge		mann.aufstehen_rand
	set_class_anim sitdown_chair	mann.hinsetzen_stuhl
	set_class_anim sitchairbore		mann.sitzen_stuhl_langw
	set_class_anim sitchairloop		mann.sitzen_stuhl_loop
	set_class_anim standup_chair	mann.aufstehen_stuhl

	set_class_anim standsleepstart	mann.stehend_schlafen_start
	set_class_anim standsleeploop	mann.stehend_schlafen_loop
	set_class_anim standsleepstop	mann.stehend_schlafen_end
	set_class_anim drinktubstart	mann.bottich_trinken_start
	set_class_anim drinktubloop		mann.bottich_trinken_loop
	set_class_anim drinktubstop		mann.bottich_trinken_end
	set_class_anim discoa			mann.disco_a
	set_class_anim discoc			mann.disco_c
	set_class_anim discod			mann.disco_d
	set_class_anim drinkbarrelstart	mann.trinken_fass_start
	set_class_anim drinkbarrelloop	mann.trinken_fass_loop
	set_class_anim drinkbarrelstop	mann.trinken_fass_end

	set_class_anim aud_look			mann.publikum_gucken
	set_class_anim aud_hand			mann.publikum_hand
	set_class_anim aud_bore			mann.publikum_langweilen
	set_class_anim aud_laola		mann.publikum_laola
	set_class_anim aud_idle			mann.publikum_standloop
	set_class_anim aud_wave			mann.publikum_winken

//	call scripts/misc/obj_attribs.tcl

	def_event evt_timer0
	def_event evt_timer1
	// states are:
	// idle - idle
	// task - idependent task

	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members

		set gnome_gender "unset"
		set was_baby 1
		set is_counterwiggle 0
		set gnome_initialized 0
		set info_string {}
		set last_laola 0
		//  {idle positiv negativ expecting shocking conversation}
		set behaviour_ratio {6 1 0 0 0 0}
		// status's: stand sit lean
		set status "stand"
		set laola_list [list]
		set standactions [list]
		lappend standactions {standstill standloopa standloopb standloopc}
		lappend standactions {standloopd wait jumpa jumpb teeter_t scratch breathe stretch "knit 3" "carve 2" cough teeter_w wipenose kneebend "smokepipe 4"}
		lappend standactions {bowlwin cheer applaud}
		lappend standactions {boo bowllose warmbutt}
		lappend standactions {"do expecting"}
		lappend standactions {"do shocked"}
		lappend standactions {"do find_talkpartner"}
		set sitactions [list]

		lappend sitactions {aud_bore aud_idle}
		lappend sitactions {aud_look aud_hand aud_wave}
		lappend sitactions {"do laola"}
		set_texturevariation this [irandom 4] 0
		set_anim this mann.standard 0 $ANIM_LOOP		;# set standard anim
		set_collision this 1
		set_attrib this carrycap 1
		set_attrib this hitpoints 1
		set_hoverable this 0
		set_selectable this 0

		state_reset this
		state_trigger this idle
		state_enable this
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3
		proc play_anim {anim} {
			state_disable this
			action this anim $anim {state_enable this}
			return true
		}
		proc loop_anim {anim cnt} {
			tasklist_add this "play_anim ${anim}start"
			for {set i 0} {$i<$cnt} {incr i} {
				tasklist_add this "play_anim ${anim}loop"
			}
			tasklist_add this "play_anim ${anim}stop"
		}
		proc levelize_ratiolist {} {
			global behaviour_ratio
			set sum 0.0
			set nl [list]
			foreach fract $behaviour_ratio {fincr sum $fract}
			if {$sum<0.01} {set behaviour_ratio {1 0 0 0 0 0};return}
			foreach fract $behaviour_ratio { lappend nl [expr $fract / $sum ] }
			set behaviour_ratio $nl
		}
		proc rotate_tognome {} {
			global status
			if {[set item [obj_query this "-class Zwerg -range 12 -limit 1"]]} {
				set angle [expr 1.57+[vector_angle [get_pos this] [get_pos $item]]]
				if {$status=="stand"} {
					state_disable this;
					action this rotate $angle {state_enable this}
				} else {
					set_roty this $angle
				}
				return 1
			} else {return 0}
		}
		proc expecting {} {
			if {[rotate_tognome]} {
				tasklist_add this "play_anim [lindex {wait impatient applaud} [irandom 3]]"
			} else {
				tasklist_add this "play_anim [lindex {scout leftright} [irandom 2]]"
			}
		}
		proc shocked {} {state_disable this;action this wait 1 {state_enable this}}
		proc find_talkpartner {} {state_disable this;action this wait 1 {state_enable this}}
		proc get_info {name} {
			global info_string
			if { ![info exists info_string] } {set info_string ""}
			foreach item $info_string {
				set inam [lindex $item 0]
				set ival [lindex $item 1]
				if { $name == $inam } {
					return $ival
				}
			}
			return 0
		}
	}
	method change_behaviour {ratiolist} {
		foreach ratio $ratiolist {
			set nr [string map {idle 0 pos 1 neg 2 expect 3 shock 4 talk 5} [lindex $ratio 0]]
			//set behaviour_ratio [lreplace $behaviour_ratio $nr $nr [lindex $ratio 1]
			lrep behaviour_ratio $nr [lindex $ratio 1]
		}
		levelize_ratiolist
	}
	method set_behaviour {ratiolist} {
		set behaviour_ratio $ratiolist
		levelize_ratiolist
	}
	method call_behaviour {activity} {
		tasklist_add this $activity
		state_triggerfresh this task
	}
	method Editor_Set_Info {infolist} {
		global info_string
		set info_string $infolist
		foreach sublist $infolist {
			switch [lindex $sublist 0] {
				"status" {set status [lindex $sublist 1]}
				"behave" {set behaviour_ratio [lindex $sublist 1];levelize_ratiolist}
			}
		}
	}

	method invoke_laola {} {
		global last_laola laola_list
		set now [gettime]
		if { $now - $last_laola < 6  } { return }
		set last_laola $now

		set xn -1.2
		set xp 1.2
		set bbn [vector_pack $xn -2 -5]
		set bbp [vector_pack $xp +2 +5]
		set zl [obj_query this "-class Zuschauer -boundingbox \{$bbn $bbp\}"]
		set laola_list [list]
		if { $zl != 0 } {
			foreach item $zl {
				if { [call_method $item get_status] == "sit" } {
					lappend laola_list $item
				}
			}
		}
		play_anim aud_laola
		timer_event this evt_timer1 -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 0.2]
	}

	method get_status {} {
		return $status
	}

	call scripts/classes/zwerg/z_methods.tcl
	handle_event evt_timer0 {
		set_owner this [irandom 5]
		levelize_ratiolist
		call_method this init
	}

	handle_event evt_timer1 {
		foreach item $laola_list {
			call_method $item invoke_laola
		}
	}


	state idle {
		set rnd [expr rand()]
		set currentlist [subst $${status}actions]
		set level 0.0
		for {set i 0} {$i<6} {incr i} {
			fincr level [lindex $behaviour_ratio $i]
			if {$rnd<$level} {
				set actionlist [lindex $currentlist $i]
				set activity [lindex $actionlist [irandom [llength $actionlist]]]
				if {$activity==""} {continue}
				if {[llength $activity]==1} {
					if { [irandom 10] == 5 && $status == "stand" } {
						tasklist_add this "rotate_tognome"
					}
					tasklist_add this "play_anim $activity"
				} elseif {[lindex $activity 0]=="do"} {
					eval [lindex $activity 1]
				} else {
					if { [irandom 10] == 5 && $status == "stand" } {
						tasklist_add this "rotate_tognome"
					}
					eval "loop_anim $activity"
				}
				state_triggerfresh this task
				return
			}
		}
		state_disable this
		action this wait 1 { state_enable this }
	}

	state task {
//		log "state:task"
		if { [tasklist_cnt this] == 0 } {
			state_trigger this idle
		} else {
			set command [tasklist_get this 0]
			tasklist_rem this 0
//			#log "[get_objname this]: Task to do:'$command'"
			catch {eval $command}
		}
	}
}

def_class Trompeter none gnome 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members
	def_event evt_timer0
	handle_event evt_timer0 {
		sel /obj
	//	set trumpet [new Dummy_Trompete]
	//	link_obj $trumpet this 0
		set helm [new Dummy_Muetze_kampf_01_a]
		link_obj $helm this 4
	}


	call scripts/classes/zwerg/z_anims.tcl
	call scripts/classes/zwerg/z_faceanim.tcl


	set_class_anim fanfare			mann.fanfare
	set_class_anim standstill		mann.standard
	set_class_anim standloopa		mann.stand_anim_a
	set_class_anim standloopb		mann.stand_anim_b
	set_class_anim standloopc		mann.stand_anim_c
	set_class_anim standloopd		mann.stand_anim_d
	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members
		call scripts/classes/zwerg/z_faceanim.tcl
		set gnome_gender "unset"
		set is_counterwiggle 0
		set was_baby 0
		set info_string ""
		set_textureanimation this 0 4 0 0
		set_textureanimation this 1 4 0 0
		set_textureanimation this 2 3 0 0
		set_textureanimation this 4 8 0 0
		set_anim this mann.standard 0 $ANIM_LOOP		;# set standard anim
		set_collision this 1
		set_attrib this carrycap 1
		set_attrib this hitpoints 1
		set_hoverable this 0
		set_selectable this 0
		state_triggerfresh this idle
		set mode standard
		set counter -1
		set idle_anims {standstill standloopa standloopb standloopc standloopd}

		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3

        proc set_idle_anim {} {
        	if { [get_gnomeposition this] == 0 } {
        			switch [hf2i [random 4]] {
        				0 	{set_anim this mann.stand_anim_a 0 2 }
        				1	{set_anim this mann.stand_anim_b 0 2 }
        				2	{set_anim this mann.stand_anim_c 0 2 }
        				3	{set_anim this mann.stand_anim_d 0 2 }
        			}
        	} else {
        		if {abs([get_roty this]-3.14)<0.1} {set_roty this 3.14}
        		set_anim this mann.kletterstand_anim 0 2 ;#set idle anim
        	}
        }

	}

	method idle_anim {} {
		set_idle_anim
	}

	method set_action {activity} {
		set mode [lindex $activity 0]
		set counter [lindex $activity 1]
		if {$counter==""} {set counter -1}
	}
	state idle {
		state_disable this
		if {$counter&&$mode=="fanfare"} {
			action this anim fanfare {state_enable this}
		} else {
			action this anim [lindex $idle_anims [irandom 5]] {state_enable this}
		}
	}
}


def_class Koenig none gnome 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members
	def_event evt_timer0
	handle_event evt_timer0 {
		sel /obj
		set m [new Dummy_Krone]
		link_obj $m this 4
	}

	call scripts/classes/zwerg/z_anims.tcl
	call scripts/classes/zwerg/z_faceanim.tcl


	set_class_anim fanfare			mann.fanfare
	set_class_anim standstill		mann.standard
	set_class_anim standloopa		mann.stand_anim_a
	set_class_anim standloopb		mann.stand_anim_b
	set_class_anim standloopc		mann.stand_anim_c
	set_class_anim standloopd		mann.stand_anim_d
	set_class_anim idle				mann.publikum_standloop
	set_class_anim wave				mann.publikum_winken

	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members
		call scripts/classes/zwerg/z_faceanim.tcl
		set gnome_gender "unset"
		set was_baby 0
		set info_string ""
		set_textureanimation this 1 18
		set_textureanimation this 0 4
		set_anim this mann.publikum_standloop 0 $ANIM_LOOP		;# set standard anim
		set_collision this 1
		set_attrib this carrycap 1
		set_attrib this hitpoints 1
		set_hoverable this 0
		set_selectable this 0
		state_triggerfresh this idle
		set mode standard
		set counter -1
		set is_counterwiggle 0
		set idle_anims {standstill standloopa standloopb standloopc standloopd}
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3

        proc set_idle_anim {} {
        	if { [get_gnomeposition this] == 0 } {
        			switch [hf2i [random 4]] {
        				0 	{set_anim this mann.stand_anim_a 0 2 }
        				1	{set_anim this mann.stand_anim_b 0 2 }
        				2	{set_anim this mann.stand_anim_c 0 2 }
        				3	{set_anim this mann.stand_anim_d 0 2 }
        			}
        	} else {
        		if {abs([get_roty this]-3.14)<0.1} {set_roty this 3.14}
        		set_anim this mann.stand_anim_c 0 2 ;#set idle anim
        	}
        }



	}
	method set_action {activity} {
		set mode [lindex $activity 0]
		set counter [lindex $activity 1]
		if {$counter==""} {set counter -1}
	}
	method idle_anim {} {
		set_idle_anim
	}

	state idle {
		set anim "idle"
		if { [irandom 10] == 1 } {
			set anim "wave"
		}
		state_disable this
		action this anim $anim "state_enable [get_ref this]"

	}
}

def_class Koenig_im_Bett none gnome 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/classes/zwerg/z_faceanim.tcl

	set_class_anim kingbedloopa			mann.koenig_bett_loop_a
	set_class_anim kingbedloopb			mann.koenig_bett_loop_b
	set_class_anim kingwakeupstop		mann.koenig_bett_wecken_end
	set_class_anim kingwakeuploop		mann.koenig_bett_wecken_loop
	set_class_anim kingwakeupstart		mann.koenig_bett_wecken_start
	set_class_anim kingbadmood			mann.koenig_boese
	set_class_anim kingendloop			mann.koenig_ende_loop
	set_class_anim kingendtalk			mann.koenig_ende_reden
	set_class_anim kingendstart			mann.koenig_ende_start
	set_class_anim kingtired			mann.koenig_gaehnen
	set_class_anim kingendsleepstart	mann.koenig_ende_schlafen_start
	set_class_anim kingendsleeploop		mann.koenig_ende_schlafen_loop
	set_class_anim kinglookupa			mann.koenig_hochgucken_a
    set_class_anim kinglookupbloop		mann.koenig_hochgucken_b_loop
    set_class_anim kinglookupbstop		mann.koenig_hochgucken_b_end
    set_class_anim kingwithoutclock		mann.koenig_ohneuhr_b
    set_class_anim kingtalka			mann.koenig_reden_a
    set_class_anim kingtalkb			mann.koenig_reden_b
    set_class_anim kingsleeploop		mann.koenig_schlafen_loop
    set_class_anim kingsitstandanim		mann.koenig_sitzen_standanim
    set_class_anim kingturnaround		mann.koenig_umdrehen


	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members
		call scripts/classes/zwerg/z_faceanim.tcl
		set gnome_gender "unset"
		set is_counterwiggle 0
		set was_baby 0
		set info_string ""
		set_textureanimation this 1 12
		set_textureanimation this 0 11
		set_collision this 1
		set_hoverable this 0
		set_selectable this 0
    }

	method idle_anim {} {
	}

}


def_class Einsiedler none tool 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members
	def_event evt_timer0
	handle_event evt_timer0 {
		sel /obj
        set muetze [new Dummy_Bigbart]
        link_obj $muetze this 4
	}

	call scripts/classes/zwerg/z_anims.tcl
	call scripts/classes/zwerg/z_faceanim.tcl

	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members
		call scripts/classes/zwerg/z_faceanim.tcl
		set current_common_mood bad_normal
		set_fanim_feeling $current_common_mood
		set is_counterwiggle 0
		set info_string ""

		set_textureanimation this 1 13
		set_textureanimation this 0 12
		set_textureanimation this 2 5
		set_anim this mann.sitzen_sofa_loop 0 $ANIM_LOOP		;# set standard anim
		set_collision this 1
		set_attrib this carrycap 1
		set_attrib this hitpoints 1
		set_hoverable this 1
		set_selectable this 0
		state_triggerfresh this idle
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3
		set_objname this "Einsiedler"

        proc set_idle_anim {} {
        	if { [get_gnomeposition this] == 0 } {
        			switch [hf2i [random 4]] {
        				0 	{set_anim this mann.stand_anim_a 0 2 }
        				1	{set_anim this mann.stand_anim_b 0 2 }
        				2	{set_anim this mann.stand_anim_c 0 2 }
        				3	{set_anim this mann.stand_anim_d 0 2 }
        			}
        	} else {
        		if {abs([get_roty this]-3.14)<0.1} {set_roty this 3.14}
        		set_anim this mann.kletterstand_anim 0 2 ;#set idle anim
        	}
        }
		set_hoverable this 0

	}
	method set_action {activity} {
		set mode [lindex $activity 0]
		set counter [lindex $activity 1]
		if {$counter==""} {set counter -1}
	}

	method idle_anim {} {
		set_idle_anim
	}

	state idle {
		set anim "couchloopa"
		state_disable this
		action this anim $anim "state_enable [get_ref this]"

	}
}


def_class Torwaechterin none tool 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members
	def_event evt_timer0
	handle_event evt_timer0 {
		set_alternateanimdb this true
		sel /obj
		if { [get_info untereTW]==1 && [sm_get_event Torwaechterin_befreit]==0} {
			set_visibility this 0
		} else {
			set_visibility this 1
		}
//		Axel was here, again...
		set_textureanimation this 0 5
		set_textureanimation this 1 5
		set_textureanimation this 2 5
		set muetze [new Dummy_Voodoo_haare_b]
		set_textureanimation $muetze 0 5
		link_obj $muetze this 4
		set muetze2 [new Dummy_Voodoo_Muetze_b]
		link_obj $muetze2 this 4
	}

	call scripts/classes/zwerg/z_anims.tcl
	call scripts/classes/zwerg/z_faceanim.tcl


	obj_init {
		call scripts/misc/animclassinit.tcl	// anim members
		call scripts/classes/zwerg/z_faceanim.tcl
		set current_common_mood bad_dizzy
		set_fanim_feeling $current_common_mood
		set info_string {{untereTW 0}}
		set is_counterwiggle 0
		set_anim this mann.schlafen_boden_loop 0 $ANIM_LOOP		;# set standard anim
		set_collision this 1
		set_visibility this 1
		set_attrib this carrycap 1
		set_attrib this hitpoints 1
		set_hoverable this 1
		set_selectable this 0
		state_triggerfresh this idle
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3
		set_objname this "Torwaechterin"
		set muetze 0
		set untereTW 0

		set type standing
		catch { sm_add_event Torwaechterin_befreit }
		if { [sm_get_event Torwaechterin_befreit] } {
			set type lying

		}

        proc set_idle_anim {} {
        	if { [get_gnomeposition this] == 0 } {
        			switch [hf2i [random 4]] {
        				0 	{set_anim this mann.stand_anim_a 0 2 ;#set idle anim}
        				1	{set_anim this mann.stand_anim_b 0 2 ;#set idle anim}
        				2	{set_anim this mann.stand_anim_c 0 2 ;#set idle anim}
        				3	{set_anim this mann.stand_anim_d 0 2 ;#set idle anim}
        			}
        	} else {
        		if {abs([get_roty this]-3.14)<0.1} {set_roty this 3.14}
        		set_anim this mann.kletterstand_anim 0 2 ;#set idle anim
        	}
        }

        proc get_random_of {str} {
         	set rlist [split $str ""]
         	set which [irandom [llength $rlist]]
         	return [lindex $rlist $which]
        }
        proc get_info {key} {
        	global info_string
        	foreach entry $info_string {
        		if {[lindex $entry 0]==$key} {return [lindex $entry 1]}
        	}
        	log "no such key ($key) in InfoString"
        	return ""
        }
		set_hoverable this 0
	}
	method Editor_Set_Info {ifo} {
		set info_string $ifo
		foreach entry $ifo {
			eval "set $entry"
		}
	}
	method set_action {activity} {
		set mode [lindex $activity 0]
		set counter [lindex $activity 1]
		if {$counter==""} {set counter -1}
	}
    method idle_anim {} {
     	set_idle_anim
    }

    method destroy {} {
    	set_visibility $muetze 0
    	set_visibility this 0
    	del $muetze
    	del this
    }

	state idle {
		if { [sm_get_event Torwaechterin_befreit] } {
			set type lying

		}
		if { $type == "lying" } {
			set anim "sleepside"
		} else {
			set anim standloop[get_random_of abcd]
		}

		state_disable this
		action this anim $anim "state_enable [get_ref this]"

	}
}

def_class Geisel none gnome 0 {} {
	call scripts/misc/animclassinit.tcl
	def_event evt_timer_update
	handle_event evt_timer_update {
		if {[get_owner this]==6} {
			set mood normal
		} elseif {1||[get_owner this]=="dieguten"} {
			// hier koennte TR/CT unterschieden werden
			set mood good
		} else {
			set mood bad
		}
		if {$lifecounter>1000} {
			set eye tired
		} elseif {$lifecounter>600} {
			set eye dizzy
		} elseif {$lifecounter>200} {
			set eye normal
		} else {
			set eye awake
		}
		if {$auto_fanim_state} {
			set_fanim_feeling $current_common_mood
			random_fanim_sequence
		}
		incr lifecounter 3
	}
	call scripts/misc/genericfight.tcl
	call scripts/classes/zwerg/z_anims.tcl
	call scripts/classes/zwerg/z_faceanim.tcl
	method set_walk_anim {nr} {
		set mywalk [lindex {2 4 8 9 12} $nr]
	}
	method youre_kidnapped {user} {
		state_triggerfresh this follow
		set_eye_focus $follow
		set follow $user
		set text [smalltalk get "msc"]
		set tl [llength $text]
		set text [lindex $text [irandom $tl]]
		speechicon this clear
		set sptime [expr {[string length $text]*0.04}]
		speechicon this add $text $sptime 1

		call_method_static GameObserver ExecBroadcast "sound play hos[get_random_of 123456] 1 \{[get_pos this]\}"
	}

	method add_logoff_code {code} {
		append logoff_code " ; $code"
	}

	method destroy {} {
		catch { eval " $logoff_code " }
		del this
	}

	obj_init {
		call scripts/misc/genericfight.tcl
		call scripts/classes/zwerg/z_faceanim.tcl
		timer_event this evt_timer_update -repeat -1 -interval 3 -attime [expr [gettime]+1]
		set lifecounter 0
		set is_counterwiggle 0
		//if {rand()<0.5} {
			set_alternateanimdb this true
			set_textureanimation this 0 9
			set_textureanimation this 1 8
			set_textureanimation this 2 [irandom 5]
			set gender female
		//} else {
		//	set_textureanimation this 0 9
		//	set_textureanimation this 1 [expr {9+[irandom 2]}]
		//	set gender male
		//}
		set mywalk 9

		set follow 0
		set logoff_code ""

		state_triggerfresh this idle

		proc play_anim {anim} {
			global sparetime_talkevents
			if {[string first "accident" $anim]!=-1} {
				lappend sparetime_talkevents "uqw"
			}

			state_disable this
			action this anim $anim {state_enable this}
			if {[set fanim [get_classaniminfo $anim]]!=""} {
				if {$fanim=="Illegel Anim"} {log "illegal Anim: $anim!!!";return true}
				set submesh [lindex $fanim 0]
				set fanim [lrange $fanim 1 end]
				start_fanim_sequence $fanim "-mesh $submesh"
			}
			return true
		}

		proc play_idle {} {
			if {rand()<0.8} {
				play_anim [lindex {standloopa standloopb standloopc standloopd} [irandom 4]]
			} else {
				state_disable this
				action this rotate [random 6.3] {
					state_enable this
					play_anim scout
				}
			}
		}

		proc play_confused {} {
			play_anim [lindex {talkrepoc jumpa jumpb kneebend scratchhead scratch impatient} [irandom 7]]
		}

		proc get_random_of {str} {
        	set rlist [split $str ""]
        	set which [irandom [llength $rlist]]
        	return [lindex $rlist $which]
        }


	}

	state idle {
		play_idle
	}

	state follow {
		if {$follow==0} {
			state_triggerfresh this idle
		} elseif {![obj_valid $follow]} {
			set follow 0
		} else {
			set fpos [get_pos $follow]
			set mpos [get_pos this]
			set dist [vector_dist3d $mpos $fpos]
			if {$dist>2} {
				set place [get_place -center $fpos -mindist 0.8 -circle 2 -nearpos $mpos -except this]
				if {[lindex $place 0]>0} {
					state_disable this
					action this walk "-target \{$place\} -animsets \{$mywalk -1 3\}" {
						state_enable this
						if {[get_walkresult this]!=4} {
							play_confused
						}
					}
				} else {
					play_confused
				}
			} else {
				play_idle
			}
		}
	}

	state_leave idle {
		set_eye_focus 0
	}
}
