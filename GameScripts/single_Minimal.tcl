show_loading yes
obj_clear
map create 200 200

;# create dummy object !!! don't touch it !!!
sel /obj

// WARNING: single_Campaign.tcl calls this script !!!

generate_color_variation 0 0 250 250 0

#set_fow_begin 33
set_light_begin 33
set_view_begin 23

set_view 83.0 28.1 1.5 -0.12 0.0		;# set inital camera view (x y zoom)

//call scripts/classes/misc/fee.tcl

//call templates/urw_unq_start.tcl


//call templates/urw_unq_startx.tcl
//call templates/test.tcl
map_template data/templates/unq_start.pmp 0 0


//call templates/urw_unq_einsiedler.tcl

//call templates/swf_unq_drache.tcl

//source data/templates/urw_unq_feen.tcl

//call templates/urw_unq_troll_005_a.tcl
//call templates/urw_unq_metalltor.tcl

//MapTemplateSet 48 0

#map_template templates/gng_010.pmp 96 32


show_loading no


keybind set F1 {
	if {[set g [get_selectedobject]]} {
		if {[get_objclass $g]=="Zwerg"} {
			set gp [get_pos $g]
			set dp [dig_next $gp $g]
			while {[lindex $dp 0]>0} {
				dig_apply $dp $g
				set dp [dig_next $gp $g 1 1]
			}
		}
	}
}


keybind set F6 "change_particlesource 2 0 0 {0 -0.1 0} {0 -0.3 0} 32 1 0"
keybind set F7 "set_particlesource 2 0 1"

#set st [new Stein]
#set_pos $st {97.25 40 12}
#set_physic $st 0








//keybind set F6 "change_particlesource 2 1 19 {0 0 0} {2 2 4} 1024 8 0"
//keybind set F7 "set_particlesource 2 1 1"

keybind set F6 "change_particlesource 2 1 25 {0 -1 0} {0 0 0} 32 1 0"
keybind set F7 "set_particlesource 2 1 1"
//keybind set F1 "qnew Fee"
//set_fsource this -pos {0 -1 0} -type water -vpf 50 -volume 5000

set_brightness 2

proc beam {} {
	laser [get_pos 2] [get_pos 3] {1 0 0}
}

keybind set F1 "beam"
