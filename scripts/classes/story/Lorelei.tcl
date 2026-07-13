// Klassen für das Lorelei - Rätsel


def_class Reflektor metal tool 2 {} {
	call scripts/misc/autodef.tcl

	// Init-Timer: Kristalle in der Nähe einfangen und einhängen
	def_event evt_timer0
	handle_event evt_timer0 {
		set crys [obj_query this "-class Kristall -range 3.0 -limit 1"]
		if {$crys != 0  &&  [obj_valid $crys]} {
			call_method this snap_obj $crys
		}
		set lasercollector [obj_query this "-class Lasercollector -limit 1"]
		if {$lasercollector == 0} {
			set lasercollector -1
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
			log "[get_objname this] : Lasercollector not found, will try again!"
		}
	}


	// Update-Timer: feststellen, ob ein Kristall der eben noch drin war entfernt wurde
	//               in diesem Fall muss das ganze Rätsel geupdated werden!
	def_event evt_timer_update
	handle_event evt_timer_update {
		if {$snapped_obj != -1} {
			if {![obj_valid $snapped_obj]  ||  [get_linked_to $snapped_obj] != [get_ref this]} {
				set snapped_obj -1
				if {$lasercollector > 0} {
					call_method $lasercollector updateall
				}
			}
		}
		if {$activedir != -1  &&  [llength $activedir] == 3} {
//			log "checking hmap at $activedir; returned [get_hmap [lindex $activedir 0] [lindex $activedir 1]]"
			if {[expr [get_hmap [lindex $activedir 0] [lindex $activedir 1]] + 0.1] < [lindex $activedir 2]} {
				call_method $lasercollector updateall
			}
		}
	}


	// Lasertreffer aus Richtung from_dir ( up | down | left | right )
	// mit ttl (time-to-live)

	method laserhit {from_dir ttl} {
		global LASERDURATION north south east west activedir

		if {[call_method this get_snapped_obj] < 0} {
			set to_dir [opposite_dir $from_dir]
		} else {
			set to_dir [call_method this get_info $from_dir]
			set_light this 1

			set destroyclass [call_method this get_info destroy]
			if {$destroyclass != "undefined"} {
				set objlist [obj_query this "-class $destroyclass"]
				if {$objlist != 0} {
					foreach item $objlist {
						call_method $item destroy
					}
				}
			}
		}
//		log "[get_objname this] hit by laser from $from_dir, reflecting to $to_dir, ttl is $ttl, activedir is $activedir"

		if {$ttl <= 0} {
			return
		}

		if {$activedir != -1} {
			call_method this destroy_crystal
			return
		}

		set ttl [expr {$ttl - 1}]

		set sourcepos [vector_add [get_pos this] [get_linkpos this 0]]
		switch $to_dir {
			n		{ set var north	}
			s		{ set var south }
			e		{ set var east  }
			w		{ set var west  }
			default { log "ERROR: method laserhit : illegal to_dir direction $todir" }
		}

		if {[llength [subst $$var]] == 1  &&  [subst $$var] != -1  &&  [obj_valid [subst $$var]]} {
			set targetobj [subst $$var]
			set targetpos [vector_add [get_pos $targetobj] [get_linkpos $targetobj 0]]
		} else {
			set targetobj [find_next_reflector [get_ref this] $to_dir]
			set $var $targetobj
			if {[llength $targetobj] == 1} {
				set targetpos [vector_add [get_pos $targetobj] [get_linkpos $targetobj 0]]
			} else {
				set targetpos $targetobj
				set targetobj -1
			}
		}

		set dirvec [vector_normalize [vector_sub $targetpos $sourcepos]]
		laserbeam $sourcepos $targetpos $dirvec $LASERDURATION 0.0 0.15 0.4
		if {$targetobj != -1} {
			set activedir $targetobj
			call_method $targetobj laserhit [opposite_dir $to_dir] $ttl
		} else {
			set activedir $targetpos
		}
	}


	method snapping_action {user item} {
		global snapped_obj

		set dummypos [vector_add [get_pos this] [get_linkpos this 0]]
		set pos 	 [vector_add $dummypos {-0.3 0.9 0}]

		tasklist_add $user "walk_pos \{$pos\}"
		tasklist_add $user "if {\[get_gnomeposition this\] == 0} { rotate_towards \{[vector_add [get_pos this] [get_linkpos this 0]]\} }"
		tasklist_add $user "play_anim \[putdown_anim\]"
		tasklist_add $user "inv_rem $user [inv_find_obj $user $item]; call_method [get_ref this] snap_obj $item"
	}


	method snap_obj {obj} {
		global snapped_obj

		if {[obj_valid $obj]} {
			set snapped_obj $obj
			link_obj $obj [get_ref this] 0;
			set_pos $obj [vector_add [get_pos this] [get_linkpos this 0]]
			if {$lasercollector > 0} {
				call_method $lasercollector updateall
			}
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


	method deactivate {} {
		global activedir
		set activedir -1
		set_light this 0
	}


	method destroy_crystal {} {
		set crystal [call_method this get_snapped_obj]
		if {$crystal > 0} {
			destruct $crystal
			del $crystal
		}
	}


	method Editor_Set_Info {ifo} {
		global info_string
		set info_string $ifo
	}


	method get_info {name} {
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


	obj_init {
		call scripts/misc/autodef.tcl

		// Sucht einen Reflektor in der angegebenen Richtung, der vom Sourcereflektor aus getroffen werden würde
		// direction: n | s | w | e
		// returns:   ID des Reflektors oder -1, falls keiner gefunden

		proc find_next_reflector {sourceobj direction} {
			global MAXRANGE XTOLERANCE YTOLERANCE

			set objlist [obj_query $sourceobj "-class Reflektor -range $MAXRANGE"]
	//		log "objlist: $objlist"
			if {$objlist == 0} {
				return
			}
			set myx [get_posx $sourceobj]
			set myy [get_posy $sourceobj]

			set besthit_obj -1
			set besthit_dist [expr {$MAXRANGE + 1}]
			foreach item $objlist {
				set linkx [lindex [get_linkpos $item 0] 0]
				set linky [lindex [get_linkpos $item 0] 1]
				set objx [expr {[get_posx $item] + $linkx}]
				set objy [expr {[get_posy $item] + $linky}]
				set ok 0
	//			log "checking [get_objname $item] for possible hit... (my: $myx $myy (tol: $XTOLERANCE) other: $objx $objy (tol: ($YTOLERANCE))"
				switch $direction {
					n	{ if {$objy < $myy  && [expr abs ($myx - $objx)] <= $XTOLERANCE} {set ok 1} }
					s	{ if {$objy > $myy  && [expr abs ($myx - $objx)] <= $XTOLERANCE} {set ok 1} }
					w	{ if {$objx < $myx  && [expr abs ($myy - $objy)] <= $YTOLERANCE} {set ok 1} }
					e 	{ if {$objx > $myx  && [expr abs ($myy - $objy)] <= $YTOLERANCE} {set ok 1} }
				}
				if {$ok} {
					set dist [vector_dist3d [get_pos $sourceobj] [get_pos $item]]
					if {$dist < $besthit_dist} {
						set besthit_dist $dist
						set besthit_obj  $item
					}
				}
			}


			if {$besthit_obj == -1} {
				// kein Zielobjekt gefunden --> Trefferpunkt auf der Scape suchen
				switch $direction {
					n {set dirvec { 0 -1 0}}
					s {set dirvec { 0  1 0}}
					e {set dirvec { 1  0 0}}
					w {set dirvec {-1  0 0}}
				}
				set objpos [vector_add [get_pos $sourceobj] [get_linkpos $sourceobj 0]]
				set endpos [get_block_point $objpos [vector_add $objpos [vector_mul $dirvec $MAXRANGE]] $dirvec [lindex $objpos 2]]
				return $endpos
			} else {
				// Zielobjekt gefunden --> Weg auf Kollision mit der Scape untersuchen
				set pos1 [vector_add [get_pos $sourceobj] [get_linkpos $sourceobj 0]]
				set pos2 [vector_add [get_pos $besthit_obj] [get_linkpos $besthit_obj 0]]
				set dirvec [vector_normalize [vector_sub $pos2 $pos1]]
				set pos3 [get_block_point $pos1 $pos2 $dirvec 11]
				if {$pos3 == -1} {
					return $besthit_obj
				} else {
					return $pos3
			 	}
			}
		}


		proc get_block_point {startpos endpos dirvec maxheight} {
			set x  	 [lindex $startpos 0]
			set y 	 [lindex $startpos 1]
			set endx [lindex $endpos 0]
			set endy [lindex $endpos 1]
			set dx   [lindex $dirvec 0]
			set dy   [lindex $dirvec 1]

			if {$dx == 0  &&  $dy == 0} {
				return -1
			}

			while {1} {
				if {[get_hmap $x $y] > $maxheight} {
					set z [get_hmap $x $y]
					if {[lindex $startpos 2] < $z} {
						set z [lindex $startpos 2]
					}
					return "$x $y $z"
				}

				if {$dx > 0} {
					set x [expr {$x + $dx}]
					if {$x > $endx} {
						return -1
					}
				}
				if {$dx < 0} {
					set x [expr {$x + $dx}]
					if {$x < $endx} {
						return -1
					}
				}

				if {$dy > 0} {
					set y [expr {$y + $dy}]
					if {$y > $endy} {
						return -1
					}
				}

				if {$dy < 0} {
					set y [expr {$y + $dy}]
					if {$y < $endy} {
						return -1
					}
				}
			}
		}


		proc opposite_dir {direction} {
			switch $direction {
				n		{ return s }
				s		{ return n }
				e		{ return w }
				w 		{ return e }
				default	{ return error }
			}
		}

		set_anim this kris_laser_c.standard 0 $ANIM_LOOP

		set_hoverable this 0
		set_physic this 0
		set_viewinfog this 1


		set snapped_obj 	-1
		set lasercollector 	-1

		set activedir 		-1		;// aktive Feuerrichtung: n, s, e, w oder -1
		set north			-1		;// jeweilige Ziele in Richtung, entweder ID eines Reflektors oder Vektor oder -1
		set south			-1
		set east			-1
		set west			-1

		change_light this [get_linkpos this 0] 3.0 {0.1 0.3 1.0} 1.0
		set_light this 0

		set MAXRANGE 200.0					;// so weit reicht ein Laserstrahl
		set XTOLERANCE 1.5					;// Abweichungen, die bei der Suche nach einem Ziel-Reflektor
		set YTOLERANCE 1.5					;// in Kauf genommen werden (Ziel darf z.B. um Y-Tol. höher oder tiefer liegen)
		set LASERDURATION 100000

		set info_string ""

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0
	}
}




def_class Kristall stone material 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/classes/items/calls/resources.tcl
	class_objsnapclass Reflektor 1.5
	class_defaultanim  kristall.standard
	method change_owner {new_owner} {
		add_owner_attrib [get_owner this] [get_objclass this] -1
		set_owner this $new_owner
		add_owner_attrib [get_owner this] [get_objclass this] 1
	}
	obj_init {
		call scripts/classes/items/calls/resources.tcl
		set_anim this kristall.standard 0 $ANIM_LOOP
		set_viewinfog this 1
		set_storable this 1
		set_physic this 1
		set_attrib this weight 0.08
		set_hoverable this 1
	}
}




def_class Lorelei metal tool 2 {} {
	call scripts/misc/autodef.tcl

	def_event evt_timer_abduct

	handle_event evt_timer_abduct {
		abduct_males
	}

	method destroy {} {
		log "Lorelei has been destroyed. Yeah!"
		catch { sm_add_event Lorelei_zerstoert }
		catch { sm_set_event Lorelei_zerstoert }
		set trg [new Trigger_Crystal_Lorelei_Vernichtung]
		set_pos $trg [get_pos this]

		// tell the mirror to become a ring (lasers will not work any more)
		set ring [obj_query this "-class Ring_Der_Magie -limit 1"]
		if {$ring != 0} {
			call_method $ring become_ring
		}

		// tell the collector to update the lasers
		set collector [obj_query this "-class Lasercollector -limit 1"]
		if {$collector != 0} {
			call_method $collector updateall
		}

		// tell the reflektors to destroy all their crystals
		set reflektorlist [lnand 0 [obj_query this "-class Reflektor"]]
		foreach item $reflektorlist {
			call_method $item destroy_crystal
		}

		timer_unset this 1
	}


	method abduct_males {} {
		timer_event this evt_timer_abduct -repeat -1 -interval 20 -userid 1 -attime [expr {[gettime]+1}]
	}

	method free_males {} {
		global victimlist freezlist
		foreach gnome $victimlist {
			set_activegameplay $gnome 1
			set_owner $gnome 0
			set_hoverable $gnome 1
			set_event $gnome evt_zwerg_break -target $gnome
		}
		foreach item $freezlist {
			call_method $item destroy
		}

		set victimlist [list]
		set freezlist [list]
	}


	obj_init {
		call scripts/misc/autodef.tcl

		set_anim this kris_lorelei_kris.standard 0 $ANIM_LOOP

		set_physic this 0
		set_hoverable this 0
		set_visibility this 1
		set_collision this 1
		set_viewinfog this 1

		adaptive_sound marker sirenen [get_pos this] 99
		set victimlist [list]
		set freezlist [list]

		proc abduct_males {} {
			global victimlist freezlist
			set newvictimlist [lnand 0 [obj_query this "-class Zwerg -owner 0 -flagpos male"]]
			set PL [obj_query this -class PseudoLorelei -limit 1]
			if {$PL} {
				set newvictimlist [lor $newvictimlist [call_method $PL get_victim_list]]
				del $PL
			}
			set base [vector_add [get_pos this] {5.0 0 -10.0}]
			foreach gnome $newvictimlist {
				set_event $gnome evt_zwerg_break -target $gnome
				action $gnome wait 2 {}
				set_owner $gnome -1
				set_hoverable $gnome 0
				set_visibility $gnome 1
				set_activegameplay $gnome 0
				set_rot $gnome {0 0 0}

				call_method $gnome inv_rem_all
							
				for {set i 0} {$i < 16} {incr i} {
					free_particlesource $gnome $i
				} 
				call_method $gnome del_current_muetze				
				set_user_groups $gnome ""

				set pos {-1 -1 -1}
				while {[lindex $pos 0] <=0} {
					set z [random 10.0]
					set x [random 17.0]
					set pos [vector_add $base "-$x 0 $z"]
					set pos [get_place -center $pos -rect -4 0 4 5]
				}
				set_pos $gnome $pos
				set freez [new Lorelei_freez]
				lappend freezlist $freez
				set_pos $freez $pos
				set_roty $freez [random 6.0]

				switch [irandom 8] {
					0	{set_anim $gnome mann.aufbauen_links_u_metall 0 0}
					1	{set_anim $gnome mann.applaudieren			  6 0}
					2	{set_anim $gnome mann.verlegen				  2 0}
					3	{set_anim $gnome mann.werkeln_feuer			  3 0}
					4	{set_anim $gnome mann.weissnich				  2 0}
					5	{set_anim $gnome mann.stand_anim_a			  0 0}
					6	{set_anim $gnome mann.ablegen_a				  0 0}
					7	{set_anim $gnome mann.anlehnen_start		  0 0}
				}
			}
			set victimlist [lor $victimlist $newvictimlist]
 		}
	}
}

// Kidnappt maennliche Zwerge solange Lorelei noch nicht in Map
def_class PseudoLorelei none tool 0 {} {

	def_event evt_timer_abduct

	handle_event evt_timer_abduct {
		set lorel [obj_query this -class Lorelei -limit 1]
		if {$lorel} {
			call_method $lorel abduct_males
			timer_unset this 1
		} else {
			abduct_males
		}
	}

	method start_abducting {} {
		timer_event this evt_timer_abduct -repeat -1 -userid 1 -interval 50 -attime [expr {[gettime]+50}]
		set mypos [get_pos this]
		abduct_males
	}

	method get_victim_list {} {
		return $victimlist
	}

	obj_init {

		set victimlist {}

		proc abduct_males {} {
			global mypos victimlist
			set newvictims [lnand 0 [obj_query this "-class Zwerg -owner 0 -flagpos male"]]
			foreach gnome $newvictims {
				set_event $gnome evt_zwerg_break -target $gnome
				action $gnome wait 2 {}
				set_owner $gnome -1
				set_hoverable $gnome 0
				set_activegameplay $gnome 0
				set_visibility $gnome 0
				
				call_method $gnome inv_rem_all
				
				for {set i 0} {$i < 16} {incr i} {
					free_particlesource $gnome $i
				} 
				call_method $gnome del_current_muetze
				set_user_groups $gnome ""

				set_pos $gnome $mypos
			}
			set victimlist [lor $victimlist $newvictims]
		}
	}
}

def_class Laseremitter metal tool 2 {} {
	call scripts/misc/autodef.tcl

	def_event evt_timer0
	handle_event evt_timer0 {
		set lasercollector [obj_query this "-class Lasercollector -limit 1"]
		if {$lasercollector <= 0} {
			log "[get_objname this] : Could not find Lasercollector, will try again!"
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
			return
		}
		set baserotation [get_anglexz [get_pos this] [get_pos $lasercollector]]
		set_roty this $baserotation
		set rotationstep 0
		call_method this rotate [expr {[irandom 7]+1}]
	}

	method objaction {user} {
		tasklist_add $user "rotate_towards [get_ref this]"
		tasklist_add $user "play_anim planeloop; call_method [get_ref this] rotate 1"
	}


	method get_standoff_dist {} {
		return 0.5
	}


	method rotate {step} {
		global rotationstep baserotation
		set rotationstep [expr {($rotationstep + $step) % 8}]
		set_roty this [expr {$baserotation + ($rotationstep * 0.785)}]
		call_method $lasercollector updateall
	}


	method get_rotationstep {} {
		global rotationstep
		return $rotationstep
	}


	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this kris_laser_a.standard 0 $ANIM_LOOP

		set_physic this 0
		set_hoverable this 1
		set_collision this 1
		set_viewinfog this 1

		set rotationstep 0
		set baserotation 0.0

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
	}
}



def_class Lasercollector metal tool 2 {} {
	call scripts/misc/autodef.tcl

	def_event evt_system_gamestart
	handle_event evt_system_gamestart {
		log "[get_objname this] evt_system_gamestart : updating lasers!"
		call_method this updateall
	}


	// Init-Timer: Notwendige Objekte suchen
	def_event evt_timer0
	handle_event evt_timer0 {
		set reflektorlist [obj_query this "-class Reflektor"]
		if {$reflektorlist != 0} {
			foreach element $reflektorlist {
				if {[call_method $element get_info firstreflektor] == "yes"} {
					set firstreflektor $element
				}
			}
		}

		if {$firstreflektor <= 0} {
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
			log "[get_objname this] : First Reflektor not found, will try again!"
			return
		}

		set emitterlist [obj_query this "-class Laseremitter"]
		if {[llength $emitterlist] < 4} {
			timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
			log "[get_objname this] : Could not find all Laseremitters, will try again!"
			return
		}
		call_method this updateall
	}


	// Update-Timer: feststellen, ob sich am Spiegel was verändert hat
	def_event evt_timer_update
	handle_event evt_timer_update {
		if {$snapped_obj != -1} {
			if {![obj_valid $snapped_obj]  ||  [get_linked_to $snapped_obj] != [get_ref this]} {
				set snapped_obj -1
				call_method this updateall
			}
		}
	}


	// eine Änderung hat sich bei einem Teilnehmer ergeben --> gesamtes Rätsel neu berechnen

	method updateall {} {
		global snapped_obj firstreflektor emitterlist LASERDURATION

		if {$firstreflektor <= 0} {
			return
		}

		delete_laserbeams
		set reflectorlist [obj_query this "-class Reflektor"]
		if {$reflectorlist != 0} {
			foreach item $reflectorlist {
				call_method $item deactivate
			}
		}

		set i 0
		set ok 1
		set pos2 [vector_add [get_pos this] [get_linkpos this 0]]
		foreach item $emitterlist {
			if {$i == 0} {set r 0.0; 	set g 0.1;		set b 0.25}
			if {$i == 1} {set r 0.0; 	set g 0.2;		set b 0.1}
			if {$i == 2} {set r 0.2; 	set g 0.0;		set b 0.0}
			if {$i == 3} {set r 0.15; 	set g 0.15;		set b 0.0}

			set pos1 [vector_add [get_pos $item] [get_linkpos $item 0]]
			if {[call_method $item get_rotationstep] != 0 } {
				set ok 0
				set vec  {0.0 0.0 4.0}
				set vec [vector_roty $vec [get_roty $item]]
				set vec "[lindex $vec 0] [lindex $vec 1] [expr {[lindex $vec 2]*2}]"
				laserbeam $pos1 [vector_add $pos1 $vec] [vector_normalize $vec] $LASERDURATION $r $g $b
			} else {
				laserbeam $pos1 $pos2 [vector_normalize [vector_sub $pos2 $pos1]] $LASERDURATION $r $g $b
			}
			incr i
		}

		if {!$ok} {
			return
		}

		if {[call_method this get_snapped_obj] <= 0} {
			return
		}

		if {[call_method $snapped_obj is_mirror] == 0} {
			return
		}

		call_method $firstreflektor laserhit w 25
		set pos1 [vector_add [get_pos this] [get_linkpos this 0]]
		set pos2 [vector_add [get_pos $firstreflektor] [get_linkpos $firstreflektor 0]]
		set dirvec [vector_normalize [vector_sub $pos2 $pos1]]
		laserbeam $pos1 $pos2 $dirvec $LASERDURATION 0.0 0.15 0.4
	}


	method snapping_action {user item} {
		global snapped_obj

		tasklist_add $user "walk_pos \{[vector_add [get_pos this] {1 0 -0.5}]\}"
		tasklist_add $user "rotate_toleft"
		tasklist_add $user "play_anim putjump"
		tasklist_add $user "inv_rem $user [inv_find_obj $user $item]; call_method [get_ref this] snap_obj $item"
	}


	method snap_obj {obj} {
		global snapped_obj

		if {[obj_valid $obj]} {
			set snapped_obj $obj
			link_obj $obj [get_ref this] 0;
			set_pos $obj [vector_add [get_pos this] [get_linkpos this 0]]
			set_hoverable $obj 0  ;// der Spiegel läßt sich nach dem Einhängen vorerst nicht mehr entfernen
			call_method this updateall
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


	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this kris_laser_b.standard 0 $ANIM_LOOP

		set firstreflektor -1
		set snapped_obj -1

		set LASERDURATION 100000

		adaptive_sound marker sirenen [get_pos this]
		set_physic this 0
		set_hoverable this 0
		set_collision this 1
		set_viewinfog this 1

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0
	}
}



def_class Laserraetseltuer none tool 0 {} {
	call scripts/misc/autodef.tcl

	method destroy {} {
		destruct this
		del this
	}

	obj_init {
		call scripts/misc/autodef.tcl

		set_collision this 1
		set_hoverable this 0
		set_viewinfog this 1

		set_anim this kristalltuer.standard 0 0

		set_buildupstate this 1
		set_pf_influence this -1 -20 +8 +4 INT_MAX 0
	}

	obj_exit {
		set_pf_influence this 0 0 0 0 0 0
	}
}



def_class Ring_Der_Magie none tool 0 {} {
	call scripts/misc/autodef.tcl

	class_defaultanim magiering.standard
	class_objsnapclass Lasercollector 4.0

	obj_init {
		call scripts/misc/autodef.tcl

		set_physic this 1
		set_hoverable this 1
		set_viewinfog this 1

		set is_mirror 1
		call_method this reset
	}


	method is_mirror {} {
		global is_mirror
		return $is_mirror
	}


	method become_ring {} {
		global is_mirror

		set_hoverable this 1
		set is_mirror 0
	}

	method setstandardanim {} {
		set_anim this magiering.standard 0 2
		change_particlesource this 0 13 {0 0 0} {0 -0.07 0} 128 2 0
		set_particlesource this 0 1
	}

	method reset {} {
		set_anim this magiering.drehen 0 2
		change_particlesource this 0 13 {0 0.2 0} {0 -0.07 0} 128 2 0
		set_particlesource this 0 1
	}
}




def_class Lorelei_freez none tool 0 {} {
	call scripts/misc/autodef.tcl

	method destroy {} {
		destruct this
		del this
	}

	obj_init {
		call scripts/misc/autodef.tcl

		set_hoverable this 0
		set_collision this 1
		set_viewinfog this 1

		switch [irandom 3] {
			0 { set_anim this lorelei_freez_a.standard 0 0 }
			1 { set_anim this lorelei_freez_b.standard 0 0 }
			2 { set_anim this lorelei_freez_c.standard 0 0 }
		}
	}
}
