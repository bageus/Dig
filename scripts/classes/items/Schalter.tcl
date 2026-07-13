//Klassenname: Schalter_<[knopf|hebel>[_sonstige Merkmale]

//Dummy-Class für Icon
def_class Schalter none dummy 0 {} {}

def_class Schalter_knopf_stein none tool 0 {} {
	set_class_anim press schalter_a.rein
	set_class_anim release schalter_a.raus
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim schalter_a.standard
		set standardframe 0
		set switchanim schalter_a.raus
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
	}
}

def_class Schalter_knopf_metall none tool 0 {} {
	set_class_anim press met_schalter_a.standard
	set_class_anim release met_schalter_a.standard
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim met_schalter_a.standard
		set standardframe 0
		set switchanim met_schalter_a.standard
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
	}
}

def_class Schalter_hebel_holz_up none tool 0 {} {
	set_class_anim press schalter_b.rauf
	set_class_anim release schalter_b.runter
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim schalter_b.rauf
		set standardframe 0
		set switchanim schalter_b.runter
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
	}
}

def_class Schalter_hebel_holz_down none tool 0 {} {
	set_class_anim press schalter_b.runter
	set_class_anim release schalter_b.rauf
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim schalter_b.standard
		set standardframe 0
		set switchanim schalter_b.rauf
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
	}
}


def_class Schalter_hebel_lore_1 none tool 0 {} {
	set_class_anim press lore_schalter_b.standard
	set_class_anim release lore_schalter_b.standard
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim lore_schalter_b.standard
		set standardframe 0
		set switchanim lore_schalter_b.standard
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
		
		call_method this set_actiononpress { set dampflore [obj_query this "-class Dampflore -range 100"]; if {\$dampflore != 0} {call_method \$dampflore activate} }
	}
}


def_class Schalter_hebel_lore_2 none tool 0 {} {
	set_class_anim press lore_schalter_c.standard
	set_class_anim release lore_schalter_c.standard
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim lore_schalter_c.standard
		set standardframe 0
		set switchanim lore_schalter_c.standard
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
		
		call_method this set_actiononpress { set dampflore [obj_query this "-class Dampflore -range 100"]; if {\$dampflore != 0} {call_method \$dampflore activate} }
	}
}



def_class Schalter_hebel_lore_3 none tool 0 {} {
	set_class_anim press lore_schalter_d.standard
	set_class_anim release lore_schalter_d.standard
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim lore_schalter_d.standard
		set standardframe 0
		set switchanim lore_schalter_d.standard
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
		
		call_method this set_actiononpress { set dampflore [obj_query this "-class Dampflore -range 100"]; if {\$dampflore != 0} {call_method \$dampflore activate} }
	}
}



def_class Schalter_hebel_lore_4 none tool 0 {} {
	set_class_anim press lore_schalter_e.standard
	set_class_anim release lore_schalter_e.standard
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim lore_schalter_e.standard
		set standardframe 0
		set switchanim lore_schalter_e.standard
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
		
		call_method this set_actiononpress { set dampflore [obj_query this "-class Dampflore -range 100"]; if {\$dampflore != 0} {call_method \$dampflore activate} }
	}
}


def_class Schalter_hebel_lore_power none tool 0 {} {
	set_class_anim press lore_schalter_a.standard
	set_class_anim release lore_schalter_a.standard
	call scripts/classes/items/calls/switcher.tcl
	obj_init {
		set standardanim lore_schalter_a.standard
		set standardframe 0
		set switchanim lore_schalter_a.standard
		set switchframe 0
		call scripts/classes/items/calls/switcher.tcl
		call_method this set_switchmode toggle
		call_method this set_actiononpress { 
			set dampflore \[obj_query this "-class Dampflore -range 100"\];
			if \{\$dampflore != 0\} \{
				call_method \$dampflore set_power 1;
				call_method \$dampflore activate
			\} 
		}
		call_method this set_actiononrelease { 
			set dampflore \[obj_query this "-class Dampflore -range 100"\];
			if \{\$dampflore != 0\} \{
				call_method \$dampflore set_power 0
			\} 
		}
	}
}
