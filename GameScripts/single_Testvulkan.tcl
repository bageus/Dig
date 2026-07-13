show_loading yes
obj_clear
map create 300 300
sel /obj

set_view 32.2 32.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)

call templates/lava_unq_vampy_002.tcl
MapTemplateSet 64 8

call templates/urw_unq_start.tcl
MapTemplateSet 4 4

call templates/lava_gng_022_a.tcl
MapTemplateSet 240 64

call templates/lava_gng_003_a.tcl
MapTemplateSet 52 40

call templates/lava_gng_021_a.tcl
MapTemplateSet 48 40

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				{64	8	lava_unq_vampy_002}
				{4	4	urw_unq_start}
				{240	64	lava_gng_022_a}
				{52	40	lava_gng_003_a}
				{48	40	lava_gng_021_a}
				}
