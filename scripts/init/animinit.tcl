;# animinit.tcl
;# allgemeine definitionen zum abspielen von animationen

set ANIM_STILL 	0	;# spiele keine animation ab
set ANIM_ONCE 	1	;# spiele die animation genau einmal ab
set ANIM_LOOP 	2	;# spiele die animation staendig ab

;# laden der animationsdatenbanken
db_load mann
if { ! [minimalrun] } {
	db_load frau
}
db_load werkzeuge
db_load halbzeuge
db_load muetzen
db_load licht
db_load muetzen
db_load produktionsstaetten
db_load rohstoffe
db_load nahrung
db_load kampf
db_load hamster
if { ! [minimalrun] } {
	db_load freizeit
	db_load transport
	db_load wuker
	db_load troll
	db_load fenrir
	db_load einrichtung
	db_load baby
	db_load fisch_a
	db_load ringe

	db_load wandboden
	db_load hinterlegung
	db_load trollhausen
	db_load flusslandschaft
	db_load metalltech
	db_load oberwelt
	db_load kristall
	db_load lavawelt
	db_load auskleidung
	db_load drache
	db_load drache01
	db_load krake
	db_load spinne
	db_load brut
	db_load riesenhamster
	db_load riesenelfe
	db_load joinedobjs
	db_load odin
	db_load odinshand
	db_load fifi
}


;# funktion zum generieren der dummyclasses (deko wie gras, moos, ...)
// 0 mal
proc SetDummyClasses {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		set anim 0
		if { [string toupper [lindex $objclass 2]] == "ANIM" } {
			set anim 2
		}

		set firstanim [lindex $animlist 0]
		set classname Dummy_$classname
		def_idiobjclass $classname "
			call scripts/misc/animclassinit.tcl
			set classanimlist \{$animlist\}
			set animname \[lindex \$classanimlist 0\]
			class_defaultanim \${animname}.standard $anim
			class_viewinfog 1
			class_physic 1
			#class_disablescripting
			# keine obj_init für Dummy Classen
		"
	}
}

;# funktion zum generieren der dummyclasses (deko wie gras, moos, ...) kein Z-Write
// 6 mal
proc SetDummyClassesNoZ {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		set anim 0
		if { [string toupper [lindex $objclass 2]] == "ANIM" } {
			set anim 2
		}

		set firstanim [lindex $animlist 0]
		set classname Dummy_$classname
		def_idiobjclass $classname "
			call scripts/misc/animclassinit.tcl
			set classanimlist \{$animlist\}
			set animname \[lindex \$classanimlist 0\]
			class_defaultanim \${animname}.standard $anim
			class_defaultmaterial modelnozwrite
			class_viewinfog 1
			class_physic 0
			#class_disablescripting
			# keine obj_init für Dummy Classen
		"
	}
}

;# funktion zum generieren der dummyclasses (deko wie gras, moos, ...) unsichtbar im fogofwar
// 0 mal
proc SetDummyClassesF {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		set firstanim [lindex $animlist 0]
		set classname Dummy_$classname
		def_class $classname none dummy 0 {} "
			call scripts/misc/animclassinit.tcl
			set classanimlist \{$animlist\}
			set animname \[lindex \$classanimlist 0\]
			class_defaultanim \${animname}.standard
			class_physic 1
			class_disablescripting
			# keine obj_init für Dummy Classen
		"
	}
}

;# funktion zum generieren der dummyclasses (deko wie gras, moos, ...) unsichtbar im fogofwar, no physix
// 0 mal
proc SetDummyClassesFNoPhys {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		set firstanim [lindex $animlist 0]
		set classname Dummy_$classname
		def_class $classname none dummy 0 {} "
			call scripts/misc/animclassinit.tcl
			set classanimlist \{$animlist\}
			set animname \[lindex \$classanimlist 0\]
			class_defaultanim \${animname}.standard
			class_physic 0
			class_disablescripting
			# keine obj_init für Dummy Classen
		"
	}
}

;# funktion zum generieren der dummyclasses (deko wie gras, moos, ...) unsichtbar im fogofwar, no physix
// 2 mal
proc SetDummyClassesFNoPhysNoIdiObj {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		set firstanim [lindex $animlist 0]
		set anim 0
		if { [string toupper [lindex $objclass 2]] == "ANIM" } {
			set anim 2
		}
		set classname Dummy_$classname
		def_class $classname none dummy 0 {} "
			call scripts/misc/animclassinit.tcl
			set classanimlist \{$animlist\}
			set animname \[lindex \$classanimlist 0\]
			class_defaultanim \${animname}.standard $anim
			class_physic 0
			def_event evt_dummy
			handle_event evt_dummy \{\}
			method dummy \{\} \{\}
			obj_init \{\}
		"
	}
}

;# wie SetDummyClasses nur die objecte der classen koennen vor der landschaft plaziert werden
// 0 mal
proc SetFrontDummyClasses {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		set classname Dummy_$classname
		def_class $classname none dummy 0 {} "
			call scripts/misc/animclassinit.tcl
			set classanimlist \{$animlist\}
			set animname \[lindex \$classanimlist 0\]
			class_defaultanim \${animname}.standard
			class_physic 1
			class_viewinfog 1
			class_disablescripting
			# keine obj_init für Dummy Classen
		"
	}
}

// wie SetDummyClasses nur die objecte der classen koennen vor der landschaft plaziert werden
// 10 mal
proc SetFrontDummyClassesNoPhys {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		set classanimlist $animlist
		set animname [lindex $classanimlist 0]
		if {$animname==""} {log "ERROR in dummy.tcl: no animname for class $classname!";continue}
		set classname Dummy_$classname
		set anim 0
		set animstartframe 0

		// defaults für set_txtanim
		set txtanimparam1 -1
		set txtanimparam2 -1
		set txtanimparam3 0
		set txtanimparam4 2

		set txtprmshift 0
		// bei ANIM wird TXTANIM um eine stelle verschoben erwartet

		if { [string toupper [lindex $objclass 2]] == "ANIM" } {
			set anim 2
			incr txtprmshift
			if { [string toupper [lindex $objclass 3]] == "RANDOM" } {
				set animstartframe [irandom [db_animlength $animname.standard]]
				log "random anims for $classname ($animstartframe)"
				incr txtprmshift
			}
		}
		if { [string toupper [lindex $objclass [expr {2 + $txtprmshift}]]] == "TXTANIM" } {
			// Parameter 1 - Submesh
			set txtanimparam1		[lindex $objclass [expr {3 + $txtprmshift}]]
			// Parameter 2 - Liste mit Animationsframes
			set txtanimparam2 [list [lindex $objclass [expr {4 + $txtprmshift}]]]
			// optionaler Parameter 3 - Startframe oder "random"
			if {[llength $objclass]>[expr {5 + $txtprmshift}]} {
				set txtanimparam3	[lindex $objclass [expr {5 + $txtprmshift}]]
				if {[string toupper $txtanimparam3] == "RANDOM"} {
					set txtanimparam3 -1
				}
			}
			//# optionaler Parameter 4 - Animmode (0=stop,1=once,2=loop)
			if {[llength $objclass]>[expr {6 + $txtprmshift}]} {
				set txtanimparam4	[lindex $objclass [expr {6 + $txtprmshift}]]
			}
		}
		def_class $classname none dummy 0 {} "
			call scripts/misc/animclassinit.tcl
			class_defaultanim $animname.standard $anim
			class_defaulttxtanim $txtanimparam1 $txtanimparam2 $txtanimparam3 $txtanimparam4
			class_viewinfog 1
			if { [has_cmap $animname.standard] } {
				class_collision 1
			}
			class_disablescripting
			# keine obj_init für Dummy Classen
		"
	}
}

if {![info exists noidiobjclassescnt]} {set noidiobjclassescnt 0}
proc SetDummyClassesAll {params objclasslist} {
	global noidiobjclassescnt
	// funktioniert in allen Kombinationen

	// Defaultwerte der Klasseneigenschaften
	set viewinfog 1
	set physic 0
	set idiobj 1
	set zwrite 1
	set front 1
	set collision -1

	// Modifizierung durch Parameterstring
	foreach var {viewinfog physic idiobj zwrite front collision} {
		if {[lsearch $params $var]!=-1} {
			set $var 1
		} elseif {[lsearch $params no$var]!=-1} {
			set $var 0
		}
	}

	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		set firstanim [lindex $animlist 0]
		set opts [lrange $objclass 2 end]
		set anim 0
		set animrand 0
		set txtanim 0
		set c_idiobj $idiobj

		// Animation, fuehrt zu noidiobj
		if { [string match -nocase "*anim*" $opts] } {
			set anim 2
			set c_idiobj 0
			if { [string match -nocase "*animrandom*" $opts] } {
				set animlen [db_animlength $firstanim.standard]
				set animrand 1
			}
		}

		// Texturanimation, fuehrt zu noidiobj
		if { [string match -nocase "*txtanim*" $opts] } {
			set txtanim 1
			set c_idiobj 0
			set idx 1
			foreach entry $opts {
				if { [string match -nocase "txtanim" $entry] } {
					break
				}
				incr idx
			}
			set txtsubmesh [lindex $opts $idx]
			incr idx
			set txtanimlist [lindex $opts $idx]
			incr idx
			set txtstart [lindex $opts $idx]
			if {[string match -nocase "random" $txtstart]} {
				set txtstart -1
			} elseif {$txtstart==""||![string is integer $txtstart]} {
				set txtstart 0
			}
			incr idx
			set txtmode [lindex $opts $idx]
			if {$txtmode==""||![string is integer $txtmode]} {set txtmode 2}
		}

		// Collision - wenn angegeben ignoriert db-Eigenschaft
		if { $collision == -1 && [has_cmap $firstanim.standard] } {
			set c_collision 1
		} elseif { $collision == 1 } {
			set c_collision 1
		} else {
			set c_collision 0
		}

		set classname Dummy_$classname
		set defcode "class_defaultanim $firstanim.standard $anim"
		if {$txtanim} {append defcode ";class_defaulttxtanim $txtsubmesh \{$txtanimlist\} $txtstart $txtmode"}
		append defcode ";class_viewinfog $viewinfog"
		append defcode ";class_physic $physic"
		append defcode ";class_collision $c_collision"
		if {!$zwrite} {append defcode {;class_defaultmaterial modelnozwrite}}
		if {$c_idiobj} {
			append defcode {;class_disablescripting}
		} else {
			append defcode {;def_event edummy;handle_event edummy {};method mdummy {} {}}
			if {$animrand} {
				append defcode ";obj_init \{set_anim this $firstanim.standard \[irandom $animlen\] $anim\}"
			} else {
				append defcode {;obj_init {}}
			}
		}
		if {!$c_idiobj} {
			incr noidiobjclassescnt
			//log ${classname}-$defcode
		}
		def_class $classname none dummy 0 {} $defcode
	}
}

;# wie SetDummyClasses nur die objecte der classen koennen vor der landschaft plaziert werden
proc SetWeaponClasses {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]
		def_class $classname metal tool 0 {} "
			call scripts/misc/animclassinit.tcl
			set classanimlist \{$animlist\}
			set animname \[lindex \$classanimlist 0\]
			class_defaultanim \${animname}.standard
			class_physic 1
			class_viewinfog 1
			method is_weapon {} {}
			method destroy {} { del this }
			obj_init \"
				set_selectable 	this 1
				set_hoverable 	this 1
				set_storable 	this 1
			\"
		"
	}
}

proc SetCaveSkinClasses {objclasslist} {
	foreach objclass $objclasslist {
		set classname [lindex $objclass 0]
		set animlist [lindex $objclass 1]

    	set nameidlist 	[split $classname "_"]
    	set sSet		[lindex $nameidlist 0]
    	set sSize       [lindex $nameidlist 2]
    	set sDetail		[lindex $nameidlist 4]

		switch $sSet {
			"Urw"		{set iSet 0}
			"Met"		{set iSet 1}
			"Kris"		{set iSet 2}
			"Golden"	{set iSet 3}
			default 	{set iSet 0; log "SetCaveSkinClasses: unknown set: $sSet"}
		}

		switch $sSize {
			"1"		{set iSize 0}
			"2"		{set iSize 1}
			"3"		{set iSize 2}
			"4"		{set iSize 3}
			default {set iSize 0; log "SetCaveSkinClasses: unknown size: $sSize"}
		}

		switch $sDetail {
			"a"		{set iDetail 2}
			"b"		{set iDetail 1}
			"c"		{set iDetail 0}
			default {set iDetail 0; log "SetCaveSkinClasses: unknown detail: $sDetail"}
		}

		if { [llength $objclass] >= 3 } {set iSet [lindex $objclass 2]}
		if { [llength $objclass] >= 4 } {set iDetail [lindex $objclass 3]}
		if { [llength $objclass] >= 5 } {set iSize [lindex $objclass 4]}

		def_class $classname none dummy 0 {} "
			call scripts/misc/animclassinit.tcl
			set classanimlist \{$animlist\}
			set animname \[lindex \$classanimlist 0\]
			class_defaultanim \${animname}.standard
			class_physic 0
			class_viewinfog 1
			class_collision 0
//			log \"add_cave_skin_class($classname) $iSet $iDetail $iSize\"
			add_cave_skin_class $iSet $iDetail $iSize
		"

	}
}

proc Create_Instant_Trigger_Class {classname seqfile {existclasses "none"}} {
    set defcode {
    	call scripts/classes/story/sequencer.tcl
    	obj_init {
    		call scripts/classes/story/sequencer.tcl
    	}
    	method run {} {
    		if { "existclasses" != "none" } {

				set ol [obj_query this -class existclasses -limit 1]
				if { $ol == 0 } {
					set_undeletable this 0
					del this
					return
				}
			}
    		set sequencescript seqfile
    		sequencer_activate
    	}
    	method create {} {
    		set nt [new classname]
    		set_owner $nt 0
    		set_pos $nt [get_pos this]
    		call_method $nt run
    	}
    }
    set defcode [string map "classname $classname seqfile $seqfile existclasses $existclasses" $defcode]
	def_class $classname none dummy 0 {} $defcode
}



