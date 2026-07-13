show_loading yes
obj_clear
map create 300 300

;# create dummy object !!! don't touch it !!!
sel /obj


set_view 4 4 1.5 -0.3 0.0		;# set inital camera view (x y zoom)
set_fow_begin 200

call templates/urw_unq_metalltor.tcl
MapTemplateSet 4 4

show_loading no





