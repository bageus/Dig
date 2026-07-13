show_loading yes
obj_clear
map create 300 300
sel /obj

set_view 27.2 52.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)

call templates/urw_unq_feen.tcl
MapTemplateSet 24 16

call templates/zwerg_exit_right.tcl
MapTemplateSet 20 52

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				24 16 urw_unq_feen_test.tcl
				20 52 zwerg_exit_right.tcl
				}
