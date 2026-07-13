show_loading yes
obj_clear
map create 300 300
sel /obj

set_view 32.2 32.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)

call templates/urw_unq_start.tcl
MapTemplateSet 244 12

call templates/kris_unq_altesandburg.tcl
MapTemplateSet 0 0

call templates/kris_gng_004_a.tcl
MapTemplateSet 244 48

call templates/kris_gng_004_a.tcl
MapTemplateSet 260 48

call templates/kris_gng_004_a.tcl
MapTemplateSet 276 48

call templates/kris_gng_022_b.tcl
MapTemplateSet 292 48

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				244 12 urw_unq_start.tcl
				0 0 kris_unq_altesandburg.tcl
				244 48 kris_gng_004_a.tcl
				260 48 kris_gng_004_a.tcl
				276 48 kris_gng_004_a.tcl
				292 48 kris_gng_022_b.tcl
				}
