// Klassen für die Lavawelt-Kampagne


def_class Trigger_soundmarker_kathedrale none dummy 0 {} {
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

		proc set_music {} {
			set pos [get_pos this]
			adaptive_sound marker kathedrale $pos 20
			adaptive_sound marker kathedrale [vector_add $pos {28.5 0 0}] 70
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

// Eine unsichtbare Lavabarriere, läßt von unten keine Lava durch und
// pumpt Lava oberhalb ab; wird vom Kathedralenrätsel benutzt
def_class LavaStopperK none info 0 {} {

	method letmebeanobj {} {}

	obj_init {
		set_anim this trigger_fahne3.standard 0 0
		set_physic this 0
		set_visibility this 0

		set_fstopper this {-5 0} {5 0} 0
		set_fsource this -volume 10000 -vpf -35 -type lavawater -pos {0 -0.5 0}
	}
}


def_class GleipnirHammer none tool 0 {} {

	class_defaultanim lava_hammer.standard

	method letmebeanobj {} {}

	obj_init {
		set_anim this lava_hammer.standard 0 0
		set_physic this 1
		set_visibility this 1
		set_viewinfog this 1
	}
}



def_class Gleipnir none tool 0 {} {

	class_defaultanim gneipnir_boden.standard

	method setanim_hand {} {
		set_anim this gneipnir_hand.standard 0 0
	}

	method restore {} {
		set_anim this gneipnir_boden.standard 0 0

	}

	obj_init {
		set_anim this gneipnir_boden.standard 0 0
		set_physic this 1
		set_visibility this 1
		set_viewinfog this 1
	}
}


// Landschaftsobjekt, das Zwerge knusprig braun röstet, wenn sie nicht
// aufpassen :-)

def_class FlammenwerferK none tool 0 {} {
	call scripts/misc/info_obj.tcl

	def_event evt_timer0_init
	handle_event evt_timer0_init {
		set active_time [call_method this get_info activetime]
		set pause_time [call_method this get_info pausetime]
		set dir   [call_method this get_info dir]

		if {$dir == "left"} {
			change_particlesource this 0 0 {0 0 0} {-0.15 0.02 0} 64 2 2
			set lava_checkpos [vector_add [get_pos this] {-1 0 0}]
			set flamingbox "-2.0 0.0 -3.0  0.0 1.2 3.0"
		} else {
			change_particlesource this 0 0 {0 0 0} { 0.15 0.02 0} 64 2 2
			set lava_checkpos [vector_add [get_pos this] {-1 0 0}]
			set flamingbox " 0.0 0.0 -3.0  2.0 1.2 3.0"
		}

	}

	def_event evt_timer_update
	handle_event evt_timer_update {
		update
	}

	method set_timing {atime ptime} {
		global active_time pause_time current_time
		set active_time $atime
		set pause_time $ptime
		set current_time 0
	}

	obj_init {
		call scripts/misc/info_obj.tcl

		set_anim this lava_flammenwerfer.standard 0 0
		set_physic this 0
		set_hoverable this 0
		set_visibility this 1
		set_viewinfog this 1

		set active 1
		set active_time -1
		set pause_time   0
		set current_time 0
		set lava_checkpos {1 1 1}
		set flamingbox "-1 -1 -1  1 1 1"

		timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+1]
		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0


		proc update {} {
			global active active_time pause_time current_time lava_checkpos flamingbox

			incr current_time
			if {$active} {
				if {$active_time != -1} {
					if {$current_time > $active_time} {
						set active 0
						set current_time 0
					}
				}
			} else {
				if {$pause_time != -1} {
					if {$current_time > $pause_time} {
						set active 1
						set current_time 0
					}
				}
			}


			if {$active  &&  ![isunderwater $lava_checkpos]} {
				set_particlesource this 0 1
				set attacklist [obj_query this "-class Zwerg -boundingbox \{ $flamingbox \} -cloaked 1"]
				if {$attacklist == 0} {
					return
				}
				foreach obj $attacklist {
					add_attrib $obj atr_Hitpoints -0.015
					call_method $obj burn
				}
			} else {
				set_particlesource this 0 0
			}
		}

	}
}


// Kontrolliert den Lavastand in einem senkrechten Gang
// d.h. man kann eine gewünschte Höhe angeben und die LavaPumpeK
// pumpt entsprechend Lava rein oder raus
// wird im Kathedralenrätsel und im Vulkan benutzt

def_class LavaPumpeK none info 0 {} {
	call scripts/misc/info_obj.tcl

	def_event evt_timer0
	handle_event evt_timer0 {
		set i [call_method this get_info height]
		if {$i > 0} {
			set height $i
			set oldheight $i
		}

		set mypos [get_pos this]
		set fstopper    [new LavaStopperK]
		set fstopperold [new LavaStopperK]
	}

	def_event evt_timer_update
	handle_event evt_timer_update {
		update
	}

	method set_height {h} {
		global height oldheight
		set oldheight $height
		set height $h
		set height_change 1
	}

	method get_height {} {
		global height
		return $height
	}


	obj_init {
		call scripts/misc/info_obj.tcl

		set_anim this trigger_fahne2.standard 0 0
		set_physic this 0
		set_visibility this 0

		set height 		3
		set oldheight 	3
		set height_change 1
		set mypos [get_pos this]
		set fstopper    -1
		set fstopperold -1

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0

		proc update {} {
			global fstopper fstopperold fullpos freepos height oldheight height_change mypos

			if {$fstopper <= 0} {
				return
			}

			if {$height_change} {
				set height_change 0
				set fullpos [vector_add $mypos "0 -$height 0"]
				// y auf ganzen Block runden, sonst landet die Fsource im falschen Block!
				set fullpos "[lindex $fullpos 0] [hf2i [lindex $fullpos 1]] [lindex $fullpos 2]"
				
				set freepos [vector_add $fullpos {0 -1 0}]
				if {$height >= $oldheight} {
					set_pos $fstopperold $freepos
				} else {
					set_pos $fstopperold [get_pos $fstopper]
				}
				set_pos $fstopper $freepos
//				log "[get_objname this] fstopper    : [get_posy $fstopper]"
//				log "[get_objname this] fstopperold : [get_posy $fstopperold]"
			}

			if {[isunderwater $fullpos] == 0} {
				// nicht voll genug - Lava reinpumpen
				set_fsource this -volume 10000 -vpf 25 -type lavawater -pos {0 -1 0}
	//			log "pumpe: rein"
				return
			}

			// genau richtig - Pumpe abstellen
			rem_fsource this
	//		log "pumpe: stop"
		}
	}
}


// Tueren in der Kathedrale

def_class Tuer_LavaK none tool 0 {} {
	set_class_anim opena		lava_tuer.oeffnen
	set_class_anim closea		lava_tuer.schliessen
	set_class_anim openb		lava_tuer.oeffnen
	set_class_anim closeb		lava_tuer.schliessen
	call scripts/classes/items/calls/doors.tcl

	method oeffnen_overload {requestor time} {
		set mypos [get_pos this]
		if {[vector_dist $mypos [get_pos $requestor]] > 1.5} {
			return
		}

		// Tür nur öffnen, wenn nicht unter Wasser (oder unter Lava!)
		if {[isunderwater [vector_add $mypos {-1 -0.5 0}]]  ||  [isunderwater [vector_add $mypos {1 -0.5 0}]]} {
			tasklist_add $requestor "play_anim dontknow"
			return
		}

		if {[random 1.0] < 0.5} {
			tasklist_add $requestor "play_anim kickmachine"
		} else {
			tasklist_add $requestor "play_anim pressbutton"
		}
		tasklist_add $requestor "call_method [get_ref this] oeffnen $requestor $time"
	}

	method objaction {user} {
		if {[get_posx this] < [get_posx $user]} {
			set pos [vector_add [get_pos this] { 0.7 0 -2}]
		} else {
			set pos [vector_add [get_pos this] {-0.7 0 -2}]
		}

		tasklist_add $user "walk_pos \{ $pos \}"
		tasklist_add $user "rotate_towards \{ [vector_add [get_pos this] {0 0 -2}] \}"
		tasklist_add $user "call_method [get_ref this] oeffnen_overload $user 3"
	}


	method get_standoff_dist {} {
		return -1
	}

	obj_init {
		set influence_pf 1
		set standanim lava_tuer.standard
		set openanima lava_tuer.offen
		set openanimb lava_tuer.offen
		call scripts/classes/items/calls/doors.tcl
		set_fstopper this {0 -2} {0 1} 0
		set_hoverable this 1
		set_viewinfog this 1
	}
}


// Zeigt wahlweise nicht, Pfeil hoch oder Pfeil runter an

def_class Lava_DisplayK none tool 0 {} {
	call scripts/misc/animclassinit.tcl

	def_event evt_timer_reset
	handle_event evt_timer_reset {
		set_anim this lava_pfeil.standard 0 $ANIM_LOOP
	}

	method switch {dir} {
		timer_unset this 0
		if {$dir == "up"} {
			timer_event this evt_timer_reset -repeat 0 -userid 0 -attime [expr [gettime]+4]
			set_anim this lava_pfeil.auf 0 $ANIM_LOOP
		}
		if {$dir == "down"} {
			timer_event this evt_timer_reset -repeat 0 -userid 0 -attime [expr [gettime]+4]
			set_anim this lava_pfeil.ab 0 $ANIM_LOOP
		}
	}

	obj_init {
		call scripts/misc/animclassinit.tcl

		set_anim this lava_pfeil.standard 0 $ANIM_LOOP
		set_selectable this 0
		set_hoverable this 0
		set_physic this 0
		set_viewinfog this 1
	}
}



// 4 Schalter für das Lavaraetsel

def_class Schalter_knopf_lava_1 none tool 0 {} {
	set_class_anim press lava_schalter_a.standard
	set_class_anim release lava_schalter_a.standard
	call scripts/classes/items/calls/switcher.tcl

	def_event evt_timer0_init
	handle_event evt_timer0_init {
		set lavacontroler [obj_query this "-class LavaControlerK -range 20 -limit 1"]
		if {$lavacontroler == 0} {
			log "[get_objname this] LavaControlerK not found - will try again!"
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
		}
	}

	obj_init {
		set standardanim lava_schalter_a.standard
		set standardframe 0
		set switchanim lava_schalter_a.anim
		set switchframe 0

		set lavacontroler -1
		set_viewinfog this 1

		call scripts/classes/items/calls/switcher.tcl
		timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+1]

		call_method this set_actiononpress {
			if {\$lavacontroler <= 0} { return };
			call_method \$lavacontroler press 1
		}
	}
}


def_class Schalter_knopf_lava_2 none tool 0 {} {
	set_class_anim press lava_schalter_a.standard
	set_class_anim release lava_schalter_a.standard
	call scripts/classes/items/calls/switcher.tcl

	def_event evt_timer0_init
	handle_event evt_timer0_init {
		set lavacontroler [obj_query this "-class LavaControlerK -range 20 -limit 1"]
		if {$lavacontroler == 0} {
			log "[get_objname this] LavaControlerK not found - will try again!"
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
		}
	}

	obj_init {
		set standardanim lava_schalter_a.standard
		set standardframe 0
		set switchanim lava_schalter_a.anim
		set switchframe 0

		set lavacontroler -1
		set_viewinfog this 1

		call scripts/classes/items/calls/switcher.tcl
		timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+1]

		call_method this set_actiononpress {
			if {\$lavacontroler <= 0} { return };
			call_method \$lavacontroler press 2
		}
	}
}


def_class Schalter_knopf_lava_3 none tool 0 {} {
	set_class_anim press lava_schalter_a.standard
	set_class_anim release lava_schalter_a.standard
	call scripts/classes/items/calls/switcher.tcl

	def_event evt_timer0_init
	handle_event evt_timer0_init {
		set lavacontroler [obj_query this "-class LavaControlerK -range 20 -limit 1"]
		if {$lavacontroler == 0} {
			log "[get_objname this] LavaControlerK not found - will try again!"
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
		}
	}

	obj_init {
		set standardanim lava_schalter_a.standard
		set standardframe 0
		set switchanim lava_schalter_a.anim
		set switchframe 0

		set lavacontroler -1
		set_viewinfog this 1

		call scripts/classes/items/calls/switcher.tcl
		timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+1]

		call_method this set_actiononpress {
			if {\$lavacontroler <= 0} { return };
			call_method \$lavacontroler press 3
		}
	}
}


def_class Schalter_knopf_lava_4 none tool 0 {} {
	set_class_anim press lava_schalter_a.standard
	set_class_anim release lava_schalter_a.standard
	call scripts/classes/items/calls/switcher.tcl

	def_event evt_timer0_init
	handle_event evt_timer0_init {
		set lavacontroler [obj_query this "-class LavaControlerK -range 20 -limit 1"]
		if {$lavacontroler == 0} {
			log "[get_objname this] LavaControlerK not found - will try again!"
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
		}
	}

	obj_init {
		set standardanim lava_schalter_a.standard
		set standardframe 0
		set switchanim lava_schalter_a.anim
		set switchframe 0

		set lavacontroler -1
		set_viewinfog this 1

		call scripts/classes/items/calls/switcher.tcl
		timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+1]

		call_method this set_actiononpress {
			if {\$lavacontroler <= 0} { return };
			call_method \$lavacontroler press 4
		}
	}
}


def_class LavaControlerK none info 0 {} {

	def_event evt_timer0_init
	handle_event evt_timer0_init {
		set pumps [obj_query this "-class LavaPumpeK -range 50 -limit 4"]
		if {[llength $pumps] != 4} {
			log "[get_objname this] : could not find all lava sources, will try again..."
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
			return
		}

		set pump1 [lindex $pumps 3]
		set pump2 [lindex $pumps 2]
		set pump3 [lindex $pumps 1]
		set pump4 [lindex $pumps 0]

		set displays [obj_query this "-class Lava_DisplayK -range 50 -limit 4"]
		if {[llength $displays] != 4} {
			log "[get_objname this] : could not find all displays, will try again..."
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
			return
		}

		set display1 [lindex $displays 0]
		set display2 [lindex $displays 1]
		set display3 [lindex $displays 2]
		set display4 [lindex $displays 3]

		log "Lavacontroler is [get_ref this]"
		show
	}

	method press {button} {
		global diabled

		if {$disabled} {
			return
		}
		press $button
	}


	method disable {} {
		global disabled
		set disabled 1

		set n [realheight 0]
		call_method $display1 switch down
		call_method $pump1 set_height $n
		call_method $display2 switch down
		call_method $pump2 set_height $n
		call_method $display3 switch down
		call_method $pump3 set_height $n
		call_method $display4 switch down
		call_method $pump4 set_height $n
	}


	obj_init {
		set_anim this trigger_fahne3.standard 0 0
		set_physic this 0
		set_visibility this 0
		timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+1]

		proc reset {} {
			global a b c d
			set a 2
			set b 1
			set c 3
			set d 2
		}

		reset
		set pump1 		-1
		set pump2 		-1
		set pump3 		-1
		set pump4 		-1
		set display1	-1
		set display2	-1
		set display3	-1
		set display4	-1

		set disabled     0

		proc realheight {h} {
			switch $h {
				"0" { return 1.5  }
				"1" { return 8.5  }
				"2" { return 15.5 }
				"3" { return 25   }
				"4" { return 30   }
			}
		}

		proc show {} {
			global a b c d pump1 pump2 pump3 pump4 display1 display2 display3 display4

			if {$display1 <= 0} {			;# anscheinend noch nicht initialisiert!
				return
			}

			set o [call_method $pump1 get_height]
			set n [realheight $a]
			if {$n > $o} {
				call_method $display1 switch up
			} elseif {$n < $o} {
				call_method $display1 switch down
			}
			call_method $pump1 set_height $n

			set o [call_method $pump2 get_height]
			set n [realheight $b]
			if {$n > $o} {
				call_method $display2 switch up
			} elseif {$n < $o} {
				call_method $display2 switch down
			}
			call_method $pump2 set_height $n

			set o [call_method $pump3 get_height]
			set n [realheight $c]
			if {$n > $o} {
				call_method $display3 switch up
			} elseif {$n < $o} {
				call_method $display3 switch down
			}
			call_method $pump3 set_height $n

			set o [call_method $pump4 get_height]
			set n [realheight $d]
			if {$n > $o} {
				call_method $display4 switch up
			} elseif {$n < $o} {
				call_method $display4 switch down
			}
			call_method $pump4 set_height $n

			log "   A  B  C  D"
			for {set i 4} {$i > 0} {incr i -1}	{

				set out "$i "

				if {$a < $i} {
					set out "$out\[ \]"
				} else {
					set out "$out\[X\]"
				}
				if {$b < $i} {
					set out "$out\[ \]"
				} else {
					set out "$out\[X\]"
				}
				if {$c < $i} {
					set out "$out\[ \]"
				} else {
					set out "$out\[X\]"
				}
				if {$d < $i} {
					set out "$out\[ \]"
				} else {
					set out "$out\[X\]"
				}

				log $out
			}
			log "0 \[X\]\[X\]\[X\]\[X\]"
		}

		proc press {button} {
			global a b c d
			switch $button {
			"1"	{ 	if {$a > 0  &&  $c < 4} {
						incr a -1
						incr c
					}
				}
			"2"	{	if {$b < 4  &&  $d > 0} {
						incr b
						incr d -1
					}
				}
			"3"	{	if {$a < 4  &&  $c > 0} {
						incr a
						incr c -1
					}
				}
			"4"	{	if {$d < 4  &&  $b > 0} {
						incr d
						incr b -1
					}
				}
			}
			show
		}
	}
}


def_class Amboss none tool 0 {} {

	method objaction {user} {
		global lavaready ALLRINGS

		log "Amboss angeklickt!"

		if {![check_rings]} {
			// nicht alle Ringe da!
			set trg [new Trigger_Lava_Amboss_190]
			set_pos $trg [get_pos this]
			return
		}

		if {!$lavaready} {
			// keine Lava da!

			set FR1 [new FogRemover]; #eine Stelle wo der Nebel weggeht
			set_pos $FR1 [vector_add [get_pos this] {-4 19 7}]
			call_method $FR1 fog_remove 0 -3 -3
			call_method $FR1 timer_delete 15

			set FR2 [new FogRemover]; #eine Stelle wo der Nebel weggeht
			set_pos $FR2 [vector_add [get_pos this] {1 17 7}]
			call_method $FR2 fog_remove 0 -2 -14
			call_method $FR2 timer_delete 15

			set trg [new Trigger_Lava_Amboss_195]
			set_pos $trg [get_pos this]

			return
		}


		// alles vorhanden - Ringe löschen, Zwerge beginnen zu schmieden

		set rings [obj_query this -class $ALLRINGS]
		foreach obj $rings {
			del $obj
		}
		set_hoverable this 0

		set trg [new Trigger_Lava_Amboss_200]
		set_pos $trg [get_pos this]
	}


	method get_standoff_dist {} {
		return 3.0
	}

	method LavaReady {} {
		global lavaready
		if {$lavaready} {
			return
		}

		set lavaready 1
		set_anim this lava_amboss.standard 0 2

		set FR [new FogRemover]; #eine Stelle wo der Nebel weggeht
		set_pos $FR [vector_add [get_pos this] {1 17 7}]
		call_method $FR fog_remove 0 -2 -14
		call_method $FR timer_delete 15
	}

	obj_init {

		set_anim this lava_amboss_b.standard 0 2
		set_physic this 0
		set_visibility this 1
		set_viewinfog this 1
		set_hoverable this 1
		set_collision this 1
		set ALLRINGS {Ring_Des_Lebens Ring_Der_Magie Ring_Des_Wassers Ring_Des_Feuers Ring_Der_Erde Ring_Der_Luft GleipnirHammer}
		set lavaready 		0

		proc check_rings {} {
			global ALLRINGS

			set ringsfound 0
			set rings [obj_query this -range 8 -class $ALLRINGS]
			log "rings: $rings"
			if {$rings != 0} {
				incr ringsfound [llength $rings]
			}
			log "[get_objname this] sole rings found : $ringsfound"


			set gnomes [obj_query this -range 8 -class Zwerg -owner 0 -cloaked 1]
			foreach gnome $gnomes {
				foreach obj [inv_list $gnome] {
					if {[string first [get_objclass $obj] $ALLRINGS] >= 0} {
						incr ringsfound
					}
				}
			}

			log "[get_objname this] total rings found : $ringsfound"
			if {$ringsfound >= 7} {
				return 1
			} else {
				return 0
			}
		}
	}
}


// Ventil für die Lava im Lavarätsel
// damit kann man am Ende das Rätsel abschalten

def_class Schalter_knopf_lava_5 none tool 0 {} {
	set_class_anim press lava_ventil.anim
	set_class_anim release lava_ventil.anim
	call scripts/classes/items/calls/switcher.tcl

	def_event evt_timer0_init
	handle_event evt_timer0_init {
		set lava [obj_query this "-class LavaPumpeK -range 30 -limit 1"]
		if {$lava == 0} {
			log "[get_objname this] LavaPumpeK not found - will try again!"
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
		}

		set amboss [obj_query this "-class Amboss -range 30 -limit 1"]
		if {$amboss == 0} {
			log "[get_objname this] Amboss not found - will try again!"
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
		}

	}

	obj_init {
		set standardanim lava_ventil.standard
		set standardframe 0
		set switchanim lava_ventil.anim
		set switchframe 0

		set lava -1
		set amboss -1
		set_viewinfog this 1

		call scripts/classes/items/calls/switcher.tcl
		timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+1]

		call_method this set_actiononpress {
			if {\$lava <= 0} { return };
			call_method \$lava set_height 30;
			call_method \$amboss LavaReady
		}
	}
}




def_class Schalter_knopf_lava_6 none tool 0 {} {
	set_class_anim press lava_ventil.anim
	set_class_anim release lava_ventil.anim
	call scripts/classes/items/calls/switcher.tcl

	def_event evt_timer0_init
	handle_event evt_timer0_init {
		set lavacontroler [obj_query this "-class LavaControlerK -range 100 -limit 1"]
		if {$lavacontroler == 0} {
			log "[get_objname this] LavaControlerK not found - will try again!"
			timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+3]
		}
	}

	obj_init {
		set standardanim lava_ventil.standard
		set standardframe 0
		set switchanim lava_ventil.anim
		set switchframe 0

		set lavacontroler -1
		set_viewinfog this 1

		call scripts/classes/items/calls/switcher.tcl
		timer_event this evt_timer0_init -repeat 0 -userid 0 -attime [expr [gettime]+1]

		call_method this set_actiononpress {
			if {\$lavacontroler <= 0} { return };
			call_method \$lavacontroler disable
		}
	}
}


def_class Fenris_Karte metal tool 0 {} {
	class_defaultanim fenris_karte.standard

 	method let_me_be_an_obj {} { }

	obj_init {
		set_hoverable this 0
		set_viewinfog this 1
	}
}

def_class Troll_Blume metal tool 0 {} {
	class_defaultanim troll_blume.standard

 	method let_me_be_an_obj {} { }

	obj_init {
		set_hoverable this 0
		set_viewinfog this 1
	}
}

def_class Troll_Grammophon metal tool 0 {} {
	class_defaultanim troll_grammaphon.standard

 	method let_me_be_an_obj {} { }

	obj_init {
		set_hoverable this 0
		set_viewinfog this 1
	}
}

def_class Troll_Wecker metal tool 0 {} {
	class_defaultanim troll_wecker.standard

 	method let_me_be_an_obj {} { }

	obj_init {
		set_hoverable this 0
		set_viewinfog this 1
	}
}

def_class Troll_Klavier metal tool 0 {} {
	class_defaultanim troll_klavier.standard

 	method let_me_be_an_obj {} { }

	obj_init {
		set_hoverable this 0
		set_viewinfog this 1
	}
}

def_class Troll_Rettungsring metal tool 0 {} {
	class_defaultanim troll_rettungsring.standard

 	method let_me_be_an_obj {} { }

	obj_init {
		set_hoverable this 0
		set_viewinfog this 1
	}
}


def_class Fenris_Stuhl metal tool 0 {} {
	class_defaultanim fenris_stuhl.standard

 	method let_me_be_an_obj {} { }

	obj_init {
		set_hoverable this 0
		set_viewinfog this 1
		adaptive_sound marker fenris [get_pos this] 70
	}
}



// Der Stöpsel in Fenris' Badewanne: kann rausgezogen und wieder reingesteckt werden

def_class Fenris_Stoepsel metal tool 0 {} {
	call scripts/misc/animclassinit.tcl

	class_defaultanim kette.drin

	method objaction {user} {
		tasklist_add $user "walk_pos \{ [get_pos this] \}"
		tasklist_add $user "rotate_toright"
		tasklist_add $user "play_anim benda"
		tasklist_add $user "call_method [get_ref this] pullout; play_anim bendb"
	}


	method get_standoff_dist {} {
		return -1
	}


	method pullout {} {
		global is_out

		if {$is_out == 0} {
			set is_out 1
			set wanne [obj_query this "-class Fenris_Wanne -range 30 -limit 1"]
			if {$wanne > 0} {
				call_method $wanne empty
			}
			action this anim kette.rausziehen {
				set_anim this kette.liegen 0 $ANIM_LOOP
			}

			set fenris [obj_query this "-class Fenris -range 200 -limit 1"]
			if {$fenris > 0} {
				call_method $fenris bathempty
			}
		}
	}


	method putin {} {
		global is_out

		if {$is_out} {
			set_anim this kette.drin 0 $ANIM_LOOP
			set is_out 0
			set wanne [obj_query this "-class Fenris_Wanne -range 30 -limit 1"]
			if {$wanne > 0} {
				call_method $wanne fill
			}
		}
	}


	obj_init {
		call scripts/misc/animclassinit.tcl

		set_anim this kette.drin 0 $ANIM_LOOP
		set_hoverable this 1
		set_viewinfog this 1

		set is_out 		0
	}
}



// Der Fenriskrug steht bei Fenris in der Höhle auf dem Tisch
// er kann von einem Zwerg vergiftet werden, wenn der Zwerg Pilzschnaps dabei hat
// Fenris kann den vergifteten Trank trinken, das muss mehrmals geschehen.

def_class Fenris_Krug metal tool 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/classes/story/sequencer.tcl

	class_defaultanim fenris_krug.standard

	method objaction {user} {
		global is_poisoned

		set idx [inv_find $user Pilzschnaps]
		tasklist_add $user "walk_pos \{[vector_fix [vector_add [get_pos this] {-0.6 1.5 5.0}]]\}"
		set fenris [obj_query this "-class Fenris -range 10 -limit 1"]
		if {$idx >= 0  &&  $is_poisoned  == 0  &&  $fenris == 0} {
			// Pilzschnaps gefunden & benutzt
			set obj [inv_get $user $idx]
			tasklist_add $user "rotate_toback"
			tasklist_add $user "play_anim fenrispoison"
			tasklist_add $user "inv_rem $user $idx; del $obj; call_method [get_ref this] poison"
		} else {
			tasklist_add $user "play_anim scratchhead"
			tasklist_add $user "play_anim dontknow"
		}
	}


	method poison {} {
		global is_poisoned
		set is_poisoned 1
	}



	method drink {} {
		global is_poisoned

		set was_poisoned $is_poisoned
		set is_poisoned 0
		return $was_poisoned
	}

	method get_standoff_dist {} {
		return 5.0
	}


	method hide {} {
		set_particlesource this 0 0
		set_visibility this 0
	}

	method show {} {
		set_particlesource this 0 1
		set_visibility this 1
	}

	obj_init {
		call scripts/misc/animclassinit.tcl
		call scripts/classes/story/sequencer.tcl

		set_anim this fenris_krug.standard 0 $ANIM_STILL
		change_particlesource this 0 11 {0 -1 0} {0 -0.1 0} 64 1 0
		set_particlesource this 0 1
		set_hoverable this 1
		set_visibility this 1
		set_viewinfog this 1

		set is_poisoned   0
	}
}



// Der Quietschewiggle liegt bei Fenris in der Höhle rum und kann gedrückt werden
// er macht dann ein komisches Geräusch, auf das Fenris reagiert

def_class Quietschewiggle metal tool 0 {} {
	call scripts/misc/animclassinit.tcl

	class_defaultanim quitschewiggle.standard

	method objaction {user} {
		set myref [get_ref this]

		tasklist_add $user "walk_near_item $myref 0.5"
		tasklist_add $user "rotate_towards $myref"

		if {$active} {
			tasklist_add $user "play_anim pressbutton"
			tasklist_add $user "call_method $myref squeak"
		} else {
			tasklist_add $user "play_anim leftright"
			tasklist_add $user "play_anim dontknow"
		}
	}


	// quietschen!

	method squeak {} {
		set fenris [obj_query this "-class Fenris -range 200 -limit 1"]
		if {$fenris <= 0} {
			return
		}
		log "Quietschewiggle: SQUEEEEEEEAK!"
		call_method $fenris squeaksound
	}

	method get_standoff_dist {} {
		return 2.5
	}

	// Der Quietschewiggle läßt sich erst drücken, nachdem die Wanne zweimal ausgelassen wurde
	method activate {} {
		set active 1
	}

	obj_init {
		call scripts/misc/animclassinit.tcl

		set_anim this quitschewiggle.standard 0 $ANIM_LOOP
		set_hoverable this 1
		set_viewinfog this 1

		set active 0
	}
}


// Die Fenriswanne ist ein magisches Objekt, das Fenris' Badewanne leeren und Auffüllen kann

def_class Fenris_Wanne metal tool 0 {} {

	def_event evt_timer_update
	handle_event evt_timer_update {
		global is_full

		if {$is_full} {
			// Becken sollte gefüllt sein

			if {![isunderwater [vector_add [get_pos this] {0.0 0.5 0.0}]]} {
				set_fsource this -volume 10.0 -vpf 1.0 -type lavawater -pos {0 1.5 0}
//				log "Fenris_Wanne: reinpumpen!"
			} else {
				rem_fsource this
			}

		} else {
			// Becken sollte leer sein

			rem_fsource this
			set_fsource this -pos {0 3 0} -vpf -5.0
//			log "Fenris_Wanne: auspumpen!"
		}
	}

	method fill {} {
		global is_full
		set is_full 1
	}

	method empty {} {
		global is_full
		set is_full 0
	}

	obj_init {
		set_anim this trigger_fahne3.standard 0 0
		set_selectable this 0
		set_hoverable this 0
		if {![get_mapedit]} {
			set_visibility this 0
		} else {
			set_visibility this 1
		}

		set_fstopper this {-15 0} {5 0} 0
		set is_full 1

		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0
	}
}



// Das Dimensionstor erscheint, nachdem Fenris besiegt worden ist
// Zwerge können durch das Tor in den Endkampf wechseln

def_class Dimensionstor metal tool 0 {} {

	// Timer-Event: checkt, ob keine Zwerge mehr vorhanden
	// es könnte nämlich sein, dass der letzte Zwerg in der Welt stirbt und nie ins Portal geht...
	
	def_event evt_timer_update
	handle_event evt_timer_update {
		set gnomes [obj_query this -class Zwerg -owner 0]
		if {$gnomes == 0} {
			if {[llength [call_method this get_gnomes]] > 0} {
				journey
			}
		}
	}


	method objaction {user} {
		if {$active == 0} {
			return
		}

		tasklist_add $user "walk_pos \{ [vector_add [get_pos this] {0 0 5}] \}"
		tasklist_add $user "rotate_towards \{ [vector_add [get_pos this] {0 0 -2}] \}"
		tasklist_add $user "play_anim dimensiongate_in"
		tasklist_add $user "call_method [get_ref this] dissolve $user"
	}

	method deactivate {} {
		set_hoverable this 0
		set active 0
		timer_unset this 0
	}


	method get_standoff_dist {} {
		return 3
	}
	
	
	method dissolve {gnome} {
		set_visibility $gnome 0
		set_owner $gnome -1
		set_activegameplay $gnome 0
		inv_add this $gnome
		set_pos $gnome -100 -100 0


		set remaining [obj_query 0 "-class Zwerg -owner 0"]
		if {$remaining == 0} {
			journey
		}
	}


	method get_gnomes {} {
		return [inv_list this]
	}


	obj_init {
		set_anim this dimensionstor.standard 0 2
		set_hoverable this 1
		set_visibility this 1
		set_collision this 1
		set_viewinfog this 1

		set active 1

		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0


		// die Reise kann beginnen!
		proc journey {} {
           	start_fade 1 0
           	// GameOverCheck ausschalten, da wir scheinbar (!) keine Zwerge mehr haben
           	sm_set_event GameOverCheck 0
			action this wait 1.0 {sm_send_message this "alleweg"}
		}

	}
}
