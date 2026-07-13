show_loading yes
obj_clear
map create 150 150

;# create dummy object !!! don't touch it !!!
sel /obj


set_view 62.2 25.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)

call templates/urw_unq_einsiedler.tcl
MapTemplateSet 8 16

call templates/zwerg_exit_left_einsiedler.tcl
MapTemplateSet 64 24

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				8 16 urw_unq_einsiedler.tcl
				64 24 zwerg_exit_left_einsiedler.tcl
				}

