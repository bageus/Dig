def_class Zauntor_a none tool 0 {} {
	call scripts/misc/animclassinit.tcl
	method oeffnen {time} {
		if {$status=="closed"} {
			set_anim this obw_zaun_a.oeffnen 0 $ANIM_ONCE
			set status open
			if {-1<$time} {
				action this wait [expr $time + 1] {
					set_anim this obw_zaun_a.schliessen 0 $ANIM_ONCE
					set status closed
				}
			}
		}
	}
	method schliessen {} {
		if {$status=="open"} {
			set_anim this obw_zaun_a.schliessen 0 $ANIM_ONCE
			set status closed
		}
	}

	obj_init {
		call scripts/misc/animclassinit.tcl
		set_hoverable this 0
		set_anim this obw_zaun_a.standard 0 0
		set status closed
		set_collision this 1
	}
}

def_class Tuer_kaserne none tool 0 {} {
	set_class_anim opena		tuer_kaserne.oeffnen_a
	set_class_anim closea		tuer_kaserne.schliessen_a
	set_class_anim openb		tuer_kaserne.oeffnen_b
	set_class_anim closeb		tuer_kaserne.schliessen_b
	call scripts/classes/items/calls/doors.tcl
	obj_init {
		set standanim tuer_kaserne.standard
		set openanima tuer_kaserne.offen_a
		set openanimb tuer_kaserne.offen_b
		set influence_pf 1
		call scripts/classes/items/calls/doors.tcl
		set_fstopper this {0 -2} {0 1} 0
	}
}

def_class Tuer_metall none tool 0 {} {
	set_class_anim opena		metalltuer_b.oeffnen
	set_class_anim closea		metalltuer_b.schliessen
	set_class_anim openb		metalltuer_b.oeffnen
	set_class_anim closeb		metalltuer_b.schliessen
	call scripts/classes/items/calls/doors.tcl
	obj_init {
		set standanim metalltuer_b.zu
		set openanima metalltuer_b.auf
		set openanimb metalltuer_b.auf
		set influence_pf 1
		call scripts/classes/items/calls/doors.tcl
		set_fstopper this {0 -2} {0 1} 0
	}
}

def_class Tuer_verlies none tool 0 {} {
	set_class_anim opena		tuer_kaserne.oeffnen_a
	set_class_anim closea		tuer_kaserne.schliessen_a
	set_class_anim openb		tuer_kaserne.oeffnen_b
	set_class_anim closeb		tuer_kaserne.schliessen_b
	call scripts/classes/items/calls/doors.tcl
	obj_init {
		set standanim tuer_kaserne.standard
		set openanima tuer_kaserne.offen_a
		set openanimb tuer_kaserne.offen_b
		set influence_pf 1
		call scripts/classes/items/calls/doors.tcl
		set_fstopper this {0 -2} {0 1} 0
	}
}

def_class Gefaengnis_gitter_a none tool 0 {} {
	set_class_anim opena		gitter_a.hoch
	set_class_anim closea		gitter_a.runter
	set_class_anim openb		gitter_a.hoch
	set_class_anim closeb		gitter_a.runter
	call scripts/classes/items/calls/doors.tcl
	obj_init {
		set standanim gitter_a.standard
		set openanima gitter_a.runter
		set openanimb gitter_a.runter
		set influence_pf 0
		call scripts/classes/items/calls/doors.tcl
	}
}
def_class Gefaengnis_gitter_b none tool 0 {} {
	set_class_anim opena		gitter_c.hoch
	set_class_anim closea		gitter_c.runter
	set_class_anim openb		gitter_c.hoch
	set_class_anim closeb		gitter_c.runter
	call scripts/classes/items/calls/doors.tcl
	obj_init {
		set standanim gitter_c.standard
		set openanima gitter_c.runter
		set openanimb gitter_c.runter
		set influence_pf 0
		call scripts/classes/items/calls/doors.tcl
	}
}
def_class Tuer_kristall none tool 0 {} {
	set_class_anim opena		kristalltuer.links_oeffnen
	set_class_anim closea		kristalltuer.links_schliessen
	set_class_anim openb		kristalltuer.rechts_oeffnen
	set_class_anim closeb		kristalltuer.rechts_schliessen
	call scripts/classes/items/calls/doors.tcl
	obj_init {
		set standanim kristalltuer.standard
		set openanima kristalltuer.links_offen
		set openanimb kristalltuer.rechts_offen
		set influence_pf 1
		call scripts/classes/items/calls/doors.tcl
		set_fstopper this {0 -2} {0 1} 0
	}
}



def_class Trolltor_links stone protection 0 {} {
	set_class_anim open_r		troll_zinnen_l.offen
	set_class_anim opening_r	troll_zinnen_l.hoch
	set_class_anim closing_r	troll_zinnen_l.runter
	set_class_anim closed		troll_zinnen_l.standard
	set_class_anim open_l		troll_zinnen_l.offen
	set_class_anim opening_l	troll_zinnen_l.hoch
	set_class_anim closing_l	troll_zinnen_l.runter

	class_defaultanim troll_zinnen_l.standard
	class_fightdist 1.5

    def_event evt_timer0

	call scripts/misc/genericprod.tcl

	handle_event evt_timer0 {
		create_doorlogic this
		set_doorproperties this openforclasses { Troll }
	}

	obj_init {
		call scripts/misc/genericprod.tcl
		set standard_anim troll_zinnen_l
		set_anim this troll_zinnen_l.standard 0 2
		set_collision this 1
		set_visibility this 1
		set_hoverable this 1
		set_viewinfog this 1
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
	}
	// dummy event -> darf nicht gelöscht werden !
	handle_event evt_timer_delete {
	}
}

def_class Trolltuer stone protection 0 {} {
	set_class_anim open_r		stopp_tuer.offen_rechts
	set_class_anim opening_r	stopp_tuer.oeffnen_rechts
	set_class_anim closing_r	stopp_tuer.schliessen_rechts
	set_class_anim closed		stopp_tuer.standard
	set_class_anim open_l		stopp_tuer.offen_links
	set_class_anim opening_l	stopp_tuer.oeffnen_links
	set_class_anim closing_l	stopp_tuer.schliessen_links

    def_event evt_timer0

	call scripts/misc/genericprod.tcl
	class_fightdist 1.5

	handle_event evt_timer0 {
		create_doorlogic this
		set_doorproperties this openforclasses { Troll }
	}

	method has_handlers {} {}

	obj_init {
		call scripts/misc/genericprod.tcl
		set standard_anim stopp_tuer
		set_anim this stopp_tuer.standard 0 1
		set_collision this 1
		set_visibility this 1
		set_hoverable this 1
		set_viewinfog this 1
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
	}

	// dummy event -> darf nicht gelöscht werden !
	handle_event evt_timer_delete {
	}
}


def_class Trolltor_rechts stone protection 0 {} {
	set_class_anim open_r		troll_zinnen_m.offen
	set_class_anim opening_r	troll_zinnen_m.hoch
	set_class_anim closing_r	troll_zinnen_m.runter
	set_class_anim closed		troll_zinnen_m.standard
	set_class_anim open_l		troll_zinnen_m.offen
	set_class_anim opening_l	troll_zinnen_m.hoch
	set_class_anim closing_l	troll_zinnen_m.runter

	class_defaultanim troll_zinnen_m.standard
	class_fightdist 1.5

    def_event evt_timer0

	call scripts/misc/genericprod.tcl

	handle_event evt_timer0 {
		create_doorlogic this
		set_doorproperties this openforclasses { Troll }
	}

	obj_init {
		call scripts/misc/genericprod.tcl
		set standard_anim troll_zinnen_m
		set_anim this troll_zinnen_m.standard 0 2
		set_collision this 1
		set_visibility this 1
		set_hoverable this 1
		set_viewinfog this 1
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
	}
	// dummy event -> darf nicht gelöscht werden !
	handle_event evt_timer_delete {
	}
}
