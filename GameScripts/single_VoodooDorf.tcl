show_loading yes
obj_clear
map create 300 300

;# create dummy object !!! don't touch it !!!
sel /obj


set_view 32.2 32.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)
set_fow_begin 200

call templates/urw_unq_vodo_003_a.tcl
MapTemplateSet 76 12

call templates/urw_unq_vodo_002_a.tcl
MapTemplateSet 40 4

call templates/urw_unq_vodo_008_a.tcl
MapTemplateSet 36 4

call templates/urw_unq_vodo_004_a.tcl
MapTemplateSet 4 4

call templates/urw_unq_vodo_007_a.tcl
MapTemplateSet 12 24

call templates/urw_unq_vodo_005_a.tcl
MapTemplateSet 40 24

call templates/urw_unq_vodo_006_a.tcl
MapTemplateSet 60 24


show_loading no





