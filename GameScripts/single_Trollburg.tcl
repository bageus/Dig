show_loading yes
obj_clear
map create 300 300
sel /obj

set_view 32.2 32.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)

call templates/urw_unq_troll_005_a.tcl
MapTemplateSet 24 8

call templates/urw_hol_013_a.tcl
MapTemplateSet 0 12

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				24 8 urw_unq_troll_005_a.tcl
				0 12 urw_hol_013_a.tcl
				}
