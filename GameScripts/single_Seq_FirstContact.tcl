show_loading yes
obj_clear
map create 300 300
sel /obj

set_view 62.2 22.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)

call templates/urw_unq_vodo_001_a.tcl
MapTemplateSet 12 12

call templates/urw_gng_004_a.tcl
MapTemplateSet 44 20

call templates/zwerg_exit_left.tcl
MapTemplateSet 60 20

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				12 12 urw_unq_vodo_001_a.tcl
				44 20 urw_gng_004_a.tcl
				60 20 zwerg_exit_left.tcl
				}
