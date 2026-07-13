obj_clear
map create 300 300
sel /obj

set_view 32.2 32.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)
//set_light_begin -10
//catch { set_ground_begin 26 }

//mat {3 160 40 36 68} {3 164 108 32 12}

call templates/unq_ende_b.tcl
MapTemplateSet 16 44

call templates/unq_zwergenkoenig.tcl
MapTemplateSet 8 4

call templates/unq_fenris_first.tcl
MapTemplateSet 32 152

