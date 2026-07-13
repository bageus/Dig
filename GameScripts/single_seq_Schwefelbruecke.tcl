show_loading yes
obj_clear
map create 300 300

;# create dummy object !!! don't touch it !!!
sel /obj


set_view 29.49 43.3 1.5 -0.2 0.1		;# set inital camera view (zoom x y)

call templates/swf_unq_bruecke.tcl
MapTemplateSet 0 32

set_pos [qnew Zwerg] {29.49 46.3 3.0}

set_fow_begin 200

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				0 32 swf_unq_bruecke.tcl
}
