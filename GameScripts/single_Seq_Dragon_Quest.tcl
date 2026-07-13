show_loading yes
obj_clear
map create 300 300
sel /obj

set_view 32.2 12.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)
set_fow_begin 75

call templates/swf_unq_drache.tcl
MapTemplateSet 24 16

call templates/swf_gng_023_a.tcl
MapTemplateSet 40 40

call templates/swf_gng_022_a.tcl
MapTemplateSet 56 28

call templates/swf_gng_014_c.tcl
MapTemplateSet 36 12

call templates/zwerg_exit_right.tcl
MapTemplateSet 32 12

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				24 16 swf_unq_drache.tcl
				40 40 swf_gng_023_a.tcl
				56 28 swf_gng_022_a.tcl
				36 12 swf_gng_014_c.tcl
				32 12 zwerg_exit_right.tcl
				}

