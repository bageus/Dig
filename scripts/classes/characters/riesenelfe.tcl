// Die tötliche Riesenelfe

def_class Riesenelfe none tool 0 {} {
	call scripts/misc/animclassinit.tcl

	set_class_anim	standanim 		riesenelfe.standanim
	set_class_anim	still			riesenelfe.standard
	set_class_anim	fire			riesenelfe.blitze_schleudern
	set_class_anim  firestart		riesenelfe.blitze_start
	set_class_anim  fireloop 		riesenelfe.blitze_loop
	set_class_anim  fireend 		riesenelfe.blitze_end
	set_class_anim	talka			riesenelfe.reden_a
	set_class_anim	falldown		riesenelfe.absturz

	class_defaultanim riesenelfe.standard

	// Walk
	set_class_animset 0 {
		{standard			riesenelfe.standanim	}
		{walk_start			riesenelfe.standanim	}
		{walk_loop			riesenelfe.standanim	}
		{walk_stop			riesenelfe.standanim	}

		{turn_left_90		riesenelfe.standanim	}
		{turn_right_90		riesenelfe.standanim	}
		{turn_left_180		riesenelfe.standanim	}
		{turn_right_180		riesenelfe.standanim	}

		{climb_standard		riesenelfe.standanim	}
		{climb_up			riesenelfe.standanim	}
		{climb_down			riesenelfe.standanim	}
		{climb_right		riesenelfe.standanim	}
		{climb_left			riesenelfe.standanim	}

		{ground_to_wall		riesenelfe.standanim	}
		{wall_to_ground		riesenelfe.standanim	}

		{walk_loop_wave		riesenelfe.standanim	}
		{ladder_climb_up  	riesenelfe.standanim	}
		{ladder_climb_down	riesenelfe.standanim	}
		{ground_to_ladder	riesenelfe.standanim	}
		{ladder_to_ground	riesenelfe.standanim	}
	}

	method flyto {pos} {
		flyto $pos
	}

	method flytowaypoint {wp} {
		flytowaypoint $wp
	}

	method spreadfire {startpos endpos steps} {
		laser_spreadfire $startpos $endpos $steps
	}


	// ein erzwungener Tod für die Elfe (zum Testen der Sequenz)

	method killme {} {
		global winga wingb wingc wingd 
		set winga -1
		set wingb -1
		set wingc -1
		call_method this report_wing_status d 0.0
	}

	method report_wing_status {wing hitpoints} {
		global winga wingb wingc wingd waypointlist

		switch $wing {
			a 		{ set textureid 4 }
			b 		{ set textureid 3 }
			c 		{ set textureid 5 }
			d 		{ set textureid 6 }
			default { return }
		}

		if {$hitpoints < 0.01} {

			// Flügel zerstört
			set_textureanimation this $textureid {4}
			switch $wing {
				a { set winga -1 }
				b { set wingb -1 }
				c { set wingc -1 }
				d { set wingd -1 }
			}
		} elseif {$hitpoints < 0.30} {
			set_textureanimation this $textureid {3}
		} elseif {$hitpoints < 0.60} {
			set_textureanimation this $textureid {2}
		} elseif {$hitpoints < 0.90} {
			set_textureanimation this $textureid {1}
		}

		if {$winga <= 0  &&  $wingb <= 0  &&  $wingc <= 0  &&  $wingd <= 0} {
			state_disable this
			log "creating trigger_extro_470"
			set i [new Trigger_Extro_470]
			set_pos $i [lindex $waypointlist 19]
			action this anim standanim {}
			call_method this destroy
		}
	}

	// Destruktor

	method destroy {} {
		set_visibility this 0
		del this
		if {$winga > 0} {del $winga}
		if {$wingb > 0} {del $wingb}
		if {$wingc > 0} {del $wingc}
		if {$wingd > 0} {del $wingd}
	}


	def_event evt_timer0
	handle_event evt_timer0 {

		// zuerst alle wegpunkte suchen

		set objlist [obj_query this "-class Info_Riesenelfe_Waypoint"]
		if {$objlist == 0} {
			set objlist ""
		}
		set i 0
		while {[llength $objlist] > 0} {
			set waypoint 0
			foreach obj $objlist {
				if {[call_method $obj get_info wp] == $i} {
					lrem objlist [lsearch $objlist $obj]
					set waypoint [get_pos $obj]
					del $obj
					log "waypoint $i : $waypoint"
					break
				}
			}
			incr i
			lappend waypointlist $waypoint
		}

		// Target - Infoobjs suchen und ordnen

		set objlist [obj_query this "-class Info_Riesenelfe_Target"]
		if {$objlist == 0} {
			set objlist ""
		}
		set i 0
		while {[llength $objlist] > 0} {
			set target 0
			foreach obj $objlist {
				if {[call_method $obj get_info target] == $i} {
					lrem objlist [lsearch $objlist $obj]
					set target $obj
					log "target $i : $target"
					break
				}
			}
			incr i
			lappend targetlist $target
		}

		flyto [get_pos this]			;# erster Wegpunkt für Flug ist meine Position

		// die 4 Elfenfluegel erzeugen

		set myref [get_ref this]
		set mypos [get_pos this]

		set winga	[new ElfenFluegelA]
		set wingb	[new ElfenFluegelB]
		set wingc	[new ElfenFluegelC]
		set wingd	[new ElfenFluegelD]
		set winga_pos  {-5 -10.3  0}
		set wingb_pos  { 5 -10.3  0}
		set wingc_pos  {-5  -2   -2}
		set wingd_pos  { 5  -2   -2}
		set_pos $winga [vector_add [get_pos this] $winga_pos]
		set_pos $wingb [vector_add [get_pos this] $wingb_pos]
		set_pos $wingc [vector_add [get_pos this] $wingc_pos]
		set_pos $wingd [vector_add [get_pos this] $wingd_pos]
		call_method $winga set_elfe $myref
		call_method $wingb set_elfe $myref
		call_method $wingc set_elfe $myref
		call_method $wingd set_elfe $myref

		set_owner $winga -1
		set_owner $wingb -1
		set_owner $wingc -1
		set_owner $wingd -1

		change_particlesource this 0 33 {0 -6 0} {0 0 0} 128 3 0
		set_particlesource this 0 1
		state_triggerfresh this moving
	}


	state spreadfire {

		for {set i 0} {$i < 3} {incr i} {
			if {[random 1.0] > 0.5} {
				set sourcepos [vector_add [get_pos this] [get_linkpos this 5]]
			} else {
				set sourcepos [vector_add [get_pos this] [get_linkpos this 4]]
			}

			set jittered_targetpos [ vector_add $targetpos "[random -0.5 0.5] 0 [random -2.0 2.0]" ]

			lightning $sourcepos $jittered_targetpos [vector_sub $targetpos $sourcepos] 0.3 0.1 0.2 [random 0.3 0.8]
			create_particlesource 3 $jittered_targetpos {0 0 0} 1 1
		}

		if {$current_attack_frame % 5 == 0} {
			set victims [obj_query this "-pos \{$targetpos\} -boundingbox {-2 -2 -5 2 2 5} -class Zwerg -cloaked 1"]
			log "victims: $victims"
			if {$victims != 0} {
				foreach obj $victims {
					if {[lsearch $victimlist $obj] == -1} {
						// ...damit niemand doppelt getroffen wird

						log "[get_objname this] Hit [get_objname $obj]"
						lappend victimlist $obj
						call_method $obj fall
						call_method $obj burn
						add_attrib $obj atr_Hitpoints -[random 0.1 0.2]
					}
				}
		 	}
		}

		set targetpos [vector_add $targetpos $dirvec] 	;# Zielposition aktualisieren
		incr current_attack_frame

		if {$current_attack_frame < $max_attack_frames} {
			waittime 0.09						;# einen Tick warten
		} else {
			state_disable this
			action this anim fireend {
				set_anim this standanim 0 2;
				state_enable this;
				state_trigger this moving		;# oder fertig
			}
		}
	}


	state moving {
		set act [lindex $actionlist $actionindex]
		incr actionindex
		if {$actionindex >= [llength $actionlist]} {
			set actionindex 0
		}
		log "Elfe Action: $act"
		eval $act
	}


	obj_init {
		set_anim this riesenelfe.standanim 0 $ANIM_LOOP
		set_viewinfog this 1
		set_visibility this 1
		set_hoverable this 0

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		set waypointlist ""							;# Liste mit den Koordinaten der Waypoints
		set targetlist   ""							;# Liste mit Infoobj zum Zielen

		// die folgenden Variablen werden von den Angriffs-States benutzt

		set current_attack_frame		0			;# aktueller Angriff Frame
		set max_attack_frames			0			;# insges. Frames im aktuellen Angriff
		set sourcepos 					0			;# Strahlenquelle
		set dirvec						0			;# Richtungsvector
		set targetpos					0			;# Strahlenziel
		set victimlist					""			;# Liste von Strahlenopfern

		set winga	-1
		set wingb	-1
		set wingc	-1
		set wingd	-1
		set winga_pos  {0 0 0}
		set wingb_pos  {0 0 0}
		set wingc_pos  {0 0 0}
		set wingd_pos  {0 0 0}

		set actionindex 				0

		set actionlist [list]
		lappend actionlist "flytowaypoint 0"						;# Mitte unten
		lappend actionlist "flytowaypoint 1"
		lappend actionlist "waittime 2"
		lappend actionlist "spreadfire_at_target 2 3 -5 30"			;# T2
		lappend actionlist "waittime 2"
		lappend actionlist "flytowaypoint 3"
		lappend actionlist "flytowaypoint 7"
		lappend actionlist "waittime 2"
		lappend actionlist "spreadfire_at_target 1 3 -5 30"			;# T1
		lappend actionlist "waittime 2"
		lappend actionlist "flytowaypoint 6"
		lappend actionlist "flytowaypoint 9"
		lappend actionlist "flytowaypoint 10"
		lappend actionlist "flytowaypoint 11"						;# linke Tour
		lappend actionlist "waittime 2"
		lappend actionlist "spreadfire_at_target 5 3 -5 30"			;# T5
		lappend actionlist "waittime 2"
		lappend actionlist "spreadfire_at_target 6 -3 5 30"			;# T6
		lappend actionlist "flytowaypoint 15"
		lappend actionlist "flytowaypoint 16"

		lappend actionlist "flytowaypoint 0"						;# wieder Mitte
		lappend actionlist "flytowaypoint 2"
		lappend actionlist "waittime 2"
		lappend actionlist "spreadfire_at_target 4 -3 5 30"			;# T4
		lappend actionlist "waittime 2"
		lappend actionlist "flytowaypoint 3"
		lappend actionlist "flytowaypoint 8"
		lappend actionlist "spreadfire_at_target 3 -3 5 30"			;# T3
		lappend actionlist "flytowaypoint 5"
		lappend actionlist "flytowaypoint 12"
		lappend actionlist "flytowaypoint 13"
		lappend actionlist "flytowaypoint 14"						;# rechte Tour
		lappend actionlist "waittime 2"
		lappend actionlist "spreadfire_at_target 8 -3 5 30"			;# T8
		lappend actionlist "waittime 2"
		lappend actionlist "spreadfire_at_target 7  3 -5 30"		;# T7
		lappend actionlist "waittime 2"
		lappend actionlist "flytowaypoint 18"						;# rechte Tour
		lappend actionlist "flytowaypoint 16"
		lappend actionlist "flytowaypoint 0"


		proc waittime {time} {
			state_disable this
			action this wait $time {state_enable this}
		}


		proc flyto {pos {speed 3}} {
			global winga wingb wingc wingd winga_pos wingb_pos wingc_pos wingd_pos

			log "flyto: $pos"

			state_disable this
			action this lore "-waypoints { {{$pos} {0 0 0} $speed $speed} }" {state_enable this}
			if {$winga > 0  &&  [obj_valid $winga]} {
				action $winga lore "-waypoints { {{[vector_add $pos $winga_pos]} {0 0 0} $speed $speed} }"
			}
			if {$wingb > 0  &&  [obj_valid $wingb]} {
				action $wingb lore "-waypoints { {{[vector_add $pos $wingb_pos]} {0 0 0} $speed $speed} }"
			}
			if {$wingc > 0  &&  [obj_valid $wingc]} {
				action $wingc lore "-waypoints { {{[vector_add $pos $wingc_pos]} {0 0 0} $speed $speed} }"
			}
			if {$wingd > 0  &&  [obj_valid $wingd]} {
				action $wingd lore "-waypoints { {{[vector_add $pos $wingd_pos]} {0 0 0} $speed $speed} }"
			}

		}


		proc flytowaypoint {wp} {
			global waypointlist
			set pos [lindex $waypointlist $wp]
			if {$pos != 0} {
				flyto $pos
			}
		}


		proc spreadfire_at_target {target startrange endrange rays} {
			global targetlist

			set targetpos [get_pos [lindex $targetlist $target]]
			laser_spreadfire [vector_add $targetpos "$startrange 0 0"] [vector_add $targetpos "$endrange 0 0"] $rays
		}


		proc laser_spreadfire {startpos endpos steps} {
			global current_attack_frame max_attack_frames targetpos dirvec victimlist

			set dirvec [vector_sub $endpos $startpos]
			set dirvec [vector_mul $dirvec [expr 1.0/$steps]]

			set targetpos $startpos
			set current_attack_frame 0
			set max_attack_frames $steps
			set victimlist ""

			state_disable this
			action this anim firestart {
				state_enable this;
				set_anim this fireloop 0 2;
				state_triggerfresh this spreadfire
			}
		}

	}
}




def_class ElfenFluegelA none monster 2 {} {
	call scripts/misc/animclassinit.tcl

	set_class_anim  kungfustillani		riesenelfe_dummy2_a.standard
	class_fightdist 6.0

	def_event evt_fight_get_hurt
	handle_event evt_fight_get_hurt {
		set hploss [event_get this -num1]
		set attacker [event_get this -subject1]

		add_attrib this atr_Hitpoints -$hploss
		create_particlesource 33 [get_pos this] {0 0 0} 128 1

		set hp [get_attrib this atr_Hitpoints]
		log "[get_objname this] : I have $hp Hitpoints left."

		if {$elfe > 0} {
			call_method $elfe report_wing_status a $hp
		}
		if {$hp < 0.01} {
			del this
		}
	}

	
	// state, damit Ballistic-Actions funktionieren
	state idle {
		set hp [get_attrib this atr_Hitpoints]

		if {$elfe > 0} {
			call_method $elfe report_wing_status a $hp
		}
		if {$hp < 0.01} {
			del this
		}		
	}


	// virtual
	method im_attacking_you {} {}

	method check_first_strike {caller} {
		return 1
	}

	method set_elfe {e} {
		global elfe
		set elfe $e
	}

	obj_init {
		call scripts/misc/animclassinit.tcl

		set_anim this riesenelfe_dummy2_a.standard 0 2
		set_physic this 0
		set_hoverable this 1
		set_visibility this 1
		set_selectable this 0
		set_attrib this atr_Hitpoints 1.0
		set elfe -1
		state_triggerfresh this idle
	}
}



def_class ElfenFluegelB none monster 2 {} {
	call scripts/misc/animclassinit.tcl

	set_class_anim  kungfustillani		riesenelfe_dummy2_b.standard
	class_fightdist 6.0

	def_event evt_fight_get_hurt
	handle_event evt_fight_get_hurt {
		set hploss [event_get this -num1]
		set attacker [event_get this -subject1]

		add_attrib this atr_Hitpoints -$hploss
		create_particlesource 33 [get_pos this] {0 0 0} 128 1

		set hp [get_attrib this atr_Hitpoints]
		log "[get_objname this] : I have $hp Hitpoints left."

		if {$elfe > 0} {
			call_method $elfe report_wing_status b $hp
		}
		if {$hp < 0.01} {
			del this
		}
	}

	// state, damit Ballistic-Actions funktionieren
	state idle {
		set hp [get_attrib this atr_Hitpoints]

		if {$elfe > 0} {
			call_method $elfe report_wing_status b $hp
		}
		if {$hp < 0.01} {
			del this
		}		
	}

	// virtual
	method im_attacking_you {} {}

	method check_first_strike {caller} {
		return 1
	}

	method set_elfe {e} {
		global elfe
		set elfe $e
	}


	obj_init {
		call scripts/misc/animclassinit.tcl

		set_anim this riesenelfe_dummy2_b.standard 0 2
		set_physic this 0
		set_hoverable this 1
		set_visibility this 1
		set_selectable this 0
		set_attrib this atr_Hitpoints 1.0
		set elfe -1
		state_triggerfresh this idle
	}
}



def_class ElfenFluegelC none monster 2 {} {
	call scripts/misc/animclassinit.tcl
	
	set_class_anim  kungfustillani		riesenelfe_dummy2_c.standard
	class_fightdist 6.0

	def_event evt_fight_get_hurt
	handle_event evt_fight_get_hurt {
		set hploss [event_get this -num1]
		set attacker [event_get this -subject1]

		add_attrib this atr_Hitpoints -$hploss
		create_particlesource 33 [get_pos this] {0 0 0} 128 1

		set hp [get_attrib this atr_Hitpoints]
		log "[get_objname this] : I have $hp Hitpoints left."

		if {$elfe > 0} {
			call_method $elfe report_wing_status c $hp
		}
		if {$hp < 0.01} {
			del this
		}
	}

	// state, damit Ballistic-Actions funktionieren
	state idle {
		set hp [get_attrib this atr_Hitpoints]

		if {$elfe > 0} {
			call_method $elfe report_wing_status c $hp
		}
		if {$hp < 0.01} {
			del this
		}		
	}

	// virtual
	method im_attacking_you {} {}

	method check_first_strike {caller} {
		return 1
	}

	method set_elfe {e} {
		global elfe
		set elfe $e
	}


	obj_init {
		call scripts/misc/animclassinit.tcl

		set_anim this riesenelfe_dummy2_c.standard 0 2
		set_physic this 0
		set_hoverable this 1
		set_visibility this 1
		set_selectable this 0
		set_attrib this atr_Hitpoints 1.0
		set elfe -1
		state_triggerfresh this idle
	}
}



def_class ElfenFluegelD none monster 2 {} {
	call scripts/misc/animclassinit.tcl
	
	set_class_anim  kungfustillani		riesenelfe_dummy2_d.standard
	class_fightdist 6.0

	def_event evt_fight_get_hurt
	handle_event evt_fight_get_hurt {
		set hploss [event_get this -num1]
		set attacker [event_get this -subject1]

		add_attrib this atr_Hitpoints -$hploss
		create_particlesource 33 [get_pos this] {0 0 0} 128 1

		set hp [get_attrib this atr_Hitpoints]
		log "[get_objname this] : I have $hp Hitpoints left."

		if {$elfe > 0} {
			call_method $elfe report_wing_status d $hp
		}
		if {$hp < 0.01} {
			del this
		}
	}

	// state, damit Ballistic-Actions funktionieren
	state idle {
		set hp [get_attrib this atr_Hitpoints]

		if {$elfe > 0} {
			call_method $elfe report_wing_status d $hp
		}
		if {$hp < 0.01} {
			del this
		}		
	}

	// virtual
	method im_attacking_you {} {}

	method check_first_strike {caller} {
		return 1
	}

	method set_elfe {e} {
		global elfe
		set elfe $e
	}


	obj_init {
		call scripts/misc/animclassinit.tcl

		set_anim this riesenelfe_dummy2_d.standard 0 2
		set_physic this 0
		set_hoverable this 1
		set_visibility this 1
		set_selectable this 0
		set_attrib this atr_Hitpoints 1.0
		set elfe -1
		state_triggerfresh this idle
	}
}


def_class Riesenelfe_Sequenz none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	set_class_anim	standanim 		riesenelfe.standanim
	set_class_anim  standanimlong   riesenelfe.standanim_lang
	set_class_anim	still			riesenelfe.standard
	set_class_anim	fire			riesenelfe.blitze_schleudern
	set_class_anim  firestart		riesenelfe.blitze_start
	set_class_anim  fireloop 		riesenelfe.blitze_loop
	set_class_anim  fireend 		riesenelfe.blitze_end
	set_class_anim	talka			riesenelfe.reden_a
	set_class_anim	talkb			riesenelfe.reden_b
	set_class_anim	talkc			riesenelfe.reden_c
	set_class_anim	talkd			riesenelfe.reden_d
	set_class_anim	falldown		riesenelfe.absturz
	set_class_anim	falldown_a		riesenelfe.absturz_a
	set_class_anim	falldown_b		riesenelfe.absturz_b
	set_class_anim	falldown_c		riesenelfe.absturz_c
	
	class_defaultanim riesenelfe.standard

 	method idle_anim {} {
		// absichtlich leer!
	}

	// Der Elfe werden die Fluegel gestutzt
	method fluegelab {} {
		set_textureanimation this 3 {4}
		set_textureanimation this 4 {4}
		set_textureanimation this 5 {4}
		set_textureanimation this 6 {4}
	}

	// Elfe stuerzt ab (sollte aus eine Höhe von 9.5 Zwergenmeter passieren!)
	method absturz {} {
		set pos1 [get_pos this]
		set pos2 [vector_add [get_pos this] {0 9.5 0}]
		action this anim falldown_a {
			set_particlesource this 0 0
			set_anim this falldown_b 0 2
			action this lore "-waypoints { {{$pos1} {0 0 0} 10 10}  {{$pos2} {0 0 0} 10 10} }" {
				set_anim this falldown_c 0 1
			}
		}
	}

	obj_init {
		set_anim this riesenelfe.standanim_lang 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0
		set_visibility this 1
		change_particlesource this 0 33 {0 -6 0} {0 0 0} 128 3 0
		set_particlesource this 0 1
	}
}
