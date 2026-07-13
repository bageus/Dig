show_loading yes
obj_clear
map create 300 300
sel /obj

set_view 32.2 32.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)

//mat

call templates/unq_ende_seq.tcl
MapTemplateSet 0 0

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				{0	0	unq_ende_seq}
				{mat }
				}
set_fow_begin 200
