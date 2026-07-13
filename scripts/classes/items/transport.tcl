call scripts/misc/utility.tcl

def_class Aufzug metal elevator 2 {} {

	class_fightdist 4.0

	method prod_items {}			{return [list]}
	method prod_item_materials item	{return [list]}
	method prod_inventions {} 		{return [list]}
	
	method_const obj_use_callback {item} {
		set itemowner [get_owner $item]
		set myowner [get_owner this]
		if {[get_diplomacy $myowner $itemowner]=="enemy"} {
			return 0
		} else {
			return 1
		}
	}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim	aufzugantrieb.standard
	set_class_anim		lift_still	aufzugantrieb.standard
	set_class_anim		lift_up		aufzugantrieb.anim
	set_class_anim		lift_down	aufzugantrieb.anim

	def_event evt_timer0

	obj_init {
		call scripts/misc/genericprod.tcl
		set_anim this aufzugantrieb.standard 0 $ANIM_STILL

        set_fogofwar this 8 8
        set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set_physic this 1
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3

		set build_dummys [list 16 17 18 19]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechtsholz oben_linksholz oben_rechtsholz oben_rechtsholz}
		set damage_dummys {24 26}
		set standard_anim aufzugantrieb.standard
	}

	handle_event evt_timer0 {
		sel /obj
		set own [get_owner this]
		set k [new Aufzugkabine]
		create_elevatorlogic this $k 1
		set_elevatorproperties this 1000 0 2.0			;# MAXRANGE=32, ACCMODE = Linear, Geschwindigkeit = 1.0
	}
}

def_class Aufzugkabine metal dummy 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim aufzugfahrkorb.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this aufzugfahrkorb.standard 0 $ANIM_STILL
		set_physic this 0
		set_hoverable this 0
		set_selectable this 0
		set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set_forceipol this 1
		set standard_anim aufzugfahrkorb.standard
	}
}

def_class Dampfaufzug metal elevator 2 {} {

	class_fightdist 4.0

	method prod_items {}			{return [list]}
	method prod_item_materials item	{return [list]}
	method prod_inventions {} 		{return [list]}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim dampfaufzugantrieb.standard
	set_class_anim		lift_still	dampfaufzugantrieb.standard
	set_class_anim		lift_up		dampfaufzugantrieb.anim
	set_class_anim		lift_down	dampfaufzugantrieb.anim

	def_event evt_timer0

	method_const obj_use_callback {item} {
		set itemowner [get_owner $item]
		set myowner [get_owner this]
		if {[get_diplomacy $myowner $itemowner]=="enemy"} {
			return 0
		} else {
			return 1
		}
	}

	obj_init {
		call scripts/misc/genericprod.tcl
		set_anim this dampfaufzugantrieb.standard 0 $ANIM_STILL

		sel /obj;

		set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set_physic this 1
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3

		set build_dummys [list 16 17 18 19 20]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {oben_rechtsmetall oben_rechtsmetall oben_linksmetall unten_rechtsmetall unten_rechtsmetall}
		set damage_dummys {24 26}
		set standard_anim dampfaufzugantrieb.standard
	}

	handle_event evt_timer0 {
		sel /obj
		set own [get_owner this]
		set k [new Dampfaufzugkabine]
		create_elevatorlogic this $k 1
		set_elevatorproperties this 1000 1 0.2 0.4	;# MAXRANGE=32, ACCMODE = quadratisch, Beschleunigung = 0.2 Bremsbeschleunigung = 0.4
	}
}

def_class Dampfaufzugkabine metal dummy 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim dampfaufzugfahrkorb.standard
	set_class_anim display dampfaufzugfahrkorb.anim

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this dampfaufzugfahrkorb.standard 0 $ANIM_STILL
		set_physic this 0
		set_hoverable this 0
		set_selectable this 0

		set_forceipol this 1

		set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set standard_anim dampfaufzugfahrkorb.standard
	}
}


def_class Kristallaufzug metal elevator 2 {} {

	class_fightdist 4.0

	method prod_items {}			{return [list]}
	method prod_item_materials item	{return [list]}
	method prod_inventions {} 		{return [list]}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim kristallaufzug.standard
	set_class_anim		lift_still	kristallaufzug.standard
	set_class_anim		lift_up		kristallaufzug.standard
	set_class_anim		lift_down	kristallaufzug.standard

	method_const obj_use_callback {item} {
		set itemowner [get_owner $item]
		set myowner [get_owner this]
		if {[get_diplomacy $myowner $itemowner]=="enemy"} {
			return 0
		} else {
			return 1
		}
	}

	def_event evt_timer0

	obj_init {
		call scripts/misc/genericprod.tcl
		set_anim this kristallaufzug.standard 0 $ANIM_STILL

        set_fogofwar this 8 8
        set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set_physic this 1
		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3
		set standard_anim kristallaufzug.standard

		set build_dummys [list 16 17]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {oben_rechtsstein oben_linksstein}
		set damage_dummys {24 26}
	}

	handle_event evt_timer0 {
		sel /obj
		set own [get_owner this]
		set k [new Kristallaufzugkabine]
		create_elevatorlogic this $k 0
		set_elevatorproperties this 1000 0 100.0 100.0 1
	}
}

def_class Kristallaufzugkabine metal dummy 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim kristallfahrkorb.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this kristallfahrkorb.standard 0 $ANIM_STILL
		set_physic this 0
		set_hoverable this 0
		set_selectable this 0
		set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set standard_anim kristallfahrkorb.standard
	}
}



def_class Holzkiepe_ wood tool 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim holzkiepe.standard

	method set_related_pannier {pannier} {
		if {[obj_valid $pannier] && [get_objclass $pannier] == "Holzkiepe"} {
			set related_pannier $pannier
		}
	}

	method get_related_pannier {} {
		if {![obj_valid $related_pannier] || $related_pannier == -1} {
			set related_pannier [new Holzkiepe]
		}
		return $related_pannier
	}

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this holzkiepe.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
		
		set related_pannier -1
	}
}


def_class Grosse_Holzkiepe_ wood tool 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim big_holzkiepe.standard

	method set_related_pannier {pannier} {
		if {[obj_valid $pannier] && [get_objclass $pannier] == "Grosse_Holzkiepe"} {
			set related_pannier $pannier
		}
	}

	method get_related_pannier {} {
		if {![obj_valid $related_pannier] || $related_pannier == -1} {
			set related_pannier [new Grosse_Holzkiepe]
		}
		return $related_pannier
	}

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this big_holzkiepe.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
		
		set related_pannier -1
	}
}


def_class Holzkiepe wood transport 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim holzkiepe.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this holzkiepe.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Grosse_Holzkiepe wood transport 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim big_holzkiepe.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this big_holzkiepe.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}


def_class Schubkarren wood transport 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim schubkarren.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this schubkarren.standard 0 $ANIM_STILL

		set_attrib this weight 0.1
		set_attrib this hitpoints 0.5
	}
}

def_class Hamsterkarren food transport 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim hamsterkarren.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this hamsterkarren.standard 0 $ANIM_STILL

		set_attrib this weight 0.1
		set_attrib this hitpoints 0.5
	}
}


def_class Leiter metal elevator 2 {} {

	class_fightdist 1.5

	method prod_items {}			{return [list]}
	method prod_item_materials item	{return [list]}
	method prod_item_tools item		{return [list]}
	method prod_inventions {} 		{return [list]}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim leiter.unten_b
	def_event evt_timer0


	// destroy aus genericprod überschreiben
	method destroy {} {
		delete_transportlogic this
		destruct this
		del this
	}

	obj_init {
		call scripts/misc/genericprod.tcl

		sel /obj

		set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set_physic this 1
		set_snaptowall this 1

		set_textureanimation this 0 {0} 0 0

        set_fogofwar this 8 8

		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3
	}

	handle_event evt_timer0 {
		create_ladderlogic this
		set_ladderrange this 22
        set_fogofwar this 8 8
	}

}

def_class Leiter_Metall metal elevator 2 {} {

	class_fightdist 1.5

	method prod_items {}			{return [list]}
	method prod_item_materials item	{return [list]}
	method prod_item_tools item		{return [list]}
	method prod_inventions {} 		{return [list]}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim leiter.unten_b
	def_event evt_timer0

	// destroy aus genericprod überschreiben
	method destroy {} {
		delete_transportlogic this
		destruct this
		del this
	}

	obj_init {
		call scripts/misc/genericprod.tcl

		sel /obj

		set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set_physic this 1
		set_snaptowall this 1

		set_textureanimation this 0 {1} 0 0

		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3
	}

	handle_event evt_timer0 {
		create_ladderlogic this
		set_ladderrange this 32
        set_fogofwar this 8 8
	}
}

def_class Leiter_Kristall metal elevator 2 {} {

	class_fightdist 1.5

	method prod_items {}			{return [list]}
	method prod_item_materials item	{return [list]}
	method prod_item_tools item		{return [list]}
	method prod_item_blueprint item	{return [list]}
	method prod_inventions {} 		{return [list]}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim leiter.unten_b
	def_event evt_timer0

	// destroy aus genericprod überschreiben
	method destroy {} {
		delete_transportlogic this
		destruct this
		del this
	}

	obj_init {
		call scripts/misc/genericprod.tcl

		sel /obj

		set_attrib this weight 0.4
		set_attrib this hitpoints 1
		set_physic this 1
		set_snaptowall this 1

		set_textureanimation this 0 {2} 0 0

        set_fogofwar this 8 8

		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3
	}

	handle_event evt_timer0 {
		create_ladderlogic this
		set_ladderrange this 40
        set_fogofwar this 8 8
	}
}



def_class Hoverboard metal tool 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim hover_board.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this hover_board.standard 0 $ANIM_STILL

		set_attrib this weight 0.1
		set_attrib this hitpoints 0.5
	}
}


def_class Reithamster wood tool 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim hamster.stand_anim

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this hamster.stand_anim 0 $ANIM_LOOP

		set_attrib this weight 0.1
		set_attrib this hitpoints 0.5

		set_textureanimation this 0 1 0 0
	}
}
